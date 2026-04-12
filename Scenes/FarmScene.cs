using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Entities;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Main farm gameplay scene. Contains: TileMap, Player, Farming systems, HUD, day/night cycle.
/// Handles hotbar drag-and-drop when inventory overlay is not open.
/// </summary>
public class FarmScene : Scene
{
    private TileMap _map = null!;
    private PlayerEntity _player = null!;
    private GridManager _gridManager = null!;
    private CropManager _cropManager = null!;
    private ToolController _toolController = null!;
    private HUD _hud = null!;
    private Texture2D _pixel = null!;
    private InventoryManager _inventory = null!;
    private MainQuest _mainQuest = null!;
    private CombatManager _combat = null!;
    private ProjectileManager _projectiles = null!;
    private SlashEffect _slash = new();
    private SpriteAtlas _spriteAtlas = null!;
    private HotbarRenderer _hotbar = null!;
    private readonly List<ItemDropEntity> _itemDrops = new();
    private readonly List<EnemyEntity> _enemies = new();
    private EnemySpawner _spawner = null!;
    private BossEntity? _boss;
    private SpriteFont _font = null!;
    private GameState? _loadedState;
    private readonly Random _lootRng = new();
    private bool _debugDraw;
    private readonly string _fromScene;

    public FarmScene(ServiceContainer services, string fromScene = "Fresh") : base(services)
    {
        _fromScene = fromScene;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        var content = Services.Content;

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Map
        _map = new TileMap();
        _map.Load("Content/Maps/test_farm.tmx", device);

        // Player -- reuse Services.Player across scenes so state (HP, HP, inventory-linked position)
        // survives transitions (WLD-04). First entry creates it; subsequent FarmScene entries reuse.
        if (Services.Player == null)
        {
            _player = new PlayerEntity { Position = TileMap.TileCenterWorld(10, 10) };
            var playerTex = LoadTexture(device, "Content/Sprites/Player/player_spritesheet.png");
            _player.LoadContent(playerTex);
            Services.Player = _player;
            Services.PlayerSpriteSheet = playerTex;
        }
        else
        {
            _player = Services.Player;
        }

        // Per-entry spawn adjustment (WLD-04). First boot ("Fresh") keeps tile (10,10);
        // returning from Village places the player just west of the east-edge trigger.
        _player.Position = _fromScene switch
        {
            "Village" => new Vector2(896, 272),
            _ => _player.Position, // keep existing (either save-loaded or tile(10,10) fresh)
        };

        // Crops
        CropRegistry.Initialize(device);
        _gridManager = new GridManager(_map);
        _gridManager.LoadContent(device);
        _cropManager = new CropManager(_gridManager, CropRegistry.GetAllCrops());

        // Camera
        Services.Camera.Zoom = 3f;
        Services.Camera.Bounds = _map.GetWorldBounds();

        Services.Time.OnDayAdvanced += OnDayAdvanced;

        // Items (must be before HUD, Combat, and ToolController so they can reference inventory)
        ItemRegistry.Initialize();
        _inventory = new InventoryManager();
        Services.Inventory = _inventory;

        // Main quest container -- lives in Services so any scene can observe/advance it
        _mainQuest = new MainQuest();
        Services.Quest = _mainQuest;

        // ToolController depends on _inventory — must be created after it
        _toolController = new ToolController(_gridManager, _cropManager, _player, _inventory, SpawnItemDrop);

        // Combat
        _combat = new CombatManager(_inventory);
        _projectiles = new ProjectileManager();
        _projectiles.OnPlayerHit = (damage) => _combat.TryPlayerTakeDamage(_player, damage);

        // Enemies
        _spawner = new EnemySpawner();
        _enemies.AddRange(_spawner.SpawnAll());
        _boss = _spawner.SpawnBoss();

        // HUD (requires player and combat for HP bar and cooldown)
        _font = content.Load<SpriteFont>("DefaultFont");
        _hud = new HUD(Services.Time, _player.Stats, _toolController, _player, _combat);
        _hud.LoadContent(device, _font);
        _hud.SetQuest(_mainQuest);

        var itemSheet = LoadTexture(device, "Content/Sprites/Items/Pickup_Items.png");
        _spriteAtlas = SpriteAtlas.CreateDefault(itemSheet);
        var foodSheet = LoadTexture(device, "Content/Sprites/Items/Fruits and Vegetables/Food_Icons.png");
        _spriteAtlas.RegisterFoodIcons(foodSheet);
        var toolSheet = LoadTexture(device, "Content/Sprites/Items/Tools/Tool_Icons.png");
        _spriteAtlas.RegisterTools(toolSheet);
        var handTex = LoadTexture(device, "Content/Sprites/Items/Tools/hand.png");
        _spriteAtlas.RegisterHand(handTex);
        _hotbar = new HotbarRenderer(_inventory, _spriteAtlas);
        _hotbar.LoadContent(device, _font);

        // Load save
        var save = SaveManager.Load();
        if (save != null)
        {
            Services.Time.SetDay(save.DayNumber);
            Services.Time.SetGameTime(save.GameTime);
            _player.Position = new Vector2(save.PlayerX, save.PlayerY);
            _player.Stats.SetStamina(save.StaminaCurrent);
            _gridManager.LoadFromSaveData(save.FarmCells, CropRegistry.All);
            _inventory.LoadFromState(save);
            _mainQuest.LoadFromState(save);
            Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
            _loadedState = save;
        }
        else
        {
            _loadedState = new GameState();
            Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
        }

        // Publish loaded state so other scenes can update CurrentScene on entry
        Services.GameState = _loadedState;
        _loadedState.CurrentScene = "Farm";

        // Seed starter tools into hotbar slots 0-2 on a fresh game
        if (save == null)
        {
            _inventory.SetHotbarRef(0, "Hoe");
            _inventory.SetHotbarRef(1, "Watering_Can");
            _inventory.SetHotbarRef(2, "Scythe");
            _inventory.TryAdd("Hoe");
            _inventory.TryAdd("Watering_Can");
            _inventory.TryAdd("Scythe");
        }

        // Test items (only when no save or empty inventory)
        if (save == null || save.Inventory.Count == 0)
        {
            _inventory.TryAdd("Cabbage", 5);
            _inventory.TryAdd("Iron_Sword");
            _inventory.TryAdd("Magic_Staff");
            _inventory.TryAdd("Cosmic_Carrot", 3);
            _inventory.TryAdd("Flame_Blade");
            _inventory.TryAdd("Leather_Armor");
            _inventory.TryAdd("Health_Potion", 10);
            _inventory.TryAdd("Bread", 5);
            _inventory.TryAdd("Leather_Helmet");
            _inventory.TryAdd("Iron_Legs");
            _inventory.TryAdd("Leather_Boots");
            _inventory.TryAdd("Wooden_Shield");
            _inventory.TryAdd("Silver_Ring");
            _inventory.TryAdd("Iron_Necklace");

            // Default hotbar refs for testing
            _inventory.SetHotbarRef(0, "Iron_Sword");
            _inventory.SetHotbarRef(1, "Flame_Blade");
            _inventory.SetHotbarRef(2, "Cabbage");

            // Default consumable ref (Q slot only)
            _inventory.SetConsumableRef(0, "Health_Potion");
        }

        Console.WriteLine($"[FarmScene] Entered from {_fromScene}, spawn ({_player.Position.X},{_player.Position.Y})");
        Console.WriteLine("[FarmScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;
        var viewport = Services.GraphicsDevice.Viewport;

        // Sleep
        if (input.IsKeyPressed(Keys.P))
        {
            Console.WriteLine("[FarmScene] Player sleeping...");
            Services.Time.ForceSleep();
        }

        // Test scene
        if (input.IsKeyPressed(Keys.T))
        {
            Services.SceneManager.Push(new TestScene(Services));
            return;
        }

        // Debug draw toggle
        if (input.IsKeyPressed(Keys.F3))
            _debugDraw = !_debugDraw;

        // Debug: grant test kit (tools + seeds + consumables)
        if (input.IsKeyPressed(Keys.F2))
            GrantDebugKit();

        // Hotbar selection (1-8)
        for (int i = 0; i < 8; i++)
        {
            if (input.IsKeyPressed(Keys.D1 + i))
                _inventory.SetActiveHotbar(i);
        }

        // Consumable quick-use: Q = slot 0 (E kept free for actions)
        if (input.IsKeyPressed(Keys.Q))
        {
            float heal = _inventory.UseConsumable(0);
            if (heal > 0)
            {
                _player.HP = Math.Min(_player.MaxHP, _player.HP + heal);
                Console.WriteLine($"[FarmScene] Used consumable Q, healed {heal} HP");
            }
        }

        // Open inventory
        if (input.IsKeyPressed(Keys.I))
        {
            _hotbar.CancelDrag();
            Services.SceneManager.PushImmediate(
                new InventoryScene(Services, _inventory, _spriteAtlas, _hotbar));
            return;
        }

        // Escape → pause menu
        if (input.IsKeyPressed(Keys.Escape))
        {
            _hotbar.CancelDrag();
            Services.SceneManager.PushImmediate(new PauseScene(Services));
            return;
        }

        // Hotbar drag-and-drop (only when no overlay)
        _hotbar.Update(input.MousePosition, viewport.Width, viewport.Height);

        // Time
        Services.Time.Update(deltaTime);

        // Combat input and update
        _combat.HandleInput(input, _player);
        _combat.Update(deltaTime);

        // Spawn fireball if requested
        if (_combat.ConsumeFireballRequest())
        {
            _projectiles.SpawnFireball(_player.Position, _player.FacingDirection);
        }

        // Trigger slash visual on melee swing start
        if (_combat.Melee.IsSwinging && _combat.Melee.SwingProgress < 0.1f)
            _slash.Trigger(_player.Position, _player.FacingDirection);
        _slash.Update(deltaTime);

        // Update projectiles with enemy list for collision detection (include boss)
        var enemiesAsEntities = new List<Core.Entity>(_enemies);
        if (_boss != null && _boss.IsAlive)
            enemiesAsEntities.Add(_boss);
        _projectiles.Update(deltaTime, enemiesAsEntities, _player);

        // Melee hitbox checks against enemies
        if (_combat.Melee.IsSwinging)
        {
            var hitbox = _combat.Melee.GetHitbox(_player.Position, _player.FacingDirection);
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;
                if (hitbox.Intersects(enemy.HitBox) && !_combat.Melee.HasHit(enemy))
                {
                    float damage = _combat.CalculateMeleeDamage();
                    enemy.TakeDamage(damage);
                    _combat.Melee.RecordHit(enemy);

                    // Knockback away from player with resistance
                    var knockDir = enemy.Position - _player.Position;
                    if (knockDir != Vector2.Zero) knockDir.Normalize();
                    enemy.ApplyKnockbackWithResistance(knockDir, 32f);

                    Console.WriteLine($"[FarmScene] Melee hit {enemy.Data.Name} for {damage:F0} damage");
                }
            }
        }

        // Update enemies: AI, movement, attacks
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            enemy.Update(deltaTime, _player.Position, _projectiles);

            // Enemy melee attack vs player (uses extended reach hitbox, like boss slash)
            if (enemy.IsMeleeAttackReady)
            {
                if (enemy.GetMeleeAttackHitbox().Intersects(_player.HitBox))
                {
                    _combat.TryPlayerTakeDamage(_player, enemy.Data.AttackDamage);
                }
                enemy.ConsumeMeleeAttack();
            }

            // Handle enemy death: roll loot and remove
            if (!enemy.IsAlive)
            {
                var drops = enemy.Data.Loot.Roll(_lootRng);
                foreach (var (itemId, quantity) in drops)
                {
                    SpawnItemDrop(itemId, quantity, enemy.Position);
                    Console.WriteLine($"[FarmScene] {enemy.Data.Name} dropped {quantity}x {itemId}");
                }
                _enemies.RemoveAt(i);
            }
        }

