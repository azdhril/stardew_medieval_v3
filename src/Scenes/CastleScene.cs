using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Entities;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.UI;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Castle interior. Hosts the King NPC: proximity prompt + dialogue overlay +
/// quest activation (NPC-01/02). Cross-cutting concerns in <see cref="GameplayScene"/>.
/// </summary>
public class CastleScene : GameplayScene
{
    private NpcEntity? _king;
    private Texture2D? _kingSprite;
    private Texture2D? _kingPortrait;
    private InteractionPrompt _prompt = null!;
    private bool _showPrompt;

    private static readonly Dictionary<string, Vector2> Spawns = new()
    {
        ["Village"] = new Vector2(208, 416),
    };

    public CastleScene(ServiceContainer services, string fromScene) : base(services, fromScene) { }

    protected override string MapPath => "assets/Maps/castle.tmx";
    protected override string SceneName => "Castle";

    protected override Vector2 GetSpawn(string fromScene)
    {
        if (TryReadTmxSpawn(fromScene, out var tmxPos))
        {
            Console.WriteLine($"[CastleScene] Spawn from {fromScene} resolved via TMX at ({tmxPos.X},{tmxPos.Y})");
            return tmxPos;
        }
        if (Spawns.TryGetValue(fromScene, out var p))
        {
            Console.WriteLine($"[CastleScene] Spawn from {fromScene} resolved via dict at ({p.X},{p.Y})");
            return p;
        }
        var fallback = new Vector2(208, 416);
        Console.WriteLine($"[CastleScene] Spawn from {fromScene} no match - using default ({fallback.X},{fallback.Y})");
        return fallback;
    }

    protected override void OnLoad()
    {
        var device = Services.GraphicsDevice;
        try
        {
            _kingSprite = LoadTexture(device, "assets/Sprites/NPCs/king.png");
            _kingPortrait = LoadTexture(device, "assets/Sprites/Portraits/king.png");
            var kingPos = new Vector2(320, 100);
            _king = new NpcEntity("king", _kingSprite, _kingPortrait, kingPos);
            Console.WriteLine($"[CastleScene] King NPC spawned at ({kingPos.X},{kingPos.Y})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CastleScene] King asset load failed: {ex.Message}");
        }

        _prompt = new InteractionPrompt();
    }

    private static Texture2D LoadTexture(GraphicsDevice device, string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }

    protected override bool OnPreUpdate(float deltaTime, InputManager input)
    {
        _showPrompt = false;
        if (_king != null && _king.IsInInteractRange(Player.Position))
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
                return true;
            }
        }
        return false;
    }

    protected override void OnDrawWorldAfterPlayer(SpriteBatch sb, Rectangle viewArea)
    {
        _king?.Draw(sb);
    }

    protected override void OnDrawScreen(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        if (_showPrompt && _king != null)
        {
            var cam = Services.Camera.GetTransformMatrix();
            var screenPos = Vector2.Transform(_king.Position, cam);
            _prompt.Draw(sb, Font, Pixel, screenPos, "Press E to talk");
        }

        var qstate = Services.Quest?.State ?? MainQuestState.NotStarted;
        HUD.DrawQuestTracker(sb, Font, Pixel, qstate, viewportWidth);
    }

    protected override bool HandleTrigger(string triggerName)
    {
        if (triggerName == "exit_to_village")
        {
            Services.SceneManager.TransitionTo(new VillageScene(Services, "Castle"));
            return true;
        }
        return false;
    }

    protected override void OnUnload()
    {
        _kingSprite?.Dispose();
        _kingPortrait?.Dispose();
    }
}
