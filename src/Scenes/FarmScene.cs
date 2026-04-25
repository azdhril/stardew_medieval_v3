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
using stardew_medieval_v3.Progression;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Main farm gameplay scene. Farming systems, enemies, boss, item drops, projectiles.
/// Cross-cutting behavior (HUD, input, pause, inventory, hotbar) lives in
/// <see cref="GameplayScene"/>.
/// </summary>
public class FarmScene : GameplayScene
{
    private static readonly Point StarterChestTile = new(8, 10);
    private static readonly Point StarterTreeTile = new(11, 10);

    /// <summary>
    /// Hardcoded farm enemy spawn points. Owned by FarmScene now (was previously
    /// inside EnemySpawner). DungeonScene supplies its own list from DungeonRegistry.
    /// </summary>
    private static readonly (string id, Vector2 pos)[] FarmSpawnPoints = new[]
    {
        ("Skeleton", new Vector2(400, 200)),
        ("Skeleton", new Vector2(500, 350)),
        ("DarkMage", new Vector2(600, 250)),
        ("Golem", new Vector2(450, 400))
    };

    private static readonly Vector2 FarmBossSpawn = new(600, 400);

    private GridManager _gridManager = null!;
    private CropManager _cropManager = null!;
    private ToolController _toolController = null!;
    private InventoryManager _inventory = null!;
    private MainQuest _mainQuest = null!;
    private CombatManager _combat = null!;
    private ProjectileManager _projectiles = null!;
    private SlashEffect _slash = new();
    private SpriteAtlas _spriteAtlas = null!;
    private HotbarRenderer _hotbar = null!;
    private HUD _hud = null!;
    private MinimapRenderer _minimap = null!;
    private ChestManager _chestManager = null!;
    private ResourceManager _resourceManager = null!;
    private InteractionPrompt _chestPrompt = null!;
    private readonly List<ItemDropEntity> _itemDrops = new();
    private readonly List<EnemyEntity> _enemies = new();
    private EnemySpawner _spawner = null!;
    private Pathfinder? _pathfinder;
    private BossEntity? _boss;
    private GameState? _loadedState;
    private ChestInstance? _promptChest;
    private ChestInstance? _pendingChestOpen;
    private readonly Random _lootRng = new();
    private bool _debugDraw;

    public FarmScene(ServiceContainer services, string fromScene = "Fresh") : base(services, fromScene) { }

    protected override string MapPath => "assets/Maps/test_farm.tmx";
    protected override string SceneName => "Farm";

    protected override Vector2 GetSpawn(string fromScene) => fromScene switch
    {
        "Village" => new Vector2(560, 240),
        _ => Services.Player?.Position ?? TileMap.TileCenterWorld(10, 10),
    };

