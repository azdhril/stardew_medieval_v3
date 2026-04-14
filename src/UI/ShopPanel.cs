using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Buy/Sell shop panel (720x400, centered horizontally, 48px from top).
/// Owned by <see cref="stardew_medieval_v3.Scenes.ShopOverlayScene"/>.
/// UI-SPEC §Component Inventory / §State machine: shop buy/sell.
/// </summary>
public class ShopPanel
{
    private enum Tab { Buy, Sell }

    // Layout (UI-SPEC §Component Inventory)
    private const int PanelWidth = 720;
    private const int PanelHeight = 400;
    private const int ScreenWidth = 960;
    private static readonly int PanelX = (ScreenWidth - PanelWidth) / 2; // 120
    private const int PanelY = 48;
    private const int RowHeight = 40;
    private const int VisibleRows = 8;

    // Colors (design fingerprint)
    private static readonly Color Dim = Color.Black * 0.55f;
    private static readonly Color PanelFill = new Color(60, 40, 30);
    private static readonly Color Bevel = new Color(90, 60, 45);
    private static readonly Color SelectBg = Color.Gold * 0.4f;

    // Disabled-reason copy (UI-SPEC §Copywriting — MUST match exactly)
    private const string ReasonNotEnoughGold = "Not enough gold";
    private const string ReasonInventoryFull = "Inventory full";
    private const string ReasonSelectToSell = "Select an item to sell";
    private const string ReasonCannotSell = "Cannot sell this item";

    private readonly InventoryManager _inv;
    private readonly SpriteAtlas _atlas;

    private Tab _tab = Tab.Buy;
    private int _selectedIndex;

    // Cached layout rects (recomputed each Update via UpdateLayoutCache) — shared with Draw for hit-test consistency.
    private Rectangle _panelRect;
    private Rectangle _buyTabRect;
    private Rectangle _sellTabRect;
    private Rectangle _closeRect;
    private readonly Rectangle[] _rowRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _actionBtnRects = new Rectangle[VisibleRows];
    private int _hoveredRow = -1; // index into the visible-row slice (0..visible-1), NOT absolute rowIndex
    private int _scrollOffset;    // user-controlled (wheel), kept in range by clamp

    // Per-row quantity state (Stardew-style; D-04). Index is ABSOLUTE row index into the current tab.
    private int[] _rowQty = Array.Empty<int>();
    private Tab _rowQtyTab = Tab.Buy;
    private int _rowQtyRowCount = 0;

    // Per-visible-row qty widget rects (mirror _rowRects / _actionBtnRects).
    private readonly Rectangle[] _qtyMinusRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _qtyLabelRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _qtyPlusRects  = new Rectangle[VisibleRows];

    private Rectangle _scrollTrackRect;
    private Rectangle _scrollThumbRect;

    /// <summary>Outbound signal from <see cref="Update"/>: a toast the caller should show.</summary>
    public record ToastRequest(string Text, Color Color);

    public ShopPanel(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inv = inventory;
        _atlas = atlas;
    }

    /// <summary>
    /// Drive input. Returns <c>true</c> if the user pressed Escape (the overlay should close).
    /// <paramref name="toast"/> is set when a transaction completed.
    /// </summary>
    public bool Update(float dt, InputManager input, out ToastRequest? toast)
    {
        toast = null;

        int rows = GetRowCount();

        EnsureRowQtySized(rows);

        // Mouse wheel → scroll offset (one tick = one row)
        int wheel = input.ScrollWheelDelta;
        if (wheel != 0)
        {
            int maxScroll = Math.Max(0, rows - VisibleRows);
            _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(wheel), 0, maxScroll);
        }

        // --- Recompute cached layout (shared with Draw via cached fields) ---
        UpdateLayoutCache();
        int visible = Math.Min(rows, VisibleRows);

        // --- Mouse hit-test (BEFORE keyboard so click-outside can fire this frame) ---
        var mp = input.MousePosition;
        _hoveredRow = -1;
        for (int i = 0; i < visible; i++)
        {
            if (_rowRects[i].Contains(mp)) { _hoveredRow = i; break; }
        }

