using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a 20-slot inventory grid (5 columns x 4 rows) with item icons,
/// rarity color tinting, quantity labels, and drag-and-drop support.
/// Also renders equipment slots alongside the grid for unified interaction.
/// </summary>
public class InventoryGridRenderer
{
    private const int Columns = 5;
    private const int Rows = 4;
    private const int SlotSize = 40;
    private const int Padding = 2;
    private const int IconPadding = 4;

    // Equipment slot sizes
    private const int EquipSlotSize = 44;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    // Drag state
    private bool _isDragging;
    private int _dragSourceSlot = -1;
    private string? _dragSourceEquip; // "weapon" or "armor" if dragging from equipment
    private Point _dragPosition;

    /// <summary>Currently dragging an item.</summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Create a new InventoryGridRenderer.
    /// </summary>
    public InventoryGridRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    /// <summary>Cancel any active drag operation.</summary>
    public void CancelDrag()
    {
        _isDragging = false;
        _dragSourceSlot = -1;
        _dragSourceEquip = null;
    }

    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var normalStream = File.OpenRead("Content/Sprites/System/UI Elements/Slot/UI_Slot_Normal.png");
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
            using var selectedStream = File.OpenRead("Content/Sprites/System/UI Elements/Slot/UI_Slot_Selected.png");
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

    /// <summary>
    /// Handle mouse press (start drag from grid or equipment slot).
    /// </summary>
    public void HandleMouseDown(Point mousePos, int gridOffsetX, int gridOffsetY, int equipOffsetX, int equipOffsetY)
    {
        // Check grid slots
        int hitSlot = HitTestGrid(mousePos, gridOffsetX, gridOffsetY);
        if (hitSlot >= 0 && _inventory.GetSlot(hitSlot) != null)
        {
            _isDragging = true;
            _dragSourceSlot = hitSlot;
            _dragSourceEquip = null;
            _dragPosition = mousePos;
            return;
        }

        // Check equipment slots
        var weaponRect = GetWeaponRect(equipOffsetX, equipOffsetY);
        var armorRect = GetArmorRect(equipOffsetX, equipOffsetY);

        if (weaponRect.Contains(mousePos) && _inventory.WeaponId != null)
        {
            _isDragging = true;
            _dragSourceSlot = -1;
            _dragSourceEquip = "weapon";
            _dragPosition = mousePos;
            return;
        }

        if (armorRect.Contains(mousePos) && _inventory.ArmorId != null)
        {
            _isDragging = true;
            _dragSourceSlot = -1;
            _dragSourceEquip = "armor";
            _dragPosition = mousePos;
            return;
        }
    }

    /// <summary>
    /// Update drag position while mouse is held.
    /// </summary>
    public void UpdateDrag(Point mousePos)
    {
        if (_isDragging)
            _dragPosition = mousePos;
    }

    /// <summary>
    /// Handle mouse release (drop item onto grid or equipment slot).
    /// </summary>
    public void HandleMouseUp(Point mousePos, int gridOffsetX, int gridOffsetY, int equipOffsetX, int equipOffsetY)
    {
        if (!_isDragging)
            return;

        int targetSlot = HitTestGrid(mousePos, gridOffsetX, gridOffsetY);
        var weaponRect = GetWeaponRect(equipOffsetX, equipOffsetY);
        var armorRect = GetArmorRect(equipOffsetX, equipOffsetY);

        if (_dragSourceEquip != null)
        {
            // Dragging FROM equipment TO grid
            if (targetSlot >= 0)
            {
                _inventory.TryUnequip(_dragSourceEquip, targetSlot);
            }
        }
        else if (_dragSourceSlot >= 0)
        {
            // Dragging FROM grid
            if (targetSlot >= 0 && targetSlot != _dragSourceSlot)
            {
                // Drop onto another grid slot — move/swap
                _inventory.MoveItem(_dragSourceSlot, targetSlot);
            }
            else if (weaponRect.Contains(mousePos))
            {
                // Drop onto weapon slot — try equip
                var stack = _inventory.GetSlot(_dragSourceSlot);
                if (stack != null)
                {
                    var def = ItemRegistry.Get(stack.ItemId);
                    if (def != null && def.Type == ItemType.Weapon)
                        _inventory.TryEquip(_dragSourceSlot);
                }
            }
            else if (armorRect.Contains(mousePos))
            {
                // Drop onto armor slot — try equip
                var stack = _inventory.GetSlot(_dragSourceSlot);
                if (stack != null)
                {
                    var def = ItemRegistry.Get(stack.ItemId);
                    if (def != null && def.Type == ItemType.Armor)
                        _inventory.TryEquip(_dragSourceSlot);
                }
            }
        }

        CancelDrag();
    }

