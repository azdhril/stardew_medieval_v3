using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Entities;
using stardew_medieval_v3.Progression;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// One scene class instantiated per dungeon room (id passed to ctor). Loads the
/// room's TMX, spawns the configured enemies via <see cref="EnemySpawner"/>, runs
/// the shared <see cref="CombatLoop"/>, gates exits on <see cref="DungeonState"/>
/// flags, and routes player death back to FarmScene with a fresh BeginRun.
///
/// Plan 02 adds: <see cref="DungeonDoor"/> parsing + collision + auto-open on
/// clear; chest seeding from <see cref="DungeonState.ChestContents"/> (idempotent
/// across re-entry); TMX object-group-driven spawns (fallback to registry).
/// </summary>
public class DungeonScene : GameplayScene
{
    private readonly string _roomId;
    private readonly DungeonRoomData _room;

    private CombatManager _combat = null!;
    private ProjectileManager _projectiles = null!;
    private EnemySpawner _spawner = null!;
    private ChestManager _chestManager = null!;
    private readonly List<EnemyEntity> _enemies = new();
    private readonly List<DungeonDoor> _doors = new();
    private readonly List<ItemDropEntity> _itemDrops = new();
    private Pathfinder? _pathfinder;
    private BossEntity? _boss;
    private bool _bossVictoryHandled;
    private ChestInstance? _promptChest;
    private ChestInstance? _pendingChestOpen;
    private readonly Random _lootRng;
    private bool _clearedThisEntry;

    /// <summary>Fired once per scene-entry when the room becomes cleared.</summary>
    public event Action<string>? OnRoomCleared;

    public DungeonScene(ServiceContainer services, string roomId, string fromScene)
        : base(services, fromScene)
    {
        _roomId = roomId;
        // Resolve the room in the ctor so MapPath is valid before the base class
        // calls Map.Load(MapPath, device) in LoadContent — which runs *before*
        // OnLoad(). Lazy-init crashed on first dungeon entry (NRE).
        _room = DungeonRegistry.Get(roomId);
        int seed = services.Dungeon?.RunSeed ?? 0;
        _lootRng = seed != 0 ? new Random(seed) : new Random();
    }

    protected override string MapPath => _room.TmxPath;
    protected override string SceneName => $"Dungeon:{_room.Id}";

    /// <summary>
    /// Prefer a "Spawn" TMX object named <c>from_&lt;prev&gt;</c>. Falls back to
    /// the preserved player position, then a safe interior default.
    /// </summary>
    protected override Vector2 GetSpawn(string fromScene)
    {
        if (Map != null)
        {
            var spawns = Map.GetObjectGroup("Spawn");
            string key = $"from_{fromScene}";
            foreach (var s in spawns)
            {
                if (string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase))
                    return s.Point;
            }
            // Fallback: any spawn marker in the group
            if (spawns.Count > 0) return spawns[0].Point;
        }