        // Enemy-vs-enemy separation: push overlapping pairs apart so creatures don't stack
        ResolveEnemySeparation();

        // Boss update
        if (_boss != null && _boss.IsAlive)
        {
            _boss.Update(deltaTime, _player.Position, _projectiles);

            // Check boss summon phases: spawn skeleton minions at HP thresholds
            var minions = _boss.CheckSummonPhase();
            if (minions != null)
                _enemies.AddRange(minions);

            // Check boss telegraphed slash attack
            if (_boss.IsBossSlashReady)
            {
                var slashHitbox = _boss.GetBossSlashHitbox();
                if (slashHitbox.Intersects(_player.HitBox))
                {
                    _combat.TryPlayerTakeDamage(_player, _boss.Data.AttackDamage);
                    Console.WriteLine("[FarmScene] Boss slash hit player!");
                }
            }

            // Melee hitbox checks: player melee vs boss
            if (_combat.Melee.IsSwinging)
            {
                var hitbox = _combat.Melee.GetHitbox(_player.Position, _player.FacingDirection);
                if (hitbox.Intersects(_boss.HitBox) && !_combat.Melee.HasHit(_boss))
                {
                    float damage = _combat.CalculateMeleeDamage();
                    _boss.TakeDamage(damage);
                    _combat.Melee.RecordHit(_boss);

                    var knockDir = _boss.Position - _player.Position;
                    if (knockDir != Vector2.Zero) knockDir.Normalize();
                    _boss.ApplyKnockbackWithResistance(knockDir, 32f);

                    Console.WriteLine($"[FarmScene] Melee hit Skeleton King for {damage:F0} damage");
                }
            }

            // Handle boss death
            if (!_boss.IsAlive)
            {
                var bossLoot = _boss.GetBossLoot(_loadedState!.BossKilled);
                foreach (var (itemId, quantity) in bossLoot)
                {
                    SpawnItemDrop(itemId, quantity, _boss.Position);
                    Console.WriteLine($"[FarmScene] Skeleton King dropped {quantity}x {itemId}");
                }
                _loadedState!.BossKilled = true;
                _boss = null;
                Console.WriteLine("[FarmScene] Skeleton King defeated!");
            }
        }

