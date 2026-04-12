using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Castle interior scene. Single screen, player enters/exits via south door trigger.
/// King NPC lands in Plan 04-03; this is a shell.
/// </summary>
public class CastleScene : Scene
{
    private TileMap _map = null!;
    private PlayerEntity _player = null!;
    private Texture2D _pixel = null!;
    private readonly string _fromScene;

    private static readonly Dictionary<string, Vector2> SpawnPoints = new()
    {
        ["Village"] = new Vector2(208, 416),
    };

    public CastleScene(ServiceContainer services, string fromScene) : base(services)
    {
        _fromScene = fromScene;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _map = new TileMap();
        _map.Load("Content/Maps/castle.tmx", device);

        _player = Services.Player ?? throw new InvalidOperationException(
            "[CastleScene] Services.Player is null.");

        var spawn = SpawnPoints.TryGetValue(_fromScene, out var p) ? p : new Vector2(208, 416);
        _player.Position = spawn;

        Services.Camera.Zoom = 3f;
        Services.Camera.Bounds = _map.GetWorldBounds();
        Services.Camera.Follow(_player.Position, 0f);

        if (Services.GameState != null)
            Services.GameState.CurrentScene = "Castle";

        Console.WriteLine($"[CastleScene] Entered from {_fromScene}, spawn ({spawn.X},{spawn.Y})");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        if (input.IsKeyPressed(Keys.Escape))
        {
            Services.SceneManager.PushImmediate(new PauseScene(Services));
            return;
        }

        Services.Time.Update(deltaTime);
        _player.Update(deltaTime, input.Movement, _map, null);
        Services.Camera.Follow(_player.Position, deltaTime);

        var pBox = _player.CollisionBox;
        foreach (var t in _map.Triggers)
        {
            if (!pBox.Intersects(t.Bounds)) continue;
            if (t.Name == "exit_to_village")
            {
                Services.SceneManager.TransitionTo(new VillageScene(Services, "Castle"));
                return;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var device = Services.GraphicsDevice;
        var viewport = device.Viewport;
        var transform = Services.Camera.GetTransformMatrix();

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            null, null, null, transform);

        var topLeft = Services.Camera.ScreenToWorld(Vector2.Zero);
        var bottomRight = Services.Camera.ScreenToWorld(new Vector2(viewport.Width, viewport.Height));
        var viewArea = new Rectangle(
            (int)topLeft.X - 16, (int)topLeft.Y - 16,
            (int)(bottomRight.X - topLeft.X) + 32,
            (int)(bottomRight.Y - topLeft.Y) + 32);

        _map.Draw(spriteBatch, viewArea);
        _player.Draw(spriteBatch);

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[CastleScene] Unloaded");
    }
}
