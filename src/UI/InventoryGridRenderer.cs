using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a 20-slot inventory grid (5 columns x 4 rows) with item icons,
/// rarity color tinting, quantity labels, and drag-and-drop support.
/// Also renders 7 defensive equipment slots around a character silhouette.
/// Drops onto the screen-bottom hotbar/consumable slots are handled via
/// HotbarRenderer hit-testing.
/// </summary>
public class InventoryGridRenderer
{
    /// <summary>Column count for the inventory grid. Scenes compute this from pane width
    /// each frame so the grid fills horizontally and wraps naturally as capacity grows.</summary>
    public int Columns { get; set; } = 5;
    public const int SlotSize = 60;
    private const int Padding = 0;
    public const int EquipSlotSize = 67;
    public const int EquipGap = 4;
    private const int EquipStride = EquipSlotSize + EquipGap;

    private static readonly EquipSlot[] EquipSlots = {
        EquipSlot.Helmet, EquipSlot.Necklace,
        EquipSlot.Armor, EquipSlot.Shield,
        EquipSlot.Ring, EquipSlot.Legs,
        EquipSlot.Boots
    };

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;
    private HotbarRenderer? _hotbar;
    private int _screenWidth, _screenHeight;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFontBase _font = null!;
    private Texture2D _pixel = null!;
    private UITheme? _theme;

    // Drag state
    private bool _isDragging;
    private int _dragSourceSlot = -1;
    private EquipSlot? _dragSourceEquip;
    private int _dragSourceHotbar = -1;       // dragging FROM hotbar ref slot
    private int _dragSourceConsumable = -1;   // dragging FROM consumable ref slot
    private Point _dragPosition;

    public bool IsDragging => _isDragging;

    public InventoryGridRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    /// <summary>Set the hotbar renderer for drop hit-testing.</summary>
    public void SetHotbar(HotbarRenderer hotbar, int screenWidth, int screenHeight)
    {
        _hotbar = hotbar;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    /// <summary>Inject the shared <see cref="UITheme"/> so the equipment silhouette can be wrapped in a 9-slice frame.</summary>
    public void SetTheme(UITheme theme) => _theme = theme;

    public void CancelDrag()
    {
        _isDragging = false;
        _dragSourceSlot = -1;
        _dragSourceEquip = null;
        _dragSourceHotbar = -1;
        _dragSourceConsumable = -1;
        _hotbar?.SetExternalDragSource(-1, -1);
    }

    public void LoadContent(GraphicsDevice device, SpriteFontBase font)
    {
        _font = font;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var normalStream = File.OpenRead("assets/Sprites/System/UI Elements/Slot/UI_Slot_Normal.png");
            _slotNormal = Texture2D.FromStream(device, normalStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryGridRenderer] Failed to load UI_Slot_Normal: {ex.Message}");
            _slotNormal = new Texture2D(device, 1, 1);
            _slotNormal.SetData(new[] { new Color(60, 40, 30) });
        }

        try
        {
            using var selectedStream = File.OpenRead("assets/Sprites/System/UI Elements/Slot/UI_Slot_Selected.png");
            _slotSelected = Texture2D.FromStream(device, selectedStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryGridRenderer] Failed to load UI_Slot_Selected: {ex.Message}");
            _slotSelected = new Texture2D(device, 1, 1);
            _slotSelected.SetData(new[] { Color.Gold });
        }

        Console.WriteLine("[InventoryGridRenderer] Content loaded");
    }

