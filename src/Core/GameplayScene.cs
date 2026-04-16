using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Scenes;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Abstract base for gameplay scenes (Farm, Village, Castle, Shop, ...).
/// Owns the cross-cutting concerns: map load, shared player, camera setup,
/// global input (inventory, hotbar, consumable, pause, debug kit), HUD/hotbar
/// rendering, and trigger dispatch. Subclasses supply map path, scene name,
/// per-entry spawn, and per-scene hooks.
/// </summary>
public abstract class GameplayScene : Scene
{
    protected TileMap Map = null!;
    protected PlayerEntity Player = null!;
    protected Texture2D Pixel = null!;
    protected SpriteFont Font = null!;
    protected string FromScene { get; }

    /// <summary>Periodic auto-save interval in seconds.</summary>
    private const float AutoSaveInterval = 30f;

    /// <summary>Accumulator for periodic auto-save timer.</summary>
    private float _autoSaveAccumulator;

    /// <summary>Guard flag to prevent duplicate OnLevelUp save subscriptions.</summary>
    private bool _levelUpSaveSubscribed;

    /// <summary>TMX path loaded in LoadContent (e.g., "assets/Maps/village.tmx").</summary>
    protected abstract string MapPath { get; }

    /// <summary>GameState.CurrentScene value (e.g., "Village"). Used for saves.</summary>
    protected abstract string SceneName { get; }

    protected GameplayScene(ServiceContainer services, string fromScene) : base(services)
    {
        FromScene = fromScene;
    }

    /// <summary>Preferred zoom when map is larger than viewport (scales up to fit otherwise).</summary>
    protected virtual float PreferredZoom => 3.0f;

    /// <summary>
    /// Set camera zoom to max(PreferredZoom, minZoomToFillViewport). Guarantees
    /// no black bars when the window/viewport exceeds the map dimensions.
    /// </summary>
    private void ApplyFitZoom() => Services.Camera.FitZoomToViewport(PreferredZoom);

    /// <summary>Spawn position for this entry. Default keeps current player position.</summary>
    protected virtual Vector2 GetSpawn(string fromScene) => Services.Player?.Position ?? Vector2.Zero;

    /// <summary>
    /// Look up a TMX "Spawn" object-group entry named <c>from_&lt;prev&gt;</c>
    /// (case-insensitive on the prev portion). Returns true and the object's
    /// center Point on hit. Callers typically fall back to a hardcoded dict.
    /// Mirrors the pattern used by <see cref="DungeonScene.GetSpawn"/>.
    /// </summary>
    protected bool TryReadTmxSpawn(string fromScene, out Vector2 pos)
    {
        pos = Vector2.Zero;
        if (Map == null || string.IsNullOrEmpty(fromScene)) return false;

        var spawns = Map.GetObjectGroup("Spawn");
        string key = $"from_{fromScene}";
        foreach (var s in spawns)
        {
            if (string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                pos = s.Point;
                return true;
            }
        }
        return false;
    }

    /// <summary>Called at end of LoadContent for subclass-specific setup.</summary>
    protected virtual void OnLoad() { }

    /// <summary>
    /// Called before player movement each frame. Return true to short-circuit
    /// the rest of Update (e.g., a scene-transition push was enqueued).
    /// </summary>
    protected virtual bool OnPreUpdate(float deltaTime, InputManager input) => false;

    /// <summary>Called after player movement and trigger dispatch each frame.</summary>
    protected virtual void OnPostUpdate(float deltaTime, InputManager input) { }

    /// <summary>World-space draw before the player sprite (tiles/overlays/crops/entities behind player).</summary>
    protected virtual void OnDrawWorld(SpriteBatch sb, Rectangle viewArea) { }

    /// <summary>World-space draw after the player sprite (projectiles, slash FX, debug rects).</summary>
    protected virtual void OnDrawWorldAfterPlayer(SpriteBatch sb, Rectangle viewArea) { }