    protected override void OnLoad()
    {
        var device = Services.GraphicsDevice;
        var content = Services.Content;

        // Player — reuse Services.Player across scenes so state survives transitions.
        // First entry creates it; subsequent FarmScene entries reuse.
        if (Services.Player == null)
        {
            var player = new PlayerEntity { Position = TileMap.TileCenterWorld(10, 10) };
            var playerTex = LoadTexture(device, "assets/Sprites/Player/player_spritesheet.png");
            player.LoadContent(playerTex);
            Services.Player = player;
            Services.PlayerSpriteSheet = playerTex;
        }
        var pl = Services.Player!;

        // Crops
        CropRegistry.Initialize(device);
        ChestRegistry.Initialize(device);
        ResourceRegistry.Initialize(device);
        _gridManager = new GridManager(Map);
        _gridManager.LoadContent(device);
        _cropManager = new CropManager(_gridManager, CropRegistry.GetAllCrops());
        _chestManager = new ChestManager();
        _resourceManager = new ResourceManager();
        _chestPrompt = new InteractionPrompt();

        // Publish to Services so GameStateSnapshot.SaveNow can read live state.
        Services.ChestManager = _chestManager;
        Services.ResourceManager = _resourceManager;

        // Singleton DungeonState lives on Services so dungeon flags survive
        // FarmScene <-> DungeonScene transitions. Created lazily on first farm entry.
        if (Services.Dungeon == null)
            Services.Dungeon = new DungeonState();

        Services.Time.OnDayAdvanced += OnDayAdvanced;

        // Items (before HUD, Combat, ToolController)
        // First-entry guard: create live Inventory + Quest once, reuse on Farm re-entry.
        // Mirrors the Services.Player guard above so live in-memory state survives scene transitions.
        ItemRegistry.Initialize();
        bool firstEntry = Services.Inventory == null;
        if (firstEntry)
        {
            _inventory = new InventoryManager();
            Services.Inventory = _inventory;
        }
        else
        {
            _inventory = Services.Inventory!;
        }

        if (Services.Quest == null)
        {
            _mainQuest = new MainQuest();
            Services.Quest = _mainQuest;
        }
        else
        {
            _mainQuest = Services.Quest!;
        }

        // Progression — create once, reuse on Farm re-entry (same pattern as Inventory/Quest)
        if (Services.Progression == null)
        {
            Services.Progression = new ProgressionManager(pl, pl.Stats);
        }

        _toolController = new ToolController(_gridManager, _cropManager, pl, _inventory, SpawnItemDrop, TryUseWorldTool);

        // Combat
        _combat = new CombatManager(_inventory);
        _combat.DamageBonus = Services.Progression?.BaseDamageBonus ?? 0;
        // Keep damage bonus in sync with level-ups during this scene
        if (Services.Progression != null)
            Services.Progression.OnLevelUp += (_) => _combat.DamageBonus = Services.Progression.BaseDamageBonus;
        _projectiles = new ProjectileManager();
        _projectiles.OnPlayerHit = (damage) => _combat.TryPlayerTakeDamage(pl, damage);
        _projectiles.OnEnemyHit = () => _combat.OnPlayerSpellHit(pl);

        // Spawn the scene's initial enemy population from the shared data-driven spawner.
        // FarmScene keeps the live list so combat, drawing, drops, and daily respawns
        // all operate on the same enemy instances.
        _spawner = new EnemySpawner();
        _spawner.SpawnAll(FarmSpawnPoints, _enemies);
        _boss = _spawner.SpawnBoss(FarmBossSpawn);

        // Build A* walkability grid from map collision polygons
        _pathfinder = new Pathfinder();
        _pathfinder.BuildGrid(Map);

        // HUD
        _hud = new HUD(Services.Time, pl.Stats, _toolController, pl, _combat, _inventory);
        _hud.LoadContent(device, Font, Services.Fonts!.GetFont(FontRole.Body, 15));
        _hud.SetQuest(_mainQuest);
        _hud.SetProgression(Services.Progression);

        // Ensure UITheme is loaded early so HUD NineSlice panels render from frame 1
        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(device);
        }
        _hud.SetTheme(Services.Theme);

        BossHealthBar.LoadContent(device);

        _minimap = new MinimapRenderer();
        _minimap.LoadContent(device);
        _minimap.Rebuild(Map, device);

        var itemSheet = LoadTexture(device, "assets/Sprites/Items/Pickup_Items.png");
        _spriteAtlas = SpriteAtlas.CreateDefault(itemSheet);
        var foodSheet = LoadTexture(device, "assets/Sprites/Items/Fruits and Vegetables/Food_Icons.png");
        _spriteAtlas.RegisterFoodIcons(foodSheet);
        var toolSheet = LoadTexture(device, "assets/Sprites/Items/Tools/Tool_Icons.png");
        _spriteAtlas.RegisterTools(toolSheet);
        var handTex = LoadTexture(device, "assets/Sprites/Items/Tools/hand.png");
        _spriteAtlas.RegisterHand(handTex);
        var potionSheet = LoadTexture(device, "assets/Sprites/Items/Consumables/potions.png");
        _spriteAtlas.RegisterPotions(potionSheet);
        _hotbar = new HotbarRenderer(_inventory, _spriteAtlas);
        _hotbar.LoadContent(device, Font);
        var goldCoinTex = LoadTexture(device, "assets/Sprites/Items/Bag_of_coins.png");
        _spriteAtlas.RegisterGoldCoin(goldCoinTex);
        var bagsSheet = LoadTexture(device, "assets/Sprites/Items/Tools/bags_upgrades.png");
        _spriteAtlas.RegisterBagUpgrades(bagsSheet);
        Services.Atlas = _spriteAtlas;
        Services.Hud = _hud;
        Services.Hotbar = _hotbar;