    /// <summary>Handle mouse press — start drag from grid, equipment, hotbar ref, or consumable ref.</summary>
    public void HandleMouseDown(Point mousePos, int gridX, int gridY, int equipX, int equipY)
    {
        int hitSlot = HitTestGrid(mousePos, gridX, gridY);
        if (hitSlot >= 0 && _inventory.GetSlot(hitSlot) != null)
        {
            _isDragging = true;
            _dragSourceSlot = hitSlot;
            _dragPosition = mousePos;
            return;
        }

        for (int i = 0; i < EquipSlots.Length; i++)
        {
            var rect = GetEquipRect(i, equipX, equipY);
            if (rect.Contains(mousePos) && _inventory.GetEquipped(EquipSlots[i]) != null)
            {
                _isDragging = true;
                _dragSourceEquip = EquipSlots[i];
                _dragPosition = mousePos;
                return;
            }
        }

        // Hotbar / consumable ref slots (bottom of screen). Dragging a ref out
        // to the grid (or anywhere that isn't another ref slot) clears it,
        // which effectively "unequips" the hotbar slot back to Hand.
        if (_hotbar != null)
        {
            int hotbarHit = _hotbar.HitTestMain(mousePos, _screenWidth, _screenHeight);
            if (hotbarHit >= 0 && _inventory.GetHotbarRef(hotbarHit) != null)
            {
                _isDragging = true;
                _dragSourceHotbar = hotbarHit;
                _dragPosition = mousePos;
                _hotbar.SetExternalDragSource(hotbarHit, -1);
                return;
            }

            int consHit = _hotbar.HitTestConsumable(mousePos, _screenWidth, _screenHeight);
            if (consHit >= 0 && _inventory.GetConsumableRef(consHit) != null)
            {
                _isDragging = true;
                _dragSourceConsumable = consHit;
                _dragPosition = mousePos;
                _hotbar.SetExternalDragSource(-1, consHit);
                return;
            }
        }
    }

    public void UpdateDrag(Point mousePos)
    {
        if (_isDragging) _dragPosition = mousePos;
    }

    /// <summary>
    /// If a drag is active and the cursor is OUTSIDE <paramref name="panelRect"/>, dispatch
    /// the dragged grid item to <paramref name="spawn"/> (the scene will hand it off to
    /// <c>Services.SpawnItemDrop</c>) and clear the source slot. Equipment / hotbar /
    /// consumable drags ignored — only grid items can be tossed to the floor.
    /// Returns true when the drop was consumed (so the caller skips HandleMouseUp).
    /// </summary>
    public bool TryDropOutsidePanel(Point mousePos, Rectangle panelRect, System.Action<string, int> spawn)
    {
        if (!_isDragging) return false;
        if (panelRect.Contains(mousePos)) return false;
        if (_dragSourceSlot < 0 || _dragSourceEquip.HasValue) return false;

        // Hotbar and consumable strips live OUTSIDE the inventory panel rect — releasing
        // the drag onto one of them is a "set hotbar/consumable reference" gesture, not a
        // toss-to-floor. Defer to HandleMouseUp in those cases.
        if (_hotbar != null)
        {
            if (_hotbar.HitTestMain(mousePos, _screenWidth, _screenHeight) >= 0) return false;
            if (_hotbar.HitTestConsumable(mousePos, _screenWidth, _screenHeight) >= 0) return false;
        }

        var stack = _inventory.GetSlot(_dragSourceSlot);
        if (stack == null) { CancelDrag(); return false; }

        spawn(stack.ItemId, stack.Quantity);
        _inventory.SetSlot(_dragSourceSlot, null);
        CancelDrag();
        return true;
    }

