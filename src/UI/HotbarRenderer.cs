using System;
using System.IO;
using FontStashSharp;
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
    private const int SlotSize = 50;
    private const int Padding = 0;
    private const int BottomMargin = 5;
    private const int IconPadding = 1;
    private const float IconScale = 0.80f;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private Texture2D? _handIcon;
    private Texture2D? _panelSlotPane;
    private Texture2D? _panelSlotPaneShort;
    private SpriteFontBase _font = null!;
    private Texture2D _pixel = null!;

    // When the Short panel is drawn, consumable slots are repositioned to center inside it.
    private int _consumablePanelCenterX = -1;

    // Drag state for hotbar rearranging
    private bool _isDragging;
    private int _dragSourceMain = -1;      // main hotbar slot being dragged
    private int _dragSourceConsumable = -1; // consumable slot being dragged
    private Point _dragPosition;
    private bool _wasMouseDown;

    // Click vs. drag disambiguation — a quick press+release without movement
    // selects the hotbar slot (same as pressing 1-8); movement past threshold
    // enters drag mode for reordering.
    private const int ClickDragThreshold = 4;
    private bool _pressActive;
    private int _pressSlotMain = -1;
    private int _pressSlotConsumable = -1;
    private Point _pressStartPos;

    // External drag suppression — set by InventoryGridRenderer when it is
    // dragging a hotbar/consumable ref, so we hide the icon on the source slot.
    private int _externalDragMain = -1;
    private int _externalDragConsumable = -1;

    /// <summary>Suppress icon rendering on a slot being dragged by an external renderer.</summary>
    public void SetExternalDragSource(int mainIndex, int consumableIndex)
    {
        _externalDragMain = mainIndex;
        _externalDragConsumable = consumableIndex;
    }

    public bool IsDragging => _isDragging;

    public HotbarRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
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
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Slot_Normal: {ex.Message}");
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
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Slot_Selected: {ex.Message}");
            _slotSelected = new Texture2D(device, 1, 1);
            _slotSelected.SetData(new[] { Color.Gold });
        }

        try
        {
            using var handStream = File.OpenRead("assets/Sprites/Items/Tools/hand.png");
            _handIcon = Texture2D.FromStream(device, handStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotbarRenderer] Failed to load hand.png: {ex.Message}");
            _handIcon = null;
        }

        try
        {
            using var paneStream = File.OpenRead("assets/Sprites/System/UI Elements/Panel/UI_Panel_SlotPane.png");
            _panelSlotPane = Texture2D.FromStream(device, paneStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Panel_SlotPane: {ex.Message}");
            _panelSlotPane = null;
        }

        try
        {
            using var shortStream = File.OpenRead("assets/Sprites/System/UI Elements/Panel/UI_Panel_SlotPane Short.png");
            _panelSlotPaneShort = Texture2D.FromStream(device, shortStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotbarRenderer] Failed to load UI_Panel_SlotPane Short: {ex.Message}");
            _panelSlotPaneShort = null;
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
            // Mouse just pressed — remember which slot (if any) and wait for
            // either a release (click = select) or movement (drag).
            int mainHit = HitTestMain(mousePos, screenWidth, screenHeight);
            int consHit = HitTestConsumable(mousePos, screenWidth, screenHeight);

            if (mainHit >= 0 || consHit >= 0)
            {
                _pressActive = true;
                _pressSlotMain = mainHit;
                _pressSlotConsumable = consHit;
                _pressStartPos = mousePos;
            }
        }
        else if (mouseDown && _pressActive && !_isDragging)
        {
            // Still pressed — promote to drag only if we moved past threshold
            // AND the press slot actually has an item to drag.
            int dx = mousePos.X - _pressStartPos.X;
            int dy = mousePos.Y - _pressStartPos.Y;
            if (dx * dx + dy * dy >= ClickDragThreshold * ClickDragThreshold)
            {
                if (_pressSlotMain >= 0 && _inventory.GetHotbarRef(_pressSlotMain) != null)
                {
                    _isDragging = true;
                    _dragSourceMain = _pressSlotMain;
                    _dragPosition = mousePos;
                }
                else if (_pressSlotConsumable >= 0 && _inventory.GetConsumableRef(_pressSlotConsumable) != null)
                {
                    _isDragging = true;
                    _dragSourceConsumable = _pressSlotConsumable;
                    _dragPosition = mousePos;
                }
                else
                {
                    // Moved away from an empty slot — cancel press so release doesn't select it.
                    _pressActive = false;
                }
            }
        }
        else if (mouseDown && _isDragging)
        {
            _dragPosition = mousePos;
        }
        else if (!mouseDown && _isDragging)
        {
            // Drop — swap if over a different slot
            int mainTarget = HitTestMain(mousePos, screenWidth, screenHeight);
            int consTarget = HitTestConsumable(mousePos, screenWidth, screenHeight);

            if (_dragSourceMain >= 0 && mainTarget >= 0 && mainTarget != _dragSourceMain)
                _inventory.SwapHotbarRefs(_dragSourceMain, mainTarget);
            else if (_dragSourceConsumable >= 0 && consTarget >= 0 && consTarget != _dragSourceConsumable)
                _inventory.SwapConsumableRefs(_dragSourceConsumable, consTarget);

            CancelDrag();
            _pressActive = false;
        }
        else if (!mouseDown && _pressActive)
        {
            // Released without dragging — treat as a click. Selecting a main
            // hotbar slot is the same as pressing its 1-8 key. Consumable
            // slots have no "active" state, so clicks there are no-ops.
            int mainTarget = HitTestMain(mousePos, screenWidth, screenHeight);
            if (mainTarget >= 0 && mainTarget == _pressSlotMain)
                _inventory.SetActiveHotbar(mainTarget);

            _pressActive = false;
            _pressSlotMain = -1;
            _pressSlotConsumable = -1;
        }

        _wasMouseDown = mouseDown;
    }

    /// <summary>Cancel any active drag or pending click.</summary>
    public void CancelDrag()
    {
        _isDragging = false;
        _dragSourceMain = -1;
        _dragSourceConsumable = -1;
        _pressActive = false;
        _pressSlotMain = -1;
        _pressSlotConsumable = -1;
    }

    public void Draw(SpriteBatch sb, int screenWidth, int screenHeight)
    {
        // Draw Short panel behind consumable Q slot(s) — match main panel height.
        if (_panelSlotPaneShort != null && _panelSlotPane != null && InventoryManager.ConsumableSlotCount > 0)
        {
            // Compute main panel height to match
            var firstMain = GetMainSlotRect(0, screenWidth, screenHeight);
            var lastMain = GetMainSlotRect(InventoryManager.HotbarSize - 1, screenWidth, screenHeight);
            float mainScale = (float)((lastMain.Right - firstMain.X) + 56) / _panelSlotPane.Width;
            int mainPanelH = (int)(_panelSlotPane.Height * mainScale);
            int mainPanelLeft = firstMain.X - 28;

            // Short panel: force same height, derive width from aspect, anchor right edge with gap
            int cTargetH = mainPanelH;
            float cScaleY = (float)cTargetH / _panelSlotPaneShort.Height;
            int cTargetW = (int)(_panelSlotPaneShort.Width * cScaleY);

            int gap = 8;
            int cRight = mainPanelLeft - gap;
            int cLeft = cRight - cTargetW;

            var firstCons = GetConsumableSlotRect(0, screenWidth, screenHeight);
            int cCenterY = firstCons.Y + firstCons.Height / 2;
            sb.Draw(_panelSlotPaneShort, new Rectangle(
                cLeft,
                cCenterY - cTargetH / 2,
                cTargetW,
                cTargetH), Color.White);

            // Reposition consumable slot(s) to center inside the Short panel
            _consumablePanelCenterX = cLeft + cTargetW / 2;
        }
        else
        {
            _consumablePanelCenterX = -1;
        }

        // Draw consumable slots (repositioned to center inside Short panel when present)
        string[] consumableKeys = { "Q" };
        for (int i = 0; i < InventoryManager.ConsumableSlotCount; i++)
        {
            var rect = GetConsumableSlotRect(i, screenWidth, screenHeight);
            if (_consumablePanelCenterX > 0)
                rect.X = _consumablePanelCenterX - rect.Width / 2;
            sb.Draw(_slotSelected, rect, Color.White);

            bool consHidden = (_isDragging && _dragSourceConsumable == i) || _externalDragConsumable == i;
            if (!consHidden)
            {
                var stack = _inventory.GetConsumableStack(i);
                if (stack != null)
                    DrawItemInSlot(sb, stack, rect);
            }

            sb.DrawString(_font, consumableKeys[i], new Vector2(rect.X + 2, rect.Y), Color.Gray * 0.7f);
        }

        // Draw decorative slot-pane panel BEHIND the 8 main hotbar slots
        // (purely visual — does not intercept input; slot sprites render on top below)
        if (_panelSlotPane != null)
        {
            // Native panel is 605x201 (aspect ~3.01). Scale uniformly so width
            // matches hotbar + horizontal trim, then center the panel on the
            // slot row vertically — keeps decorative borders un-squished.
            var firstSlot = GetMainSlotRect(0, screenWidth, screenHeight);
            var lastSlot = GetMainSlotRect(InventoryManager.HotbarSize - 1, screenWidth, screenHeight);
            const int PadX = 28;
            int targetW = (lastSlot.Right - firstSlot.X) + PadX * 2;
            float scale = (float)targetW / _panelSlotPane.Width;
            int targetH = (int)(_panelSlotPane.Height * scale);
            int slotCenterY = firstSlot.Y + firstSlot.Height / 2;
            var panelRect = new Rectangle(
                firstSlot.X - PadX,
                slotCenterY - targetH / 2,
                targetW,
                targetH);
            sb.Draw(_panelSlotPane, panelRect, Color.White);
        }

        // Draw main hotbar
        for (int i = 0; i < InventoryManager.HotbarSize; i++)
        {
            var rect = GetMainSlotRect(i, screenWidth, screenHeight);
            var slotTex = i == _inventory.ActiveHotbarIndex ? _slotSelected : _slotNormal;
            sb.Draw(slotTex, rect, Color.White);

            var stack = _inventory.GetHotbarStack(i);
            bool hiddenForDrag = (_isDragging && _dragSourceMain == i) || _externalDragMain == i;

            if (stack != null && !hiddenForDrag)
            {
                DrawItemInSlot(sb, stack, rect);
            }
            else if ((stack == null || hiddenForDrag) && _handIcon != null)
            {
                // Empty slot — show the hand as a watermark to hint that an
                // empty active slot uses the Hand tool (harvest / interact).
                int hFullW = rect.Width - IconPadding * 2;
                int hFullH = rect.Height - IconPadding * 2;
                int hW = (int)(hFullW * IconScale);
                int hH = (int)(hFullH * IconScale);
                int hOx = ((hFullW - hW) / 2)-1;
                int hOy = (hFullH - hH) / 2;
                var iconRect = new Rectangle(
                    rect.X + IconPadding + hOx, rect.Y + IconPadding + hOy, hW, hH);
                sb.Draw(_handIcon, iconRect, Color.White * 0.1f);
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
            var tex = _atlas.GetTexture(def.SpriteId);
            int fullW = rect.Width - IconPadding * 2;
            int fullH = rect.Height - IconPadding * 2;
            int iconW = (int)(fullW * IconScale);
            int iconH = (int)(fullH * IconScale);
            int offsetX = (fullW - iconW) / 2;
            int offsetY = (fullH - iconH) / 2;
            var destRect = new Rectangle(
                rect.X + IconPadding + offsetX, rect.Y + IconPadding + offsetY,
                iconW, iconH);
            sb.Draw(tex, destRect, srcRect, Color.White);
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
        sb.Draw(_atlas.GetTexture(def.SpriteId), destRect, srcRect, Color.White * 0.85f);
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
        int gap = (int)(screenWidth * 0.10f);
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
        {
            var rect = GetConsumableSlotRect(i, screenWidth, screenHeight);
            if (_consumablePanelCenterX > 0)
                rect.X = _consumablePanelCenterX - rect.Width / 2;
            if (rect.Contains(mousePos))
                return i;
        }
        return -1;
    }
}
