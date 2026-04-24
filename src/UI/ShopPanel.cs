using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI.Widgets;
using WidgetTab = stardew_medieval_v3.UI.Widgets.Tab;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Buy/Sell shop panel (720x400, centered horizontally, 48px from top).
/// Owned by <see cref="stardew_medieval_v3.Scenes.ShopOverlayScene"/>.
/// UI-SPEC §Component Inventory / §State machine: shop buy/sell.
///
/// Migrated to the UI Widgets framework (quick 260424-2af): tabs, close button,
/// per-row qty steppers, and action buttons are all <see cref="IClickable"/>
/// widgets registered with the caller's <see cref="UIManager"/> via
/// <see cref="BuildWidgets"/>. Outside-click close stays scene-level
/// (Pitfall 4). Row hover for visual feedback remains imperative (not clickable).
/// </summary>
public class ShopPanel
{
    private enum ShopTab { Buy, Sell }

    // Layout (UI-SPEC §Component Inventory)
    private const int PanelWidth = 720;
    private const int PanelHeight = 370;
    private const int RowHeight = 40;
    private const int VisibleRows = 7;
    private const int IconSize = 24;
    private const int IconTextGap = 8;

    // Runtime anchors computed from the current viewport inside UpdateLayoutCache.
    private int _panelX;
    private int _panelY;

    // Colors (design fingerprint)
    private static readonly Color Dim = Color.Black * 0.55f;
    private static readonly Color RowText = new Color(78, 58, 44);
    private static readonly Color RowHoverFill = new Color(78, 58, 44);
    private static readonly Color PriceGold = new Color(184, 134, 11);

    private readonly InventoryManager _inv;
    private readonly SpriteAtlas _atlas;

    private ShopTab _tab = ShopTab.Buy;

    // Cached layout rects (recomputed each Update via UpdateLayoutCache).
    private Rectangle _panelRect;
    private Rectangle _buyTabRect;
    private Rectangle _sellTabRect;
    private Rectangle _closeRect;
    private readonly Rectangle[] _rowRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _actionBtnRects = new Rectangle[VisibleRows];
    private int _hoveredRow = -1;
    private int _scrollOffset;

    // Per-row quantity state (absolute row indices into current tab).
    private int[] _rowQty = Array.Empty<int>();
    private ShopTab _rowQtyTab = ShopTab.Buy;
    private int _rowQtyRowCount = 0;

    // Per-visible-row qty widget rects.
    private readonly Rectangle[] _qtyMinusRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _qtyLabelRects = new Rectangle[VisibleRows];
    private readonly Rectangle[] _qtyPlusRects  = new Rectangle[VisibleRows];

    private Rectangle _scrollTrackRect;
    private Rectangle _scrollThumbRect;

    // Widgets — built once by BuildWidgets; bounds refreshed each frame in Update.
    private WidgetTab _buyTab = null!;
    private WidgetTab _sellTab = null!;
    private CloseButton _closeBtn = null!;
    private readonly IconButton[] _qtyMinusBtns = new IconButton[VisibleRows];
    private readonly IconButton[] _qtyPlusBtns  = new IconButton[VisibleRows];
    private readonly TextButton[] _actionBtns   = new TextButton[VisibleRows];
    private bool _widgetsBuilt;

    // Deferred transaction signal (drained by Update into the out toast param).
    private ToastRequest? _pendingToast;
    private bool _requestedClose;

    /// <summary>Outbound signal from <see cref="Update"/>: a toast the caller should show.</summary>
    public record ToastRequest(string Text, Color Color);

    public ShopPanel(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inv = inventory;
        _atlas = atlas;
    }

    /// <summary>
    /// Register this panel's widgets with the scene-owned <see cref="UIManager"/>.
    /// Called once by <c>ShopOverlayScene.LoadContent</c>. The per-row widget
    /// pool (qty -/+ / action) is sized to <see cref="VisibleRows"/>; row
    /// <c>Bounds</c> and <c>Enabled</c> are refreshed each frame by
    /// <see cref="Update"/> so widgets track scroll + active tab correctly.
    /// </summary>
    public void BuildWidgets(UIManager ui, UITheme theme, SpriteFontBase font)
    {
        if (_widgetsBuilt) return;
        _buyTab  = new WidgetTab("Buy",  theme.TabOn, theme.TabOff, theme.TabInsets, font) { OnClickAction = () => SwitchTab(ShopTab.Buy) };
        _sellTab = new WidgetTab("Sell", theme.TabOn, theme.TabOff, theme.TabInsets, font) { OnClickAction = () => SwitchTab(ShopTab.Sell) };
        _closeBtn = new CloseButton(theme.BtnIconX) { OnClickAction = () => _requestedClose = true };
        ui.Register(_buyTab);
        ui.Register(_sellTab);
        ui.Register(_closeBtn);

        for (int k = 0; k < VisibleRows; k++)
        {
            int rowSlot = k;
            _qtyMinusBtns[k] = new IconButton(theme.IconMinus, theme.BtnCircleSmall, NineSlice.Insets.Uniform(4))
            {
                OnClickAction = () => AdjustRowQty(rowSlot, decrement: true),
            };
            _qtyPlusBtns[k] = new IconButton(theme.IconPlus, theme.BtnCircleSmall, NineSlice.Insets.Uniform(4))
            {
                OnClickAction = () => AdjustRowQty(rowSlot, decrement: false),
            };
            _actionBtns[k] = new TextButton(string.Empty, theme.YellowBtnSmall, theme.YellowBtnSmallInsets, font)
            {
                OnClickAction = () => ExecuteRow(rowSlot),
            };
            ui.Register(_qtyMinusBtns[k]);
            ui.Register(_qtyPlusBtns[k]);
            ui.Register(_actionBtns[k]);
        }
        _widgetsBuilt = true;
    }