    /// <summary>Handle mouse release — drop onto grid, equipment, hotbar, or consumable.</summary>
    public void HandleMouseUp(Point mousePos, int gridX, int gridY, int equipX, int equipY)
    {
        if (!_isDragging) return;

        int targetGrid = HitTestGrid(mousePos, gridX, gridY);
        EquipSlot? targetEquip = HitTestEquip(mousePos, equipX, equipY);

        // Check hotbar/consumable drop targets
        int targetHotbar = -1;
        int targetConsumable = -1;
        if (_hotbar != null)
        {
            targetHotbar = _hotbar.HitTestMain(mousePos, _screenWidth, _screenHeight);
            targetConsumable = _hotbar.HitTestConsumable(mousePos, _screenWidth, _screenHeight);
        }

        if (_dragSourceEquip.HasValue)
        {
            // FROM equipment → grid only
            if (targetGrid >= 0)
                _inventory.TryUnequip(_dragSourceEquip.Value, targetGrid);
        }
        else if (_dragSourceSlot >= 0)
        {
            var stack = _inventory.GetSlot(_dragSourceSlot);
            string? dragItemId = stack?.ItemId;

            if (targetGrid >= 0 && targetGrid != _dragSourceSlot)
            {
                // Grid → Grid: move/swap
                _inventory.MoveItem(_dragSourceSlot, targetGrid);
            }
            else if (targetEquip.HasValue)
            {
                // Grid → Equipment
                _inventory.TryEquipToSlot(_dragSourceSlot, targetEquip.Value);
            }
            else if (targetHotbar >= 0 && dragItemId != null)
            {
                // Grid → Hotbar: set reference
                _inventory.SetHotbarRef(targetHotbar, dragItemId);
            }
            else if (targetConsumable >= 0 && dragItemId != null)
            {
                // Grid → Consumable: set reference (validates type)
                _inventory.SetConsumableRef(targetConsumable, dragItemId);
            }
        }
        else if (_dragSourceHotbar >= 0)
        {
            // FROM hotbar ref: drop onto another hotbar slot swaps refs,
            // consumable sets ref, anywhere else (including grid) clears it
            // so the slot reverts to Hand.
            string? dragId = _inventory.GetHotbarRef(_dragSourceHotbar);
            if (targetHotbar >= 0 && targetHotbar != _dragSourceHotbar)
            {
                _inventory.SwapHotbarRefs(_dragSourceHotbar, targetHotbar);
            }
            else if (targetConsumable >= 0 && dragId != null)
            {
                if (_inventory.SetConsumableRef(targetConsumable, dragId))
                    _inventory.SetHotbarRef(_dragSourceHotbar, null);
            }
            else if (targetHotbar < 0 && targetConsumable < 0)
            {
                // Dropped away from the hotbar row → clear the reference.
                _inventory.SetHotbarRef(_dragSourceHotbar, null);
            }
        }
        else if (_dragSourceConsumable >= 0)
        {
            string? dragId = _inventory.GetConsumableRef(_dragSourceConsumable);
            if (targetConsumable >= 0 && targetConsumable != _dragSourceConsumable)
            {
                _inventory.SwapConsumableRefs(_dragSourceConsumable, targetConsumable);
            }
            else if (targetHotbar >= 0 && dragId != null)
            {
                _inventory.SetHotbarRef(targetHotbar, dragId);
                _inventory.SetConsumableRef(_dragSourceConsumable, null);
            }
            else if (targetHotbar < 0 && targetConsumable < 0)
            {
                _inventory.SetConsumableRef(_dragSourceConsumable, null);
            }
        }

        CancelDrag();
    }

    /// <summary>Draw grid + equipment + dragged item.</summary>
    public void Draw(SpriteBatch sb, int gridX, int gridY, int equipX, int equipY)
    {
        DrawGrid(sb, gridX, gridY);
        DrawEquipment(sb, equipX, equipY);
        DrawDraggedItem(sb);
    }

    private void DrawGrid(SpriteBatch sb, int offsetX, int offsetY)
    {
        // Grid is dynamic: renders up to the MAX bag tier's capacity, but only the first
        // _inventory.Capacity slots are active. Remaining slots are drawn dim (matching
        // ChestScene's beyond-capacity visual) so the player sees how much room future
        // bag upgrades unlock.
        int capacity = _inventory.Capacity;
        int maxCapacity = BagRegistry.MaxCapacity;
        for (int i = 0; i < maxCapacity; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            int x = offsetX + col * (SlotSize + Padding);
            int y = offsetY + row * (SlotSize + Padding);

            var slotRect = new Rectangle(x, y, SlotSize, SlotSize);
            bool enabled = i < capacity;
            sb.Draw(_slotNormal, slotRect, enabled ? Color.White : Color.DimGray * 0.55f);

            if (!enabled)
            {
                // Dark inner panel marks the slot as locked — same visual ChestScene uses
                // for slots beyond the chest variant's capacity.
                sb.Draw(_slotNormal, new Rectangle(slotRect.X + 3, slotRect.Y + 3, slotRect.Width - 6, slotRect.Height - 6), Color.Black * 0.28f);
                continue;
            }

            if (_isDragging && _dragSourceSlot == i && _dragSourceEquip == null)
                continue;

            DrawSlotContents(sb, _inventory.GetSlot(i), slotRect);
        }
    }