    /// <summary>
    /// Draw the full inventory panel: grid + equipment + dragged item.
    /// </summary>
    public void Draw(SpriteBatch sb, int gridOffsetX, int gridOffsetY, int equipOffsetX, int equipOffsetY)
    {
        DrawGrid(sb, gridOffsetX, gridOffsetY);
        DrawEquipment(sb, equipOffsetX, equipOffsetY);
        DrawDraggedItem(sb);
    }

    /// <summary>
    /// Draw the 20-slot inventory grid.
    /// </summary>
    private void DrawGrid(SpriteBatch sb, int offsetX, int offsetY)
    {
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            int x = offsetX + col * (SlotSize + Padding);
            int y = offsetY + row * (SlotSize + Padding);

            // Slot background
            var slotRect = new Rectangle(x, y, SlotSize, SlotSize);
            sb.Draw(_slotNormal, slotRect, Color.White);

            // Skip drawing the item if it's being dragged from this slot
            if (_isDragging && _dragSourceSlot == i && _dragSourceEquip == null)
                continue;

            var stack = _inventory.GetSlot(i);
            if (stack != null)
            {
                var def = ItemRegistry.Get(stack.ItemId);
                if (def != null)
                {
                    // Item icon
                    var srcRect = _atlas.GetRect(def.SpriteId);
                    var destRect = new Rectangle(
                        x + IconPadding, y + IconPadding,
                        SlotSize - IconPadding * 2, SlotSize - IconPadding * 2);
                    sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);

                    // Rarity tint
                    Color? rarityColor = def.Rarity switch
                    {
                        Rarity.Uncommon => new Color(50, 205, 50) * 0.3f,
                        Rarity.Rare => new Color(255, 215, 0) * 0.3f,
                        _ => null
                    };
                    if (rarityColor.HasValue)
                        sb.Draw(_pixel, slotRect, rarityColor.Value);
                }

                // Quantity label
                if (stack.Quantity > 1)
                {
                    string qtyText = stack.Quantity.ToString();
                    var qtySize = _font.MeasureString(qtyText);
                    sb.DrawString(_font, qtyText,
                        new Vector2(x + SlotSize - qtySize.X - 2, y + SlotSize - qtySize.Y),
                        Color.White);
                }
            }
        }
    }

    /// <summary>
    /// Draw the equipment slots (weapon + armor) with silhouette and stats.
    /// </summary>
    private void DrawEquipment(SpriteBatch sb, int offsetX, int offsetY)
    {
        var weaponRect = GetWeaponRect(offsetX, offsetY);
        var armorRect = GetArmorRect(offsetX, offsetY);

        // Character silhouette
        var silhouetteRect = new Rectangle(offsetX + 15, offsetY + 10, 50, 80);
        sb.Draw(_pixel, silhouetteRect, Color.Gray * 0.3f);
        // Head
        var headRect = new Rectangle(offsetX + 25, offsetY - 5, 30, 25);
        sb.Draw(_pixel, headRect, Color.Gray * 0.3f);

        // Weapon slot (left of body)
        sb.DrawString(_font, "Wpn", new Vector2(weaponRect.X, weaponRect.Y - 14), Color.LightGray);
        sb.Draw(_slotNormal, weaponRect, Color.White);

        // Armor slot (right of body)
        sb.DrawString(_font, "Arm", new Vector2(armorRect.X, armorRect.Y - 14), Color.LightGray);
        sb.Draw(_slotNormal, armorRect, Color.White);

        // Draw equipped weapon icon (skip if being dragged)
        if (_inventory.WeaponId != null && !(_isDragging && _dragSourceEquip == "weapon"))
        {
            var weaponDef = ItemRegistry.Get(_inventory.WeaponId);
            if (weaponDef != null)
            {
                var srcRect = _atlas.GetRect(weaponDef.SpriteId);
                sb.Draw(_atlas.Texture,
                    new Rectangle(weaponRect.X + 4, weaponRect.Y + 4, EquipSlotSize - 8, EquipSlotSize - 8),
                    srcRect, Color.White);
            }
        }

        // Draw equipped armor icon (skip if being dragged)
        if (_inventory.ArmorId != null && !(_isDragging && _dragSourceEquip == "armor"))
        {
            var armorDef = ItemRegistry.Get(_inventory.ArmorId);
            if (armorDef != null)
            {
                var srcRect = _atlas.GetRect(armorDef.SpriteId);
                sb.Draw(_atlas.Texture,
                    new Rectangle(armorRect.X + 4, armorRect.Y + 4, EquipSlotSize - 8, EquipSlotSize - 8),
                    srcRect, Color.White);
            }
        }

        // Stats below silhouette
        var (attack, defense) = EquipmentData.GetEquipmentStats(_inventory.WeaponId, _inventory.ArmorId);
        int statsY = silhouetteRect.Bottom + 8;
        sb.DrawString(_font, $"ATK:{attack:F0}", new Vector2(offsetX, statsY), Color.OrangeRed);
        sb.DrawString(_font, $"DEF:{defense:F0}", new Vector2(offsetX, statsY + 16), Color.CornflowerBlue);
    }

    /// <summary>
    /// Draw the item being dragged at the mouse cursor.
    /// </summary>
    private void DrawDraggedItem(SpriteBatch sb)
    {
        if (!_isDragging)
            return;

        string? itemId = null;
        int quantity = 1;

        if (_dragSourceEquip != null)
        {
            itemId = _dragSourceEquip == "weapon" ? _inventory.WeaponId : _inventory.ArmorId;
        }
        else if (_dragSourceSlot >= 0)
        {
            var stack = _inventory.GetSlot(_dragSourceSlot);
            if (stack != null)
            {
                itemId = stack.ItemId;
                quantity = stack.Quantity;
            }
        }

        if (itemId == null) return;

        var def = ItemRegistry.Get(itemId);
        if (def == null) return;

        int drawSize = SlotSize - 4;
        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            _dragPosition.X - drawSize / 2,
            _dragPosition.Y - drawSize / 2,
            drawSize, drawSize);

        sb.Draw(_atlas.Texture, destRect, srcRect, Color.White * 0.85f);

        if (quantity > 1)
        {
            string qtyText = quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(destRect.Right - qtySize.X, destRect.Bottom - qtySize.Y),
                Color.White);
        }
    }

    /// <summary>
    /// Hit-test grid slots. Returns slot index 0-19 or -1 if miss.
    /// </summary>
    private int HitTestGrid(Point mousePos, int offsetX, int offsetY)
    {
        int relX = mousePos.X - offsetX;
        int relY = mousePos.Y - offsetY;

        if (relX < 0 || relY < 0) return -1;

        int col = relX / (SlotSize + Padding);
        int row = relY / (SlotSize + Padding);

        if (col >= Columns || row >= Rows) return -1;

        // Check within slot bounds (not in padding)
        int slotLocalX = relX - col * (SlotSize + Padding);
        int slotLocalY = relY - row * (SlotSize + Padding);
        if (slotLocalX > SlotSize || slotLocalY > SlotSize) return -1;

        int hitSlot = row * Columns + col;
        return hitSlot < InventoryManager.SlotCount ? hitSlot : -1;
    }

    // Equipment slot positions (relative to equipment offset)
    private Rectangle GetWeaponRect(int offsetX, int offsetY)
        => new Rectangle(offsetX - 10, offsetY + 30, EquipSlotSize, EquipSlotSize);

    private Rectangle GetArmorRect(int offsetX, int offsetY)
        => new Rectangle(offsetX + 45, offsetY + 30, EquipSlotSize, EquipSlotSize);

    /// <summary>Total width of the grid in pixels.</summary>
    public int GridWidth => Columns * SlotSize + (Columns - 1) * Padding;

    /// <summary>Total height of the grid in pixels.</summary>
    public int GridHeight => Rows * SlotSize + (Rows - 1) * Padding;
}