    /// <summary>Screen-space draw after HUD and hotbar (boss bar, NPC prompt, quest tracker, etc.).</summary>
    protected virtual void OnDrawScreen(SpriteBatch sb, int viewportWidth, int viewportHeight) { }

    /// <summary>
    /// Handle a named trigger zone the player just stepped into. Return true if
    /// handled (caller will stop iterating triggers this frame).
    /// </summary>
    protected virtual bool HandleTrigger(string triggerName) => false;

    /// <summary>Solids (enemies/boss) for player collision. Default: none.</summary>
    protected virtual IEnumerable<Entity>? GetSolids() => null;

    /// <summary>Subclass cleanup. Base disposes Pixel.</summary>
    protected virtual void OnUnload() { }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;

        Pixel = new Texture2D(device, 1, 1);
        Pixel.SetData(new[] { Color.White });

        try
        {
            Font = Services.Content.Load<SpriteFont>("NotoSerif");
        }
        catch
        {
            Console.WriteLine("[GameplayScene] NotoSerif font not found, falling back to DefaultFont");
            Font = Services.Content.Load<SpriteFont>("DefaultFont");
        }

        Map = new TileMap();
        Map.Load(MapPath, device);

        // Subclass sets up systems (may create Player on first entry).
        OnLoad();

        // Player must exist by now. FarmScene creates it in OnLoad on first entry;
        // hub scenes rely on Services.Player being populated before transition.
        Player = Services.Player ?? throw new InvalidOperationException(
            $"[{SceneName}Scene] Services.Player is null. FarmScene must initialize player first.");

        Player.Position = GetSpawn(FromScene);

        Services.Camera.Bounds = Map.GetWorldBounds();
        ApplyFitZoom();
        Services.Camera.SnapTo(Player.Position);

        if (Services.GameState != null)
            Services.GameState.CurrentScene = SceneName;

        // Lazily create the shared toast so death/level-up messages survive scene transitions.
        Services.Toast ??= new UI.Toast();

        // Subscribe to level-up for auto-save (once per GameplayScene lifetime).
        if (!_levelUpSaveSubscribed && Services.Progression != null)
        {
            Services.Progression.OnLevelUp += (_) =>
            {
                GameStateSnapshot.SaveNow(Services);
                Console.WriteLine($"[{SceneName}Scene] Auto-save (level-up)");
            };
            _levelUpSaveSubscribed = true;
        }

        Console.WriteLine($"[{SceneName}Scene] Entered from {FromScene}, spawn ({Player.Position.X},{Player.Position.Y})");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;
        var viewport = Services.GraphicsDevice.Viewport;

        // --- Global input (works in every gameplay scene) ---

        if (input.IsKeyPressed(Keys.F5))
        {
            GameStateSnapshot.SaveNow(Services);
            Console.WriteLine($"[{SceneName}Scene] F5 manual save");
        }

        // Periodic auto-save (D-20, D-21)
        _autoSaveAccumulator += deltaTime;
        if (_autoSaveAccumulator >= AutoSaveInterval)
        {
            _autoSaveAccumulator = 0f;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            GameStateSnapshot.SaveNow(Services);
            sw.Stop();
            Console.WriteLine($"[{SceneName}Scene] Auto-save (periodic, {sw.ElapsedMilliseconds}ms)");
        }

        if (input.IsKeyPressed(Keys.Escape))
        {
            Services.Hotbar?.CancelDrag();
            Services.SceneManager.PushImmediate(new PauseScene(Services));
            return;
        }

        if (input.IsKeyPressed(Keys.I))
        {
            Services.Hotbar?.CancelDrag();
            if (Services.Inventory != null && Services.Atlas != null && Services.Hotbar != null)
                Services.SceneManager.PushImmediate(
                    new InventoryScene(Services, Services.Inventory, Services.Atlas, Services.Hotbar));
            return;
        }

        if (input.IsKeyPressed(Keys.F2))
            GrantDebugKit();

        for (int i = 0; i < InventoryManager.HotbarSize; i++)
        {
            if (input.IsKeyPressed(Keys.D1 + i))
                Services.Inventory?.SetActiveHotbar(i);
        }

