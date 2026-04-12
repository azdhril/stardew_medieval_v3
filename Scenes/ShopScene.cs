using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Entities;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Shop interior scene. Hosts the Shopkeeper NPC: proximity prompt → dialogue →
/// Buy/Sell overlay (<see cref="ShopOverlayScene"/>). See plan 04-04 for the
/// dialogue-then-shop flow.
/// </summary>
public class ShopScene : Scene
{
    private TileMap _map = null!;
    private PlayerEntity _player = null!;
    private Texture2D _pixel = null!;
    private readonly string _fromScene;

    private NpcEntity? _shopkeeper;
    private Texture2D? _shopkeeperSprite;
    private Texture2D? _shopkeeperPortrait;
    private InteractionPrompt _prompt = null!;
    private SpriteFont _font = null!;
    private bool _showPrompt;

    private static readonly Dictionary<string, Vector2> SpawnPoints = new()
    {
        ["Village"] = new Vector2(208, 416),
    };

    public ShopScene(ServiceContainer services, string fromScene) : base(services)
    {
        _fromScene = fromScene;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _map = new TileMap();
        _map.Load("Content/Maps/shop.tmx", device);

        _player = Services.Player ?? throw new InvalidOperationException(
            "[ShopScene] Services.Player is null.");

        var spawn = SpawnPoints.TryGetValue(_fromScene, out var p) ? p : new Vector2(208, 416);
        _player.Position = spawn;

        Services.Camera.Zoom = 3f;
        Services.Camera.Bounds = _map.GetWorldBounds();
        Services.Camera.Follow(_player.Position, 0f);

        if (Services.GameState != null)
            Services.GameState.CurrentScene = "Shop";

        // Shopkeeper NPC (centre of shop map)
        try
        {
            _shopkeeperSprite = LoadTexture(device, "Content/Sprites/NPCs/shopkeeper.png");
            _shopkeeperPortrait = LoadTexture(device, "Content/Sprites/Portraits/shopkeeper.png");
            var pos = new Vector2(320, 200);
            _shopkeeper = new NpcEntity("shopkeeper", _shopkeeperSprite, _shopkeeperPortrait, pos);
            Console.WriteLine($"[ShopScene] Shopkeeper NPC spawned at ({pos.X},{pos.Y})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopScene] Shopkeeper asset load failed: {ex.Message}");
        }

        _font = Services.Content.Load<SpriteFont>("DefaultFont");
        _prompt = new InteractionPrompt();

        Console.WriteLine($"[ShopScene] Entered from {_fromScene}, spawn ({spawn.X},{spawn.Y})");
    }

    private static Texture2D LoadTexture(GraphicsDevice device, string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
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

        // Shopkeeper NPC interaction: E → dialogue → onClose pushes ShopOverlayScene
        _showPrompt = false;
        if (_shopkeeper != null && _shopkeeper.IsInInteractRange(_player.Position))
        {
            _showPrompt = true;
            if (input.IsKeyPressed(Keys.E))
            {
                var state = Services.Quest?.State ?? MainQuestState.NotStarted;
                var lines = DialogueRegistry.Get("shopkeeper", state);
                Action onClose = () =>
                {
                    Services.SceneManager.PushImmediate(new ShopOverlayScene(Services));
                };
                Services.SceneManager.PushImmediate(
                    new DialogueScene(Services, _shopkeeper, lines, onClose));
                return;
            }
        }

        var pBox = _player.CollisionBox;
        foreach (var t in _map.Triggers)
        {
            if (!pBox.Intersects(t.Bounds)) continue;
            if (t.Name == "exit_to_village")
            {
                Services.SceneManager.TransitionTo(new VillageScene(Services, "Shop"));
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
        _shopkeeper?.Draw(spriteBatch);

        spriteBatch.End();

        // Screen-space overlays: interaction prompt + quest tracker
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_showPrompt && _shopkeeper != null && _font != null)
        {
            var cam = Services.Camera.GetTransformMatrix();
            var screenPos = Vector2.Transform(_shopkeeper.Position, cam);
            _prompt.Draw(spriteBatch, _font, _pixel, screenPos, "Press E to talk");
        }

        if (_font != null)
        {
            var state = Services.Quest?.State ?? MainQuestState.NotStarted;
            HUD.DrawQuestTracker(spriteBatch, _font, _pixel, state, viewport.Width);
        }

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        _shopkeeperSprite?.Dispose();
        _shopkeeperPortrait?.Dispose();
        Console.WriteLine("[ShopScene] Unloaded");
    }
}
