using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.UI;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Overlay scene that wraps <see cref="ShopPanel"/> and a <see cref="Toast"/>.
/// Pushed on top of <see cref="ShopScene"/> after the Shopkeeper dialogue closes.
/// Esc pops back to gameplay.
/// </summary>
public class ShopOverlayScene : Scene
{
    private readonly ShopPanel _panel;
    private readonly Toast _toast;
    private Texture2D _pixel = null!;
    private SpriteFontBase _font = null!;
    private SpriteFontBase _titleFont = null!;

    public ShopOverlayScene(ServiceContainer services) : base(services)
    {
        var inv = services.Inventory ?? throw new InvalidOperationException(
            "[ShopOverlayScene] Services.Inventory is null.");
        var atlas = services.Atlas ?? throw new InvalidOperationException(
            "[ShopOverlayScene] Services.Atlas is null.");
        _panel = new ShopPanel(inv, atlas);
        _toast = new Toast();
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);
        _titleFont = Services.Fonts!.GetFont(FontRole.Bold, 24);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        Console.WriteLine("[ShopOverlayScene] Opened");
    }

    public override void Update(float deltaTime)
    {
        var vp = Services.GraphicsDevice.Viewport;
        if (_panel.Update(deltaTime, Services.Input, vp.Width, vp.Height, out var toastReq))
        {
            Console.WriteLine("[ShopOverlayScene] Closed -> SaveNow");
            GameStateSnapshot.SaveNow(Services);
            Services.SceneManager.PopImmediate();
            return;
        }
        if (toastReq != null)
            _toast.Show(toastReq.Text, toastReq.Color);
        _toast.Update(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var vp = Services.GraphicsDevice.Viewport;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _panel.Draw(spriteBatch, _font, _titleFont, _pixel, Services.Theme!, vp.Width, vp.Height);
        _toast.Draw(spriteBatch, _font, _pixel);
        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[ShopOverlayScene] Unloaded");
    }
}