        if (input.IsLeftClickPressed)
        {
            // 1. Close X button
            if (_closeRect.Contains(mp)) return true;

            // 2. Click outside panel → close
            if (!_panelRect.Contains(mp)) return true;

            // 3. Tabs
            if (_buyTabRect.Contains(mp))
            {
                if (_tab != Tab.Buy) { _tab = Tab.Buy; _selectedIndex = 0; }
                return false;
            }
            if (_sellTabRect.Contains(mp))
            {
                if (_tab != Tab.Sell) { _tab = Tab.Sell; _selectedIndex = 0; }
                return false;
            }

            // 4. Row body click → select; if click also lands on the row's action button AND row is enabled → fire transaction
            if (_hoveredRow >= 0)
            {
                int absIndex = _hoveredRow + _scrollOffset;
                _selectedIndex = absIndex;

                if (_actionBtnRects[_hoveredRow].Contains(mp) && IsActionEnabled(absIndex))
                {
                    int q = absIndex < _rowQty.Length ? Math.Max(1, _rowQty[absIndex]) : 1;
                    if (_tab == Tab.Buy) TryBuy(absIndex, q, out toast);
                    else TrySell(absIndex, q, out toast);
                }
                return false;
            }
        }

        // --- Keyboard block (regression-safe; unchanged behavior) ---
        if (input.IsKeyPressed(Keys.Escape))
            return true;

        // Tab toggle
        if (input.IsKeyPressed(Keys.Tab))
        {
            _tab = _tab == Tab.Buy ? Tab.Sell : Tab.Buy;
            _selectedIndex = 0;
            return false;
        }

        if (rows > 0)
        {
            if (input.IsKeyPressed(Keys.Down)) _selectedIndex = (_selectedIndex + 1) % rows;
            if (input.IsKeyPressed(Keys.Up)) _selectedIndex = (_selectedIndex - 1 + rows) % rows;

            // Left/Right nudge sell quantity on the selected row (legacy; removed in Task 2)
            if (_selectedIndex < _rowQty.Length)
            {
                if (input.IsKeyPressed(Keys.Left))  _rowQty[_selectedIndex] = Math.Max(1, _rowQty[_selectedIndex] - 1);
                if (input.IsKeyPressed(Keys.Right))
                {
                    int cap = GetMaxQty(_selectedIndex);
                    _rowQty[_selectedIndex] = Math.Min(Math.Max(1, cap), _rowQty[_selectedIndex] + 1);
                }
            }
        }
        else
        {
            _selectedIndex = 0;
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            int q = (_selectedIndex < _rowQty.Length) ? Math.Max(1, _rowQty[_selectedIndex]) : 1;
            if (_tab == Tab.Buy) TryBuy(_selectedIndex, q, out toast);
            else TrySell(_selectedIndex, q, out toast);
        }