        // Handle player death: respawn at farm center with full HP
        if (!_player.IsAlive)
        {
            _player.HP = _player.MaxHP;
            _player.Position = TileMap.TileCenterWorld(10, 10);
            Console.WriteLine("[FarmScene] Player died! Respawning at farm center.");
        }

        // Tools & movement
        _toolController.Update(input);
        // Build solid entity list: enemies + boss block player movement
        var solids = new List<Core.Entity>(_enemies);
        if (_boss != null && _boss.IsAlive) solids.Add(_boss);
        _player.Update(deltaTime, input.Movement, _map, solids);
        Services.Camera.Follow(_player.Position, deltaTime);

        // Scene-transition triggers (east-edge -> village, etc.)
        var pBox = _player.CollisionBox;
        foreach (var t in _map.Triggers)
        {
            if (!pBox.Intersects(t.Bounds)) continue;
            if (t.Name == "enter_village")
            {
                Services.SceneManager.TransitionTo(new VillageScene(Services, "Farm"));
                return;
            }
        }

        // Item drops: update magnetism/pickup, remove collected
        for (int i = _itemDrops.Count - 1; i >= 0; i--)
        {
            _itemDrops[i].UpdateWithPlayer(deltaTime, _player.GetFootPosition(), _inventory);
            if (_itemDrops[i].IsCollected)
            {
                Console.WriteLine($"[FarmScene] Picked up: {_itemDrops[i].ItemId}");
                _itemDrops.RemoveAt(i);
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var device = Services.GraphicsDevice;
        var viewport = device.Viewport;
        var transform = Services.Camera.GetTransformMatrix();

        // World space
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            null, null, null, transform);

        var topLeft = Services.Camera.ScreenToWorld(Vector2.Zero);
        var bottomRight = Services.Camera.ScreenToWorld(new Vector2(viewport.Width, viewport.Height));
        var viewArea = new Rectangle(
            (int)topLeft.X - 16, (int)topLeft.Y - 16,
            (int)(bottomRight.X - topLeft.X) + 32,
            (int)(bottomRight.Y - topLeft.Y) + 32);

        _map.Draw(spriteBatch, viewArea);
        _gridManager.DrawOverlays(spriteBatch, viewArea);
        _gridManager.DrawCrops(spriteBatch, viewArea);
        foreach (var drop in _itemDrops)
            drop.Draw(spriteBatch);
        foreach (var enemy in _enemies)
        {
            enemy.Draw(spriteBatch, _pixel);
            EnemyHealthBar.Draw(spriteBatch, _pixel, enemy.Position, enemy.Data.Height, enemy.HP, enemy.MaxHP);
        }
        if (_boss != null && _boss.IsAlive)
        {
            _boss.Draw(spriteBatch, _pixel);
            EnemyHealthBar.Draw(spriteBatch, _pixel, _boss.Position, _boss.Data.Height, _boss.HP, _boss.MaxHP);
        }
        _player.Draw(spriteBatch);
        _slash.Draw(spriteBatch, _pixel);
        _projectiles.Draw(spriteBatch, _pixel);
        DrawFarmZoneHint(spriteBatch, viewArea);

        // Debug overlay: F3 toggles collision/hitbox visualization
        if (_debugDraw)
        {
            // Player CollisionBox (green) — movement/terrain
            DrawDebugRect(spriteBatch, _player.CollisionBox, Color.Lime * 0.5f);
            // Player HitBox (yellow) — combat damage target
            DrawDebugRect(spriteBatch, _player.HitBox, Color.Yellow * 0.5f);

            // Melee hitbox (red) — active swing area
            if (_combat.Melee.IsSwinging)
            {
                var meleeHitbox = _combat.Melee.GetHitbox(_player.Position, _player.FacingDirection);
                DrawDebugRect(spriteBatch, meleeHitbox, Color.Red * 0.5f);
            }

            // Enemies
            foreach (var enemy in _enemies)
            {
                DrawDebugRect(spriteBatch, enemy.CollisionBox, Color.Lime * 0.5f);
                DrawDebugRect(spriteBatch, enemy.HitBox, Color.Yellow * 0.5f);
            }

            // Boss
            if (_boss != null && _boss.IsAlive)
            {
                DrawDebugRect(spriteBatch, _boss.CollisionBox, Color.Lime * 0.5f);
                DrawDebugRect(spriteBatch, _boss.HitBox, Color.Yellow * 0.5f);
                if (_boss.IsBossSlashReady || _boss.IsWindingUp)
                    DrawDebugRect(spriteBatch, _boss.GetBossSlashHitbox(), Color.Red * 0.4f);
            }

            // Projectiles
            foreach (var proj in _projectiles.Active)
                DrawDebugRect(spriteBatch, proj.Hitbox, Color.Cyan * 0.6f);
        }

        spriteBatch.End();

        // Day/night overlay
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        float darkness = 1f - MathHelper.Clamp(Services.Time.GetLightIntensity(), 0f, 1f);
        if (darkness > 0.05f)
            spriteBatch.Draw(_pixel,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * (darkness * 0.6f));
        spriteBatch.End();

        // Screen space HUD
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _hud.Draw(spriteBatch, viewport.Width, viewport.Height);
        _hotbar.Draw(spriteBatch, viewport.Width, viewport.Height);
        if (_boss != null && _boss.IsAlive)
        {
            BossHealthBar.Draw(spriteBatch, _pixel, _font,
                "Skeleton King", _boss.HP, _boss.MaxHP, viewport.Width, viewport.Height);
        }
        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        Services.Time.OnDayAdvanced -= OnDayAdvanced;
        _pixel?.Dispose();
        Console.WriteLine("[FarmScene] Unloaded");
    }

