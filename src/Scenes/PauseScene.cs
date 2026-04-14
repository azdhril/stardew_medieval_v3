using System;
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
    private const int PanelWidth = 200;
    private const int PanelHeight = 160;
    private const int ButtonWidth = 160;
    private const int ButtonHeight = 30;
    private const int ButtonSpacing = 10;

    private static readonly string[] Options = { "Resume", "Settings", "Quit" };

    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private int _hoveredIndex = -1;

    public PauseScene(ServiceContainer services) : base(services) { }

    public override void LoadContent()
    {
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

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

        // Click handling
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && _hoveredIndex >= 0)
        {
            switch (_hoveredIndex)
            {
                case 0: // Resume
                    Services.SceneManager.PopImmediate();
                    return;
                case 1: // Settings (mocked - just log)
                    Console.WriteLine("[PauseScene] Settings not yet implemented");
                    break;
                case 2: // Quit
                    Console.WriteLine("[PauseScene] Quit requested");
                    break;
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