        if (Services.Player != null && Services.Player.Position != Vector2.Zero)
            return Services.Player.Position;
        return new Vector2(160, 120);
    }

    protected override void OnLoad()
    {
        // _room is assigned in the constructor (see note there).

        // Defensive: real dungeon entry initiates BeginRun in VillageScene. If a
        // scene loads with no active run (e.g. dev jump-to-room), seed one.
        if (Services.Dungeon == null)
            Services.Dungeon = new DungeonState();
        if (!Services.Dungeon.IsRunActive)
        {
            Services.Dungeon.BeginRun();
            DungeonChestSeeder.Seed(Services);
        }

        BossHealthBar.LoadContent(Services.GraphicsDevice);

        // Build A* walkability grid from map collision polygons
        _pathfinder = new Pathfinder();
        _pathfinder.BuildGrid(Map);

        // Per-scene combat systems mirror FarmScene composition (independent state).
        _combat = new CombatManager(Services.Inventory!);
        _combat.DamageBonus = Services.Progression?.BaseDamageBonus ?? 0;
        if (Services.Progression != null)
            Services.Progression.OnLevelUp += (_) => _combat.DamageBonus = Services.Progression.BaseDamageBonus;
        _projectiles = new ProjectileManager();
        _projectiles.OnPlayerHit = (damage) => _combat.TryPlayerTakeDamage(Services.Player!, damage);
        _projectiles.OnEnemyHit = () => _combat.OnPlayerSpellHit(Services.Player!);

        // Per-scene chest manager so FarmScene's chests are not shared.
        _chestManager = new ChestManager();
        Services.ChestManager = _chestManager;

        _spawner = new EnemySpawner();

        // Enemy spawns: prefer TMX EnemySpawns group; fall back to registry Spawns.
        var tmxSpawns = Map.GetObjectGroup("EnemySpawns");
        if (tmxSpawns.Count > 0)
        {
            var built = new List<(string, Vector2)>(tmxSpawns.Count);
            foreach (var s in tmxSpawns)
            {
                if (!s.Properties.TryGetValue("enemyId", out var id) || string.IsNullOrEmpty(id))
                {
                    Console.WriteLine($"[DungeonScene:{_room.Id}] Warning: EnemySpawn object missing enemyId");
                    continue;
                }
                built.Add((id, s.Point));
            }
            _spawner.SpawnAll(built, _enemies);
        }
        else
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] EnemySpawns group missing -- falling back to registry");
            _spawner.SpawnAll(_room.Spawns, _enemies);
        }

        // Doors: parse TMX Doors group.
        foreach (var d in Map.GetObjectGroup("Doors"))
        {
            string doorId = d.Properties.GetValueOrDefault("doorId", "door_unnamed");
            string targetRoomId = d.Properties.GetValueOrDefault("targetRoomId", "");
            var door = new DungeonDoor(doorId, targetRoomId, d.Bounds, sprite: null);
            // If the source room is already cleared this run (e.g. we returned
            // from a later room), pre-open the door so the player can pass again.
            if (Services.Dungeon.IsCleared(_room.Id))
                door.Open();
            _doors.Add(door);
        }

        // Chests: parse TMX ChestSpawns group, hydrate from DungeonState.ChestContents.
        foreach (var c in Map.GetObjectGroup("ChestSpawns"))
        {
            if (!c.Properties.TryGetValue("chestId", out var chestId) || string.IsNullOrEmpty(chestId))
            {
                Console.WriteLine($"[DungeonScene:{_room.Id}] Warning: ChestSpawn object missing chestId");
                continue;
            }
            string variant = c.Properties.GetValueOrDefault("spriteId", "chest_wood");
            var tile = new Point(c.Bounds.X / TileMap.TileSize, c.Bounds.Y / TileMap.TileSize);
            var chest = new ChestInstance(chestId, variant, tile);

            if (Services.Dungeon.IsChestOpened(chestId))
            {
                chest.SetOpenedInstant();
            }
            else if (Services.Dungeon.ChestContents.TryGetValue(chestId, out var items))
            {
                foreach (var itemId in items)
                    chest.Container.TryAdd(itemId, 1);
            }
            _chestManager.Add(chest);
        }

        // Boss room branch (Plan 03). On first entry per milestone, spawn the
        // BossEntity at the BossSpawn TMX point (fallback: map center). On
        // re-entry after victory, skip spawn and leave the exit door open.
        if (_room.IsBossRoom)
        {
            if (BossSpawnGate.ShouldSpawn(Services.Dungeon))
            {
                Vector2 bossPos = ReadBossSpawn();
                _boss = _spawner.SpawnBoss(bossPos);
                Console.WriteLine($"[DungeonScene:boss] Boss spawned at ({bossPos.X},{bossPos.Y})");
            }
            else
            {
                // Re-entry after victory — open exit door immediately, no boss.
                foreach (var d in _doors) d.Open();
                _bossVictoryHandled = true; // suppress re-fire of victory handler
                Console.WriteLine("[DungeonScene:boss] Re-entry — boss already defeated");
            }
        }

        Console.WriteLine(
            $"[DungeonScene:{_room.Id}] Loaded ({_enemies.Count} enemies, {_doors.Count} doors, " +
            $"{_chestManager.All.Count} chests, gated={_room.HasGatedExit}, boss={_boss != null})");
    }

    /// <summary>
    /// Resolve the boss spawn position from the TMX "BossSpawn" object group.
    /// Fallback (T-05-10 mitigation) is the horizontal center of the map at a
    /// safe vertical midpoint so the boss remains reachable if the group is
    /// missing or malformed.
    /// </summary>
    private Vector2 ReadBossSpawn()
    {
        if (Map != null)
        {
            var group = Map.GetObjectGroup("BossSpawn");
            foreach (var obj in group)
            {
                // Prefer Point objects; fall back to rectangle center.
                if (obj.Point != Vector2.Zero) return obj.Point;
                return new Vector2(
                    obj.Bounds.X + obj.Bounds.Width / 2f,
                    obj.Bounds.Y + obj.Bounds.Height / 2f);
            }
            Console.WriteLine("[DungeonScene:boss] Warning: BossSpawn missing, using map center");
        }
        // Map center fallback (30x20 tiles * 16px / 2)
        return new Vector2(240, 160);
    }

    protected override bool OnPreUpdate(float deltaTime, InputManager input)
    {
        _chestManager.Update(deltaTime);
        _promptChest = _chestManager.GetChestAtFacingTile(Player.GetFacingTile());

        // Chest open flow (mirrors FarmScene pattern).
        if (_pendingChestOpen != null)
        {
            if (_pendingChestOpen.IsOpen)
            {
                var chest = _pendingChestOpen;
                _pendingChestOpen = null;
                Services.Dungeon?.MarkChestOpened(chest.InstanceId);
                Services.SceneManager.PushImmediate(new ChestScene(
                    Services,
                    Services.Inventory!,
                    chest,
                    Services.Atlas!,
                    () =>
                    {
                        chest.BeginClose();
                        // Persist chest state so Take-All survives re-entry.
                        GameStateSnapshot.SaveNow(Services);
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

        _combat.HandleInput(input, Player);
        _combat.Update(deltaTime);

        if (_combat.ConsumeFireballRequest())
            _projectiles.SpawnFireball(Player.Position, Player.FacingDirection);

        var ctx = new CombatLoopContext
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
            BossFirstKill = !(Services.Dungeon?.BossDefeated ?? false),
            OnEnemyKilled = (enemy) =>
            {
                Services.Progression?.AwardXP(enemy.Data.Id);
                int gold = ProgressionManager.RollGold(enemy.Data.Id, _lootRng);
                SpawnItemDrop("Gold_Coin", gold, enemy.Position);
            },
            OnBossDefeated = _ =>
            {
                if (_bossVictoryHandled) return;
                _bossVictoryHandled = true;
                Console.WriteLine("[DungeonScene:boss] Boss defeated!");

                if (Services.Dungeon != null) Services.Dungeon.BossDefeated = true;
                Services.Quest?.Complete();

                foreach (var door in _doors) door.Open();

                GameStateSnapshot.SaveNow(Services);
            },
        };
        CombatLoop.Update(deltaTime, ctx);
        _boss = ctx.Boss;

        // Death -> apply penalty, reset run, transition back to farm.
        if (!Player.IsAlive)
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] Player died -- applying penalty, resetting run");

            // Apply death penalty BEFORE HP restore and save (D-13, Pitfall 6).
            var penalty = DeathPenalty.Apply(Services.Inventory!, _lootRng);
            var toast = Services.Toast;
            if (penalty.GoldLost > 0)
                toast?.Show($"Lost {penalty.GoldLost} gold", Color.Red);
            foreach (var itemId in penalty.ItemsLost)
                toast?.Show($"Lost: {ItemRegistry.Get(itemId)?.Name ?? itemId}", Color.OrangeRed);

            // Restore HP and stamina after penalty snapshot.
            Player.HP = Player.MaxHP;
            Player.Stats.RestoreStamina();

            // Persist BEFORE wiping: opened chests now survive BeginRun in memory, but we
            // must also flush them to disk so a crash-after-death does not forget them.
            GameStateSnapshot.SaveNow(Services);
            Services.Dungeon!.BeginRun();
            DungeonChestSeeder.Seed(Services);
            Services.SceneManager.TransitionTo(new FarmScene(Services, "DungeonDeath"));
            return true;
        }

        // Room-cleared detection (one-shot). Only gated rooms mark cleared.
        if (!_clearedThisEntry && _room.HasGatedExit && _enemies.Count == 0
            && (_boss == null || !_boss.IsAlive))
        {
            _clearedThisEntry = true;
            Services.Dungeon!.MarkCleared(_room.Id);
            // Open every door in this room (doors live on the clearing side).
            foreach (var door in _doors)
                door.Open();
            OnRoomCleared?.Invoke(_room.Id);
            Console.WriteLine($"[DungeonScene:{_room.Id}] Room cleared!");
        }

        return false;
    }

    protected override IEnumerable<Entity>? GetSolids()
    {
        var solids = new List<Entity>(_enemies);
        if (_boss != null && _boss.IsAlive) solids.Add(_boss);
        foreach (var chest in _chestManager.All)
            solids.Add(chest);
        // Doors only collide while closed (CollisionBox is Rectangle.Empty when open,
        // which PlayerEntity's overlap check treats as a no-op).
        foreach (var door in _doors)
            solids.Add(door);
        return solids;
    }

    protected override void OnPostUpdate(float deltaTime, InputManager input)
    {
        // Drive item drop physics/pickup (mirrors FarmScene pattern).
        for (int i = _itemDrops.Count - 1; i >= 0; i--)
        {
            _itemDrops[i].UpdateWithPlayer(deltaTime, Player.GetFootPosition(), Services.Inventory!);
            if (_itemDrops[i].IsCollected)
            {
                Console.WriteLine($"[DungeonScene:{_room.Id}] Picked up: {_itemDrops[i].ItemId}");
                _itemDrops.RemoveAt(i);
            }
        }
    }

    protected override void OnDrawWorld(SpriteBatch sb, Rectangle viewArea)
    {
        _chestManager.Draw(sb);
        foreach (var door in _doors)
            door.DrawFallback(sb, Pixel);
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
        _projectiles.Draw(sb, Pixel);
    }

    protected override void OnDrawScreen(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        if (_boss != null && _boss.IsAlive)
        {
            BossHealthBar.Draw(sb, Pixel, Font,
                "Skeleton King", _boss.HP, _boss.MaxHP, viewportWidth, viewportHeight);
        }
    }

    /// <summary>Spawn an item drop entity at the given world position.</summary>
    public void SpawnItemDrop(string itemId, int quantity, Vector2 worldPosition)
    {
        if (Services.Atlas == null)
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] Warning: no SpriteAtlas; dropping {quantity}x {itemId} skipped");
            return;
        }
        var drop = new ItemDropEntity(itemId, quantity, worldPosition, Services.Atlas);
        _itemDrops.Add(drop);
        Console.WriteLine($"[DungeonScene:{_room.Id}] Item drop spawned: {quantity}x {itemId}");
    }

    protected override bool HandleTrigger(string triggerName)
    {
        if (!_room.Exits.TryGetValue(triggerName, out var exit))
            return false;

        // Gated exit blocks until cleared (D-04/D-05).
        if (exit.RequiresCleared && !Services.Dungeon!.IsCleared(_room.Id))
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] Exit {triggerName} blocked -- room not cleared");
            return false;
        }

        // Boss door (r4 -> boss) additionally requires ALL main rooms cleared.
        if (exit.RoomId == "boss")
        {
            var dungeon = Services.Dungeon!;
            string[] mainRooms = { "r1", "r2", "r3", "r4" };
            foreach (var id in mainRooms)
            {
                if (!dungeon.IsCleared(id))
                {
                    Console.WriteLine($"[DungeonScene:{_room.Id}] Boss door blocked -- {id} not cleared");
                    return false;
                }
            }
        }

        if (exit.LeaveDungeon)
        {
            Services.Dungeon?.EndRun();
            Scene next = exit.TargetScene switch
            {
                "village" => new VillageScene(Services, exit.TargetTrigger ?? "Dungeon"),
                _         => new FarmScene(Services, "Dungeon"),
            };
            Services.SceneManager.TransitionTo(next);
            return true;
        }

        if (string.IsNullOrEmpty(exit.RoomId))
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] Exit {triggerName} has no RoomId -- ignoring");
            return false;
        }

        Services.SceneManager.TransitionTo(new DungeonScene(Services, exit.RoomId!, _room.Id));
        return true;
    }
}
