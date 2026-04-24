using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Mocked pause/settings menu overlay. Shows Resume, Settings, and Quit options.
/// Opened via Escape when no other overlay is active. Closed via Escape or clicking Resume.
/// </summary>
public class PauseScene : Scene
{
    private const int PanelWidth = 220;
    private const int PanelHeight = 210;
    private const int ButtonWidth = 180;
    private const int ButtonHeight = 30;
    private const int ButtonSpacing = 10;

    private static readonly string[] Options = { "Resume", "Fullscreen", "Settings", "Quit" };

    private SpriteFontBase _font = null!;
    private Texture2D _pixel = null!;
    private int _hoveredIndex = -1;
    private bool _mouseWasDown;

    public PauseScene(ServiceContainer services) : base(services) { }

    public override void LoadContent()
    {
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);

        var device = Services.GraphicsDevice;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

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

        // Hit test buttons
        var viewport = Services.GraphicsDevice.Viewport;
        int panelX = (viewport.Width - PanelWidth) / 2;
        int panelY = (viewport.Height - PanelHeight) / 2;
        Point mousePos = input.MousePosition;

        _hoveredIndex = -1;
        for (int i = 0; i < Options.Length; i++)
        {
            var rect = GetButtonRect(i, panelX, panelY);
            if (rect.Contains(mousePos))
            {
                _hoveredIndex = i;
                break;
            }
        }

        // Edge-detected click
        bool mouseDown = Mouse.GetState().LeftButton == ButtonState.Pressed;
        bool clicked = mouseDown && !_mouseWasDown;
        _mouseWasDown = mouseDown;

        if (clicked && _hoveredIndex >= 0)
        {
            switch (_hoveredIndex)
            {
                case 0: // Resume
                    Services.SceneManager.PopImmediate();
                    return;
                case 1: // Fullscreen
                    // Close and reopen the menu around the toggle. Workaround for a
                    // DesktopGL quirk where the first IsFullScreen flip while an
                    // overlay scene is on top leaves the SDL window slightly
                    // mis-sized (visible as black bars). Popping to the gameplay
                    // scene first lets its Update/Draw commit the new state, then
                    // we re-push the menu so the user doesn't see a flash.
                    Services.SceneManager.PopImmediate();
                    Services.ToggleFullscreen?.Invoke();
                    // Defer the re-push so GameplayScene gets real Update frames
                    // while the OS commits the window resize. Synchronous push
                    // here leaves black bars on first fullscreen entry.
                    Services.SceneManager.PushAfter(new PauseScene(Services), 0.25f);
                    return;
                case 2: // Settings (mocked - just log)
                    Console.WriteLine("[PauseScene] Settings not yet implemented");
                    break;
                case 3: // Quit
                    Services.QuitGame?.Invoke();
                    return;
            }
        }
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

        // Buttons
        for (int i = 0; i < Options.Length; i++)
        {
            var rect = GetButtonRect(i, panelX, panelY);
            var bgColor = i == _hoveredIndex ? Color.Gray * 0.6f : Color.Gray * 0.3f;
            spriteBatch.Draw(_pixel, rect, bgColor);

            var textSize = _font.MeasureString(Options[i]);
            spriteBatch.DrawString(_font, Options[i],
                new Vector2(rect.X + (ButtonWidth - textSize.X) / 2, rect.Y + (ButtonHeight - textSize.Y) / 2),
                Color.White);
        }

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[PauseScene] Unloaded");
    }

    private Rectangle GetButtonRect(int index, int panelX, int panelY)
    {
        int x = panelX + (PanelWidth - ButtonWidth) / 2;
        int y = panelY + 40 + index * (ButtonHeight + ButtonSpacing);
        return new Rectangle(x, y, ButtonWidth, ButtonHeight);
    }
}