        // Load save — Time/Inventory/Quest/Player/Stamina hydrated only on first Farm entry.
        // Re-entries reuse live in-memory state to avoid stale-disk overwrites of Gold/Quest/Inventory.
        // Farm cells are re-hydrated from disk every entry (existing behavior — _gridManager is per-scene).
        var save = SaveManager.Load();
        if (save != null)
        {
            _gridManager.LoadFromSaveData(save.FarmCells, CropRegistry.All);
            if (firstEntry)
            {
                Services.Time.SetDay(save.DayNumber);
                Services.Time.SetGameTime(save.GameTime);
                pl.Position = new Vector2(save.PlayerX, save.PlayerY);
                pl.Stats.SetStamina(save.StaminaCurrent);
                _inventory.LoadFromState(save);
                _mainQuest.LoadFromState(save);
                Services.Progression?.LoadFromState(save);
                Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
            }
            else
            {
                Console.WriteLine($"[FarmScene] Re-entry; live Gold={_inventory.Gold}, Quest={_mainQuest.State}");
            }
            _loadedState = save;
        }
        else
        {
            _loadedState = new GameState();
            if (firstEntry)
                Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
        }

        Services.GameState = _loadedState;
        // Note: GameplayScene.LoadContent writes Services.GameState.CurrentScene = SceneName
        // on every entry, so restamping it here is both redundant and actively wrong during
        // boot (would overwrite the save's CurrentScene before Game1 reads it). Removed in v10.
        InitializeChests(_loadedState);
        InitializeResources(_loadedState);

        // Ensure a stamina food is available for testing on the first Farm entry,
        // including existing saves created before the eating system existed.
        if (firstEntry && !_inventory.HasItem("Smoked_Meat"))
        {
            _inventory.TryAdd("Smoked_Meat", 3);
            if (_inventory.GetConsumableRef(0) == null)
                _inventory.SetConsumableRef(0, "Smoked_Meat");
        }

        if (firstEntry && !_inventory.HasItem("Axe"))
        {
            _inventory.TryAdd("Axe");
            if (_inventory.GetHotbarRef(3) == null)
                _inventory.SetHotbarRef(3, "Axe");
        }

        // Seed starter tools on fresh game (first entry only — prevents re-seeding on Farm re-entry)
        if (firstEntry && save == null)
        {
            _inventory.SetHotbarRef(0, "Hoe");
            _inventory.SetHotbarRef(1, "Watering_Can");
            _inventory.SetHotbarRef(2, "Scythe");
            _inventory.SetHotbarRef(3, "Axe");
            _inventory.TryAdd("Hoe");
            _inventory.TryAdd("Axe");
            _inventory.TryAdd("Watering_Can");
            _inventory.TryAdd("Scythe");
        }

        // Test items on fresh/empty inventory (first entry only)
        if (firstEntry && (save == null || save.Inventory.Count == 0))
        {
            _inventory.TryAdd("Cabbage", 5);
            _inventory.TryAdd("Iron_Sword");
            _inventory.TryAdd("Magic_Staff");
            _inventory.TryAdd("Cosmic_Carrot", 3);
            _inventory.TryAdd("Flame_Blade");
            _inventory.TryAdd("Leather_Armor");
            _inventory.TryAdd("Health_Potion", 10);
            _inventory.TryAdd("Smoked_Meat", 5);
            _inventory.TryAdd("Steak", 2);
            _inventory.TryAdd("Bread", 5);
            _inventory.TryAdd("Leather_Helmet");
            _inventory.TryAdd("Iron_Legs");
            _inventory.TryAdd("Leather_Boots");
            _inventory.TryAdd("Wooden_Shield");
            _inventory.TryAdd("Silver_Ring");
            _inventory.TryAdd("Iron_Necklace");

            _inventory.SetHotbarRef(0, "Iron_Sword");
            _inventory.SetHotbarRef(1, "Flame_Blade");
            _inventory.SetHotbarRef(2, "Cabbage");

            _inventory.SetConsumableRef(0, "Smoked_Meat");
        }

        Console.WriteLine("[FarmScene] Loaded");