        return false;
    }

    /// <summary>
    /// Recomputes panel/tab/row/action-button/close rects into the cached fields. Called from Update
    /// (so hit-test reflects current state) and defensively from Draw (in case Draw is invoked
    /// without a preceding Update). Pure layout — no input or rendering side effects.
    /// </summary>
    private void UpdateLayoutCache()
    {
        int rows = GetRowCount();
        int visible = Math.Min(rows, VisibleRows);

        // Clamp _scrollOffset to valid range
        int maxScroll = Math.Max(0, rows - VisibleRows);
        if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
        if (_scrollOffset < 0) _scrollOffset = 0;

        // Wheel-driven only (CONTEXT D-01/D-02): no follow-selection, no re-centering on _selectedIndex.
        // _scrollOffset is mutated only by the wheel handler in Update() and the range-clamp above.

        int scroll = _scrollOffset;

        _panelRect = new Rectangle(PanelX, PanelY, PanelWidth, PanelHeight);

        int tabY = PanelY + 40;
        _buyTabRect  = new Rectangle(PanelX + 16,      tabY, 80, 32);
        _sellTabRect = new Rectangle(PanelX + 16 + 88, tabY, 80, 32);

        // Close X button — 20x20 at top-right of header
        _closeRect = new Rectangle(PanelX + PanelWidth - 28, PanelY + 8, 20, 20);

        // Row + action-button rects
        int listX = PanelX + 16;
        int listY = tabY + 48;
        int width = PanelWidth - 32;
        for (int i = 0; i < VisibleRows; i++)
        {
            int rowIndex = i + scroll;
            if (i < visible && rowIndex < rows)
            {
                int y = listY + i * RowHeight;
                _rowRects[i] = new Rectangle(listX, y, width, RowHeight - 2);
                int actionX = listX + width - 72;
                _actionBtnRects[i] = new Rectangle(actionX, y + 8, 60, 24);
                // Per-row qty stepper: [-][qty][+] immediately left of the action button (D-04).
                _qtyMinusRects[i] = new Rectangle(actionX - 80, y + 8, 16, 24);
                _qtyLabelRects[i] = new Rectangle(actionX - 60, y + 8, 32, 24);
                _qtyPlusRects[i]  = new Rectangle(actionX - 24, y + 8, 16, 24);
            }
            else
            {
                _rowRects[i] = Rectangle.Empty;
                _actionBtnRects[i] = Rectangle.Empty;
                _qtyMinusRects[i] = Rectangle.Empty;
                _qtyLabelRects[i] = Rectangle.Empty;
                _qtyPlusRects[i]  = Rectangle.Empty;
            }
        }

        // Scrollbar rects (only when list overflows)
        int trackH = VisibleRows * RowHeight;
        if (rows > VisibleRows)
        {
            _scrollTrackRect = new Rectangle(PanelX + PanelWidth - 20, listY, 8, trackH);
            int thumbH = Math.Max(16, trackH * VisibleRows / rows);
            int thumbY = listY + (int)((trackH - thumbH) * ((float)scroll / Math.Max(1, rows - VisibleRows)));
            _scrollThumbRect = new Rectangle(_scrollTrackRect.X, thumbY, 8, thumbH);
        }
        else
        {
            _scrollTrackRect = Rectangle.Empty;
            _scrollThumbRect = Rectangle.Empty;
        }

    }

    /// <summary>Resolve the ItemStack for the currently selected Sell row (or null).</summary>
    private ItemStack? CurrentSellStack()
    {
        if (_tab != Tab.Sell) return null;
        int slot = IndexOfNthFilledSlot(_selectedIndex);
        return slot >= 0 ? _inv.GetSlot(slot) : null;
    }

    /// <summary>Row count for the active tab.</summary>
    private int GetRowCount()
    {
        if (_tab == Tab.Buy) return ShopStock.Items.Count;
        // Sell: count non-empty inventory slots
        int n = 0;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
            if (_inv.GetSlot(i) != null) n++;
        return n;
    }

    /// <summary>Check whether at least one <paramref name="itemId"/> unit could be added to inventory.</summary>
    private bool CanAddOne(string itemId)
    {
        var def = ItemRegistry.Get(itemId);
        if (def == null) return false;
        int stackLimit = def.StackLimit;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            var s = _inv.GetSlot(i);
            if (s == null) return true;
            if (s.ItemId == itemId && s.Quantity < stackLimit) return true;
        }
        return false;
    }

    /// <summary>Resize _rowQty to rowCount and reset all entries to 1. Runs on tab switch or row-count change.</summary>
    private void EnsureRowQtySized(int rowCount)
    {
        if (_rowQty.Length == rowCount && _rowQtyTab == _tab && _rowQtyRowCount == rowCount) return;
        _rowQty = new int[rowCount];
        for (int i = 0; i < rowCount; i++) _rowQty[i] = 1;
        _rowQtyTab = _tab;
        _rowQtyRowCount = rowCount;
    }

    /// <summary>Max qty allowed on row. Buy: limited by gold and stack headroom. Sell: limited by stack.Quantity.</summary>
    private int GetMaxQty(int rowIndex)
    {
        if (_tab == Tab.Buy)
        {
            if (rowIndex < 0 || rowIndex >= ShopStock.Items.Count) return 0;
            var e = ShopStock.Items[rowIndex];
            var def = ItemRegistry.Get(e.ItemId);
            if (def == null || e.Price <= 0) return 0;
            int affordable = _inv.Gold / e.Price;
            int headroom = ComputeStackHeadroom(e.ItemId, def.StackLimit);
            return Math.Max(0, Math.Min(affordable, headroom));
        }
        else
        {
            int slot = IndexOfNthFilledSlot(rowIndex);
            if (slot < 0) return 0;
            var stack = _inv.GetSlot(slot);
            if (stack == null) return 0;
            var def = ItemRegistry.Get(stack.ItemId);
            if (ShopStock.GetSellPrice(def) <= 0) return 0;
            return stack.Quantity;
        }
    }

    /// <summary>Total additional units of itemId the inventory can still accept given stackLimit.</summary>
    private int ComputeStackHeadroom(string itemId, int stackLimit)
    {
        int headroom = 0;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            var s = _inv.GetSlot(i);
            if (s == null) headroom += stackLimit;
            else if (s.ItemId == itemId) headroom += Math.Max(0, stackLimit - s.Quantity);
        }
        return headroom;
    }

    /// <summary>Buy flow: check gold → check space → debit → add. Strict order (T-04-14/T-04-15).</summary>
    private void TryBuy(int rowIndex, int qty, out ToastRequest? toast)
    {
        toast = null;
        if (rowIndex < 0 || rowIndex >= ShopStock.Items.Count) return;
        if (qty <= 0) return;
        var entry = ShopStock.Items[rowIndex];
        var def = ItemRegistry.Get(entry.ItemId);
        if (def == null)
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: unknown item {entry.ItemId}");
            return;
        }

        int totalPrice = entry.Price * qty;
        if (_inv.Gold < totalPrice)
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: not enough gold ({_inv.Gold} < {totalPrice})");
            return;
        }
        int headroom = ComputeStackHeadroom(entry.ItemId, def.StackLimit);
        if (headroom < qty)
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: inventory full (headroom {headroom} < qty {qty})");
            return;
        }
        if (!_inv.TrySpendGold(totalPrice))
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: TrySpendGold returned false");
            return;
        }
        int leftover = _inv.TryAdd(entry.ItemId, qty);
        if (leftover > 0)
        {
            // Safety: headroom said yes but TryAdd left some — refund the leftover portion.
            int refund = entry.Price * leftover;
            _inv.AddGold(refund);
            Console.WriteLine($"[ShopPanel] Buy partial: {qty - leftover}/{qty} added, refunded {refund}g for {leftover} leftover");
        }
        int delivered = qty - leftover;
        Console.WriteLine($"[ShopPanel] Bought {entry.ItemId} x{delivered} for {entry.Price * delivered}g");
        toast = new ToastRequest($"Purchased {def.Name} x{delivered}", Color.LimeGreen);

        // Row count for Buy doesn't change, but headroom/affordability does — reset qty to 1.
        EnsureRowQtySized(GetRowCount());
    }

    /// <summary>Sell flow: look up Nth non-empty slot → null-check → remove → credit (T-04-16).</summary>
    private void TrySell(int rowIndex, int qty, out ToastRequest? toast)
    {
        toast = null;
        if (qty <= 0) return;
        int slotIndex = IndexOfNthFilledSlot(rowIndex);
        if (slotIndex < 0)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: no stack at row {rowIndex}");
            return;
        }
        var stack = _inv.GetSlot(slotIndex);
        if (stack == null)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: slot vanished at {slotIndex}");
            return;
        }
        var def = ItemRegistry.Get(stack.ItemId);
        int price = ShopStock.GetSellPrice(def);
        if (def == null || price <= 0)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: item not sellable ({stack.ItemId})");
            return;
        }

        int clamped = Math.Clamp(qty, 1, stack.Quantity);
        var removed = _inv.RemoveQuantity(slotIndex, clamped);
        if (removed == null)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: RemoveQuantity returned null");
            return;
        }
        int totalPrice = price * clamped;
        _inv.AddGold(totalPrice);

        // Keep scroll + (legacy) selection valid; row count may have shrunk.
        int remainingRows = GetRowCount();
        if (_selectedIndex >= remainingRows && remainingRows > 0) _selectedIndex = remainingRows - 1;
        if (remainingRows == 0) _selectedIndex = 0;
        int maxScroll = Math.Max(0, remainingRows - VisibleRows);
        if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;

        // Resize per-row qty arrays for the new row count.
        EnsureRowQtySized(remainingRows);

        Console.WriteLine($"[ShopPanel] Sold {removed.ItemId} x{clamped} for {totalPrice}g");
        toast = new ToastRequest($"Sold {def.Name} x{clamped} for {totalPrice}g", Color.Gold);
    }

    /// <summary>Find the <paramref name="n"/>-th non-empty inventory slot, or -1.</summary>
    private int IndexOfNthFilledSlot(int n)
    {
        int seen = 0;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            if (_inv.GetSlot(i) == null) continue;
            if (seen == n) return i;
            seen++;
        }
        return -1;
    }

    // ================= Draw =================

    /// <summary>Render panel + rows + header + disabled reason.</summary>
    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        // Defensive: ensure cached layout matches current state even if Draw runs without a preceding Update.
        UpdateLayoutCache();

        // Full-screen dim
        sb.Draw(pixel, new Rectangle(0, 0, 960, 540), Dim);

        // Outline + fill
        sb.Draw(pixel, new Rectangle(PanelX - 1, PanelY - 1, PanelWidth + 2, PanelHeight + 2), Color.Black);
        sb.Draw(pixel, _panelRect, PanelFill);

        // Header strip (40px tall inside panel)
        int headerY = PanelY + 8;
        sb.DrawString(font, "Shop", new Vector2(PanelX + 16, headerY), Color.White);

        string goldText = $"Gold: {_inv.Gold}";
        var goldSize = font.MeasureString(goldText);
        // Shifted left to leave room for the close X button at the top-right
        sb.DrawString(font, goldText,
            new Vector2(PanelX + PanelWidth - 40 - goldSize.X, headerY), Color.Gold);

        // Close X button (top-right of header)
        sb.Draw(pixel, new Rectangle(_closeRect.X - 1, _closeRect.Y - 1, _closeRect.Width + 2, _closeRect.Height + 2), Color.Black);
        sb.Draw(pixel, _closeRect, Bevel);
        var xSz = font.MeasureString("X");
        sb.DrawString(font, "X",
            new Vector2(_closeRect.X + (_closeRect.Width - xSz.X) / 2,
                        _closeRect.Y + (_closeRect.Height - xSz.Y) / 2),
            Color.White);

        string escHint = "Esc or click outside to close";
        var escSize = font.MeasureString(escHint);
        sb.DrawString(font, escHint,
            new Vector2(PanelX + PanelWidth - 16 - escSize.X, PanelY + PanelHeight - 8 - escSize.Y),
            Color.Gray * 0.7f);

        // Tab strip — driven by cached rects
        int tabY = _buyTabRect.Y;
        DrawTab(sb, font, pixel, _buyTabRect.X,  _buyTabRect.Y,  "Buy",  _tab == Tab.Buy);
        DrawTab(sb, font, pixel, _sellTabRect.X, _sellTabRect.Y, "Sell", _tab == Tab.Sell);

        // Divider below tabs
        sb.Draw(pixel, new Rectangle(PanelX + 8, tabY + 40, PanelWidth - 16, 1), Bevel);

        // Item list region
        int listY = tabY + 48;

        int rows = GetRowCount();
        if (rows == 0)
        {
            string empty = _tab == Tab.Sell ? "Your inventory is empty" : "Shop is closed";
            var sz = font.MeasureString(empty);
            sb.DrawString(font, empty,
                new Vector2(PanelX + (PanelWidth - sz.X) / 2, listY + 80),
                Color.Gray * 0.7f);
            DrawDisabledReason(sb, font, _tab == Tab.Sell ? ReasonSelectToSell : ReasonNotEnoughGold);
            return;
        }

        int visible = Math.Min(rows, VisibleRows);

        for (int i = 0; i < visible; i++)
        {
            int rowIndex = i + _scrollOffset;
            if (rowIndex >= rows) break;
            bool selected = rowIndex == _selectedIndex;

            // Hover tint (only if hovered AND not selected — selection takes precedence)
            if (i == _hoveredRow && !selected)
            {
                sb.Draw(pixel, _rowRects[i], Color.White * 0.1f);
            }

            DrawRow(sb, font, pixel, _rowRects[i].X, _rowRects[i].Y, _rowRects[i].Width, rowIndex, selected);
        }

        // Scrollbar (track + thumb) when list overflows
        if (_scrollTrackRect != Rectangle.Empty)
        {
            sb.Draw(pixel, _scrollTrackRect, Bevel * 0.5f);
            sb.Draw(pixel, _scrollThumbRect, Color.Gold * 0.85f);
        }

        // Disabled reason (below list area)
        string? reason = ComputeDisabledReason();
        if (reason != null) DrawDisabledReason(sb, font, reason);
    }

    private void DrawTab(SpriteBatch sb, SpriteFont font, Texture2D pixel, int x, int y, string label, bool active)
    {
        var fill = active ? Color.Gold : Bevel;
        sb.Draw(pixel, new Rectangle(x - 1, y - 1, 82, 34), Color.Black);
        sb.Draw(pixel, new Rectangle(x, y, 80, 32), fill);
        var sz = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(x + (80 - sz.X) / 2, y + (32 - sz.Y) / 2),
            active ? Color.Black : Color.White);
    }

    private void DrawRow(SpriteBatch sb, SpriteFont font, Texture2D pixel, int x, int y, int width, int rowIndex, bool selected)
    {
        if (selected)
            sb.Draw(pixel, new Rectangle(x, y, width, RowHeight - 2), SelectBg);

        // Icon cell (16x16 centered vertically in 40px row)
        int iconX = x + 8;
        int iconY = y + (RowHeight - 16) / 2;

        string itemId;
        string label;
        int price;
        Color priceColor;

        if (_tab == Tab.Buy)
        {
            var entry = ShopStock.Items[rowIndex];
            itemId = entry.ItemId;
            price = entry.Price;
            var def = ItemRegistry.Get(entry.ItemId);
            label = def?.Name ?? entry.ItemId;
            priceColor = _inv.Gold >= price ? Color.LimeGreen : Color.Gray * 0.7f;
        }
        else
        {
            int slotIndex = IndexOfNthFilledSlot(rowIndex);
            var stack = slotIndex >= 0 ? _inv.GetSlot(slotIndex) : null;
            if (stack == null) return;
            itemId = stack.ItemId;
            var def = ItemRegistry.Get(stack.ItemId);
            label = (def?.Name ?? stack.ItemId) + (stack.Quantity > 1 ? $" x{stack.Quantity}" : "");
            price = ShopStock.GetSellPrice(def);
            priceColor = price > 0 ? Color.Gold : Color.Gray * 0.7f;
        }

        // Icon
        var iconDef = ItemRegistry.Get(itemId);
        if (iconDef != null)
        {
            var srcRect = _atlas.GetRect(iconDef.SpriteId);
            sb.Draw(_atlas.GetTexture(iconDef.SpriteId),
                new Rectangle(iconX, iconY, 16, 16), srcRect, Color.White);
        }

        // Name
        sb.DrawString(font, label, new Vector2(iconX + 24, y + (RowHeight - font.MeasureString(label).Y) / 2),
            Color.White);

        // Price (left of the per-row stepper)
        string priceText = $"{price}g";
        var priceSz = font.MeasureString(priceText);
        int actionX = x + width - 72;
        sb.DrawString(font, priceText,
            new Vector2(actionX - 80 - priceSz.X - 12, y + (RowHeight - priceSz.Y) / 2),
            priceColor);

        // Per-row qty stepper: [-][qty][+] (D-04).
        int visibleSlot = rowIndex - _scrollOffset;
        if (visibleSlot >= 0 && visibleSlot < VisibleRows && _qtyLabelRects[visibleSlot] != Rectangle.Empty)
        {
            var minus = _qtyMinusRects[visibleSlot];
            var qtyLabel = _qtyLabelRects[visibleSlot];
            var plus  = _qtyPlusRects[visibleSlot];

            sb.Draw(pixel, minus, Bevel);
            var ms = font.MeasureString("-");
            sb.DrawString(font, "-",
                new Vector2(minus.X + (minus.Width - ms.X) / 2, minus.Y + (minus.Height - ms.Y) / 2),
                Color.White);

            sb.Draw(pixel, qtyLabel, Bevel * 0.6f);
            int qtyVal = rowIndex < _rowQty.Length ? _rowQty[rowIndex] : 1;
            string qtyText = $"x{qtyVal}";
            var qs = font.MeasureString(qtyText);
            sb.DrawString(font, qtyText,
                new Vector2(qtyLabel.X + (qtyLabel.Width - qs.X) / 2, qtyLabel.Y + (qtyLabel.Height - qs.Y) / 2),
                Color.White);

            sb.Draw(pixel, plus, Bevel);
            var ps = font.MeasureString("+");
            sb.DrawString(font, "+",
                new Vector2(plus.X + (plus.Width - ps.X) / 2, plus.Y + (plus.Height - ps.Y) / 2),
                Color.White);
        }

        // Action button (60x24) on the right
        string action = _tab == Tab.Buy ? "Buy" : "Sell";
        bool enabled = IsActionEnabled(rowIndex);
        var btnFill = enabled ? Color.Gold : Bevel;
        sb.Draw(pixel, new Rectangle(actionX - 1, y + 7, 62, 26), Color.Black);
        sb.Draw(pixel, new Rectangle(actionX, y + 8, 60, 24), btnFill);
        var aSz = font.MeasureString(action);
        sb.DrawString(font, action,
            new Vector2(actionX + (60 - aSz.X) / 2, y + 8 + (24 - aSz.Y) / 2),
            enabled ? Color.Black : Color.Gray);
    }

    private bool IsActionEnabled(int rowIndex)
    {
        if (_tab == Tab.Buy)
        {
            if (rowIndex < 0 || rowIndex >= ShopStock.Items.Count) return false;
            var e = ShopStock.Items[rowIndex];
            if (_inv.Gold < e.Price) return false;
            if (!CanAddOne(e.ItemId)) return false;
            return true;
        }
        else
        {
            int slot = IndexOfNthFilledSlot(rowIndex);
            if (slot < 0) return false;
            var stack = _inv.GetSlot(slot);
            if (stack == null) return false;
            var def = ItemRegistry.Get(stack.ItemId);
            return ShopStock.GetSellPrice(def) > 0;
        }
    }

    private string? ComputeDisabledReason()
    {
        int rows = GetRowCount();
        if (rows == 0) return _tab == Tab.Sell ? ReasonSelectToSell : null;
        if (_selectedIndex < 0 || _selectedIndex >= rows) return null;

        if (_tab == Tab.Buy)
        {
            var e = ShopStock.Items[_selectedIndex];
            if (_inv.Gold < e.Price) return ReasonNotEnoughGold;
            if (!CanAddOne(e.ItemId)) return ReasonInventoryFull;
            return null;
        }
        else
        {
            int slot = IndexOfNthFilledSlot(_selectedIndex);
            if (slot < 0) return ReasonSelectToSell;
            var stack = _inv.GetSlot(slot);
            if (stack == null) return ReasonSelectToSell;
            var def = ItemRegistry.Get(stack.ItemId);
            if (def == null || ShopStock.GetSellPrice(def) <= 0) return ReasonCannotSell;
            return null;
        }
    }

    private void DrawDisabledReason(SpriteBatch sb, SpriteFont font, string reason)
    {
        var sz = font.MeasureString(reason);
        sb.DrawString(font, reason,
            new Vector2(PanelX + (PanelWidth - sz.X) / 2, PanelY + PanelHeight - 28),
            Color.Red);
    }
}
