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

        if (input.IsKeyPressed(Keys.Escape))
            return true;

        // Tab toggle
        if (input.IsKeyPressed(Keys.Tab))
        {
            _tab = _tab == Tab.Buy ? Tab.Sell : Tab.Buy;
            _selectedIndex = 0;
            return false;
        }

        int rows = GetRowCount();
        if (rows > 0)
        {
            if (input.IsKeyPressed(Keys.Down)) _selectedIndex = (_selectedIndex + 1) % rows;
            if (input.IsKeyPressed(Keys.Up)) _selectedIndex = (_selectedIndex - 1 + rows) % rows;
        }
        else
        {
            _selectedIndex = 0;
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            if (_tab == Tab.Buy) TryBuy(out toast);
            else TrySell(out toast);
        }

        return false;
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

    /// <summary>Buy flow: check gold → check space → debit → add. Strict order (T-04-14/T-04-15).</summary>
    private void TryBuy(out ToastRequest? toast)
    {
        toast = null;
        if (_selectedIndex < 0 || _selectedIndex >= ShopStock.Items.Count) return;
        var entry = ShopStock.Items[_selectedIndex];
        var def = ItemRegistry.Get(entry.ItemId);
        if (def == null)
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: unknown item {entry.ItemId}");
            return;
        }

        if (_inv.Gold < entry.Price)
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: {ReasonNotEnoughGold} ({_inv.Gold} < {entry.Price})");
            return;
        }
        if (!CanAddOne(entry.ItemId))
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: {ReasonInventoryFull}");
            return;
        }
        if (!_inv.TrySpendGold(entry.Price))
        {
            Console.WriteLine($"[ShopPanel] Buy blocked: TrySpendGold returned false");
            return;
        }
        int leftover = _inv.TryAdd(entry.ItemId, 1);
        if (leftover > 0)
        {
            // Safety: CanAddOne said yes but TryAdd failed — refund to prevent silent loss.
            _inv.AddGold(entry.Price);
            Console.WriteLine($"[ShopPanel] Buy refunded: TryAdd failed after CanAddOne=true");
            return;
        }
        Console.WriteLine($"[ShopPanel] Bought {entry.ItemId} for {entry.Price}g");
        toast = new ToastRequest($"Purchased {def.Name}", Color.LimeGreen);
    }

    /// <summary>Sell flow: look up Nth non-empty slot → null-check → remove → credit (T-04-16).</summary>
    private void TrySell(out ToastRequest? toast)
    {
        toast = null;
        int slotIndex = IndexOfNthFilledSlot(_selectedIndex);
        if (slotIndex < 0)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: {ReasonSelectToSell}");
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
            Console.WriteLine($"[ShopPanel] Sell blocked: {ReasonCannotSell} ({stack.ItemId})");
            return;
        }

        var removed = _inv.RemoveAt(slotIndex);
        if (removed == null)
        {
            Console.WriteLine($"[ShopPanel] Sell blocked: RemoveAt returned null");
            return;
        }
        // Credit price per unit times quantity removed (mirrors full-stack sell)
        int totalPrice = price * removed.Quantity;
        _inv.AddGold(totalPrice);

        // Keep selection valid
        int remainingRows = GetRowCount();
        if (_selectedIndex >= remainingRows && remainingRows > 0) _selectedIndex = remainingRows - 1;
        if (remainingRows == 0) _selectedIndex = 0;

        Console.WriteLine($"[ShopPanel] Sold {removed.ItemId} x{removed.Quantity} for {totalPrice}g");
        toast = new ToastRequest($"Sold {def.Name} for {totalPrice}g", Color.Gold);
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
        // Full-screen dim
        sb.Draw(pixel, new Rectangle(0, 0, 960, 540), Dim);

        // Outline + fill
        sb.Draw(pixel, new Rectangle(PanelX - 1, PanelY - 1, PanelWidth + 2, PanelHeight + 2), Color.Black);
        sb.Draw(pixel, new Rectangle(PanelX, PanelY, PanelWidth, PanelHeight), PanelFill);

        // Header strip (40px tall inside panel)
        int headerY = PanelY + 8;
        sb.DrawString(font, "Shop", new Vector2(PanelX + 16, headerY), Color.White);

        string goldText = $"Gold: {_inv.Gold}";
        var goldSize = font.MeasureString(goldText);
        sb.DrawString(font, goldText,
            new Vector2(PanelX + PanelWidth - 16 - goldSize.X, headerY), Color.Gold);

        string escHint = "Esc to close";
        var escSize = font.MeasureString(escHint);
        sb.DrawString(font, escHint,
            new Vector2(PanelX + PanelWidth - 16 - escSize.X, PanelY + PanelHeight - 8 - escSize.Y),
            Color.Gray * 0.7f);

        // Tab strip — 2 x (80x32), row at panelY + 40
        int tabY = PanelY + 40;
        DrawTab(sb, font, pixel, PanelX + 16, tabY, "Buy", _tab == Tab.Buy);
        DrawTab(sb, font, pixel, PanelX + 16 + 88, tabY, "Sell", _tab == Tab.Sell);

        // Divider below tabs
        sb.Draw(pixel, new Rectangle(PanelX + 8, tabY + 40, PanelWidth - 16, 1), Bevel);

        // Item list region
        int listX = PanelX + 16;
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
        int scroll = 0;
        if (_selectedIndex >= VisibleRows) scroll = _selectedIndex - VisibleRows + 1;

        for (int i = 0; i < visible; i++)
        {
            int rowIndex = i + scroll;
            if (rowIndex >= rows) break;
            int y = listY + i * RowHeight;
            bool selected = rowIndex == _selectedIndex;
            DrawRow(sb, font, pixel, listX, y, PanelWidth - 32, rowIndex, selected);
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

        // Price (right-aligned before action button area)
        string priceText = $"{price}g";
        var priceSz = font.MeasureString(priceText);
        int actionX = x + width - 72;
        sb.DrawString(font, priceText,
            new Vector2(actionX - priceSz.X - 12, y + (RowHeight - priceSz.Y) / 2),
            priceColor);

        // Action button (60x24) on the right
        string action = _tab == Tab.Buy ? "Buy" : "Sell";
        bool enabled = selected && IsActionEnabled(rowIndex);
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