    /// <summary>
    /// Resolve enemy-enemy overlap by pushing overlapping pairs apart along the axis
    /// between their centers. Single O(n^2) pass per frame — fine for small enemy counts.
    /// Includes boss as a separation target so minions don't sit inside it.
    /// </summary>
    private void ResolveEnemySeparation()
    {
        // Build the list of solids to resolve (enemies + boss)
        var bodies = new List<Core.Entity>(_enemies);
        if (_boss != null && _boss.IsAlive) bodies.Add(_boss);

        for (int i = 0; i < bodies.Count; i++)
        {
            for (int j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];
                if (!a.IsAlive || !b.IsAlive) continue;

                var ab = a.CollisionBox;
                var bb = b.CollisionBox;
                if (!ab.Intersects(bb)) continue;

                // Separation vector: from b -> a (push a away from b)
                Vector2 delta = a.Position - b.Position;
                if (delta.LengthSquared() < 0.0001f)
                    delta = new Vector2(1f, 0f); // arbitrary tiebreak when perfectly overlapping
                delta.Normalize();

                // Compute overlap on each axis; push by half the smaller axis overlap
                int overlapX = Math.Min(ab.Right, bb.Right) - Math.Max(ab.Left, bb.Left);
                int overlapY = Math.Min(ab.Bottom, bb.Bottom) - Math.Max(ab.Top, bb.Top);
                float push = Math.Min(overlapX, overlapY) * 0.5f + 0.5f;

                // Boss is heavy — don't move it; push minion/enemy the full distance instead
                bool bossIsA = a is BossEntity;
                bool bossIsB = b is BossEntity;
                if (bossIsA && !bossIsB)
                {
                    b.Position -= delta * (push * 2f);
                }
                else if (bossIsB && !bossIsA)
                {
                    a.Position += delta * (push * 2f);
                }
                else
                {
                    a.Position += delta * push;
                    b.Position -= delta * push;
                }
            }
        }
    }

    /// <summary>Draw a filled debug rectangle with the given color.</summary>
    private void DrawDebugRect(SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(_pixel, rect, color);
    }

    private void DrawFarmZoneHint(SpriteBatch sb, Rectangle viewArea)
    {
        int startX = Math.Max(0, viewArea.Left / TileMap.TileSize);
        int startY = Math.Max(0, viewArea.Top / TileMap.TileSize);
        int endX = Math.Min(_map.Width - 1, viewArea.Right / TileMap.TileSize);
        int endY = Math.Min(_map.Height - 1, viewArea.Bottom / TileMap.TileSize);

        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        {
            if (_map.IsFarmZone(x, y) && _gridManager.GetCell(new Point(x, y)) == null)
                sb.Draw(_pixel,
                    new Rectangle(x * TileMap.TileSize, y * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize),
                    Color.Green * 0.08f);
        }
    }

    /// <summary>
    /// Spawn an item drop entity at the given world position.
    /// Called by ToolController on harvest.
    /// </summary>
    public void SpawnItemDrop(string itemId, int quantity, Vector2 worldPosition)
    {
        var drop = new ItemDropEntity(itemId, quantity, worldPosition, _spriteAtlas);
        _itemDrops.Add(drop);
        Console.WriteLine($"[FarmScene] Item drop spawned: {quantity}x {itemId}");
    }

    /// <summary>
    /// Debug: grant a starter test kit directly to the inventory.
    /// Bound to F2. Use to populate inventory for testing hotbar/tool flow without farming.
    /// </summary>
    private void GrantDebugKit()
    {
        _inventory.ClearAll();
        Console.WriteLine("[FarmScene] Debug kit: inventory cleared.");

        (string id, int qty)[] kit = new[]
        {
            ("Hoe", 1),
            ("Watering_Can", 1),
            ("Scythe", 1),
            ("Iron_Sword", 1),
            ("Magic_Staff", 1),
            ("Leather_Armor", 1),
            ("Leather_Boots", 1),
            ("Health_Potion", 5),
            ("Cabbage_Seed", 10),
            ("Carrot_Seed", 10),
            ("Wheat_Seed", 10),
            ("Tomato_Seed", 10),
            ("Cabbage", 5),
        };

        int granted = 0;
        foreach (var (id, qty) in kit)
        {
            int leftover = _inventory.TryAdd(id, qty);
            int added = qty - leftover;
            if (added > 0) granted++;
            if (leftover > 0)
                Console.WriteLine($"[FarmScene] Debug kit: {id} partial ({added}/{qty}), inventory full");
        }
        Console.WriteLine($"[FarmScene] Debug kit granted ({granted}/{kit.Length} item types). F2 again to repeat.");
    }

    private void OnDayAdvanced()
    {
        Console.WriteLine($"[FarmScene] === Day {Services.Time.DayNumber} ===");
        _cropManager.OnDayAdvanced();
        _gridManager.OnDayAdvanced();
        _player.Stats.RestoreStamina();
        _spawner.Respawn(_enemies);

        // Respawn boss on day advance (replayable encounter)
        _boss = _spawner.SpawnBoss();

        var state = new GameState
        {
            DayNumber = Services.Time.DayNumber,
            Season = Services.Time.Season,
            StaminaCurrent = _player.Stats.CurrentStamina,
            PlayerX = _player.Position.X,
            PlayerY = _player.Position.Y,
            GameTime = Services.Time.GameTime,
            FarmCells = _gridManager.GetSaveData(),
            CurrentScene = "Farm",
            BossKilled = _loadedState?.BossKilled ?? false
        };
        _inventory.SaveToState(state);
        _mainQuest.SaveToState(state);
        SaveManager.Save(state);
    }

    private Texture2D LoadTexture(GraphicsDevice device, string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }
}
