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
/// Castle interior scene. Single screen, player enters/exits via south door trigger.
/// Hosts the King NPC: proximity prompt + dialogue overlay + quest activation (NPC-01/02).
/// </summary>
public class CastleScene : Scene
{
    private TileMap _map = null!;
    private PlayerEntity _player = null!;
    private Texture2D _pixel = null!;
    private readonly string _fromScene;

    private NpcEntity? _king;
    private Texture2D? _kingSprite;
    private Texture2D? _kingPortrait;
    private InteractionPrompt _prompt = null!;
    private SpriteFont _font = null!;
    private bool _showPrompt;

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

        // King NPC (centre-north of castle map)
        try
        {
            _kingSprite = LoadTexture(device, "Content/Sprites/NPCs/king.png");
            _kingPortrait = LoadTexture(device, "Content/Sprites/Portraits/king.png");
            var kingPos = new Vector2(320, 100);
            _king = new NpcEntity("king", _kingSprite, _kingPortrait, kingPos);
            Console.WriteLine($"[CastleScene] King NPC spawned at ({kingPos.X},{kingPos.Y})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CastleScene] King asset load failed: {ex.Message}");
        }

        _font = Services.Content.Load<SpriteFont>("DefaultFont");
        _prompt = new InteractionPrompt();

        Console.WriteLine($"[CastleScene] Entered from {_fromScene}, spawn ({spawn.X},{spawn.Y})");
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

        // King NPC interaction
        _showPrompt = false;
        if (_king != null && _king.IsInInteractRange(_player.Position))
        {
            _showPrompt = true;
            if (input.IsKeyPressed(Keys.E))
            {
                var quest = Services.Quest;
                var state = quest?.State ?? MainQuestState.NotStarted;
                var lines = DialogueRegistry.Get("king", state);
                Action onClose = () =>
                {
                    if (quest != null && quest.State == MainQuestState.NotStarted)
                        quest.Activate();
                };
                Services.SceneManager.PushImmediate(new DialogueScene(Services, _king, lines, onClose));
                return;
            }
        }

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
        _king?.Draw(spriteBatch);

        spriteBatch.End();

        // Screen-space overlays: interaction prompt + quest tracker
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_showPrompt && _king != null && _font != null)
        {
            // Convert king world position to screen space via camera transform
            var cam = Services.Camera.GetTransformMatrix();
            var screenPos = Vector2.Transform(_king.Position, cam);
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
        _kingSprite?.Dispose();
        _kingPortrait?.Dispose();
        Console.WriteLine("[CastleScene] Unloaded");
    }
}
