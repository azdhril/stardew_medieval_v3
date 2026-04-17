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
    private const int ChestColumns = 4;
    private const int ButtonWidth = 100;
    private const int ButtonHeight = 28;
    private const float DoubleClickWindow = 0.3f;

    private readonly InventoryManager _playerInventory;
    private readonly ChestInstance _chest;
    private readonly SpriteAtlas _atlas;
    private readonly System.Action _onClose;

    private ContainerGridRenderer _gridRenderer = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private UITheme _theme = null!;
    private bool _wasMouseDown;
    private float _timeSinceLastClick = float.MaxValue;
    private DragSourceKind _lastClickSource = DragSourceKind.None;
    private int _lastClickIndex = -1;

    private DragSourceKind _dragSource = DragSourceKind.None;
    private int _dragIndex = -1;
    private Point _dragPosition;

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
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        _theme = Services.Theme;

        _gridRenderer = new ContainerGridRenderer(_atlas);
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
        bool mouseDown = Mouse.GetState().LeftButton == ButtonState.Pressed;

        if (mouseDown && !_wasMouseDown)
            HandleMouseDown(mousePos);
        else if (mouseDown && _wasMouseDown && _dragSource != DragSourceKind.None)
            _dragPosition = mousePos;
        else if (!mouseDown && _wasMouseDown)
            HandleMouseUp(mousePos);

        _wasMouseDown = mouseDown;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.55f);
        spriteBatch.End();

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY, out int playerX, out int playerY, out int chestX, out int chestY);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Window frame (uniform pixel scale, not 9-slice)
        spriteBatch.Draw(_theme.PanePopup,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            Color.White);

        // Title plaque — centered hanging banner above the panel top edge.
        const int titlePlaqueW = 200;
        const int titlePlaqueH = 32;
        var titleRect = new Rectangle(
            panelX + (PanelWidth - titlePlaqueW) / 2,
            panelY - 16,
            titlePlaqueW, titlePlaqueH);
        NineSlice.Draw(spriteBatch, _theme.PanelTitle, titleRect, _theme.PanelTitleInsets);

        string title = ChestRegistry.Get(_chest.VariantId)?.DisplayName ?? "Chest";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(titleRect.X + (titleRect.Width - titleSize.X) / 2,
                        titleRect.Y + (titleRect.Height - titleSize.Y) / 2),
            Color.White);

        // Action buttons — shorter labels with a drop-shadow so text reads over button texture.
        DrawButton(spriteBatch, GetButtonRect(panelX, panelY, 0), "Pegar");
        DrawButton(spriteBatch, GetButtonRect(panelX, panelY, 1), "Enviar");
        DrawButton(spriteBatch, GetButtonRect(panelX, panelY, 2), "Ordenar");

        // Cream slot-pane backgrounds behind each grid — unifies the two grids visually.
        var playerPaneRect = new Rectangle(playerX - 8, playerY - 8, PlayerColumns * 40 + 16, 4 * 40 + 16);
        var chestPaneRect  = new Rectangle(chestX  - 8, chestY  - 8, ChestColumns  * 40 + 16, 4 * 40 + 16);
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, playerPaneRect, _theme.PanelSlotPaneInsets);
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, chestPaneRect,  _theme.PanelSlotPaneInsets);

        spriteBatch.DrawString(_font, "Player Inventory", new Vector2(playerX, panelY + 52), Color.LightGoldenrodYellow);
        spriteBatch.DrawString(_font, "Chest Storage",    new Vector2(chestX,  panelY + 52), Color.LightGoldenrodYellow);
        spriteBatch.DrawString(_font, "Drag items between grids. Press E or Esc to close.", new Vector2(panelX + 18, panelY + PanelHeight - 24), Color.Silver);

        int? hiddenPlayer = _dragSource == DragSourceKind.Player ? _dragIndex : null;
        int? hiddenChest = _dragSource == DragSourceKind.Chest ? _dragIndex : null;
        _gridRenderer.DrawGrid(spriteBatch, _playerInventory, PlayerColumns, playerX, playerY, hiddenPlayer);
        _gridRenderer.DrawGrid(spriteBatch, _chest.Container, ChestColumns, chestX, chestY, hiddenChest);

        var dragged = GetDraggedStack();
        if (dragged != null)
            _gridRenderer.DrawDraggedItem(spriteBatch, dragged, _dragPosition);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
    }

    private void HandleMouseDown(Point mousePos)
    {
        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY, out int playerX, out int playerY, out int chestX, out int chestY);

        var button = HitTestButton(mousePos, panelX, panelY);
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

    private void HandleMouseUp(Point mousePos)
    {
        if (_dragSource == DragSourceKind.None)
            return;

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY, out int playerX, out int playerY, out int chestX, out int chestY);

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

    private void GetLayout(int panelX, int panelY, out int playerX, out int playerY, out int chestX, out int chestY)
    {
        playerX = panelX + 18;
        playerY = panelY + 72;
        chestX = panelX + 250;
        chestY = panelY + 72;
    }

    private Rectangle GetButtonRect(int panelX, int panelY, int index)
    {
        // 3 centered buttons in a dedicated header row (below the hanging title plaque).
        const int gap = 8;
        int totalW = 3 * ButtonWidth + 2 * gap;
        int startX = panelX + (PanelWidth - totalW) / 2;
        return new Rectangle(startX + index * (ButtonWidth + gap), panelY + 18, ButtonWidth, ButtonHeight);
    }

    private ButtonAction HitTestButton(Point mousePos, int panelX, int panelY)
    {
        if (GetButtonRect(panelX, panelY, 0).Contains(mousePos)) return ButtonAction.TakeAll;
        if (GetButtonRect(panelX, panelY, 1).Contains(mousePos)) return ButtonAction.SendAll;
        if (GetButtonRect(panelX, panelY, 2).Contains(mousePos)) return ButtonAction.SortChest;
        return ButtonAction.None;
    }

    private void DrawButton(SpriteBatch sb, Rectangle rect, string text)
    {
        NineSlice.Draw(sb, _theme.CommonBtn, rect, _theme.CommonBtnInsets);

        var size = _font.MeasureString(text);
        float x = rect.X + (rect.Width - size.X) / 2f;
        float y = rect.Y + (rect.Height - size.Y) / 2f;

        // Drop shadow so the label reads over the textured button face.
        sb.DrawString(_font, text, new Vector2(x + 1, y + 1), Color.Black * 0.75f);
        sb.DrawString(_font, text, new Vector2(x, y), Color.LightGoldenrodYellow);
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
