using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders the 8-slot hotbar at the bottom center of the screen.
/// Shows item icons from SpriteAtlas, quantity labels, slot numbers,
/// and highlights the active slot with a selected texture.
/// </summary>
public class HotbarRenderer
{
    private const int SlotSize = 36;
    private const int Padding = 4;
    private const int BottomMargin = 8;
    private const int IconPadding = 2;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFont _font = null!;

    /// <summary>
    /// Create a new HotbarRenderer.
    /// </summary>
    /// <param name="inventory">The InventoryManager to read slot data from.</param>
    /// <param name="atlas">The SpriteAtlas for item icon lookups.</param>
    public HotbarRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    /// <summary>
    /// Load slot textures and font for rendering.
    /// </summary>
    /// <param name="device">Graphics device for texture loading.</param>
    /// <param name="font">SpriteFont for quantity and slot number labels.</param>
    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;

        try
        {
            using var normalStream = File.OpenRead("Content/Sprites/System/UI Elements/Slot/UI_Slot_Normal.png");
            _slotNormal = Texture2D.FromStream(device, normalStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Slot_Normal: {ex.Message}");
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
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Slot_Selected: {ex.Message}");
            _slotSelected = new Texture2D(device, 1, 1);
            _slotSelected.SetData(new[] { Color.Gold });
        }

        Console.WriteLine("[HotbarRenderer] Content loaded");
    }

    /// <summary>
    /// Draw the hotbar with 8 numbered slots, item icons, and quantity labels.
    /// </summary>
    /// <param name="sb">SpriteBatch (must already be in a Begin/End block).</param>
    /// <param name="screenWidth">Current screen width in pixels.</param>
    /// <param name="screenHeight">Current screen height in pixels.</param>
    public void Draw(SpriteBatch sb, int screenWidth, int screenHeight)
    {
        int totalWidth = InventoryManager.HotbarSize * SlotSize +
                         (InventoryManager.HotbarSize - 1) * Padding;
        int startX = (screenWidth - totalWidth) / 2;
        int startY = screenHeight - SlotSize - BottomMargin;

        for (int i = 0; i < InventoryManager.HotbarSize; i++)
        {
            int x = startX + i * (SlotSize + Padding);
            int y = startY;

            // Slot background (selected or normal)
            var slotTex = i == _inventory.ActiveHotbarIndex ? _slotSelected : _slotNormal;
            sb.Draw(slotTex, new Rectangle(x, y, SlotSize, SlotSize), Color.White);

            // Item icon
            var stack = _inventory.GetSlot(i);
            if (stack != null)
            {
                var def = ItemRegistry.Get(stack.ItemId);
                if (def != null)
                {
                    var srcRect = _atlas.GetRect(def.SpriteId);
                    var destRect = new Rectangle(
                        x + IconPadding,
                        y + IconPadding,
                        SlotSize - IconPadding * 2,
                        SlotSize - IconPadding * 2);
                    sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);
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

            // Slot number label (top-left, 1-8)
            string slotNum = (i + 1).ToString();
            sb.DrawString(_font, slotNum, new Vector2(x + 2, y), Color.Gray * 0.7f);
        }
    }
}