    /// <summary>
    /// Drive input. Returns <c>true</c> when the user requested a close (Escape,
    /// click outside panel, or Close X). <paramref name="toast"/> is set when a
    /// transaction completed.
    /// </summary>
    public bool Update(float dt, InputManager input, UIManager ui, int viewportWidth, int viewportHeight, out ToastRequest? toast)
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

        // Sync widget state from scene model each frame.
        _buyTab.Bounds = _buyTabRect;
        _sellTab.Bounds = _sellTabRect;
        _buyTab.IsActive = _tab == ShopTab.Buy;
        _sellTab.IsActive = _tab == ShopTab.Sell;
        _closeBtn.Bounds = _closeRect;

        for (int k = 0; k < VisibleRows; k++)
        {
            if (k < visible)
            {
                int abs = k + _scrollOffset;
                _qtyMinusBtns[k].Bounds = _qtyMinusRects[k];
                _qtyMinusBtns[k].Enabled = true;
                _qtyPlusBtns[k].Bounds = _qtyPlusRects[k];
                _qtyPlusBtns[k].Enabled = true;
                _actionBtns[k].Bounds = _actionBtnRects[k];
                _actionBtns[k].Enabled = IsActionEnabled(abs);
                _actionBtns[k].Label = _tab == ShopTab.Buy ? "Buy" : "Sell";
            }
            else
            {
                _qtyMinusBtns[k].Enabled = false;
                _qtyPlusBtns[k].Enabled = false;
                _actionBtns[k].Enabled = false;
            }
        }

        // Widget layer FIRST — consumes click if widget hit.
        bool consumed = ui.Update(dt, input);

        if (_requestedClose)
        {
            _requestedClose = false;
            Console.WriteLine("[ShopPanel] Close X clicked");
            return true;
        }

        // Outside-click close (Pitfall 4: scene-level rule, not a widget).
        if (!consumed && input.IsLeftClickPressed && !_panelRect.Contains(mp))
        {
            Console.WriteLine("[ShopPanel] Click outside panel -> close");
            return true;
        }

        // Keyboard: only Escape closes (D-03).
        if (input.IsKeyPressed(Keys.Escape))
        {
            Console.WriteLine("[ShopPanel] Escape pressed -> close");
            return true;
        }

