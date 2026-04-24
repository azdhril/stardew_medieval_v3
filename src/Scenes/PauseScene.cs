using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.UI.Widgets;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Mocked pause/settings menu overlay. Shows Resume, Fullscreen, Settings, and
/// Quit options. Opened via Escape when no other overlay is active. Closed via
/// Escape or clicking Resume. Migrated to the UI Widgets framework (quick
/// 260424-2af): all 4 buttons are <see cref="TextButton"/> widgets registered
/// with the scene-owned <see cref="Scene.Ui"/>; hit-test, hover, cursor, Tab
/// navigation and Enter/Space activation flow through <see cref="UIManager"/>.
/// </summary>
public class PauseScene : Scene
{
    private const int PanelWidth = 220;
    private const int PanelHeight = 210;
    private const int ButtonWidth = 180;
    private const int ButtonHeight = 30;
    private const int ButtonSpacing = 10;

    private SpriteFontBase _font = null!;
    private Texture2D _pixel = null!;

    private TextButton _resumeBtn = null!;
    private TextButton _fullscreenBtn = null!;
    private TextButton _settingsBtn = null!;
    private TextButton _quitBtn = null!;

    public PauseScene(ServiceContainer services) : base(services) { }

    public override void LoadContent()
    {
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);

        var device = Services.GraphicsDevice;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Lazy-init theme mirror (matches ChestScene/InventoryScene pattern).
        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        var theme = Services.Theme;

        _resumeBtn     = new TextButton("Resume",     theme.YellowBtnSmall, theme.YellowBtnSmallInsets, _font) { OnClickAction = OnResume };
        _fullscreenBtn = new TextButton("Fullscreen", theme.YellowBtnSmall, theme.YellowBtnSmallInsets, _font) { OnClickAction = OnFullscreen };
        _settingsBtn   = new TextButton("Settings",   theme.YellowBtnSmall, theme.YellowBtnSmallInsets, _font) { OnClickAction = OnSettings };
        _quitBtn       = new TextButton("Quit",       theme.YellowBtnSmall, theme.YellowBtnSmallInsets, _font) { OnClickAction = OnQuit };

        Ui.Register(_resumeBtn);
        Ui.Register(_fullscreenBtn);
        Ui.Register(_settingsBtn);
        Ui.Register(_quitBtn);

        Console.WriteLine("[PauseScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        if (input.IsKeyPressed(Keys.Escape))
        {
            Services.SceneManager.PopImmediate();
            return;
        }

        // Recompute button bounds each frame (viewport-responsive — Pitfall 3).
        var viewport = Services.GraphicsDevice.Viewport;
        int panelX = (viewport.Width - PanelWidth) / 2;
        int panelY = (viewport.Height - PanelHeight) / 2;
        _resumeBtn.Bounds     = GetButtonRect(0, panelX, panelY);
        _fullscreenBtn.Bounds = GetButtonRect(1, panelX, panelY);
        _settingsBtn.Bounds   = GetButtonRect(2, panelX, panelY);
        _quitBtn.Bounds       = GetButtonRect(3, panelX, panelY);

        // Widgets handle all clicks — no scene-level click fallback needed.
        Ui.Update(deltaTime, input);
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
            Color.Black * 0.5f);
        spriteBatch.End();

        int panelX = (screenWidth - PanelWidth) / 2;
        int panelY = (screenHeight - PanelHeight) / 2;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Panel background
        spriteBatch.Draw(_pixel,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            Color.DarkSlateGray * 0.95f);

        // Title
        string title = "Paused";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(panelX + (PanelWidth - titleSize.X) / 2, panelY + 10),
            Color.White);

        // Buttons (widget Draw each).
        _resumeBtn.Draw(spriteBatch);
        _fullscreenBtn.Draw(spriteBatch);
        _settingsBtn.Draw(spriteBatch);
        _quitBtn.Draw(spriteBatch);

        // Focus outline + tooltip overlay.
        Ui.Draw(spriteBatch, _pixel, _font, screenWidth, screenHeight);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _pixel?.Dispose();
        Console.WriteLine("[PauseScene] Unloaded");
    }

    private Rectangle GetButtonRect(int index, int panelX, int panelY)
    {
        int x = panelX + (PanelWidth - ButtonWidth) / 2;
        int y = panelY + 40 + index * (ButtonHeight + ButtonSpacing);
        return new Rectangle(x, y, ButtonWidth, ButtonHeight);
    }

    private void OnResume()
    {
        Services.SceneManager.PopImmediate();
    }

    private void OnFullscreen()
    {
        // Close and reopen the menu around the toggle. Workaround for a
        // DesktopGL quirk where the first IsFullScreen flip while an overlay
        // scene is on top leaves the SDL window slightly mis-sized (visible as
        // black bars). Popping to the gameplay scene first lets its
        // Update/Draw commit the new state, then we re-push the menu so the
        // user doesn't see a flash.
        Services.SceneManager.PopImmediate();
        Services.ToggleFullscreen?.Invoke();
        // Defer the re-push so GameplayScene gets real Update frames while the
        // OS commits the window resize. Synchronous push here leaves black
        // bars on first fullscreen entry.
        Services.SceneManager.PushAfter(new PauseScene(Services), 0.25f);
    }

    private void OnSettings()
    {
        Console.WriteLine("[PauseScene] Settings not yet implemented");
    }

    private void OnQuit()
    {
        Services.QuitGame?.Invoke();
    }
}