        int wheel = Math.Sign(input.ScrollWheelDelta);
        if (wheel != 0)
            Services.Inventory?.CycleActiveHotbar(-wheel);

        if (input.IsKeyPressed(Keys.Q))
        {
            var use = Services.Inventory?.UseConsumable(0) ?? ConsumableUseResult.None;
            if (use.Consumed && Player != null)
            {
                if (use.HealAmount > 0f)
                    Player.HP = Math.Min(Player.MaxHP, Player.HP + use.HealAmount);

                if (use.StaminaRestorePct > 0f)
                {
                    float staminaAmount = Player.Stats.MaxStamina * use.StaminaRestorePct;
                    Player.Stats.RestoreStamina(staminaAmount);
                }

                Console.WriteLine(
                    $"[{SceneName}Scene] Used consumable Q: {use.ItemId} " +
                    $"(heal={use.HealAmount:F0}, stamina={use.StaminaRestorePct * 100f:F0}%)");
            }
        }

        Services.Hotbar?.Update(input.MousePosition, viewport.Width, viewport.Height);

        // Keep zoom large enough to fill viewport (handles fullscreen/resize).
        ApplyFitZoom();

        // --- Time and subclass pre-update ---
        Services.Time.Update(deltaTime);

        if (OnPreUpdate(deltaTime, input)) return;

        // --- Player movement ---
        Player.Update(deltaTime, input.Movement, input.IsRunHeld, Map, GetSolids());
        Services.Camera.Follow(Player.Position, deltaTime);

        // --- Trigger dispatch ---
        var pBox = Player.CollisionBox;
        foreach (var t in Map.Triggers)
        {
            if (!pBox.Intersects(t.Bounds)) continue;
            if (HandleTrigger(t.Name)) return;
        }

        // --- Subclass post-update ---
        OnPostUpdate(deltaTime, input);