        toast = _pendingToast;
        _pendingToast = null;
        return false;
    }

    private void SwitchTab(ShopTab tab)
    {
        if (_tab == tab) return;
        _tab = tab;
        EnsureRowQtySized(GetRowCount());
        Console.WriteLine($"[ShopPanel] Tab -> {tab}");
    }

    private void AdjustRowQty(int visibleSlot, bool decrement)
    {
        int abs = visibleSlot + _scrollOffset;
        if (abs < 0 || abs >= _rowQty.Length) return;
        if (decrement)
        {
            _rowQty[abs] = Math.Max(1, _rowQty[abs] - 1);
            Console.WriteLine($"[ShopPanel] qty- row={abs} qty={_rowQty[abs]}");
        }
        else
        {
            int cap = GetMaxQty(abs);
            _rowQty[abs] = Math.Min(Math.Max(1, cap), _rowQty[abs] + 1);
            if (cap <= 0) _rowQty[abs] = 1;
            Console.WriteLine($"[ShopPanel] qty+ row={abs} qty={_rowQty[abs]} cap={cap}");
        }
    }

    private void ExecuteRow(int visibleSlot)
    {
        int abs = visibleSlot + _scrollOffset;
        if (abs < 0 || abs >= _rowQty.Length) return;
        if (!IsActionEnabled(abs))
        {
            Console.WriteLine($"[ShopPanel] action-click row={abs} disabled");
            return;
        }
        int q = Math.Clamp(_rowQty[abs], 1, Math.Max(1, GetMaxQty(abs)));
        if (_tab == ShopTab.Buy)
        {
            Console.WriteLine($"[ShopPanel] Buy-click row={abs} qty={q}");
            TryBuy(abs, q, out _pendingToast);
        }
        else
        {
            Console.WriteLine($"[ShopPanel] Sell-click row={abs} qty={q}");
            TrySell(abs, q, out _pendingToast);
        }
        // Row may have shrunk (Sell) or price/headroom changed (Buy) — reset qty to 1.
        if (abs < _rowQty.Length) _rowQty[abs] = 1;
    }

    /// <summary>
    /// Recomputes panel/tab/row/action-button/close rects into the cached fields.
    /// </summary>
    private void UpdateLayoutCache(int viewportWidth, int viewportHeight)
    {
        _panelX = (viewportWidth - PanelWidth) / 2;
        _panelY = (viewportHeight - PanelHeight) / 2;

        int rows = GetRowCount();
        int visible = Math.Min(rows, VisibleRows);

        int maxScroll = Math.Max(0, rows - VisibleRows);
        if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
        if (_scrollOffset < 0) _scrollOffset = 0;

        int scroll = _scrollOffset;

        _panelRect = new Rectangle(_panelX, _panelY, PanelWidth, PanelHeight);

        int tabY = _panelY + 16;
        _buyTabRect  = new Rectangle(_panelX + 16,      tabY, 80, 32);
        _sellTabRect = new Rectangle(_panelX + 16 + 88, tabY, 80, 32);

        // Close X button — 32x32 pixel-art icon at top-right of header.
        _closeRect = new Rectangle(_panelX + PanelWidth - 40, _panelY + 8, 32, 32);

        // Row + action-button rects
        int listX = _panelX + 16;
        int listY = tabY + 40;
        int width = PanelWidth - 32;
        int qtyBtnSize = 24;
        int qtyBtnYOffset = (RowHeight - qtyBtnSize) / 2;
        for (int i = 0; i < VisibleRows; i++)
        {
            int rowIndex = i + scroll;
            if (i < visible && rowIndex < rows)
            {
                int y = listY + i * RowHeight;
                _rowRects[i] = new Rectangle(listX, y, width, RowHeight - 2);
                int actionX = listX + width - 72;
                _actionBtnRects[i] = new Rectangle(actionX, y + 8, 60, 24);
                _qtyMinusRects[i] = new Rectangle(actionX - 80, y + qtyBtnYOffset, qtyBtnSize, qtyBtnSize);
                _qtyLabelRects[i] = new Rectangle(actionX - 58, y + 6, 32, 24);
                _qtyPlusRects[i]  = new Rectangle(actionX - 30, y + qtyBtnYOffset, qtyBtnSize, qtyBtnSize);
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
        if (_tab == ShopTab.Buy) return ShopStock.Items.Count;
        int n = 0;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
            if (_inv.GetSlot(i) != null) n++;
        return n;
    }

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

    private void EnsureRowQtySized(int rowCount)
    {
        if (_rowQty.Length == rowCount && _rowQtyTab == _tab && _rowQtyRowCount == rowCount) return;
        _rowQty = new int[rowCount];
        for (int i = 0; i < rowCount; i++) _rowQty[i] = 1;
        _rowQtyTab = _tab;
        _rowQtyRowCount = rowCount;
    }

    private int GetMaxQty(int rowIndex)
    {
        if (_tab == ShopTab.Buy)
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
            int refund = entry.Price * leftover;
            _inv.AddGold(refund);
            Console.WriteLine($"[ShopPanel] Buy partial: {qty - leftover}/{qty} added, refunded {refund}g for {leftover} leftover");
        }
        int delivered = qty - leftover;
        Console.WriteLine($"[ShopPanel] Bought {entry.ItemId} x{delivered} for {entry.Price * delivered}g");
        toast = new ToastRequest($"Purchased {def.Name} x{delivered}", Color.LimeGreen);

        EnsureRowQtySized(GetRowCount());
    }

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

        int remainingRows = GetRowCount();
        int maxScroll = Math.Max(0, remainingRows - VisibleRows);
        if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;

        EnsureRowQtySized(remainingRows);

        Console.WriteLine($"[ShopPanel] Sold {removed.ItemId} x{clamped} for {totalPrice}g");
        toast = new ToastRequest($"Sold {def.Name} x{clamped} for {totalPrice}g", Color.Gold);
    }

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

    /// <summary>Render panel + rows + header. Tab/close/qty/action widgets Draw themselves.</summary>
    public void Draw(SpriteBatch sb, SpriteFontBase font, SpriteFontBase titleFont, Texture2D pixel, UITheme theme,
        int viewportWidth, int viewportHeight)
    {
        // Defensive: ensure cached layout matches current state even if Draw runs without a preceding Update.
        UpdateLayoutCache(viewportWidth, viewportHeight);

        // Full-screen dim — covers the entire real viewport.
        sb.Draw(pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), Dim);

        NineSlice.Draw(sb, theme.PanePopup, _panelRect, theme.PanePopupInsets);

        // Title — shared helper in WidgetHelpers keeps every modal title identical.
        WidgetHelpers.DrawPanelTitle(sb, titleFont, "Shop",
            new Rectangle(_panelX + 28, _panelY - 3, PanelWidth - 56, 50),
            Color.LightGoldenrodYellow, 2f);
        int headerY = _panelY + 8;

        // Gold display — thousands separator to match HUD.
        string goldText = _inv.Gold.ToString("N0");
        var goldSize = font.MeasureString(goldText);
        int coinSize = 18;
        int goldBlockW = coinSize + 6 + (int)goldSize.X;
        int goldX = _panelX + PanelWidth - 44 - goldBlockW;
        int goldY = headerY + 10;
        sb.Draw(theme.GoldIcon, new Rectangle(goldX, goldY, coinSize, coinSize), Color.White);
        sb.DrawString(font, goldText,
            new Vector2(goldX + coinSize + 6, goldY + (coinSize - goldSize.Y) / 2),
            Color.Gold);

        // Tabs (widget-rendered via _buyTab.Draw / _sellTab.Draw)
        _buyTab.Draw(sb);
        _sellTab.Draw(sb);

        // Close X
        _closeBtn.Draw(sb);

        // Item list region
        int tabY = _buyTabRect.Y;
        int listY = tabY + 36;

        int rows = GetRowCount();
        if (rows == 0)
        {
            string empty = _tab == ShopTab.Sell ? "Your inventory is empty" : "Shop is closed";
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
                sb.Draw(pixel, _rowRects[i], RowHoverFill);

            DrawRow(sb, font, pixel, theme, _rowRects[i].X, _rowRects[i].Y, _rowRects[i].Width, rowIndex, hovered, i);
        }

        // Scrollbar
        if (_scrollTrackRect != Rectangle.Empty)
        {
            sb.Draw(pixel, _scrollTrackRect, RowHoverFill * 0.35f);
            sb.Draw(pixel, _scrollThumbRect, RowHoverFill);
        }

        // Per-row widgets (qty +/- + action button) — draw last so they render on top of row text.
        for (int i = 0; i < visible; i++)
        {
            _qtyMinusBtns[i].Draw(sb);
            _qtyPlusBtns[i].Draw(sb);
            _actionBtns[i].Draw(sb);
        }
    }

    // Local DrawCenteredTitle removed — migrated to WidgetHelpers.DrawPanelTitle (quick 260424-…).

    private void DrawRow(SpriteBatch sb, SpriteFontBase font, Texture2D pixel, UITheme theme, int x, int y, int width, int rowIndex, bool hovered, int visibleSlot)
    {
        // Icon cell (16x16 centered vertically in 40px row)
        int iconX = x + 8;
        int iconY = y + (RowHeight - IconSize) / 2;

        string itemId;
        string label;
        int price;
        Color priceColor;

        if (_tab == ShopTab.Buy)
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

        // Name
        sb.DrawString(font, label, new Vector2(iconX + IconSize + IconTextGap, y + (RowHeight - font.MeasureString(label).Y) / 2),
            hovered ? Color.White : RowText);

        // Price (left of the per-row stepper)
        string priceText = $"{price}g";
        var priceSz = font.MeasureString(priceText);
        int actionX = x + width - 72;
        sb.DrawString(font, priceText,
            new Vector2(actionX - 80 - priceSz.X - 12, y + (RowHeight - priceSz.Y) / 2),
            priceColor);

        // Qty label — plain text, brown on cream, white on hover. The +/- buttons
        // themselves are widgets and draw themselves after the row content.
        if (_qtyLabelRects[visibleSlot] != Rectangle.Empty)
        {
            var qtyLabel = _qtyLabelRects[visibleSlot];
            int qtyVal = rowIndex < _rowQty.Length ? _rowQty[rowIndex] : 1;
            string qtyText = $"x{qtyVal}";
            var qs = font.MeasureString(qtyText);
            sb.DrawString(font, qtyText,
                new Vector2(qtyLabel.X + (qtyLabel.Width - qs.X) / 2, qtyLabel.Y + (qtyLabel.Height - qs.Y) / 2),
                hovered ? Color.White : RowText);
        }
    }

    private bool IsActionEnabled(int rowIndex)
    {
        if (_tab == ShopTab.Buy)
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
