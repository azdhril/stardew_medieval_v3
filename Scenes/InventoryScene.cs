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
/// Inventory overlay scene with unified view: equipment silhouette on the left,
/// 20-slot item grid on the right. Supports drag-and-drop between grid and
/// equipment slots. Opened via I key, closed via I or Escape.
/// </summary>
public class InventoryScene : Scene
{
    // Panel: equipment (left) + grid (right) side by side
    private const int PanelWidth = 380;
    private const int PanelHeight = 220;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private InventoryGridRenderer _gridRenderer = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    // Mouse tracking for drag
    private bool _wasMouseDown;

    public InventoryScene(ServiceContainer services, InventoryManager inventory, SpriteAtlas atlas)
        : base(services)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _gridRenderer = new InventoryGridRenderer(_inventory, _atlas);
        _gridRenderer.LoadContent(device, _font);

        _wasMouseDown = false;

        Console.WriteLine("[InventoryScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        // Close inventory on I or Escape
        if (input.IsKeyPressed(Keys.I) || input.IsKeyPressed(Keys.Escape))
        {
            _gridRenderer.CancelDrag();
            Services.SceneManager.PopImmediate();
            return;
        }

        // Calculate layout positions
        var viewport = Services.GraphicsDevice.Viewport;
        int panelX = (viewport.Width - PanelWidth) / 2;
        int panelY = (viewport.Height - PanelHeight) / 2;

        // Equipment on the left side of the panel
        int equipOffsetX = panelX + 20;
        int equipOffsetY = panelY + 30;

        // Grid on the right side of the panel
        int gridOffsetX = panelX + 130;
        int gridOffsetY = panelY + 30;

        // Drag and drop handling
        bool mouseDown = Mouse.GetState().LeftButton == ButtonState.Pressed;
        Point mousePos = input.MousePosition;

        if (mouseDown && !_wasMouseDown)
        {
            // Mouse just pressed — start drag
            _gridRenderer.HandleMouseDown(mousePos, gridOffsetX, gridOffsetY, equipOffsetX, equipOffsetY);
        }
        else if (mouseDown && _wasMouseDown)
        {
            // Mouse held — update drag position
            _gridRenderer.UpdateDrag(mousePos);
        }
        else if (!mouseDown && _wasMouseDown)
        {
            // Mouse released — drop
            _gridRenderer.HandleMouseUp(mousePos, gridOffsetX, gridOffsetY, equipOffsetX, equipOffsetY);
        }

        _wasMouseDown = mouseDown;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        int screenWidth = viewport.Width;
        int screenHeight = viewport.Height;

        // Semi-transparent dark background
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.6f);
        spriteBatch.End();

        // Panel
        int panelX = (screenWidth - PanelWidth) / 2;
        int panelY = (screenHeight - PanelHeight) / 2;

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

        // Divider line between equipment and grid
        int dividerX = panelX + 120;
        spriteBatch.Draw(_pixel,
            new Rectangle(dividerX, panelY + 25, 1, PanelHeight - 35),
            Color.Gray * 0.5f);

        // Equipment offset (left side)
        int equipOffsetX = panelX + 20;
        int equipOffsetY = panelY + 30;

        // Grid offset (right side)
        int gridOffsetX = panelX + 130;
        int gridOffsetY = panelY + 30;

        // Draw everything through the unified renderer
        _gridRenderer.Draw(spriteBatch, gridOffsetX, gridOffsetY, equipOffsetX, equipOffsetY);

        // Tooltip: show hovered item name at bottom
        DrawTooltip(spriteBatch, panelX, panelY);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[InventoryScene] Unloaded");
    }

    /// <summary>
    /// Draw item name tooltip at bottom of panel when hovering a slot.
    /// </summary>
    private void DrawTooltip(SpriteBatch sb, int panelX, int panelY)
    {
        var mousePos = Services.Input.MousePosition;
        var viewport = Services.GraphicsDevice.Viewport;

        int gridOffsetX = panelX + 130;
        int gridOffsetY = panelY + 30;

        // Check if hovering a grid slot
        string? tooltipText = null;
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            int col = i % 5;
            int row = i / 5;
            int x = gridOffsetX + col * 42; // SlotSize(40) + Padding(2)
            int y = gridOffsetY + row * 42;
            var slotRect = new Rectangle(x, y, 40, 40);

            if (slotRect.Contains(mousePos))
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