        // v10: boot-time scene restore. If save.CurrentScene was non-Farm, hop there now that
        // Player/Inventory/Atlas/Hotbar are fully constructed. PushImmediate can't be used —
        // we're still inside LoadContent; TransitionTo defers the swap to the fade-out point.
        var restoreTarget = Services.PendingRestoreScene;
        if (!string.IsNullOrEmpty(restoreTarget) && restoreTarget != "Farm")
        {
            Services.PendingRestoreScene = null; // one-shot
            Scene? next = restoreTarget switch
            {
                "Village" => new VillageScene(Services, "RestoreSave"),
                "Castle"  => new CastleScene(Services, "RestoreSave"),
                "Shop"    => new ShopScene(Services, "RestoreSave"),
                _         => null,
            };
            if (next != null)
            {
                Console.WriteLine($"[FarmScene] Boot restore: transitioning to {restoreTarget}");
                Services.SceneManager.TransitionTo(next);
            }
            else
            {
                Console.WriteLine($"[FarmScene] Boot restore: unknown scene '{restoreTarget}', staying on Farm");
                // PendingRestorePosition for Farm (if any) was already consumed by
                // GameplayScene.LoadContent above — nothing else to do.
            }
        }
        else
        {
            Services.PendingRestoreScene = null; // nothing to restore; clear so no stale state
        }
    }

    protected override bool OnPreUpdate(float deltaTime, InputManager input)
    {
        _chestManager.Update(deltaTime);
        _resourceManager.Update(deltaTime);
        _promptChest = _chestManager.GetChestAtFacingTile(Player.GetFacingTile());

        if (_pendingChestOpen != null)
        {
            if (_pendingChestOpen.IsOpen)
            {
                var chest = _pendingChestOpen;
                _pendingChestOpen = null;
                Services.SceneManager.PushImmediate(new ChestScene(
                    Services,
                    _inventory,
                    chest,
                    _spriteAtlas,
                    () =>
                    {
                        chest.BeginClose();
                        SaveCurrentState();
                    }));
            }
            return true;
        }

        if (_promptChest != null && input.InteractPressed)
        {
            _promptChest.BeginOpen();
            _pendingChestOpen = _promptChest;
            return true;
        }

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
            return true;
        }

        // Debug draw toggle
        if (input.IsKeyPressed(Keys.F3))
            _debugDraw = !_debugDraw;

        if (input.IsKeyPressed(Keys.F6))
        {
            ResetResourcesForDebug();
            return false;
        }

        // Combat input and update
        _combat.HandleInput(input, Player);
        _combat.Update(deltaTime);

        if (_combat.ConsumeFireballRequest())
            _projectiles.SpawnFireball(Player.Position, Player.FacingDirection);

        if (_combat.Melee.IsSwinging && _combat.Melee.SwingProgress < 0.1f)
            _slash.Trigger(Player.Position, Player.FacingDirection);
        _slash.Update(deltaTime);

        // Shared combat tick (enemies, melee, projectiles, boss, drops).
        // BossFirstKill semantics: GetBossLoot wants `bossAlreadyKilled` -- the
        // helper inverts so callers pass "first kill = true" intuitively.
        var combatCtx = new CombatLoopContext
        {
            Player = Player,
            Enemies = _enemies,
            Boss = _boss,
            Projectiles = _projectiles,
            Combat = _combat,
            LootRng = _lootRng,
            Map = Map,
            Pathfinder = _pathfinder,
            SpawnItemDrop = SpawnItemDrop,
            BossFirstKill = !(_loadedState?.BossKilled ?? false),
            OnEnemyKilled = (enemy) =>
            {
                Services.Progression?.AwardXP(enemy.Data.Id);
                int gold = ProgressionManager.RollGold(enemy.Data.Id, _lootRng);
                SpawnItemDrop("Gold_Coin", gold, enemy.Position);
            },
            OnBossDefeated = _ =>
            {
                if (_loadedState != null) _loadedState.BossKilled = true;
            },
        };
        CombatLoop.Update(deltaTime, combatCtx);
        _boss = combatCtx.Boss;

        ResolveEnemySeparation();

        // Player death respawn (Farm-local: penalty + heal + recenter).
        if (!Player.IsAlive)
        {
            _deathBanner.Show();

            // Don't re-apply penalty if coming from DungeonDeath (already applied there).
            if (FromScene != "DungeonDeath")
            {
                var penalty = DeathPenalty.Apply(Services.Inventory!, _lootRng);
                if (penalty.GoldLost > 0)
                    Services.Toast?.Show($"Lost {penalty.GoldLost} gold", Color.Red);
                foreach (var itemId in penalty.ItemsLost)
                    Services.Toast?.Show($"Lost: {ItemRegistry.Get(itemId)?.Name ?? itemId}", Color.OrangeRed);
                Console.WriteLine("[FarmScene] Player died on farm -- penalty applied");
            }
            else
            {
                Console.WriteLine("[FarmScene] Respawned from dungeon death (penalty already applied)");
            }
            Player.HP = Player.MaxHP;
            Player.Stats.RestoreStamina();
            Player.Position = TileMap.TileCenterWorld(10, 10);
            GameStateSnapshot.SaveNow(Services);
        }

        _toolController.Update(input);
        return false;
    }

    protected override IEnumerable<Entity>? GetSolids()
    {
        var solids = new List<Entity>(_enemies);
        if (_boss != null && _boss.IsAlive) solids.Add(_boss);
        foreach (var chest in _chestManager.All)
            solids.Add(chest);
        foreach (var resource in _resourceManager.All)
            solids.Add(resource);
        return solids;
    }

    protected override bool HandleTrigger(string triggerName)
    {
        if (triggerName == "enter_village")
        {
            Services.SceneManager.TransitionTo(new VillageScene(Services, "Farm"));
            return true;
        }
        return false;
    }

    protected override void OnPostUpdate(float deltaTime, InputManager input)
    {
        // Wire the inventory-overlay drop hook every frame in case Services lifetime changes.
        Services.SpawnItemDrop = (itemId, qty, pos) =>
        {
            _itemDrops.Add(new ItemDropEntity(itemId, qty, pos, _spriteAtlas) { DroppedByPlayer = true });
            Console.WriteLine($"[FarmScene] Drag-drop spawned: {itemId} x{qty} at ({pos.X:0},{pos.Y:0})");
        };

        bool ePressed = input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E);
        for (int i = _itemDrops.Count - 1; i >= 0; i--)
        {
            _itemDrops[i].UpdateWithPlayer(deltaTime, Player.GetFootPosition(), _inventory);
            if (ePressed && !_itemDrops[i].IsCollected)
                _itemDrops[i].TryManualPickup(Player.GetFootPosition(), _inventory);
            if (_itemDrops[i].IsCollected)
            {
                Console.WriteLine($"[FarmScene] Picked up: {_itemDrops[i].ItemId}");
                _itemDrops.RemoveAt(i);
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        _minimap.PreRender(
            Services.GraphicsDevice, sb, Map, Player, _enemies, _boss, _gridManager);
        base.Draw(sb);
    }

    protected override void OnDrawWorld(SpriteBatch sb, Rectangle viewArea)
    {
        _gridManager.DrawOverlays(sb, viewArea);
        _gridManager.DrawCrops(sb, viewArea);
        _chestManager.DrawBeforePlayer(sb, Player);
        foreach (var resource in _resourceManager.All)
            resource.DrawBeforePlayer(sb, Player);
        foreach (var drop in _itemDrops)
            drop.Draw(sb);
        foreach (var enemy in _enemies)
        {
            enemy.Draw(sb, Pixel);
            EnemyHealthBar.Draw(sb, Pixel, enemy.Position, enemy.Data.Height, enemy.HP, enemy.MaxHP);
        }
        if (_boss != null && _boss.IsAlive)
        {
            _boss.Draw(sb, Pixel);
            EnemyHealthBar.Draw(sb, Pixel, _boss.Position, _boss.Data.Height, _boss.HP, _boss.MaxHP);
        }
    }

    protected override void OnDrawWorldAfterPlayer(SpriteBatch sb, Rectangle viewArea)
    {
        _chestManager.DrawAfterPlayer(sb, Player);

        foreach (var resource in _resourceManager.All)
            resource.DrawAfterPlayer(sb, Player);

        _slash.Draw(sb, Pixel);
        _projectiles.Draw(sb, Pixel);
        DrawFarmZoneHint(sb, viewArea);

        if (_debugDraw)
        {
            Map.DrawCollisionDebug(sb, Pixel, Color.Magenta);

            foreach (var chest in _chestManager.All)
                DrawDebugRect(sb, chest.CollisionBox, Color.Orange * 0.5f);

            DrawDebugRect(sb, Player.CollisionBox, Color.Lime * 0.5f);
            DrawDebugRect(sb, Player.HitBox, Color.Yellow * 0.5f);

            if (_combat.Melee.IsSwinging)
            {
                var meleeHitbox = _combat.Melee.GetHitbox(Player.Position, Player.FacingDirection);
                DrawDebugRect(sb, meleeHitbox, Color.Red * 0.5f);
            }

            foreach (var enemy in _enemies)
            {
                DrawDebugRect(sb, enemy.CollisionBox, Color.Lime * 0.5f);
                DrawDebugRect(sb, enemy.HitBox, Color.Yellow * 0.5f);
            }

            if (_boss != null && _boss.IsAlive)
            {
                DrawDebugRect(sb, _boss.CollisionBox, Color.Lime * 0.5f);
                DrawDebugRect(sb, _boss.HitBox, Color.Yellow * 0.5f);
                if (_boss.IsBossSlashReady || _boss.IsWindingUp)
                    DrawDebugRect(sb, _boss.GetBossSlashHitbox(), Color.Red * 0.4f);
            }

            foreach (var proj in _projectiles.Active)
                DrawDebugRect(sb, proj.Hitbox, Color.Cyan * 0.6f);

            foreach (var resource in _resourceManager.All)
            {
                if (!resource.CollisionBox.IsEmpty)
                    DrawDebugRect(sb, resource.CollisionBox, Color.SaddleBrown * 0.5f);
            }
        }
    }

    protected override void OnDrawScreen(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        if (_promptChest != null && _pendingChestOpen == null)
        {
            var screenPos = Vector2.Transform(_promptChest.WorldAnchor, Services.Camera.GetTransformMatrix());
            _chestPrompt.Draw(sb, Font, Pixel, screenPos, "Press E to open chest");
        }

        _minimap.Draw(sb, new Rectangle(viewportWidth - 174, 86, 160, 160));

        if (_boss != null && _boss.IsAlive)
        {
            BossHealthBar.Draw(sb, Pixel, Font,
                "Skeleton King", _boss.HP, _boss.MaxHP, viewportWidth, viewportHeight);
        }
    }

    protected override void OnUnload()
    {
        Services.Time.OnDayAdvanced -= OnDayAdvanced;
        _minimap.Dispose();
    }

    private void ResolveEnemySeparation()
    {
        var bodies = new List<Entity>(_enemies);
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

                Vector2 delta = a.Position - b.Position;
                if (delta.LengthSquared() < 0.0001f)
                    delta = new Vector2(1f, 0f);
                delta.Normalize();

                int overlapX = Math.Min(ab.Right, bb.Right) - Math.Max(ab.Left, bb.Left);
                int overlapY = Math.Min(ab.Bottom, bb.Bottom) - Math.Max(ab.Top, bb.Top);
                float push = Math.Min(overlapX, overlapY) * 0.5f + 0.5f;

                bool bossIsA = a is BossEntity;
                bool bossIsB = b is BossEntity;
                if (bossIsA && !bossIsB)
                    b.Position -= delta * (push * 2f);
                else if (bossIsB && !bossIsA)
                    a.Position += delta * (push * 2f);
                else
                {
                    a.Position += delta * push;
                    b.Position -= delta * push;
                }
            }
        }
    }

    private void DrawDebugRect(SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(Pixel, rect, color);
    }

    private void DrawFarmZoneHint(SpriteBatch sb, Rectangle viewArea)
    {
        int startX = Math.Max(0, viewArea.Left / TileMap.TileSize);
        int startY = Math.Max(0, viewArea.Top / TileMap.TileSize);
        int endX = Math.Min(Map.Width - 1, viewArea.Right / TileMap.TileSize);
        int endY = Math.Min(Map.Height - 1, viewArea.Bottom / TileMap.TileSize);

        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        {
            if (Map.IsFarmZone(x, y) && _gridManager.GetCell(new Point(x, y)) == null)
                sb.Draw(Pixel,
                    new Rectangle(x * TileMap.TileSize, y * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize),
                    Color.Green * 0.08f);
        }
    }

    /// <summary>Spawn an item drop entity at the given world position.</summary>
    public void SpawnItemDrop(string itemId, int quantity, Vector2 worldPosition)
    {
        var drop = new ItemDropEntity(itemId, quantity, worldPosition, _spriteAtlas);
        _itemDrops.Add(drop);
        Console.WriteLine($"[FarmScene] Item drop spawned: {quantity}x {itemId}");
    }

    private void OnDayAdvanced()
    {
        Console.WriteLine($"[FarmScene] === Day {Services.Time.DayNumber} ===");
        _cropManager.OnDayAdvanced();
        _gridManager.OnDayAdvanced();
        Player.Stats.RestoreStamina();
        _spawner.Respawn(FarmSpawnPoints, _enemies);

        _boss = _spawner.SpawnBoss(FarmBossSpawn);
        SaveCurrentState();
    }

    private void InitializeChests(GameState state)
    {
        if (state.Chests != null && state.Chests.Count > 0)
        {
            NormalizeChestSaves(state.Chests);
            _chestManager.LoadFrom(state.Chests);
            return;
        }

        var starterChest = new ChestInstance("farm_starter_chest", "chest_wood", StarterChestTile);
        starterChest.Container.TryAdd("Health_Potion", 2);
        starterChest.Container.TryAdd("Smoked_Meat", 2);
        starterChest.Container.TryAdd("Iron_Sword", 1);
        _chestManager.Add(starterChest);
    }

    private void InitializeResources(GameState state)
    {
        if (state.Resources != null && state.Resources.Count > 0)
        {
            NormalizeResourceSaves(state.Resources);
            _resourceManager.LoadFrom(state.Resources);
            return;
        }

        _resourceManager.Add(new ResourceNode("farm_starter_tree", "tree_broadleaf_large", StarterTreeTile));
    }

    private void ResetResourcesForDebug()
    {
        _resourceManager.Clear();
        _resourceManager.Add(new ResourceNode("farm_starter_tree", "tree_broadleaf_large", StarterTreeTile));
        Console.WriteLine("[FarmScene] Resources reset with F6");
        SaveCurrentState();
    }

    private static void NormalizeChestSaves(List<ChestSaveData> chests)
    {
        foreach (var chest in chests)
        {
            if (chest.InstanceId != "farm_starter_chest")
                continue;

            chest.TileX = StarterChestTile.X;
            chest.TileY = StarterChestTile.Y;
        }
    }

    private static void NormalizeResourceSaves(List<ResourceSaveData> resources)
    {
        foreach (var resource in resources)
        {
            if (resource.InstanceId != "farm_starter_tree")
                continue;

            resource.TileX = StarterTreeTile.X;
            resource.TileY = StarterTreeTile.Y;
        }
    }

    private void SaveCurrentState()
    {
        var state = BuildCurrentStateSnapshot();
        SaveManager.Save(state);
        _loadedState = state;
        Services.GameState = state;
    }

    private GameState BuildCurrentStateSnapshot()
    {
        var state = _loadedState ?? new GameState();
        state.DayNumber = Services.Time.DayNumber;
        state.Season = Services.Time.Season;
        state.StaminaCurrent = Player.Stats.CurrentStamina;
        state.PlayerX = Player.Position.X;
        state.PlayerY = Player.Position.Y;
        state.GameTime = Services.Time.GameTime;
        state.FarmCells = _gridManager.GetSaveData();
        // CurrentScene is maintained by GameplayScene.LoadContent on each entry —
        // no need to re-stamp it here (removed in v10 so the single source of truth
        // is the base class, enabling boot-time restore to non-Farm scenes).
        state.BossKilled = _loadedState?.BossKilled ?? false;
        state.Chests = _chestManager.GetSaveData();
        state.Resources = _resourceManager.GetSaveData();

        _inventory.SaveToState(state);
        _mainQuest.SaveToState(state);
        return state;
    }

    private bool TryUseWorldTool(ToolType tool, Point tile)
    {
        var resource = _resourceManager.GetNodeAtTile(tile);
        if (resource == null)
            return false;

        ResourceToolKind toolKind = tool switch
        {
            ToolType.Axe => ResourceToolKind.Axe,
            ToolType.Pickaxe => ResourceToolKind.Pickaxe,
            _ => ResourceToolKind.None
        };

        bool gathered = resource.TryGather(toolKind, SpawnItemDrop);
        if (gathered)
        {
            Console.WriteLine($"[FarmScene] Gathered {resource.Data.DisplayName} ({resource.HitsRemaining}/{resource.Data.MaxHits})");
            SaveCurrentState();
        }

        return gathered;
    }

    private static Texture2D LoadTexture(GraphicsDevice device, string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }
}
