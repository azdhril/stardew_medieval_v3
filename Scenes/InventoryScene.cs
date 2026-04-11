using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Inventory overlay scene: equipment silhouette on the left, 20-slot item grid on the right.
/// Dragging items to the hotbar at the bottom of the screen sets hotbar references.
/// The hotbar itself is rendered by FarmScene underneath this overlay.
/// </summary>
public class InventoryScene : Scene
{
    private const int PanelWidth = 380;
    private const int PanelHeight = 220;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;
    private readonly HotbarRenderer _hotbar;

    private InventoryGridRenderer _gridRenderer = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private bool _wasMouseDown;

    public InventoryScene(ServiceContainer services, InventoryManager inventory, SpriteAtlas atlas, HotbarRenderer hotbar)
        : base(services)
    {
        _inventory = inventory;
        _atlas = atlas;
        _hotbar = hotbar;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _gridRenderer = new InventoryGridRenderer(_inventory, _atlas);
        _gridRenderer.LoadContent(device, _font);

        var viewport = device.Viewport;
        _gridRenderer.SetHotbar(_hotbar, viewport.Width, viewport.Height);

        _wasMouseDown = false;
        Console.WriteLine("[InventoryScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        if (input.IsKeyPressed(Keys.I) || input.IsKeyPressed(Keys.Escape))
        {
            _gridRenderer.CancelDrag();
            Services.SceneManager.PopImmediate();
            return;
        }

        int panelX, panelY;
        GetPanelPosition(out panelX, out panelY);
        int equipX = panelX + 30;
        int equipY = panelY + 35;
        int gridX = panelX + 125;
        int gridY = panelY + 30;

        bool mouseDown = Mouse.GetState().LeftButton == ButtonState.Pressed;
        Point mousePos = input.MousePosition;

        if (mouseDown && !_wasMouseDown)
            _gridRenderer.HandleMouseDown(mousePos, gridX, gridY, equipX, equipY);
        else if (mouseDown && _wasMouseDown)
            _gridRenderer.UpdateDrag(mousePos);
        else if (!mouseDown && _wasMouseDown)
            _gridRenderer.HandleMouseUp(mousePos, gridX, gridY, equipX, equipY);

        _wasMouseDown = mouseDown;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        int screenWidth = viewport.Width;
        int screenHeight = viewport.Height;

        // Dim background
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.6f);
        spriteBatch.End();

        int panelX, panelY;
        GetPanelPosition(out panelX, out panelY);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Panel background
        spriteBatch.Draw(_pixel,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            Color.DarkSlateGray * 0.9f);

        // Title
        string title = "Inventory";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(panelX + (PanelWidth - titleSize.X) / 2, panelY + 6),
            Color.White);

        // Divider
        spriteBatch.Draw(_pixel,
            new Rectangle(panelX + 115, panelY + 25, 1, PanelHeight - 35),
            Color.Gray * 0.5f);

        int equipX = panelX + 30;
        int equipY = panelY + 35;
        int gridX = panelX + 125;
        int gridY = panelY + 30;

        _gridRenderer.Draw(spriteBatch, gridX, gridY, equipX, equipY);

        // Tooltip
        DrawTooltip(spriteBatch, panelX, panelY, gridX, gridY);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[InventoryScene] Unloaded");
    }

    private void GetPanelPosition(out int panelX, out int panelY)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        panelX = (viewport.Width - PanelWidth) / 2;
        // Center vertically but shift up a bit so hotbar below is visible
        panelY = (viewport.Height - PanelHeight) / 2 - 30;
    }

    private void DrawTooltip(SpriteBatch sb, int panelX, int panelY, int gridX, int gridY)
    {
        var mousePos = Services.Input.MousePosition;
        string? tooltipText = null;

        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            int col = i % 5;
            int row = i / 5;
            int x = gridX + col * 42;
            int y = gridY + row * 42;
            if (new Rectangle(x, y, 40, 40).Contains(mousePos))
            {
                var stack = _inventory.GetSlot(i);
                if (stack != null)
                {
                    var def = ItemRegistry.Get(stack.ItemId);
                    if (def != null)
                        tooltipText = $"{def.Name} ({def.Rarity})";
                }
                break;
            }
        }

        if (tooltipText != null)
        {
            var textSize = _font.MeasureString(tooltipText);
            sb.DrawString(_font, tooltipText,
                new Vector2(panelX + (PanelWidth - textSize.X) / 2, panelY + PanelHeight - 18),
                Color.LightGoldenrodYellow);
        }
    }
}