        // Shared toast (death penalty, level-up, etc.)
        Services.Toast?.Update(deltaTime);
    }

    public override void Draw(SpriteBatch sb)
    {
        var device = Services.GraphicsDevice;

        // Force the viewport to match the backbuffer every frame — some paths
        // (fullscreen toggle during a paused overlay) leave Viewport stale for
        // a frame, which manifests as black bars around the world.
        int bbW = device.PresentationParameters.BackBufferWidth;
        int bbH = device.PresentationParameters.BackBufferHeight;
        if (device.Viewport.Width != bbW || device.Viewport.Height != bbH)
            device.Viewport = new Viewport(0, 0, bbW, bbH);

        ApplyFitZoom();

        var viewport = device.Viewport;
        var transform = Services.Camera.GetTransformMatrix();

        // World space
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            null, null, null, transform);

        var topLeft = Services.Camera.ScreenToWorld(Vector2.Zero);
        var bottomRight = Services.Camera.ScreenToWorld(new Vector2(viewport.Width, viewport.Height));
        var viewArea = new Rectangle(
            (int)topLeft.X - 16, (int)topLeft.Y - 16,
            (int)(bottomRight.X - topLeft.X) + 32,
            (int)(bottomRight.Y - topLeft.Y) + 32);

        Map.Draw(sb, viewArea);
        // Decor tile-objects draw first so subclass OnDrawWorld (e.g. ResourceNode flashes)
        // layers on top. Decor back-halves go here; front-halves at 50% alpha go after Player.
        foreach (var decor in Map.Decor)
            decor.DrawBeforePlayer(sb, Player);
        OnDrawWorld(sb, viewArea);
        Player.Draw(sb);
        foreach (var decor in Map.Decor)
            decor.DrawAfterPlayer(sb, Player);
        OnDrawWorldAfterPlayer(sb, viewArea);
        DrawTriggerMarkers(sb);

        sb.End();

        // Day/night overlay
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        float darkness = 1f - MathHelper.Clamp(Services.Time.GetLightIntensity(), 0f, 1f);
        if (darkness > 0.05f)
            sb.Draw(Pixel,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * (darkness * 0.6f));
        sb.End();

        // Screen space HUD + hotbar + subclass overlays
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        Services.Hud?.Draw(sb, viewport.Width, viewport.Height);
        Services.Hotbar?.Draw(sb, viewport.Width, viewport.Height);
        Services.Toast?.Draw(sb, Font, Pixel);
        OnDrawScreen(sb, viewport.Width, viewport.Height);
        sb.End();
    }

    public override void UnloadContent()
    {
        OnUnload();
        Pixel?.Dispose();
        Console.WriteLine($"[{SceneName}Scene] Unloaded");
    }

    private static Color TriggerColor(string name) => name switch
    {
        "enter_village"    => new Color(80, 160, 255),   // blue
        "exit_to_farm"     => new Color(80, 160, 255),   // blue
        "door_castle"      => new Color(220, 60, 60),    // red
        "door_shop"        => new Color(60, 200, 80),    // green
        "exit_to_village"  => new Color(255, 210, 60),   // yellow
        _                  => new Color(255, 0, 255),    // magenta (unknown)
    };

    private void DrawTriggerMarkers(SpriteBatch sb)
    {
        foreach (var t in Map.Triggers)
        {
            var color = TriggerColor(t.Name);
            sb.Draw(Pixel, t.Bounds, color * 0.55f);
            int bx = t.Bounds.X, by = t.Bounds.Y, bw = t.Bounds.Width, bh = t.Bounds.Height;
            sb.Draw(Pixel, new Rectangle(bx, by, bw, 1), color);
            sb.Draw(Pixel, new Rectangle(bx, by + bh - 1, bw, 1), color);
            sb.Draw(Pixel, new Rectangle(bx, by, 1, bh), color);
            sb.Draw(Pixel, new Rectangle(bx + bw - 1, by, 1, bh), color);
            if (Font != null)
                sb.DrawString(Font, t.Name, new Vector2(bx + 2, by + 2), Color.White, 0f,
                    Vector2.Zero, 0.4f, SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// F2 debug kit: clear inventory and grant a starter set. Available in every
    /// gameplay scene since it only touches Services.Inventory.
    /// </summary>
    private void GrantDebugKit()
    {
        var inv = Services.Inventory;
        if (inv == null) return;

        inv.ClearAll();
        inv.AddGold(1000);
        Console.WriteLine($"[{SceneName}Scene] Debug kit: inventory cleared, +1000 gold (total {inv.Gold}g).");

        (string id, int qty)[] kit = new[]
        {
            ("Hoe", 1),
            ("Axe", 1),
            ("Watering_Can", 1),
            ("Scythe", 1),
            ("Iron_Sword", 1),
            ("Magic_Staff", 1),
            ("Leather_Armor", 1),
            ("Leather_Boots", 1),
            ("Health_Potion", 5),
            ("Smoked_Meat", 3),
            ("Cabbage_Seed", 10),
            ("Carrot_Seed", 10),
            ("Wheat_Seed", 10),
            ("Tomato_Seed", 10),
            ("Cabbage", 5),
        };

        int granted = 0;
        foreach (var (id, qty) in kit)
        {
            int leftover = inv.TryAdd(id, qty);
            int added = qty - leftover;
            if (added > 0) granted++;
            if (leftover > 0)
                Console.WriteLine($"[{SceneName}Scene] Debug kit: {id} partial ({added}/{qty}), inventory full");
        }
        Console.WriteLine($"[{SceneName}Scene] Debug kit granted ({granted}/{kit.Length} item types). F2 again to repeat.");

        // Full restore of HP and Stamina (admin convenience).
        if (Player != null)
        {
            Player.HP = Player.MaxHP;
            Player.Stats.RestoreStamina(Player.Stats.MaxStamina);
            Console.WriteLine($"[{SceneName}Scene] Debug heal: HP {Player.HP:F0}/{Player.MaxHP:F0}, Stamina {Player.Stats.CurrentStamina:F0}/{Player.Stats.MaxStamina:F0}");
        }
    }
}
