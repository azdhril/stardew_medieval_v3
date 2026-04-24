using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Overlay for transferring items between the player inventory and a world chest.
/// </summary>
public class ChestScene : Scene
{
    private const int PanelWidth = 735;
    private const int PanelHeight = 447;
    private const int PlayerColumns = 5;
    private const int ChestColumns = 5;
    private const int MinSlotSize = 40;
    private const int MaxSlotSize = 60;
    private const int IconButtonSize = 32;
    private const int IconButtonGap = 6;
    private const int CloseButtonSize = 26;
    private const int ContentMarginX = 18;
    private const int ContentTop = 72;
    private const int ContentBottom = 44;
    private const int PanePadding = 12;
    private const int PaneTitleHeight = 22;
    private const int TooltipPadding = 9;
    private const int TooltipGap = 8;
    private const int TooltipCorner = 4;
    private const int TooltipLineGap = 3;
    private const int TooltipStatGap = 5;
    private const int ContextMenuWidth = 118;
    private const int ContextMenuRowHeight = 24;
    private const float DoubleClickWindow = 0.3f;

    private readonly InventoryManager _playerInventory;
    private readonly ChestInstance _chest;
    private readonly SpriteAtlas _atlas;
    private readonly System.Action _onClose;

    private ContainerGridRenderer _gridRenderer = null!;
    private SpriteFontBase _font = null!;
    private SpriteFontBase _smallFont = null!;
    private SpriteFontBase _titleFont = null!;
    private Texture2D _pixel = null!;
    private UITheme _theme = null!;
    private bool _wasMouseDown;
    private bool _wasRightMouseDown;
    private float _timeSinceLastClick = float.MaxValue;
    private DragSourceKind _lastClickSource = DragSourceKind.None;
    private int _lastClickIndex = -1;

    private DragSourceKind _dragSource = DragSourceKind.None;
    private int _dragIndex = -1;
    private Point _dragPosition;
    private bool _contextMenuOpen;
    private DragSourceKind _contextSource = DragSourceKind.None;
    private int _contextIndex = -1;
    private Point _contextPosition;

    private enum DragSourceKind
    {
        None,
        Player,
        Chest
    }

    private enum ButtonAction
    {
        None,
        TakeAll,
        SendAll,
        SortChest
    }

    private enum ContextAction
    {
        Use,
        Equip,
        Drop,
        Compare
    }

    private readonly struct TooltipStat
    {
        public TooltipStat(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    public ChestScene(ServiceContainer services, InventoryManager playerInventory, ChestInstance chest, SpriteAtlas atlas, System.Action onClose)
        : base(services)
    {
        _playerInventory = playerInventory;
        _chest = chest;
        _atlas = atlas;
        _onClose = onClose;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);
        _smallFont = Services.Fonts!.GetFont(FontRole.Body, 15);
        // Native bold title (50% larger than previous 16pt baseline).
        _titleFont = Services.Fonts!.GetFont(FontRole.Bold, 24);

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        _theme = Services.Theme;

        _gridRenderer = new ContainerGridRenderer(_atlas, MaxSlotSize);
        _gridRenderer.LoadContent(device, _font);
    }

    public override void Update(float deltaTime)
    {
        _timeSinceLastClick += deltaTime;

        var input = Services.Input;
        if (input.IsKeyPressed(Keys.Escape) || input.IsKeyPressed(Keys.E) || input.IsKeyPressed(Keys.I))
        {
            CloseOverlay();
            return;
        }

        Point mousePos = input.MousePosition;
        var mouseState = Mouse.GetState();
        bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
        bool rightMouseDown = mouseState.RightButton == ButtonState.Pressed;

        if (rightMouseDown && !_wasRightMouseDown)
        {
            OpenContextMenu(mousePos);
            _wasRightMouseDown = rightMouseDown;
            _wasMouseDown = mouseDown;
            return;
        }

        if (mouseDown && !_wasMouseDown)
            HandleMouseDown(mousePos);
        else if (mouseDown && _wasMouseDown && _dragSource != DragSourceKind.None)
            _dragPosition = mousePos;
        else if (!mouseDown && _wasMouseDown)
            HandleMouseUp(mousePos);

        _wasMouseDown = mouseDown;
        _wasRightMouseDown = rightMouseDown;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.55f);
        spriteBatch.End();

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY,
            out Rectangle playerPaneRect, out Rectangle chestPaneRect,
            out int playerX, out int playerY, out int chestX, out int chestY);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        NineSlice.Draw(spriteBatch, _theme.PanePopup,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            _theme.PanePopupInsets);

