using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
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
    private const int PanelHeight = 370;
    private const int RowHeight = 40;
    private const int VisibleRows = 7;
    private const int IconSize = 24;
    private const int IconTextGap = 8;

    // Runtime anchors computed from the current viewport inside UpdateLayoutCache
    // (replaces the old hardcoded-960x540 constants so the panel centers correctly
    // in fullscreen / any non-960x540 window size).
    private int _panelX;
    private int _panelY;

    // Colors (design fingerprint)
    private static readonly Color Dim = Color.Black * 0.55f;
    private static readonly Color PanelFill = new Color(60, 40, 30);
    private static readonly Color Bevel = new Color(90, 60, 45);
    private static readonly Color RowText = new Color(78, 58, 44);
    private static readonly Color RowHoverFill = new Color(78, 58, 44);
    private static readonly Color PriceGold = new Color(184, 134, 11);

    private readonly InventoryManager _inv;
    private readonly SpriteAtlas _atlas;

    private Tab _tab = Tab.Buy;

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
    public bool Update(float dt, InputManager input, int viewportWidth, int viewportHeight, out ToastRequest? toast)
    {
        toast = null;

        int rows = GetRowCount();
        EnsureRowQtySized(rows);

        // Mouse wheel → scroll offset (one tick = one row). D-02: pure wheel, no follow-selection.
        int wheel = input.ScrollWheelDelta;
        if (wheel != 0)
        {
            int maxScroll = Math.Max(0, rows - VisibleRows);
            _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(wheel), 0, maxScroll);
        }

        UpdateLayoutCache(viewportWidth, viewportHeight);
        int visible = Math.Min(rows, VisibleRows);

        var mp = input.MousePosition;
        _hoveredRow = -1;
        for (int i = 0; i < visible; i++)
        {
            if (_rowRects[i].Contains(mp)) { _hoveredRow = i; break; }
        }

        if (input.IsLeftClickPressed)
        {
            if (_closeRect.Contains(mp))
            {
                Console.WriteLine("[ShopPanel] Close X clicked");
                return true;
            }
            if (!_panelRect.Contains(mp))
            {
                Console.WriteLine("[ShopPanel] Click outside panel -> close");
                return true;
            }
            if (_buyTabRect.Contains(mp))
            {
                if (_tab != Tab.Buy)
                {
                    _tab = Tab.Buy;
                    EnsureRowQtySized(GetRowCount());
                    Console.WriteLine("[ShopPanel] Tab -> Buy");
                }
                return false;
            }
            if (_sellTabRect.Contains(mp))
            {
                if (_tab != Tab.Sell)
                {
                    _tab = Tab.Sell;
                    EnsureRowQtySized(GetRowCount());
                    Console.WriteLine("[ShopPanel] Tab -> Sell");
                }
                return false;
            }

            // Per-visible-row hit-test: qty-/qty+/action, in that priority (D-04, D-05).
            for (int k = 0; k < visible; k++)
            {
                int abs = k + _scrollOffset;
                if (abs < 0 || abs >= _rowQty.Length) continue;

                if (_qtyMinusRects[k].Contains(mp))
                {
                    _rowQty[abs] = Math.Max(1, _rowQty[abs] - 1);
                    Console.WriteLine($"[ShopPanel] qty- row={abs} qty={_rowQty[abs]}");
                    return false;
                }
                if (_qtyPlusRects[k].Contains(mp))
                {
                    int cap = GetMaxQty(abs);
                    _rowQty[abs] = Math.Min(Math.Max(1, cap), _rowQty[abs] + 1);
                    if (cap <= 0) _rowQty[abs] = 1;
                    Console.WriteLine($"[ShopPanel] qty+ row={abs} qty={_rowQty[abs]} cap={cap}");
                    return false;
                }
                if (_actionBtnRects[k].Contains(mp))
                {
                    if (!IsActionEnabled(abs))
                    {
                        Console.WriteLine($"[ShopPanel] action-click row={abs} disabled");
                        return false;
                    }
                    int q = Math.Clamp(_rowQty[abs], 1, Math.Max(1, GetMaxQty(abs)));
                    if (_tab == Tab.Buy)
                    {
                        Console.WriteLine($"[ShopPanel] Buy-click row={abs} qty={q}");
                        TryBuy(abs, q, out toast);
                    }
                    else
                    {
                        Console.WriteLine($"[ShopPanel] Sell-click row={abs} qty={q}");
                        TrySell(abs, q, out toast);
                    }
                    // Row may have shrunk (Sell) or price/headroom changed (Buy) — reset qty to 1.
                    if (abs < _rowQty.Length) _rowQty[abs] = 1;
                    return false;
                }
            }
        }

        // Keyboard: only Escape closes (D-03). All other keys ignored inside the panel.
        if (input.IsKeyPressed(Keys.Escape))
        {
            Console.WriteLine("[ShopPanel] Escape pressed -> close");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Recomputes panel/tab/row/action-button/close rects into the cached fields. Called from Update
    /// (so hit-test reflects current state) and defensively from Draw (in case Draw is invoked
    /// without a preceding Update). Pure layout — no input or rendering side effects.
    /// </summary>
    private void UpdateLayoutCache(int viewportWidth, int viewportHeight)
    {
        // Center the panel in the current viewport (was hardcoded to 960x540 origin).
        _panelX = (viewportWidth - PanelWidth) / 2;
        _panelY = (viewportHeight - PanelHeight) / 2;

        int rows = GetRowCount();
        int visible = Math.Min(rows, VisibleRows);

        // Clamp _scrollOffset to valid range
        int maxScroll = Math.Max(0, rows - VisibleRows);
        if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
        if (_scrollOffset < 0) _scrollOffset = 0;

        // Wheel-driven only (CONTEXT D-01/D-02): no follow-selection, no re-centering on _selectedIndex.
        // _scrollOffset is mutated only by the wheel handler in Update() and the range-clamp above.

        int scroll = _scrollOffset;

        _panelRect = new Rectangle(_panelX, _panelY, PanelWidth, PanelHeight);

        int tabY = _panelY + 16;
        _buyTabRect  = new Rectangle(_panelX + 16,      tabY, 80, 32);
        _sellTabRect = new Rectangle(_panelX + 16 + 88, tabY, 80, 32);

        // Close X button — 20x20 at top-right of header
        _closeRect = new Rectangle(_panelX + PanelWidth - 28, _panelY + 8, 20, 20);

        // Row + action-button rects
        int listX = _panelX + 16;
        int listY = tabY + 36;
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
                _qtyMinusRects[i] = new Rectangle(actionX - 80, y + 10, 20, 20);
                _qtyLabelRects[i] = new Rectangle(actionX - 60, y + 8, 32, 24);
                _qtyPlusRects[i]  = new Rectangle(actionX - 24, y + 10, 20, 20);
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
            _scrollTrackRect = new Rectangle(_panelX + PanelWidth - 12, listY, 4, trackH);
            int thumbH = Math.Max(16, trackH * VisibleRows / rows);
            int thumbY = listY + (int)((trackH - thumbH) * ((float)scroll / Math.Max(1, rows - VisibleRows)));
            _scrollThumbRect = new Rectangle(_scrollTrackRect.X, thumbY, 4, thumbH);
        }
        else
        {
            _scrollTrackRect = Rectangle.Empty;
            _scrollThumbRect = Rectangle.Empty;
        }

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

        // Keep scroll valid; row count may have shrunk.
        int remainingRows = GetRowCount();
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
    public void Draw(SpriteBatch sb, SpriteFontBase font, SpriteFontBase titleFont, Texture2D pixel, UITheme theme,
        int viewportWidth, int viewportHeight)
    {
        // Defensive: ensure cached layout matches current state even if Draw runs without a preceding Update.
        UpdateLayoutCache(viewportWidth, viewportHeight);

        // Full-screen dim — covers the entire real viewport, not a fixed 960x540.
        sb.Draw(pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), Dim);

        NineSlice.Draw(sb, theme.PanePopup, _panelRect, theme.PanePopupInsets);

        // Title — plain gold text centered in the top chrome (matches ChestScene style).
        DrawCenteredTitle(sb, titleFont, "Shop",
            new Rectangle(_panelX + 28, _panelY, PanelWidth - 56, 50),
            Color.LightGoldenrodYellow, 2f);
        int headerY = _panelY + 8;

        // Gold display: coin icon + value (matches HUD style).
        string goldText = _inv.Gold.ToString();
        var goldSize = font.MeasureString(goldText);
        int coinSize = 18;
        int goldBlockW = coinSize + 6 + (int)goldSize.X;
        int goldX = _panelX + PanelWidth - 44 - goldBlockW;  // leaves room for X button
        int goldY = headerY + 10;
        sb.Draw(theme.GoldIcon, new Rectangle(goldX, goldY, coinSize, coinSize), Color.White);
        sb.DrawString(font, goldText,
            new Vector2(goldX + coinSize + 6, goldY + (coinSize - goldSize.Y) / 2),
            Color.Gold);

        // Close X icon (stretched to fit _closeRect; icon keeps its native transparent bg)
        NineSlice.DrawStretched(sb, theme.BtnIconX, _closeRect);

        // Tab strip — driven by cached rects
        int tabY = _buyTabRect.Y;
        DrawTab(sb, font, theme, _buyTabRect.X,  _buyTabRect.Y,  "Buy",  _tab == Tab.Buy);
        DrawTab(sb, font, theme, _sellTabRect.X, _sellTabRect.Y, "Sell", _tab == Tab.Sell);

        // Item list region
        int listY = tabY + 36;

        int rows = GetRowCount();
        if (rows == 0)
        {
            string empty = _tab == Tab.Sell ? "Your inventory is empty" : "Shop is closed";
            var sz = font.MeasureString(empty);
            sb.DrawString(font, empty,
                new Vector2(_panelX + (PanelWidth - sz.X) / 2, listY + 80),
                Color.Gray * 0.7f);
            return;
        }

        int visible = Math.Min(rows, VisibleRows);

        for (int i = 0; i < visible; i++)
        {
            int rowIndex = i + _scrollOffset;
            if (rowIndex >= rows) break;

            bool hovered = i == _hoveredRow;
            if (hovered)
            {
                sb.Draw(pixel, _rowRects[i], RowHoverFill);
            }

            DrawRow(sb, font, pixel, theme, _rowRects[i].X, _rowRects[i].Y, _rowRects[i].Width, rowIndex, hovered);
        }

        // Scrollbar (track + thumb) when list overflows — left as thin solid rects per plan
        if (_scrollTrackRect != Rectangle.Empty)
        {
            sb.Draw(pixel, _scrollTrackRect, RowHoverFill * 0.35f);
            sb.Draw(pixel, _scrollThumbRect, RowHoverFill);
        }

    }

    private static void DrawCenteredTitle(SpriteBatch sb, SpriteFontBase font, string text, Rectangle rect,
        Color color, float letterSpacing)
    {
        var raw = font.MeasureString(text);
        float width = raw.X + (text.Length > 1 ? letterSpacing * (text.Length - 1) : 0f);
        float height = raw.Y;
        var pos = new Vector2(
            rect.X + (rect.Width - width) / 2f,
            rect.Y + (rect.Height - height) / 2f);

        float x = pos.X;
        for (int i = 0; i < text.Length; i++)
        {
            string c = text[i].ToString();
            sb.DrawString(font, c, new Vector2(x, pos.Y), color);
            sb.DrawString(font, c, new Vector2(x + 1, pos.Y), color);
            sb.DrawString(font, c, new Vector2(x, pos.Y + 1), color * 0.82f);
            x += font.MeasureString(c).X + letterSpacing;
        }
    }

    private void DrawTab(SpriteBatch sb, SpriteFontBase font, UITheme theme, int x, int y, string label, bool active)
    {
        var tabRect = new Rectangle(x, y, 80, 32);
        var tex = active ? theme.TabOn : theme.TabOff;
        NineSlice.Draw(sb, tex, tabRect, theme.TabInsets);
        var sz = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(x + (80 - sz.X) / 2, y + (32 - sz.Y) / 2),
            active ? Color.Black : Color.White);
    }

    private void DrawRow(SpriteBatch sb, SpriteFontBase font, Texture2D pixel, UITheme theme, int x, int y, int width, int rowIndex, bool hovered)
    {
        // Icon cell (16x16 centered vertically in 40px row)
        int iconX = x + 8;
        int iconY = y + (RowHeight - IconSize) / 2;

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
            priceColor = _inv.Gold >= price
                ? (hovered ? Color.White : PriceGold)
                : Color.Gray * 0.7f;
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
            priceColor = price > 0
                ? (hovered ? Color.White : PriceGold)
                : Color.Gray * 0.7f;
        }

        // Icon
        var iconDef = ItemRegistry.Get(itemId);
        if (iconDef != null)
        {
            var srcRect = _atlas.GetRect(iconDef.SpriteId);
            sb.Draw(_atlas.GetTexture(iconDef.SpriteId),
                new Rectangle(iconX, iconY, IconSize, IconSize), srcRect, Color.White);
        }

        // Name — brown on cream; flips to white on hover (contrasts the flat brown fill).
        sb.DrawString(font, label, new Vector2(iconX + IconSize + IconTextGap, y + (RowHeight - font.MeasureString(label).Y) / 2),
            hovered ? Color.White : RowText);

        // Price (left of the per-row stepper)
        string priceText = $"{price}g";
        var priceSz = font.MeasureString(priceText);
        int actionX = x + width - 72;
        sb.DrawString(font, priceText,
            new Vector2(actionX - 80 - priceSz.X - 12, y + (RowHeight - priceSz.Y) / 2),
            priceColor);

        // Per-row qty stepper: (-)[ x1 ](+) using circle buttons with +/- glyphs.
        // Hit-test rects in _qtyMinusRects / _qtyPlusRects stay identical (layout invariant).
        int visibleSlot = rowIndex - _scrollOffset;
        if (visibleSlot >= 0 && visibleSlot < VisibleRows && _qtyLabelRects[visibleSlot] != Rectangle.Empty)
        {
            var minus = _qtyMinusRects[visibleSlot];
            var qtyLabel = _qtyLabelRects[visibleSlot];
            var plus  = _qtyPlusRects[visibleSlot];

            // Minus circle button + Icon_minus overlay
            sb.Draw(theme.BtnCircleSmall, minus, Color.White);
            int glyph = 10;
            var minusGlyphRect = new Rectangle(
                minus.X + (minus.Width - glyph) / 2,
                minus.Y + (minus.Height - glyph) / 2,
                glyph, glyph);
            sb.Draw(theme.IconMinus, minusGlyphRect, Color.White);

            // Qty label — plain text, brown on cream, white on hover.
            int qtyVal = rowIndex < _rowQty.Length ? _rowQty[rowIndex] : 1;
            string qtyText = $"x{qtyVal}";
            var qs = font.MeasureString(qtyText);
            sb.DrawString(font, qtyText,
                new Vector2(qtyLabel.X + (qtyLabel.Width - qs.X) / 2, qtyLabel.Y + (qtyLabel.Height - qs.Y) / 2),
                hovered ? Color.White : RowText);

            // Plus circle button + Icon_plus overlay
            sb.Draw(theme.BtnCircleSmall, plus, Color.White);
            var plusGlyphRect = new Rectangle(
                plus.X + (plus.Width - glyph) / 2,
                plus.Y + (plus.Height - glyph) / 2,
                glyph, glyph);
            sb.Draw(theme.IconPlus, plusGlyphRect, Color.White);
        }

        // Action button — pixel-art yellow button via 9-slice (ornate corners intact, middle stretches).
        string action = _tab == Tab.Buy ? "Buy" : "Sell";
        bool enabled = IsActionEnabled(rowIndex);
        var btnRect = new Rectangle(actionX, y + 8, 60, 24);
        NineSlice.Draw(sb, theme.YellowBtnSmall, btnRect, theme.YellowBtnSmallInsets,
            enabled ? Color.White : Color.White * 0.4f);
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

}
