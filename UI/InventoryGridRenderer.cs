using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a 20-slot inventory grid (5 columns x 4 rows) with item icons,
/// rarity color tinting, quantity labels, and click-to-move selection.
/// </summary>
public class InventoryGridRenderer
{
    private const int Columns = 5;
    private const int Rows = 4;
    private const int SlotSize = 40;
    private const int Padding = 2;
    private const int IconPadding = 4;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private int _selectedSlot = -1;

    /// <summary>Currently selected slot index, or -1 if none.</summary>
    public int SelectedSlot => _selectedSlot;

    /// <summary>
    /// Create a new InventoryGridRenderer.
    /// </summary>
    /// <param name="inventory">The InventoryManager to read slot data from.</param>
    /// <param name="atlas">The SpriteAtlas for item icon lookups.</param>
    public InventoryGridRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    /// <summary>Clear the current slot selection.</summary>
    public void ClearSelection() => _selectedSlot = -1;

    /// <summary>
    /// Load slot textures, font, and pixel texture for rendering.
    /// </summary>
    /// <param name="device">Graphics device for texture loading.</param>
    /// <param name="font">SpriteFont for quantity labels.</param>
    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;

        // 1x1 pixel texture for rarity tint overlays
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
    /// Handle a mouse click on the grid. Selects a slot or moves an item.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen coordinates.</param>
    /// <param name="offsetX">Grid X offset on screen.</param>
    /// <param name="offsetY">Grid Y offset on screen.</param>
    /// <returns>True if the click hit a valid slot, false otherwise.</returns>
    public bool HandleClick(Point mousePos, int offsetX, int offsetY)
    {
        int relX = mousePos.X - offsetX;
        int relY = mousePos.Y - offsetY;

        if (relX < 0 || relY < 0)
            return false;

        int col = relX / (SlotSize + Padding);
        int row = relY / (SlotSize + Padding);

        if (col >= Columns || row >= Rows)
            return false;

        // Check that click is actually within the slot rect (not in padding)
        int slotLocalX = relX - col * (SlotSize + Padding);
        int slotLocalY = relY - row * (SlotSize + Padding);
        if (slotLocalX > SlotSize || slotLocalY > SlotSize)
            return false;

        int hitSlot = row * Columns + col;
        if (hitSlot < 0 || hitSlot >= InventoryManager.SlotCount)
            return false;

        if (_selectedSlot == -1)
        {
            // First click: select the slot
            _selectedSlot = hitSlot;
        }
        else
        {
            // Second click: move item from selected to hit slot
            _inventory.MoveItem(_selectedSlot, hitSlot);
            _selectedSlot = -1;
        }

        return true;
    }

    /// <summary>
    /// Draw the 20-slot inventory grid with item icons, rarity tints, and quantity labels.
    /// </summary>
    /// <param name="sb">SpriteBatch (must already be in a Begin/End block).</param>
    /// <param name="offsetX">Grid X offset on screen.</param>
    /// <param name="offsetY">Grid Y offset on screen.</param>
    public void Draw(SpriteBatch sb, int offsetX, int offsetY)
    {
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            int x = offsetX + col * (SlotSize + Padding);
            int y = offsetY + row * (SlotSize + Padding);

            // Slot background (selected or normal)
            var slotTex = i == _selectedSlot ? _slotSelected : _slotNormal;
            var slotRect = new Rectangle(x, y, SlotSize, SlotSize);
            sb.Draw(slotTex, slotRect, Color.White);

            // Item icon and rarity
            var stack = _inventory.GetSlot(i);
            if (stack != null)
            {
                var def = ItemRegistry.Get(stack.ItemId);
                if (def != null)
                {
                    // Draw item icon
                    var srcRect = _atlas.GetRect(def.SpriteId);
                    var destRect = new Rectangle(
                        x + IconPadding,
                        y + IconPadding,
                        SlotSize - IconPadding * 2,
                        SlotSize - IconPadding * 2);
                    sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);

                    // Draw rarity tint overlay
                    Color? rarityColor = def.Rarity switch
                    {
                        Rarity.Uncommon => new Color(50, 205, 50) * 0.3f,
                        Rarity.Rare => new Color(255, 215, 0) * 0.3f,
                        _ => null
                    };

                    if (rarityColor.HasValue)
                    {
                        sb.Draw(_pixel, slotRect, rarityColor.Value);
                    }
                }

                // Quantity label (bottom-right, only if > 1)
                if (stack.Quantity > 1)
                {
                    string qtyText = stack.Quantity.ToString();
                    var qtySize = _font.MeasureString(qtyText);
                    var qtyPos = new Vector2(
                        x + SlotSize - qtySize.X - 2,
                        y + SlotSize - qtySize.Y);
                    sb.DrawString(_font, qtyText, qtyPos, Color.White);
                }
            }
        }
    }

    /// <summary>
    /// Get the total width of the grid in pixels.
    /// </summary>
    public int TotalWidth => Columns * SlotSize + (Columns - 1) * Padding;

    /// <summary>
    /// Get the total height of the grid in pixels.
    /// </summary>
    public int TotalHeight => Rows * SlotSize + (Rows - 1) * Padding;
}
