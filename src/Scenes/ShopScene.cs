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
/// Shop interior. Shopkeeper NPC: proximity prompt → dialogue → ShopOverlayScene.
/// Cross-cutting concerns in <see cref="GameplayScene"/>.
/// </summary>
public class ShopScene : GameplayScene
{
    private NpcEntity? _shopkeeper;
    private Texture2D? _shopkeeperSprite;
    private Texture2D? _shopkeeperPortrait;
    private InteractionPrompt _prompt = null!;
    private bool _showPrompt;

    private static readonly Dictionary<string, Vector2> Spawns = new()
    {
        ["Village"] = new Vector2(208, 416),
    };

    public ShopScene(ServiceContainer services, string fromScene) : base(services, fromScene) { }

    protected override string MapPath => "assets/Maps/shop.tmx";
    protected override string SceneName => "Shop";

    protected override Vector2 GetSpawn(string fromScene) =>
        Spawns.TryGetValue(fromScene, out var p) ? p : new Vector2(208, 416);

    protected override void OnLoad()
    {
        var device = Services.GraphicsDevice;
        try
        {
            _shopkeeperSprite = LoadTexture(device, "assets/Sprites/NPCs/shopkeeper.png");
            _shopkeeperPortrait = LoadTexture(device, "assets/Sprites/Portraits/shopkeeper.png");
            var pos = new Vector2(320, 200);
            _shopkeeper = new NpcEntity("shopkeeper", _shopkeeperSprite, _shopkeeperPortrait, pos);
            Console.WriteLine($"[ShopScene] Shopkeeper NPC spawned at ({pos.X},{pos.Y})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopScene] Shopkeeper asset load failed: {ex.Message}");
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
        if (_shopkeeper != null && _shopkeeper.IsInInteractRange(Player.Position))
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
                return true;
            }
        }
        return false;
    }

    protected override void OnDrawWorldAfterPlayer(SpriteBatch sb, Rectangle viewArea)
    {
        _shopkeeper?.Draw(sb);
    }

    protected override void OnDrawScreen(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        if (_showPrompt && _shopkeeper != null)
        {
            var cam = Services.Camera.GetTransformMatrix();
            var screenPos = Vector2.Transform(_shopkeeper.Position, cam);
            _prompt.Draw(sb, Font, Pixel, screenPos, "Press E to talk");
        }

        var qstate = Services.Quest?.State ?? MainQuestState.NotStarted;
        HUD.DrawQuestTracker(sb, Font, Pixel, qstate, viewportWidth);
    }

    protected override bool HandleTrigger(string triggerName)
    {
        if (triggerName == "exit_to_village")
        {
            Services.SceneManager.TransitionTo(new VillageScene(Services, "Shop"));
            return true;
        }
        return false;
    }

    protected override void OnUnload()
    {
        _shopkeeperSprite?.Dispose();
        _shopkeeperPortrait?.Dispose();
    }
}
