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
    private const int PanelWidth = 470;
    private const int PanelHeight = 250;
    private const int PlayerColumns = 5;
    private const int ChestColumns = 4;

    private readonly InventoryManager _playerInventory;
    private readonly ChestInstance _chest;
    private readonly SpriteAtlas _atlas;
    private readonly System.Action _onClose;

    private ContainerGridRenderer _gridRenderer = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private bool _wasMouseDown;

    private DragSourceKind _dragSource = DragSourceKind.None;
    private int _dragIndex = -1;
    private Point _dragPosition;

    private enum DragSourceKind
    {
        None,
        Player,
        Chest
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

        _gridRenderer = new ContainerGridRenderer(_atlas);
        _gridRenderer.LoadContent(device, _font);
    }

    public override void Update(float deltaTime)
    {
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

        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, PanelWidth, PanelHeight), new Color(35, 28, 24) * 0.97f);
        spriteBatch.Draw(_pixel, new Rectangle(panelX - 2, panelY - 2, PanelWidth + 4, PanelHeight + 4), Color.Black);

        string title = ChestRegistry.Get(_chest.VariantId)?.DisplayName ?? "Chest";
        spriteBatch.DrawString(_font, title, new Vector2(panelX + 18, panelY + 12), Color.Gold);
        spriteBatch.DrawString(_font, "Player Inventory", new Vector2(playerX, panelY + 42), Color.White);
        spriteBatch.DrawString(_font, "Chest Storage", new Vector2(chestX, panelY + 42), Color.White);
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

        int playerHit = _gridRenderer.HitTest(mousePos, _playerInventory.Capacity, PlayerColumns, playerX, playerY);
        if (playerHit >= 0 && _playerInventory.GetSlot(playerHit) != null)
        {
            _dragSource = DragSourceKind.Player;
            _dragIndex = playerHit;
            _dragPosition = mousePos;
            return;
        }

        int chestHit = _gridRenderer.HitTest(mousePos, _chest.Container.Capacity, ChestColumns, chestX, chestY);
        if (chestHit >= 0 && _chest.Container.GetSlot(chestHit) != null)
        {
            _dragSource = DragSourceKind.Chest;
            _dragIndex = chestHit;
            _dragPosition = mousePos;
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
        playerY = panelY + 62;
        chestX = panelX + 250;
        chestY = panelY + 62;
    }
}
