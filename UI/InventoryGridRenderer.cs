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
/// Also renders 7 defensive equipment slots around a character silhouette.
/// Drops onto the screen-bottom hotbar/consumable slots are handled via
/// HotbarRenderer hit-testing.
/// </summary>
public class InventoryGridRenderer
{
    private const int Columns = 5;
    private const int Rows = 4;
    private const int SlotSize = 40;
    private const int Padding = 0;
    private const int IconPadding = 1;
    private const int EquipSlotSize = 36;

    private static readonly EquipSlot[] EquipSlots = {
        EquipSlot.Helmet, EquipSlot.Necklace,
        EquipSlot.Armor, EquipSlot.Shield,
        EquipSlot.Ring, EquipSlot.Legs,
        EquipSlot.Boots
    };

    private static readonly string[] EquipLabels = {
        "Hlm", "Nck", "Arm", "Shd", "Rng", "Leg", "Bts"
    };

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;
    private HotbarRenderer? _hotbar;
    private int _screenWidth, _screenHeight;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    // Drag state
    private bool _isDragging;
    private int _dragSourceSlot = -1;
    private EquipSlot? _dragSourceEquip;
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

    /// <summary>Handle mouse press — start drag from grid or equipment.</summary>
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
    }

    public void UpdateDrag(Point mousePos)
    {
        if (_isDragging) _dragPosition = mousePos;
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
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            int x = offsetX + col * (SlotSize + Padding);
            int y = offsetY + row * (SlotSize + Padding);

            var slotRect = new Rectangle(x, y, SlotSize, SlotSize);
            sb.Draw(_slotNormal, slotRect, Color.White);

            if (_isDragging && _dragSourceSlot == i && _dragSourceEquip == null)
                continue;

            DrawSlotContents(sb, _inventory.GetSlot(i), slotRect);
        }
    }

    private void DrawEquipment(SpriteBatch sb, int offsetX, int offsetY)
    {
        // Silhouette
        sb.Draw(_pixel, new Rectangle(offsetX + 20, offsetY + 25, 40, 70), Color.Gray * 0.3f);
        sb.Draw(_pixel, new Rectangle(offsetX + 28, offsetY + 8, 24, 22), Color.Gray * 0.3f);

        for (int i = 0; i < EquipSlots.Length; i++)
        {
            var rect = GetEquipRect(i, offsetX, offsetY);
            sb.DrawString(_font, EquipLabels[i], new Vector2(rect.X, rect.Y - 12), Color.LightGray);
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
        }

        var (attack, defense) = EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment());
        int statsY = offsetY + 100;
        sb.DrawString(_font, $"ATK:{attack:F0}", new Vector2(offsetX, statsY), Color.OrangeRed);
        sb.DrawString(_font, $"DEF:{defense:F0}", new Vector2(offsetX, statsY + 16), Color.CornflowerBlue);
    }

    private void DrawSlotContents(SpriteBatch sb, ItemStack? stack, Rectangle slotRect)
    {
        if (stack == null) return;
        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null) return;

        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            slotRect.X + IconPadding, slotRect.Y + IconPadding,
            slotRect.Width - IconPadding * 2, slotRect.Height - IconPadding * 2);
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White);

        Color? rarityColor = def.Rarity switch
        {
            Rarity.Uncommon => new Color(50, 205, 50) * 0.3f,
            Rarity.Rare => new Color(255, 215, 0) * 0.3f,
            _ => null
        };
        if (rarityColor.HasValue)
            sb.Draw(_pixel, slotRect, rarityColor.Value);

        if (stack.Quantity > 1)
        {
            string qtyText = stack.Quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(slotRect.Right - qtySize.X - 2, slotRect.Bottom - qtySize.Y),
                Color.White);
        }
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
        if (col >= Columns || row >= Rows) return -1;

        int slotLocalX = relX - col * (SlotSize + Padding);
        int slotLocalY = relY - row * (SlotSize + Padding);
        if (slotLocalX > SlotSize || slotLocalY > SlotSize) return -1;

        int hitSlot = row * Columns + col;
        return hitSlot < InventoryManager.SlotCount ? hitSlot : -1;
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
        int s = EquipSlotSize;
        return index switch
        {
            0 => new Rectangle(offsetX + 22, offsetY - 5, s, s),   // Helmet
            1 => new Rectangle(offsetX - 15, offsetY + 15, s, s),  // Necklace
            2 => new Rectangle(offsetX + 22, offsetY + 35, s, s),  // Armor
            3 => new Rectangle(offsetX + 62, offsetY + 20, s, s),  // Shield
            4 => new Rectangle(offsetX - 15, offsetY + 55, s, s),  // Ring
            5 => new Rectangle(offsetX + 22, offsetY + 70, s, s),  // Legs
            6 => new Rectangle(offsetX + 22, offsetY + 105, s, s), // Boots
            _ => Rectangle.Empty
        };
    }

    public int GridWidth => Columns * SlotSize + (Columns - 1) * Padding;
    public int GridHeight => Rows * SlotSize + (Rows - 1) * Padding;
}
