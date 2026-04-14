using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
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
    private DungeonRoomData _room = null!;

    private CombatManager _combat = null!;
    private ProjectileManager _projectiles = null!;
    private EnemySpawner _spawner = null!;
    private ChestManager _chestManager = null!;
    private readonly List<EnemyEntity> _enemies = new();
    private readonly List<DungeonDoor> _doors = new();
    private BossEntity? _boss;
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
        _room = DungeonRegistry.Get(_roomId);

        // Defensive: real dungeon entry initiates BeginRun in VillageScene. If a
        // scene loads with no active run (e.g. dev jump-to-room), seed one.
        if (Services.Dungeon == null)
            Services.Dungeon = new DungeonState();
        if (!Services.Dungeon.IsRunActive)
        {
            Services.Dungeon.BeginRun();
            DungeonChestSeeder.Seed(Services);
        }

        // Per-scene combat systems mirror FarmScene composition (independent state).
        _combat = new CombatManager(Services.Inventory!);
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

            // Seed contents from the run-scoped ChestContents (D-10 idempotency).
            if (Services.Dungeon.ChestContents.TryGetValue(chestId, out var items))
            {
                foreach (var itemId in items)
                    chest.Container.TryAdd(itemId, 1);
            }
            _chestManager.Add(chest);
        }

        Console.WriteLine(
            $"[DungeonScene:{_room.Id}] Loaded ({_enemies.Count} enemies, {_doors.Count} doors, " +
            $"{_chestManager.All.Count} chests, gated={_room.HasGatedExit})");
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
            SpawnItemDrop = (id, qty, pos) =>
            {
                // Item drop entities for dungeon kills are a later-plan concern;
                // for now we log and skip (Plan 02 scope is rooms + chests).
                Console.WriteLine($"[DungeonScene:{_room.Id}] Drop pending: {qty}x {id} at {pos}");
            },
            BossFirstKill = !(Services.Dungeon?.BossDefeated ?? false),
            OnBossDefeated = _ =>
            {
                if (Services.Dungeon != null) Services.Dungeon.BossDefeated = true;
            },
        };
        CombatLoop.Update(deltaTime, ctx);
        _boss = ctx.Boss;

        // Death -> reset run + transition back to farm.
        if (!Player.IsAlive)
        {
            Console.WriteLine($"[DungeonScene:{_room.Id}] Player died -- resetting run");
            Services.Dungeon!.BeginRun();
            DungeonChestSeeder.Seed(Services);
            Player.HP = Player.MaxHP;
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

    protected override void OnDrawWorld(SpriteBatch sb, Rectangle viewArea)
    {
        _chestManager.Draw(sb);
        foreach (var door in _doors)
            door.DrawFallback(sb, Pixel);
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
