using System;
using FontStashSharp;
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
///
/// Per quick 260424-2af: this scene has NO standalone clickable buttons — only
/// the drag-driven slot grid plus a slot-hover tooltip. Left-click EDGE detection
/// uses <see cref="InputManager.IsLeftClickPressed"/> while press-HELD detection
/// (for drag continuation) continues via <see cref="Mouse.GetState"/>, matching
/// the ChestScene pattern. <c>_wasMouseDown</c> is retained solely for MouseUp
/// edge detection in the drag state machine (legitimate per RESEARCH audit).
/// </summary>
public class InventoryScene : Scene
{
    private const int PanelWidth = 735;
    private const int PanelHeight = 447;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;
    private readonly HotbarRenderer _hotbar;

    private InventoryGridRenderer _gridRenderer = null!;
    private SpriteFontBase _font = null!;
    private Texture2D _pixel = null!;
    private UITheme _theme = null!;

    // Drag state-machine tracker — legitimate use per RESEARCH audit.
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
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        _theme = Services.Theme;

        _gridRenderer = new InventoryGridRenderer(_inventory, _atlas);
        _gridRenderer.LoadContent(device, _font);
        _gridRenderer.SetTheme(_theme);

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
        int equipX = panelX + 28;
        int equipY = panelY + 30;
        int gridX = panelX + 160;
        int gridY = panelY + 35;

        // Press-held detection for drag continuation. Edge detection flows via
        // InputManager.IsLeftClickPressed for the initial press; MouseUp edge
        // is detected by comparing the current held state to _wasMouseDown.
        var ms = Mouse.GetState();
        bool mouseDown = ms.LeftButton == ButtonState.Pressed;
        Point mousePos = input.MousePosition;

        if (input.IsLeftClickPressed)
            _gridRenderer.HandleMouseDown(mousePos, gridX, gridY, equipX, equipY);
        else if (mouseDown && _wasMouseDown)
            _gridRenderer.UpdateDrag(mousePos);
        else if (!mouseDown && _wasMouseDown)
            _gridRenderer.HandleMouseUp(mousePos, gridX, gridY, equipX, equipY);

        // _wasMouseDown retained SOLELY for drag MouseUp edge detection
        // (per RESEARCH audit — legitimate drag-state use, not button click tracking).
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

        // Window frame (uniform pixel scale, not 9-slice)
        spriteBatch.Draw(_theme.PanePopup,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            Color.White);

        // Title plaque (9-slice, horizontal) centered at the top of the panel
        const int titlePlaqueW = 180;
        const int titlePlaqueH = 28;
        var titleRect = new Rectangle(
            panelX + (PanelWidth - titlePlaqueW) / 2,
            panelY - 6,                 // slightly overlapping the top edge
            titlePlaqueW, titlePlaqueH);
        NineSlice.Draw(spriteBatch, _theme.PanelTitle, titleRect, _theme.PanelTitleInsets);

        string title = "Inventory";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(titleRect.X + (titleRect.Width - titleSize.X) / 2,
                        titleRect.Y + (titleRect.Height - titleSize.Y) / 2),
            Color.White);

        // Divider between equipment cluster (left) and inventory grid (right).
        NineSlice.DrawStretched(spriteBatch, _theme.ImageDeco,
            new Rectangle(panelX + 150, panelY + 25, 1, PanelHeight - 45));

        int equipX = panelX + 28;
        int equipY = panelY + 30;
        int gridX = panelX + 160;
        int gridY = panelY + 35;

        _gridRenderer.Draw(spriteBatch, gridX, gridY, equipX, equipY);

        // Tooltip
        DrawTooltip(spriteBatch, panelX, panelY, gridX, gridY);

        // Focus outline + widget tooltip overlay (no widgets registered today;
        // no-op in practice, but future-safe and consistent with migrated scenes).
        Ui.Draw(spriteBatch, _pixel, _font, screenWidth, screenHeight);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
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
