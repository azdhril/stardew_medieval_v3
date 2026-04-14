using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
/// Doors / chest seeding live in Plan 02 (this is the infrastructure shell).
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
    private BossEntity? _boss;
    private readonly Random _lootRng;
    private bool _clearedThisEntry;

    /// <summary>Fired once per scene-entry when the room becomes cleared.</summary>
    public event Action<string>? OnRoomCleared;

    public DungeonScene(ServiceContainer services, string roomId, string fromScene)
        : base(services, fromScene)
    {
        _roomId = roomId;
        // Seed loot RNG from DungeonState.RunSeed when available so chest/loot
        // contents are deterministic per run (fallback to time-based on cold start).
        int seed = services.Dungeon?.RunSeed ?? 0;
        _lootRng = seed != 0 ? new Random(seed) : new Random();
    }

    protected override string MapPath => _room.TmxPath;
    protected override string SceneName => $"Dungeon:{_room.Id}";

    /// <summary>Default spawn at top-center of map until rooms author entry markers.</summary>
    protected override Vector2 GetSpawn(string fromScene)
    {
        if (Services.Player != null && Services.Player.Position != Vector2.Zero)
            return Services.Player.Position;
        return new Vector2(160, 80);
    }

    protected override void OnLoad()
    {
        _room = DungeonRegistry.Get(_roomId);

        // Defensive: real dungeon entry initiates BeginRun in Plan 02. If a scene
        // loads with no active run (e.g. dev jump-to-room), seed one so flags work.
        if (Services.Dungeon == null)
            Services.Dungeon = new DungeonState();
        if (!Services.Dungeon.IsRunActive)
            Services.Dungeon.BeginRun();

        // Per-scene combat systems mirror FarmScene composition (independent state).
        _combat = new CombatManager(Services.Inventory!);
        _projectiles = new ProjectileManager();
        _projectiles.OnPlayerHit = (damage) => _combat.TryPlayerTakeDamage(Services.Player!, damage);
        _projectiles.OnEnemyHit = () => _combat.OnPlayerSpellHit(Services.Player!);

        // Per-scene chest manager. Seeding contents from DungeonState.ChestContents
        // happens in Plan 02 (where DungeonDoor + chest authoring lands).
        _chestManager = new ChestManager();
        Services.ChestManager = _chestManager;

        // Spawn from registry data (no hardcoded coords).
        _spawner = new EnemySpawner();
        _spawner.SpawnAll(_room.Spawns, _enemies);

        Console.WriteLine($"[DungeonScene:{_room.Id}] Loaded ({_enemies.Count} enemies, gated={_room.HasGatedExit})");
    }

    protected override bool OnPreUpdate(float deltaTime, InputManager input)
    {
        // Combat input + tick (mirrors FarmScene minus farming/world-tool logic).
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
                // Item drop entities for dungeon are wired in Plan 02; for now log + skip.
                Console.WriteLine($"[DungeonScene:{_room.Id}] Drop pending (Plan 02): {qty}x {id} at {pos}");
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
        return solids;
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

        if (exit.LeaveDungeon)
        {
            Services.Dungeon?.EndRun();
            Scene next = exit.TargetScene switch
            {
                "village" => new VillageScene(Services, "Dungeon"),
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
