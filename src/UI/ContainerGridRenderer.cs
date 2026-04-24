using System;
using System.IO;
using FontStashSharp;
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
    private const int DefaultSlotSize = 40;
    private const int Padding = 0;

    private readonly SpriteAtlas _atlas;
    private Texture2D _slotNormal = null!;
    private SpriteFontBase _font = null!;

    public int SlotPixelSize { get; private set; }

    public ContainerGridRenderer(SpriteAtlas atlas, int slotPixelSize = DefaultSlotSize)
    {
        _atlas = atlas;
        SlotPixelSize = slotPixelSize;
    }

    public void SetSlotPixelSize(int slotPixelSize)
    {
        SlotPixelSize = Math.Max(16, slotPixelSize);
    }

    public void LoadContent(GraphicsDevice device, SpriteFontBase font)
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
        DrawGrid(sb, container, columns, offsetX, offsetY, hiddenIndex, container.Capacity, container.Capacity);
    }

    public void DrawGrid(SpriteBatch sb, IItemSlotCollection container, int columns, int offsetX, int offsetY,
        int? hiddenIndex, int visibleSlots, int enabledSlots, int slotOffset = 0)
    {
        for (int i = 0; i < visibleSlots; i++)
        {
            int absoluteSlot = slotOffset + i;
            var rect = GetSlotRect(i, columns, offsetX, offsetY);
            bool enabled = absoluteSlot < enabledSlots;
            sb.Draw(_slotNormal, rect, enabled ? Color.White : Color.DimGray * 0.55f);

            if (!enabled)
            {
                sb.Draw(_slotNormal, new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6), Color.Black * 0.28f);
                continue;
            }

            if (hiddenIndex.HasValue && hiddenIndex.Value == absoluteSlot)
                continue;

            DrawSlotContents(sb, absoluteSlot < container.Capacity ? container.GetSlot(absoluteSlot) : null, rect);
        }
    }

    /// <summary>
    /// Return the absolute slot index at <paramref name="mousePos"/> (=<paramref name="slotOffset"/>
    /// + local index) or -1 when the cursor isn't over an enabled slot. Pagination callers pass the
    /// active page's base offset in <paramref name="slotOffset"/>; the returned index is directly
    /// usable against the underlying container.
    /// </summary>
    public int HitTest(Point mousePos, int capacity, int columns, int offsetX, int offsetY, int slotOffset = 0)
    {
        if (mousePos.X < offsetX || mousePos.Y < offsetY)
            return -1;

        int col = (mousePos.X - offsetX) / (SlotPixelSize + Padding);
        int row = (mousePos.Y - offsetY) / (SlotPixelSize + Padding);
        if (col < 0 || col >= columns || row < 0)
            return -1;

        int localX = mousePos.X - offsetX - col * (SlotPixelSize + Padding);
        int localY = mousePos.Y - offsetY - row * (SlotPixelSize + Padding);
        if (localX > SlotPixelSize || localY > SlotPixelSize)
            return -1;

        int absolute = slotOffset + row * columns + col;
        return absolute < capacity ? absolute : -1;
    }

    /// <summary>
    /// Return the on-screen rect for slot <paramref name="index"/>. Pagination callers pass
    /// the active page's base offset so they can supply the absolute slot index directly;
    /// internally the helper subtracts the offset to land back in the visible grid.
    /// </summary>
    public Rectangle GetSlotRect(int index, int columns, int offsetX, int offsetY, int slotOffset = 0)
    {
        int local = index - slotOffset;
        int col = local % columns;
        int row = local / columns;
        return new Rectangle(
            offsetX + col * (SlotPixelSize + Padding),
            offsetY + row * (SlotPixelSize + Padding),
            SlotPixelSize,
            SlotPixelSize);
    }

    public void DrawDraggedItem(SpriteBatch sb, ItemStack stack, Point dragPosition)
    {
        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null) return;

        int drawSize = SlotPixelSize - 4;
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
        int iconPadding = Math.Max(3, SlotPixelSize / 10);
        var destRect = new Rectangle(
            slotRect.X + iconPadding,
            slotRect.Y + iconPadding,
            slotRect.Width - iconPadding * 2,
            slotRect.Height - iconPadding * 2);
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