    private void DrawEquipment(SpriteBatch sb, int offsetX, int offsetY)
    {
        // Dark thumbnail frame removed — InventoryScene now wraps equipment in the cream
        // PanelSlotPane (same visual as Bolsa/Baú in ChestScene) for a consistent look.

        for (int i = 0; i < EquipSlots.Length; i++)
        {
            var rect = GetEquipRect(i, offsetX, offsetY);
            sb.Draw(_slotNormal, rect, Color.White);

            if (_isDragging && _dragSourceEquip == EquipSlots[i]) continue;

            var itemId = _inventory.GetEquipped(EquipSlots[i]);
            if (itemId != null)
            {
                var def = ItemRegistry.Get(itemId);
                if (def != null)
                {
                    var srcRect = _atlas.GetRect(def.SpriteId);
                    sb.Draw(_atlas.GetTexture(def.SpriteId),
                        new Rectangle(rect.X + 3, rect.Y + 3, EquipSlotSize - 6, EquipSlotSize - 6),
                        srcRect, Color.White);
                }
            }
            else if (_theme != null)
            {
                // Empty-slot watermark icon (Tibia-style). Very faint so the slot reads as empty
                // while still hinting at what belongs there.
                var watermark = GetEquipWatermark(EquipSlots[i], _theme);
                if (watermark != null)
                {
                    int icon = 18;
                    var iconRect = new Rectangle(
                        rect.X + (EquipSlotSize - icon) / 2,
                        rect.Y + (EquipSlotSize - icon) / 2,
                        icon, icon);
                    sb.Draw(watermark, iconRect, Color.White * 0.28f);
                }
            }
        }

        // ATK/DEF badges moved to InventoryScene.Draw so they can anchor to the
        // equipment pane's bottom-left / bottom-right corners instead of tracking
        // the cluster origin. Keeps the renderer focused on the slot grid itself.
    }

    private static Texture2D? GetEquipWatermark(EquipSlot slot, UITheme theme) => slot switch
    {
        EquipSlot.Helmet   => theme.IconEquipHelmet,
        EquipSlot.Necklace => theme.IconEquipNecklace,
        EquipSlot.Armor    => theme.IconEquipArmor,
        EquipSlot.Shield   => theme.IconEquipShield,
        EquipSlot.Ring     => theme.IconEquipRing,
        EquipSlot.Legs     => theme.IconEquipLegs,
        EquipSlot.Boots    => theme.IconEquipBoots,
        _ => null
    };

    private void DrawSlotContents(SpriteBatch sb, ItemStack? stack, Rectangle slotRect)
    {
        if (stack == null) return;
        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null) return;

