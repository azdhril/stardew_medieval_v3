using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Reusable slot-grid renderer for generic item containers.
/// </summary>
public class ContainerGridRenderer
{
    private const int SlotSize = 40;
    private const int Padding = 0;
    private const int IconPadding = 1;

    private readonly SpriteAtlas _atlas;
    private Texture2D _slotNormal = null!;
    private SpriteFont _font = null!;

    public int SlotPixelSize => SlotSize;

    public ContainerGridRenderer(SpriteAtlas atlas)
    {
        _atlas = atlas;
    }

    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;

        try
        {
            using var normalStream = File.OpenRead("assets/Sprites/System/UI Elements/Slot/UI_Slot_Normal.png");
            _slotNormal = Texture2D.FromStream(device, normalStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContainerGridRenderer] Failed to load UI_Slot_Normal: {ex.Message}");
            _slotNormal = new Texture2D(device, 1, 1);
            _slotNormal.SetData(new[] { new Color(60, 40, 30) });
        }
    }

    public void DrawGrid(SpriteBatch sb, IItemSlotCollection container, int columns, int offsetX, int offsetY, int? hiddenIndex = null)
    {
        for (int i = 0; i < container.Capacity; i++)
        {
            var rect = GetSlotRect(i, columns, offsetX, offsetY);
            sb.Draw(_slotNormal, rect, Color.White);

            if (hiddenIndex.HasValue && hiddenIndex.Value == i)
                continue;

            DrawSlotContents(sb, container.GetSlot(i), rect);
        }
    }

    public int HitTest(Point mousePos, int capacity, int columns, int offsetX, int offsetY)
    {
        if (mousePos.X < offsetX || mousePos.Y < offsetY)
            return -1;

        int col = (mousePos.X - offsetX) / (SlotSize + Padding);
        int row = (mousePos.Y - offsetY) / (SlotSize + Padding);
        if (col < 0 || col >= columns || row < 0)
            return -1;

        int localX = mousePos.X - offsetX - col * (SlotSize + Padding);
        int localY = mousePos.Y - offsetY - row * (SlotSize + Padding);
        if (localX > SlotSize || localY > SlotSize)
            return -1;

        int index = row * columns + col;
        return index < capacity ? index : -1;
    }

    public Rectangle GetSlotRect(int index, int columns, int offsetX, int offsetY)
    {
        int col = index % columns;
        int row = index / columns;
        return new Rectangle(
            offsetX + col * (SlotSize + Padding),
            offsetY + row * (SlotSize + Padding),
            SlotSize,
            SlotSize);
    }

    public void DrawDraggedItem(SpriteBatch sb, ItemStack stack, Point dragPosition)
    {
        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null) return;

        int drawSize = SlotSize - 4;
        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            dragPosition.X - drawSize / 2,
            dragPosition.Y - drawSize / 2,
            drawSize,
            drawSize);
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White * 0.85f);

        if (stack.Quantity > 1)
        {
            string qtyText = stack.Quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(destRect.Right - qtySize.X, destRect.Bottom - qtySize.Y),
                Color.White);
        }
    }

    private void DrawSlotContents(SpriteBatch sb, ItemStack? stack, Rectangle slotRect)
    {
        if (stack == null) return;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null) return;

        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            slotRect.X + IconPadding,
            slotRect.Y + IconPadding,
            slotRect.Width - IconPadding * 2,
            slotRect.Height - IconPadding * 2);
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White);

        if (stack.Quantity > 1)
        {
            string qtyText = stack.Quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(slotRect.Right - qtySize.X - 2, slotRect.Bottom - qtySize.Y),
                Color.White);
        }
    }
}
