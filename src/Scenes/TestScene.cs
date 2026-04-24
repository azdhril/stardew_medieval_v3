using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Entities;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Placeholder test scene. Proves scene transitions work.
/// Press B to return to FarmScene.
/// </summary>
public class TestScene : Scene
{
    private Texture2D _pixel = null!;
    private SpriteFontBase _font = null!;
    private DummyNpc _npc = null!;

    public TestScene(ServiceContainer services) : base(services) { }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Services.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Services.Fonts!.GetFont(FontRole.Body, 12);

        var viewport = Services.GraphicsDevice.Viewport;
        _npc = new DummyNpc(_pixel, new Vector2(viewport.Width / 2f, viewport.Height / 2f + 60f));

        Console.WriteLine("[TestScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        _npc.Update(deltaTime);

        if (Services.Input.IsKeyPressed(Keys.B))
        {
            Services.SceneManager.Pop();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;

        // Dark blue background
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(20, 20, 60));

        // Label text
        string text = "Test Scene - Press B to go back";
        var textSize = _font.MeasureString(text);
        spriteBatch.DrawString(_font, text,
            new Vector2(viewport.Width / 2f - textSize.X / 2f, viewport.Height / 2f - textSize.Y / 2f),
            Color.White);

        // Entity test label
        string npcLabel = "DummyNpc (Entity test)";
        var npcLabelSize = _font.MeasureString(npcLabel);
        spriteBatch.DrawString(_font, npcLabel,
            new Vector2(viewport.Width / 2f - npcLabelSize.X / 2f, viewport.Height / 2f + textSize.Y / 2f + 4f),
            Color.LightGreen);

        // Draw DummyNpc on top of background
        _npc.Draw(spriteBatch);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[TestScene] Unloaded");
    }
}
