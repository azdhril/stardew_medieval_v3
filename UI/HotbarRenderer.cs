using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders the reference-based hotbar (8 slots) and consumable slot (1, Q)
/// at the bottom of the screen. Supports drag-and-drop reordering without
/// opening inventory.
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
    private Texture2D _pixel = null!;

    // Drag state for hotbar rearranging
    private bool _isDragging;
    private int _dragSourceMain = -1;      // main hotbar slot being dragged
    private int _dragSourceConsumable = -1; // consumable slot being dragged
    private Point _dragPosition;
    private bool _wasMouseDown;

    public bool IsDragging => _isDragging;

    public HotbarRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
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
    /// Update drag-and-drop for hotbar rearranging (called when inventory is CLOSED).
    /// </summary>
    public void Update(Point mousePos, int screenWidth, int screenHeight)
    {
        bool mouseDown = Mouse.GetState().LeftButton == ButtonState.Pressed;

        if (mouseDown && !_wasMouseDown)
        {
            // Mouse just pressed — check if on a hotbar or consumable slot
            int mainHit = HitTestMain(mousePos, screenWidth, screenHeight);
            int consHit = HitTestConsumable(mousePos, screenWidth, screenHeight);

            if (mainHit >= 0 && _inventory.GetHotbarRef(mainHit) != null)
            {
                _isDragging = true;
                _dragSourceMain = mainHit;
                _dragPosition = mousePos;
            }
            else if (consHit >= 0 && _inventory.GetConsumableRef(consHit) != null)
            {
                _isDragging = true;
                _dragSourceConsumable = consHit;
                _dragPosition = mousePos;
            }
        }
        else if (mouseDown && _isDragging)
        {
            _dragPosition = mousePos;
        }
        else if (!mouseDown && _isDragging)
        {
            // Drop
            int mainTarget = HitTestMain(mousePos, screenWidth, screenHeight);
            int consTarget = HitTestConsumable(mousePos, screenWidth, screenHeight);

            if (_dragSourceMain >= 0 && mainTarget >= 0 && mainTarget != _dragSourceMain)
                _inventory.SwapHotbarRefs(_dragSourceMain, mainTarget);
            else if (_dragSourceConsumable >= 0 && consTarget >= 0 && consTarget != _dragSourceConsumable)
                _inventory.SwapConsumableRefs(_dragSourceConsumable, consTarget);

            CancelDrag();
        }

        _wasMouseDown = mouseDown;
    }

    /// <summary>Cancel any active drag.</summary>
    public void CancelDrag()
    {
        _isDragging = false;
        _dragSourceMain = -1;
        _dragSourceConsumable = -1;
    }

    public void Draw(SpriteBatch sb, int screenWidth, int screenHeight)
    {
        // Draw consumable slots
        string[] consumableKeys = { "Q" };
        for (int i = 0; i < InventoryManager.ConsumableSlotCount; i++)
        {
            var rect = GetConsumableSlotRect(i, screenWidth, screenHeight);
            sb.Draw(_slotNormal, rect, Color.White);

            if (!(_isDragging && _dragSourceConsumable == i))
            {
                var stack = _inventory.GetConsumableStack(i);
                if (stack != null)
                    DrawItemInSlot(sb, stack, rect);
            }

            sb.DrawString(_font, consumableKeys[i], new Vector2(rect.X + 2, rect.Y), Color.Gray * 0.7f);
        }

        // Draw main hotbar
        for (int i = 0; i < InventoryManager.HotbarSize; i++)
        {
            var rect = GetMainSlotRect(i, screenWidth, screenHeight);
            var slotTex = i == _inventory.ActiveHotbarIndex ? _slotSelected : _slotNormal;
            sb.Draw(slotTex, rect, Color.White);

            if (!(_isDragging && _dragSourceMain == i))
            {
                var stack = _inventory.GetHotbarStack(i);
                if (stack != null)
                    DrawItemInSlot(sb, stack, rect);
            }

            sb.DrawString(_font, (i + 1).ToString(), new Vector2(rect.X + 2, rect.Y), Color.Gray * 0.7f);
        }

        // Draw dragged item at cursor
        if (_isDragging)
            DrawDraggedItem(sb);
    }

    private void DrawItemInSlot(SpriteBatch sb, ItemStack stack, Rectangle rect)
    {
        var def = ItemRegistry.Get(stack.ItemId);
        if (def != null)
        {
            var srcRect = _atlas.GetRect(def.SpriteId);
            var destRect = new Rectangle(
                rect.X + IconPadding, rect.Y + IconPadding,
                rect.Width - IconPadding * 2, rect.Height - IconPadding * 2);
            sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);
        }

        if (stack.Quantity > 1)
        {
            string qtyText = stack.Quantity.ToString();
            var qtySize = _font.MeasureString(qtyText);
            sb.DrawString(_font, qtyText,
                new Vector2(rect.Right - qtySize.X - 2, rect.Bottom - qtySize.Y),
                Color.White);
        }
    }

    private void DrawDraggedItem(SpriteBatch sb)
    {
        string? itemId = null;
        if (_dragSourceMain >= 0) itemId = _inventory.GetHotbarRef(_dragSourceMain);
        else if (_dragSourceConsumable >= 0) itemId = _inventory.GetConsumableRef(_dragSourceConsumable);
        if (itemId == null) return;

        var def = ItemRegistry.Get(itemId);
        if (def == null) return;

        int drawSize = SlotSize - 4;
        var srcRect = _atlas.GetRect(def.SpriteId);
        var destRect = new Rectangle(
            _dragPosition.X - drawSize / 2, _dragPosition.Y - drawSize / 2,
            drawSize, drawSize);
        sb.Draw(_atlas.Texture, destRect, srcRect, Color.White * 0.85f);
    }

    // === Layout calculation (public for InventoryScene hit-testing) ===

    /// <summary>Get the screen rectangle for a main hotbar slot.</summary>
    public Rectangle GetMainSlotRect(int index, int screenWidth, int screenHeight)
    {
        int hotbarWidth = InventoryManager.HotbarSize * SlotSize + (InventoryManager.HotbarSize - 1) * Padding;
        int startX = (screenWidth - hotbarWidth) / 2;
        int startY = screenHeight - SlotSize - BottomMargin;
        return new Rectangle(startX + index * (SlotSize + Padding), startY, SlotSize, SlotSize);
    }

    /// <summary>Get the screen rectangle for a consumable slot (left of hotbar with gap).</summary>
    public Rectangle GetConsumableSlotRect(int index, int screenWidth, int screenHeight)
    {
        int hotbarWidth = InventoryManager.HotbarSize * SlotSize + (InventoryManager.HotbarSize - 1) * Padding;
        int hotbarStartX = (screenWidth - hotbarWidth) / 2;
        // Place consumable slots to the left with ~10% screen width gap
        int gap = (int)(screenWidth * 0.04f);
        int consumableStartX = hotbarStartX - gap - InventoryManager.ConsumableSlotCount * (SlotSize + Padding);
        int startY = screenHeight - SlotSize - BottomMargin;
        return new Rectangle(consumableStartX + index * (SlotSize + Padding), startY, SlotSize, SlotSize);
    }

    /// <summary>Hit-test against main hotbar slots. Returns slot index or -1.</summary>
    public int HitTestMain(Point mousePos, int screenWidth, int screenHeight)
    {
        for (int i = 0; i < InventoryManager.HotbarSize; i++)
            if (GetMainSlotRect(i, screenWidth, screenHeight).Contains(mousePos))
                return i;
        return -1;
    }

    /// <summary>Hit-test against consumable slots. Returns slot index or -1.</summary>
    public int HitTestConsumable(Point mousePos, int screenWidth, int screenHeight)
    {
        for (int i = 0; i < InventoryManager.ConsumableSlotCount; i++)
            if (GetConsumableSlotRect(i, screenWidth, screenHeight).Contains(mousePos))
                return i;
        return -1;
    }
}