        // Proportional icon padding — matches ContainerGridRenderer so inventory and chest
        // render items at the same relative inset inside their slot frames.
        int iconPadding = Math.Max(3, slotRect.Width / 10);
        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            slotRect.X + iconPadding, slotRect.Y + iconPadding,
            slotRect.Width - iconPadding * 2, slotRect.Height - iconPadding * 2);
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White);

        // Rarity marker — inner colored border around the slot (replaces the old
        // translucent overlay so the item icon's true colors are preserved).
        Widgets.WidgetHelpers.DrawRarityBorder(sb, _pixel, slotRect, def.Rarity);

        // Watering-can charge bar — only on the watering-can slot, drawn beneath the icon.
        if (def.Id.Equals("Watering_Can", System.StringComparison.OrdinalIgnoreCase))
        {
            Widgets.WidgetHelpers.DrawChargeBar(sb, _pixel, slotRect,
                _inventory.WateringCanCharges, InventoryManager.MaxWateringCanCharges,
                new Color(90, 170, 230));
        }

        if (stack.Quantity > 1)
        {
            string qtyText = stack.Quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(slotRect.Right - qtySize.X - 2, slotRect.Bottom - qtySize.Y),
                Color.White);
        }

        if (stack.Quality > 0)
            DrawQualityStars(sb, slotRect, stack.Quality);
    }

    /// <summary>
    /// Render small star pips in the slot's top-right corner — one per quality tier.
    /// Pips are drawn as 3×3 squares so they read at any UI scale without needing a
    /// dedicated star sprite. Color tier: bronze (1⭐) / silver (2⭐) / gold (3⭐).
    /// </summary>
    private void DrawQualityStars(SpriteBatch sb, Rectangle slotRect, int quality)
    {
        Color color = quality switch
        {
            1 => new Color(205, 127, 50),   // bronze
            2 => new Color(192, 192, 200),  // silver
            _ => new Color(255, 215, 80),   // gold (3+)
        };
        const int pip = 3;
        const int gap = 1;
        int totalW = quality * pip + (quality - 1) * gap;
        int startX = slotRect.X + 3;
        int y = slotRect.Y + 3;
        // Drop shadow for readability over bright icons
        for (int i = 0; i < quality; i++)
        {
            int x = startX + i * (pip + gap);
            sb.Draw(_pixel, new Rectangle(x + 1, y + 1, pip, pip), new Color(0, 0, 0, 180));
            sb.Draw(_pixel, new Rectangle(x, y, pip, pip), color);
        }
        _ = totalW; // reserved for future right-aligned variant
    }

    private void DrawDraggedItem(SpriteBatch sb)
    {
        if (!_isDragging) return;

        string? itemId = null;
        int quantity = 1;

        if (_dragSourceEquip.HasValue)
        {
            itemId = _inventory.GetEquipped(_dragSourceEquip.Value);
        }
        else if (_dragSourceSlot >= 0)
        {
            var stack = _inventory.GetSlot(_dragSourceSlot);
            if (stack != null) { itemId = stack.ItemId; quantity = stack.Quantity; }
        }
        else if (_dragSourceHotbar >= 0)
        {
            itemId = _inventory.GetHotbarRef(_dragSourceHotbar);
        }
        else if (_dragSourceConsumable >= 0)
        {
            itemId = _inventory.GetConsumableRef(_dragSourceConsumable);
        }

        if (itemId == null) return;
        var def = ItemRegistry.Get(itemId);
        if (def == null) return;

        int drawSize = SlotSize - 4;
        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            _dragPosition.X - drawSize / 2, _dragPosition.Y - drawSize / 2,
            drawSize, drawSize);
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White * 0.85f);

        if (quantity > 1)
        {
            string qtyText = quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(destRect.Right - qtySize.X, destRect.Bottom - qtySize.Y), Color.White);
        }
    }

    // === Hit testing ===

    private int HitTestGrid(Point mousePos, int offsetX, int offsetY)
    {
        int relX = mousePos.X - offsetX;
        int relY = mousePos.Y - offsetY;
        if (relX < 0 || relY < 0) return -1;

        int col = relX / (SlotSize + Padding);
        int row = relY / (SlotSize + Padding);
        int maxRows = (_inventory.Capacity + Columns - 1) / Columns;
        if (col >= Columns || row >= maxRows) return -1;

        int slotLocalX = relX - col * (SlotSize + Padding);
        int slotLocalY = relY - row * (SlotSize + Padding);
        if (slotLocalX > SlotSize || slotLocalY > SlotSize) return -1;

        int hitSlot = row * Columns + col;
        return hitSlot < _inventory.Capacity ? hitSlot : -1;
    }

    private EquipSlot? HitTestEquip(Point mousePos, int offsetX, int offsetY)
    {
        for (int i = 0; i < EquipSlots.Length; i++)
            if (GetEquipRect(i, offsetX, offsetY).Contains(mousePos))
                return EquipSlots[i];
        return null;
    }

    private Rectangle GetEquipRect(int index, int offsetX, int offsetY)
    {
        // Tibia-style 3-column layout (columns 0/1/2 × rows 0..3), 32px slots + 4px gaps:
        //   [ · ][Hlm][ · ]
        //   [Nck][Arm][Shd]
        //   [Rng][Leg][ · ]
        //   [ · ][Bts][ · ]
        int s = EquipSlotSize;
        return index switch
        {
            0 => new Rectangle(offsetX + EquipStride,     offsetY,                     s, s), // Helmet (col 1 row 0)
            1 => new Rectangle(offsetX,                   offsetY + EquipStride,       s, s), // Necklace (col 0 row 1)
            2 => new Rectangle(offsetX + EquipStride,     offsetY + EquipStride,       s, s), // Armor (col 1 row 1)
            3 => new Rectangle(offsetX + EquipStride * 2, offsetY + EquipStride,       s, s), // Shield (col 2 row 1)
            4 => new Rectangle(offsetX,                   offsetY + EquipStride * 2,   s, s), // Ring (col 0 row 2)
            5 => new Rectangle(offsetX + EquipStride,     offsetY + EquipStride * 2,   s, s), // Legs (col 1 row 2)
            6 => new Rectangle(offsetX + EquipStride,     offsetY + EquipStride * 3,   s, s), // Boots (col 1 row 3)
            _ => Rectangle.Empty
        };
    }

    public int GridWidth => Columns * SlotSize + (Columns - 1) * Padding;
    public int GridHeight
    {
        get
        {
            int rows = (_inventory.Capacity + Columns - 1) / Columns;
            return rows * SlotSize + Math.Max(0, rows - 1) * Padding;
        }
    }
}