        // Title plaque — centered hanging banner above the panel top edge.
        string title = ChestRegistry.Get(_chest.VariantId)?.DisplayName ?? "Chest";
        DrawCenteredText(spriteBatch, _titleFont, title,
            new Rectangle(panelX + 28, panelY + 6, PanelWidth - 56, 50),
            Color.LightGoldenrodYellow, 2f, withShadow: true);
        DrawCloseButton(spriteBatch, GetCloseButtonRect(panelX, panelY));

        // Action buttons — shorter labels with a drop-shadow so text reads over button texture.
        // Cream slot-pane backgrounds behind each grid — unifies the two grids visually.
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, playerPaneRect, _theme.PanelSlotPaneInsets);
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, chestPaneRect,  _theme.PanelSlotPaneInsets);

        DrawCenteredText(spriteBatch, _titleFont, "Bolsa", new Rectangle(
            playerPaneRect.X + PanePadding,
            playerPaneRect.Y + 1,
            playerPaneRect.Width - PanePadding * 2,
            PaneTitleHeight), Color.LightGoldenrodYellow, 1f);
        DrawCenteredText(spriteBatch, _titleFont, "Baú", new Rectangle(
            chestPaneRect.X + PanePadding,
            chestPaneRect.Y + 1,
            chestPaneRect.Width - PanePadding * 2,
            PaneTitleHeight), Color.LightGoldenrodYellow, 1f);
        DrawIconButton(spriteBatch, GetActionButtonRect(playerPaneRect, ButtonAction.SendAll), _theme.IconArrowRight, SpriteEffects.None);
        DrawIconButton(spriteBatch, GetActionButtonRect(chestPaneRect, ButtonAction.TakeAll), _theme.IconArrowRight, SpriteEffects.FlipHorizontally);
        DrawIconButton(spriteBatch, GetActionButtonRect(chestPaneRect, ButtonAction.SortChest), _theme.IconSort, SpriteEffects.None);
        int? hiddenPlayer = _dragSource == DragSourceKind.Player ? _dragIndex : null;
        int? hiddenChest = _dragSource == DragSourceKind.Chest ? _dragIndex : null;
        _gridRenderer.DrawGrid(spriteBatch, _playerInventory, PlayerColumns, playerX, playerY, hiddenPlayer);
        _gridRenderer.DrawGrid(spriteBatch, _chest.Container, ChestColumns, chestX, chestY,
            hiddenChest, ChestInstance.MaxCapacity, _chest.Container.Capacity);

        var dragged = GetDraggedStack();
        if (dragged != null)
            _gridRenderer.DrawDraggedItem(spriteBatch, dragged, _dragPosition);
        else
            DrawItemTooltip(spriteBatch, Mouse.GetState().Position, playerX, playerY, chestX, chestY);

        if (_contextMenuOpen)
            DrawContextMenu(spriteBatch);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
    }

    private void HandleMouseDown(Point mousePos)
    {
        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY,
            out Rectangle playerPaneRect, out Rectangle chestPaneRect,
            out int playerX, out int playerY, out int chestX, out int chestY);

        if (GetCloseButtonRect(panelX, panelY).Contains(mousePos))
        {
            CloseOverlay();
            return;
        }

        if (_contextMenuOpen)
        {
            if (TryExecuteContextAction(mousePos))
                return;

            _contextMenuOpen = false;
        }

        var button = HitTestButton(mousePos, playerPaneRect, chestPaneRect);
        if (button != ButtonAction.None)
        {
            ExecuteButton(button);
            return;
        }

        int playerHit = _gridRenderer.HitTest(mousePos, _playerInventory.Capacity, PlayerColumns, playerX, playerY);
        if (playerHit >= 0 && _playerInventory.GetSlot(playerHit) != null)
        {
            if (TryHandleDoubleClick(DragSourceKind.Player, playerHit))
                return;

            _dragSource = DragSourceKind.Player;
            _dragIndex = playerHit;
            _dragPosition = mousePos;
            RegisterClick(DragSourceKind.Player, playerHit);
            return;
        }

        int chestHit = _gridRenderer.HitTest(mousePos, _chest.Container.Capacity, ChestColumns, chestX, chestY);
        if (chestHit >= 0 && _chest.Container.GetSlot(chestHit) != null)
        {
            if (TryHandleDoubleClick(DragSourceKind.Chest, chestHit))
                return;

            _dragSource = DragSourceKind.Chest;
            _dragIndex = chestHit;
            _dragPosition = mousePos;
            RegisterClick(DragSourceKind.Chest, chestHit);
        }
    }

    private void OpenContextMenu(Point mousePos)
    {
        _contextMenuOpen = false;
        _contextSource = DragSourceKind.None;
        _contextIndex = -1;

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY,
            out _, out _,
            out int playerX, out int playerY, out int chestX, out int chestY);

        int playerHit = _gridRenderer.HitTest(mousePos, _playerInventory.Capacity, PlayerColumns, playerX, playerY);
        if (playerHit >= 0 && _playerInventory.GetSlot(playerHit) != null)
        {
            _contextSource = DragSourceKind.Player;
            _contextIndex = playerHit;
        }
        else
        {
            int chestHit = _gridRenderer.HitTest(mousePos, _chest.Container.Capacity, ChestColumns, chestX, chestY);
            if (chestHit >= 0 && _chest.Container.GetSlot(chestHit) != null)
            {
                _contextSource = DragSourceKind.Chest;
                _contextIndex = chestHit;
            }
        }

        if (_contextSource == DragSourceKind.None)
            return;

        _dragSource = DragSourceKind.None;
        _dragIndex = -1;
        _contextPosition = mousePos;
        _contextMenuOpen = true;
    }

    private void HandleMouseUp(Point mousePos)
    {
        if (_dragSource == DragSourceKind.None)
            return;

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY,
            out _, out _,
            out int playerX, out int playerY, out int chestX, out int chestY);

        int playerHit = _gridRenderer.HitTest(mousePos, _playerInventory.Capacity, PlayerColumns, playerX, playerY);
        int chestHit = _gridRenderer.HitTest(mousePos, _chest.Container.Capacity, ChestColumns, chestX, chestY);

        if (_dragSource == DragSourceKind.Player)
        {
            if (playerHit >= 0 && playerHit != _dragIndex)
                _playerInventory.MoveItem(_dragIndex, playerHit);
            else if (chestHit >= 0)
                ItemSlotTransfer.MoveOrSwap(_playerInventory, _dragIndex, _chest.Container, chestHit);
        }
        else if (_dragSource == DragSourceKind.Chest)
        {
            if (chestHit >= 0 && chestHit != _dragIndex)
                _chest.Container.MoveItem(_dragIndex, chestHit);
            else if (playerHit >= 0)
                ItemSlotTransfer.MoveOrSwap(_chest.Container, _dragIndex, _playerInventory, playerHit);
        }

        _playerInventory.PruneBrokenReferences();
        _dragSource = DragSourceKind.None;
        _dragIndex = -1;
    }

    private ItemStack? GetDraggedStack() => _dragSource switch
    {
        DragSourceKind.Player => _playerInventory.GetSlot(_dragIndex),
        DragSourceKind.Chest => _chest.Container.GetSlot(_dragIndex),
        _ => null,
    };

    private void CloseOverlay()
    {
        _contextMenuOpen = false;
        _dragSource = DragSourceKind.None;
        _dragIndex = -1;
        _onClose();
        Services.SceneManager.PopImmediate();
    }

    private void GetPanelPosition(out int panelX, out int panelY)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        panelX = (viewport.Width - PanelWidth) / 2;
        panelY = (viewport.Height - PanelHeight) / 2 - 20;
    }

    private void GetLayout(
        int panelX,
        int panelY,
        out Rectangle playerPaneRect,
        out Rectangle chestPaneRect,
        out int playerX,
        out int playerY,
        out int chestX,
        out int chestY)
    {
        int contentX = panelX + ContentMarginX;
        int contentY = panelY + ContentTop;
        int contentW = PanelWidth - ContentMarginX * 2;
        int contentH = PanelHeight - ContentTop - ContentBottom;
        int separatorW = Math.Max(8, (int)MathF.Round(contentW * 0.02f));
        int paneW = (contentW - separatorW) / 2;

        playerPaneRect = new Rectangle(contentX, contentY, paneW, contentH);
        chestPaneRect = new Rectangle(contentX + paneW + separatorW, contentY, contentW - paneW - separatorW, contentH);

        int slotSize = CalculateSlotSize(playerPaneRect, chestPaneRect);
        _gridRenderer.SetSlotPixelSize(slotSize);

        int playerGridW = PlayerColumns * slotSize;
        int chestGridW = ChestColumns * slotSize;

        playerX = playerPaneRect.X + (playerPaneRect.Width - playerGridW) / 2;
        playerY = playerPaneRect.Y + PanePadding + PaneTitleHeight;
        chestX = chestPaneRect.X + (chestPaneRect.Width - chestGridW) / 2;
        chestY = chestPaneRect.Y + PanePadding + PaneTitleHeight;
    }

    private int CalculateSlotSize(Rectangle playerPaneRect, Rectangle chestPaneRect)
    {
        int playerRows = (int)Math.Ceiling(_playerInventory.Capacity / (float)PlayerColumns);
        int chestRows = (int)Math.Ceiling(ChestInstance.MaxCapacity / (float)ChestColumns);
        int usablePlayerW = playerPaneRect.Width - PanePadding * 2;
        int usableChestW = chestPaneRect.Width - PanePadding * 2;
        int usableH = playerPaneRect.Height - PanePadding * 2 - PaneTitleHeight;

        int byPlayerW = usablePlayerW / PlayerColumns;
        int byChestW = usableChestW / ChestColumns;
        int byPlayerH = usableH / playerRows;
        int byChestH = usableH / chestRows;
        int size = Math.Min(Math.Min(byPlayerW, byChestW), Math.Min(byPlayerH, byChestH));
        return Math.Clamp(size, MinSlotSize, MaxSlotSize);
    }

    private Rectangle GetActionButtonRect(Rectangle paneRect, ButtonAction action)
    {
        int y = paneRect.Y + 5;
        int right = paneRect.Right - PanePadding;
        return action switch
        {
            ButtonAction.SendAll => new Rectangle(right - IconButtonSize, y, IconButtonSize, IconButtonSize),
            ButtonAction.SortChest => new Rectangle(right - IconButtonSize, y, IconButtonSize, IconButtonSize),
            ButtonAction.TakeAll => new Rectangle(
                right - IconButtonSize * 2 - IconButtonGap,
                y,
                IconButtonSize,
                IconButtonSize),
            _ => Rectangle.Empty,
        };
    }

    private static Rectangle GetCloseButtonRect(int panelX, int panelY) => new(
        panelX + PanelWidth - 54,
        panelY + 18,
        CloseButtonSize,
        CloseButtonSize);

    private ButtonAction HitTestButton(Point mousePos, Rectangle playerPaneRect, Rectangle chestPaneRect)
    {
        if (GetActionButtonRect(playerPaneRect, ButtonAction.SendAll).Contains(mousePos)) return ButtonAction.SendAll;
        if (GetActionButtonRect(chestPaneRect, ButtonAction.TakeAll).Contains(mousePos)) return ButtonAction.TakeAll;
        if (GetActionButtonRect(chestPaneRect, ButtonAction.SortChest).Contains(mousePos)) return ButtonAction.SortChest;
        return ButtonAction.None;
    }

    private void DrawIconButton(SpriteBatch sb, Rectangle rect, Texture2D icon, SpriteEffects effects)
    {
        NineSlice.Draw(sb, _theme.CommonBtn, rect, _theme.CommonBtnInsets);

        var iconRect = new Rectangle(rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
        sb.Draw(icon, iconRect, null, Color.LightGoldenrodYellow, 0f, Vector2.Zero, effects, 0f);
    }

    private void DrawCloseButton(SpriteBatch sb, Rectangle rect)
    {
        Color tint = rect.Contains(Mouse.GetState().Position)
            ? Color.White
            : new Color(238, 214, 151);
        NineSlice.DrawStretched(sb, _theme.BtnIconX, rect, tint);
    }

    /// <summary>
    /// Draws <paramref name="text"/> centered inside <paramref name="rect"/>. Caller
    /// passes a pre-sized <see cref="SpriteFontBase"/> (native glyph size); the helper
    /// no longer accepts a scale parameter — scaling a pre-rasterized glyph produced
    /// the bilinear smudge the FontStashSharp migration (quick 260423-tu6) fixed.
    /// When <paramref name="withShadow"/> is true, a 1px right + 1px down outline is
    /// drawn behind the glyphs for extra legibility over busy backgrounds.
    /// </summary>
    private void DrawCenteredText(
        SpriteBatch sb,
        SpriteFontBase font,
        string text,
        Rectangle rect,
        Color color,
        float letterSpacing = 0f,
        bool withShadow = false)
    {
        var size = MeasureText(font, text, letterSpacing);
        var pos = new Vector2(
            rect.X + (rect.Width - size.X) / 2f,
            rect.Y + (rect.Height - size.Y) / 2f);
        DrawText(sb, font, text, pos, color, letterSpacing, withShadow);
    }

    private static Vector2 MeasureText(SpriteFontBase font, string text, float letterSpacing)
    {
        var size = font.MeasureString(text);
        if (text.Length > 1)
            size.X += letterSpacing * (text.Length - 1);
        return size;
    }

    private static void DrawText(
        SpriteBatch sb,
        SpriteFontBase font,
        string text,
        Vector2 pos,
        Color color,
        float letterSpacing,
        bool withShadow = false)
    {
        if (letterSpacing <= 0f)
        {
            DrawString(sb, font, text, pos, color, withShadow);
            return;
        }

        float x = pos.X;
        for (int i = 0; i < text.Length; i++)
        {
            string c = text[i].ToString();
            DrawString(sb, font, c, new Vector2(x, pos.Y), color, withShadow);
            x += font.MeasureString(c).X + letterSpacing;
        }
    }

    private static void DrawString(SpriteBatch sb, SpriteFontBase font, string text, Vector2 pos, Color color, bool withShadow)
    {
        sb.DrawString(font, text, pos, color);
        if (!withShadow)
            return;

        // Fixed 1px outline offsets (previously "scale pixels"). 1px reads as a clean
        // drop shadow at 960x540 native resolution regardless of font size.
        sb.DrawString(font, text, pos + new Vector2(1f, 0f), color);
        sb.DrawString(font, text, pos + new Vector2(0f, 1f), color * 0.82f);
    }

    private void DrawContextMenu(SpriteBatch sb)
    {
        var actions = GetContextActions();
        if (actions.Length == 0)
            return;

        var viewport = Services.GraphicsDevice.Viewport;
        int height = actions.Length * ContextMenuRowHeight + 4;
        int x = Math.Clamp(_contextPosition.X, 4, viewport.Width - ContextMenuWidth - 4);
        int y = Math.Clamp(_contextPosition.Y, 4, viewport.Height - height - 4);
        var rect = new Rectangle(x, y, ContextMenuWidth, height);

        sb.Draw(_pixel, rect, new Color(40, 23, 20) * 0.96f);
        DrawRectOutline(sb, rect, new Color(226, 190, 114));

        Point mousePos = Mouse.GetState().Position;
        for (int i = 0; i < actions.Length; i++)
        {
            var row = new Rectangle(rect.X + 2, rect.Y + 2 + i * ContextMenuRowHeight, rect.Width - 4, ContextMenuRowHeight);
            if (row.Contains(mousePos))
                sb.Draw(_pixel, row, Color.LightGoldenrodYellow * 0.18f);

            sb.DrawString(_smallFont, GetContextLabel(actions[i]), new Vector2(row.X + 8, row.Y + 5), Color.LightGoldenrodYellow);
        }
    }

    private bool TryExecuteContextAction(Point mousePos)
    {
        var actions = GetContextActions();
        if (actions.Length == 0)
        {
            _contextMenuOpen = false;
            return false;
        }

        int height = actions.Length * ContextMenuRowHeight + 4;
        var viewport = Services.GraphicsDevice.Viewport;
        int x = Math.Clamp(_contextPosition.X, 4, viewport.Width - ContextMenuWidth - 4);
        int y = Math.Clamp(_contextPosition.Y, 4, viewport.Height - height - 4);
        var rect = new Rectangle(x, y, ContextMenuWidth, height);
        if (!rect.Contains(mousePos))
            return false;

        int row = (mousePos.Y - rect.Y - 2) / ContextMenuRowHeight;
        if (row < 0 || row >= actions.Length)
            return false;

        ExecuteContextAction(actions[row]);
        _contextMenuOpen = false;
        return true;
    }

    private ContextAction[] GetContextActions()
    {
        var stack = GetContextStack();
        if (stack == null)
            return Array.Empty<ContextAction>();

        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null)
            return Array.Empty<ContextAction>();

        var actions = new System.Collections.Generic.List<ContextAction>();
        if (def.Type == ItemType.Consumable)
            actions.Add(ContextAction.Use);
        if (_contextSource == DragSourceKind.Player && def.EquipSlot != null)
            actions.Add(ContextAction.Equip);
        actions.Add(ContextAction.Drop);
        if (def.Type == ItemType.Weapon)
            actions.Add(ContextAction.Compare);
        return actions.ToArray();
    }

    private ItemStack? GetContextStack() => _contextSource switch
    {
        DragSourceKind.Player => _playerInventory.GetSlot(_contextIndex),
        DragSourceKind.Chest => _chest.Container.GetSlot(_contextIndex),
        _ => null,
    };

    private static string GetContextLabel(ContextAction action) => action switch
    {
        ContextAction.Use => "Usar",
        ContextAction.Equip => "Equipar",
        ContextAction.Drop => "Largar",
        ContextAction.Compare => "Comparar",
        _ => "",
    };

    private void ExecuteContextAction(ContextAction action)
    {
        switch (action)
        {
            case ContextAction.Use:
                UseContextConsumable();
                break;
            case ContextAction.Equip:
                if (_contextSource == DragSourceKind.Player)
                    _playerInventory.TryEquip(_contextIndex);
                break;
            case ContextAction.Drop:
                SetContextSlot(null);
                break;
            case ContextAction.Compare:
                break;
        }

        _playerInventory.PruneBrokenReferences();
    }

    private void UseContextConsumable()
    {
        var stack = GetContextStack();
        if (stack == null)
            return;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def?.Type != ItemType.Consumable)
            return;

        if (Services.Player != null)
        {
            if (def.Stats.TryGetValue("heal", out float heal))
                Services.Player.HP = Math.Min(Services.Player.MaxHP, Services.Player.HP + heal);

            if (def.Stats.TryGetValue("stamina_restore_pct", out float stamina))
                Services.Player.Stats.RestoreStamina(Services.Player.Stats.MaxStamina * (stamina > 1f ? stamina / 100f : stamina));
        }

        int nextQuantity = stack.Quantity - 1;
        SetContextSlot(nextQuantity > 0
            ? new ItemStack { ItemId = stack.ItemId, Quantity = nextQuantity }
            : null);
    }

    private void SetContextSlot(ItemStack? stack)
    {
        if (_contextSource == DragSourceKind.Player)
            _playerInventory.SetSlot(_contextIndex, stack);
        else if (_contextSource == DragSourceKind.Chest)
            _chest.Container.SetSlot(_contextIndex, stack);
    }

    private void DrawItemTooltip(SpriteBatch sb, Point mousePos, int playerX, int playerY, int chestX, int chestY)
    {
        ItemStack? stack = null;
        Rectangle anchor = Rectangle.Empty;

        int playerHit = _gridRenderer.HitTest(mousePos, _playerInventory.Capacity, PlayerColumns, playerX, playerY);
        if (playerHit >= 0)
        {
            stack = _playerInventory.GetSlot(playerHit);
            anchor = _gridRenderer.GetSlotRect(playerHit, PlayerColumns, playerX, playerY);
        }
        else
        {
            int chestHit = _gridRenderer.HitTest(mousePos, _chest.Container.Capacity, ChestColumns, chestX, chestY);
            if (chestHit >= 0)
            {
                stack = _chest.Container.GetSlot(chestHit);
                anchor = _gridRenderer.GetSlotRect(chestHit, ChestColumns, chestX, chestY);
            }
        }

        if (stack == null)
            return;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null)
            return;

        var viewport = Services.GraphicsDevice.Viewport;
        string name = string.IsNullOrWhiteSpace(def.Name) ? def.Id : def.Name;
        string description = GetTooltipDescription(def);
        var stats = GetTooltipStats(def);
        var nameSize = _titleFont.MeasureString(name);
        var descSize = _font.MeasureString(description);

        float statsWidth = 0f;
        float statLabelWidth = 0f;
        float statValueWidth = 0f;
        for (int i = 0; i < stats.Length; i++)
        {
            statLabelWidth = Math.Max(statLabelWidth, _smallFont.MeasureString(stats[i].Label).X);
            statValueWidth = Math.Max(statValueWidth, _smallFont.MeasureString(stats[i].Value).X);
        }

        if (stats.Length > 0)
            statsWidth = statLabelWidth + TooltipStatGap + statValueWidth;

        int width = (int)Math.Ceiling(Math.Max(Math.Max(nameSize.X, descSize.X), statsWidth)) + TooltipPadding * 2;
        int height = (int)Math.Ceiling(nameSize.Y + TooltipLineGap + descSize.Y) + TooltipPadding * 2;
        if (stats.Length > 0)
        {
            height += TooltipLineGap + 1;
            height += stats.Length * (_smallFont.LineHeight + TooltipLineGap) - TooltipLineGap;
        }

        int x = Math.Clamp(anchor.X + anchor.Width / 2 - width / 2, 4, viewport.Width - width - 4);
        int y = anchor.Y - TooltipGap - height;
        if (y < 4)
            y = anchor.Bottom + TooltipGap;

        var rect = new Rectangle(x, y, width, height);
        DrawTooltipPanel(sb, rect);

        var textPos = new Vector2(rect.X + TooltipPadding, rect.Y + TooltipPadding);
        sb.DrawString(_titleFont, name, textPos, Color.LightGoldenrodYellow);
        textPos.Y += nameSize.Y + TooltipLineGap;
        sb.DrawString(_font, description, textPos, new Color(226, 214, 184));

        if (stats.Length == 0)
            return;

        textPos.Y += descSize.Y + TooltipLineGap + 1;
        for (int i = 0; i < stats.Length; i++)
        {
            sb.DrawString(_smallFont, stats[i].Label, textPos, new Color(188, 174, 146));
            sb.DrawString(_smallFont, stats[i].Value,
                new Vector2(textPos.X + statLabelWidth + TooltipStatGap, textPos.Y),
                Color.White);
            textPos.Y += _smallFont.LineHeight + TooltipLineGap;
        }
    }

    private static string GetTooltipDescription(ItemDefinition def)
    {
        if (!string.IsNullOrWhiteSpace(def.Description))
            return def.Description;

        if (def.Stats.Count > 0)
        {
            return def.Type switch
            {
                ItemType.Weapon => "Arma.",
                ItemType.Armor => "Equip.",
                ItemType.Consumable => "Usavel.",
                _ => "Bonus.",
            };
        }

        if (def.Id.Equals("Axe", StringComparison.OrdinalIgnoreCase))
            return "Corta madeira.";
        if (def.Id.Equals("Hoe", StringComparison.OrdinalIgnoreCase))
            return "Prepara o solo.";
        if (def.Id.Equals("Watering_Can", StringComparison.OrdinalIgnoreCase))
            return "Rega plantas.";
        if (def.Id.Equals("Scythe", StringComparison.OrdinalIgnoreCase))
            return "Colhe plantas.";
        if (def.Id.Contains("Rotten", StringComparison.OrdinalIgnoreCase))
            return "Estragado.";

        return def.Type switch
        {
            ItemType.Seed => "Planta no solo.",
            ItemType.Crop => "Venda ou use.",
            ItemType.Tool => "Ferramenta.",
            ItemType.Weapon => "Arma.",
            ItemType.Armor => "Equipamento.",
            ItemType.Consumable => "Usavel.",
            ItemType.Loot => "Material.",
            ItemType.Currency => "Moeda.",
            _ => "Item.",
        };
    }

    private static TooltipStat[] GetTooltipStats(ItemDefinition def)
    {
        if (def.Stats.Count == 0)
            return Array.Empty<TooltipStat>();

        var stats = new System.Collections.Generic.List<TooltipStat>();
        AddStat(stats, def, "damage", "Dano", "+{0:0}");
        AddStat(stats, def, "defense", "Defesa", "+{0:0}");
        AddStat(stats, def, "heal", "Vida", "+{0:0}");
        AddStat(stats, def, "stamina_restore_pct", "Vigor", "+{0:0}%");
        AddStat(stats, def, "cooldown", "Tempo", "{0:0.0}s");
        return stats.ToArray();
    }

    private static void AddStat(
        System.Collections.Generic.List<TooltipStat> stats,
        ItemDefinition def,
        string key,
        string label,
        string format)
    {
        if (def.Stats.TryGetValue(key, out float value))
            stats.Add(new TooltipStat(label, string.Format(System.Globalization.CultureInfo.InvariantCulture, format, value)));
    }

    private void DrawTooltipPanel(SpriteBatch sb, Rectangle rect)
    {
        var fill = new Color(40, 23, 20) * 0.96f;
        var border = new Color(226, 190, 114);
        int r = TooltipCorner;

        sb.Draw(_pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, rect.Height), fill);
        sb.Draw(_pixel, new Rectangle(rect.X, rect.Y + r, rect.Width, rect.Height - r * 2), fill);

        sb.Draw(_pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, 1), border);
        sb.Draw(_pixel, new Rectangle(rect.X + r, rect.Bottom - 1, rect.Width - r * 2, 1), border);
        sb.Draw(_pixel, new Rectangle(rect.X, rect.Y + r, 1, rect.Height - r * 2), border);
        sb.Draw(_pixel, new Rectangle(rect.Right - 1, rect.Y + r, 1, rect.Height - r * 2), border);

        sb.Draw(_pixel, new Rectangle(rect.X + 1, rect.Y + r - 1, r - 1, 1), border);
        sb.Draw(_pixel, new Rectangle(rect.Right - r, rect.Y + r - 1, r - 1, 1), border);
        sb.Draw(_pixel, new Rectangle(rect.X + 1, rect.Bottom - r, r - 1, 1), border);
        sb.Draw(_pixel, new Rectangle(rect.Right - r, rect.Bottom - r, r - 1, 1), border);
    }

    private void DrawRectOutline(SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        sb.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        sb.Draw(_pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }

    private void ExecuteButton(ButtonAction action)
    {
        switch (action)
        {
            case ButtonAction.TakeAll:
                MoveAllFromChestToPlayer();
                break;
            case ButtonAction.SendAll:
                MoveAllFromPlayerToChest();
                break;
            case ButtonAction.SortChest:
                _chest.Container.SortByDefault();
                break;
        }

        _playerInventory.PruneBrokenReferences();
    }

    private bool TryHandleDoubleClick(DragSourceKind source, int index)
    {
        bool isDoubleClick = _lastClickSource == source
            && _lastClickIndex == index
            && _timeSinceLastClick <= DoubleClickWindow;

        if (!isDoubleClick)
            return false;

        if (source == DragSourceKind.Player)
            SendSingleItemToChest(index);
        else if (source == DragSourceKind.Chest)
            TakeSingleItemFromChest(index);

        _playerInventory.PruneBrokenReferences();
        _lastClickSource = DragSourceKind.None;
        _lastClickIndex = -1;
        _timeSinceLastClick = float.MaxValue;
        _dragSource = DragSourceKind.None;
        _dragIndex = -1;
        return true;
    }

    private void RegisterClick(DragSourceKind source, int index)
    {
        _lastClickSource = source;
        _lastClickIndex = index;
        _timeSinceLastClick = 0f;
    }

    private void SendSingleItemToChest(int index)
    {
        var stack = _playerInventory.GetSlot(index);
        if (stack == null) return;

        int remaining = _chest.Container.TryAdd(stack.ItemId, stack.Quantity);
        if (remaining <= 0)
            _playerInventory.SetSlot(index, null);
        else if (remaining != stack.Quantity)
            _playerInventory.SetSlot(index, new ItemStack { ItemId = stack.ItemId, Quantity = remaining });
    }

    private void TakeSingleItemFromChest(int index)
    {
        var stack = _chest.Container.GetSlot(index);
        if (stack == null) return;

        int remaining = _playerInventory.TryAdd(stack.ItemId, stack.Quantity);
        if (remaining <= 0)
            _chest.Container.SetSlot(index, null);
        else if (remaining != stack.Quantity)
            _chest.Container.SetSlot(index, new ItemStack { ItemId = stack.ItemId, Quantity = remaining });
    }

    private void MoveAllFromChestToPlayer()
    {
        for (int i = 0; i < _chest.Container.Capacity; i++)
        {
            var stack = _chest.Container.GetSlot(i);
            if (stack == null) continue;

            int remaining = _playerInventory.TryAdd(stack.ItemId, stack.Quantity);
            if (remaining <= 0)
                _chest.Container.SetSlot(i, null);
            else if (remaining != stack.Quantity)
                _chest.Container.SetSlot(i, new ItemStack { ItemId = stack.ItemId, Quantity = remaining });
        }
    }

    private void MoveAllFromPlayerToChest()
    {
        for (int i = 0; i < _playerInventory.Capacity; i++)
        {
            var stack = _playerInventory.GetSlot(i);
            if (stack == null) continue;

            int remaining = _chest.Container.TryAdd(stack.ItemId, stack.Quantity);
            if (remaining <= 0)
                _playerInventory.SetSlot(i, null);
            else if (remaining != stack.Quantity)
                _playerInventory.SetSlot(i, new ItemStack { ItemId = stack.ItemId, Quantity = remaining });
        }
    }
}
