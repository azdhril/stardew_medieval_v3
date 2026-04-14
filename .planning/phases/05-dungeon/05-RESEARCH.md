# Phase 5: Dungeon - Research

**Researched:** 2026-04-14
**Domain:** MonoGame 2D top-down — multi-scene dungeon with per-room combat gating, chests, boss
**Confidence:** HIGH

## Summary

Phase 5 sits on top of a mature codebase: scenes, triggers, combat, chests, loot tables, quest state, and save infrastructure already exist and match almost 1:1 what Phase 5 requires. **The dungeon is primarily a composition and glue problem, not an invention problem.**

Three things already exist that drop in directly: `ChestInstance` + `ChestManager` + `ChestScene` (drag-and-drop overlay identical to the D-08/D-09 spec); `GameplayScene` base (map + HUD + triggers + F5 save + PauseScene); and `TriggerZone` + scene transitions (`VillageScene.HandleTrigger` is the canonical pattern). The combat loop from `FarmScene.OnPreUpdate` (melee hitbox vs enemy list, enemy AI, boss, projectile update, loot drops) is battle-tested and must be extracted into something reusable by DungeonScene.

Four things need to be built from scratch: (1) a per-room data-driven spawn system (the current `EnemySpawner` hardcodes farm positions — it must be refactored to accept a spawn list or be replaced by TMX-authored spawn objects); (2) a room-cleared event / door-state toggle (currently no code listens for "all enemies dead"); (3) a `BossEntity` generalization that is not Skeleton-King-specific, OR reuse the existing one for the dungeon boss; (4) `GameState` + `GameStateSnapshot` extension for dungeon persistence (cleared-room flags, opened-chest flags, run-state). Critically, `GameStateSnapshot.SaveNow` currently silently drops `Chests` and `Resources` on non-Farm saves — **this is a latent bug** and Phase 5 must fix it or cleared-room state will evaporate.

**Primary recommendation:** Build a shared `DungeonScene : GameplayScene` that takes a `DungeonRoomId` parameter (one TMX per room per D-01), and a `DungeonState` service that tracks run-global flags (cleared rooms, opened chests, per-run loot seed). Reuse `ChestManager` + `ChestScene` verbatim for D-07..D-10. Extract combat update from `FarmScene` into a reusable `CombatLoop` helper to keep DungeonScene from duplicating 200 lines. On death, `DungeonState.Reset()` clears all per-run flags and FarmScene respawn logic (lines 429-434) already exists — just redirect destination.

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Um arquivo TMX por sala (7 TMXs). Transições entre salas usam `TriggerZone` e troca de cena.
- **D-02:** Layout linear: 4 salas principais obrigatórias em sequência + 2 salas opcionais como desvios laterais + 1 boss room. Total: 7 salas.
- **D-03:** Mapas hand-authored (Tiled), não procedurais.
- **D-04:** Porta com dois estados visuais: fechada (bloqueia collision + transição) e aberta. Sprite-swap simples.
- **D-05:** Porta abre automaticamente quando todos os inimigos da sala são derrotados (listener → `OnRoomCleared` → troca sprite + habilita TriggerZone de saída).
- **D-06:** Salas opcionais não são gated — entrada livre; o baú é o incentivo.
- **D-07:** Um baú por sala opcional → 2 baús totais. Drop do boss é separado (garantido) e não usa baú.
- **D-08:** Interação: E perto do baú → overlay que pausa gameplay com inventário do jogador lado-a-lado com os slots do baú.
- **D-09:** Drag-and-drop do baú para o inventário (reusa sistema existente). Itens não coletados persistem até dungeon resetar.
- **D-10:** Baú abre 1x por run (sprite muda). Conteúdo gerado na entrada da dungeon via `LootTable`.
- **D-11:** Entrada: `TriggerZone` em ponto visível no `village.tmx`. Zero diálogo.
- **D-12:** Boss room tem porta própria que só abre após todas as 4 salas principais limpas. Flag `room_N_cleared` para as principais.
- **D-13:** Morte → respawn na fazenda com HP cheio + dungeon reseta completamente (inimigos respawnam, baús refecham, portas refecham, loot re-rolla).
- **D-14:** Derrotar boss = dungeon completa. Boss solta loot garantido como `ItemDropEntity`. Saída volta pra fazenda/vila (planner decide).

### Claude's Discretion
- Tileset/arte das salas (medieval/caverna)
- Variedade de inimigos por sala (reusar tipos da Phase 3)
- Decoração e props
- Nome dos arquivos TMX (ex: `dungeon_r1.tmx`)
- Posição exata da entrada na vila
- Som/SFX

### Deferred Ideas (OUT OF SCOPE)
- Múltiplas dungeons / andares
- Dificuldade escalável / NG+
- Loot raro condicional
- Sons e música ambiente
- Cutscene de boss
- Penalidade de morte (gold/item) → Phase 6

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DNG-01 | 1 dungeon completa com 5-8 salas conectadas (linear com salas opcionais) | 7-TMX layout (D-01/D-02) + DungeonScene w/ room-id + TriggerZone transitions — all patterns already proven in Farm↔Village↔Castle↔Shop |
| DNG-02 | Progressão: matar todos inimigos para abrir porta | `OnRoomCleared` event on combat-loop enemy-list-empty detection; door sprite swap + TriggerZone activation. Combat loop exists in FarmScene.OnPreUpdate — extract and reuse. |
| DNG-03 | Baús de tesouro em salas opcionais com loot aleatório | `ChestInstance/Manager/Scene` already exist and match D-08/D-09 spec verbatim. `LootTable.Roll(Random)` already exists (Phase 3) for generating chest contents. |
| DNG-04 | Boss room como sala final | `BossEntity` exists (Skeleton King) — reuse or generalize. Victory → `_mainQuest.Complete()` (the placeholder hook from Phase 4 D-12) + boss loot via existing `GetBossLoot` path. |

## Standard Stack

### Core (all `[VERIFIED: codebase grep]` unless stated)
| Library / Module | Purpose | Why Standard |
|---|---|---|
| `MonoGame.Framework.DesktopGL 3.8.*` | Engine — already in `.csproj` | Project constraint, no change |
| `TiledCS 3.3.3` | TMX parsing via `TileMap.Load` | Already used for test_farm/village/castle/shop |
| `System.Text.Json` | Save serialization via `SaveManager` | Already used for `GameState` |
| `src/Core/GameplayScene.cs` | Base scene (map + player + HUD + triggers + F5 + Pause) | All hub scenes inherit it; DungeonScene must too |
| `src/World/TileMap.cs` | TMX loader + polygon collision + `Triggers` object-group parsing | Works as-is; dungeon rooms just need `Triggers` object group in TMX |
| `src/World/TriggerZone.cs` | Named AABB record | One-liner record; `GameplayScene.Update` auto-dispatches on player intersect |
| `src/World/ChestManager.cs` + `ChestInstance.cs` | World chest entity + save roundtrip | **Drop-in reuse for D-07..D-10** |
| `src/Scenes/ChestScene.cs` | Drag-and-drop chest-player overlay, E/Esc to close | **Implements D-08/D-09 spec verbatim, already shipped** |
| `src/Combat/CombatManager.cs` | Player combat input, damage, i-frames, fireball cooldown | Per-scene instance OK (no global state) |
| `src/Combat/ProjectileManager.cs` | Fireball lifecycle | Per-scene instance |
| `src/Combat/LootTable.cs` + `LootDrop` | Independent-roll drop table | Reuse for chest contents (not just enemy drops) |
| `src/Combat/EnemyEntity.cs` + `EnemyData` + `EnemyRegistry` | Enemy archetypes (Skeleton/DarkMage/Golem) with AI FSM | Reuse archetypes, replace `EnemySpawner` spawn logic |
| `src/Combat/BossEntity.cs` | Skeleton King boss with wind-up attacks + summon phases | Reuse for dungeon boss (ctor takes Vector2 position) |
| `src/Core/GameStateSnapshot.cs` | `SaveNow(services)` entry point | Extend to cover dungeon state + fix Chests/Resources omission |

### What's NEW (to build this phase)
| Component | Purpose | Reuse Pattern |
|-----------|---------|---------------|
| `DungeonScene : GameplayScene` | One scene class, parameterized by room ID | Mirrors `VillageScene/CastleScene/ShopScene` |
| `DungeonRoomData` + registry | Static config per room (TMX path, spawn list, chest list, door rules) | Mirrors `CropRegistry` / `ChestRegistry` pattern (static class with `GetAll`) |
| `DungeonState` (run-scoped service) | Flags: cleared rooms, opened chests, boss defeated, this-run RNG seed | New field on `ServiceContainer` or on `GameState` |
| `DungeonDoor` entity | Visible barrier with open/closed sprite, collision toggle | Subclass `Entity`, similar to `ChestInstance` animation pattern |
| Room-cleared detection | When `_enemies.Count == 0 && boss-is-null` → fire event | Plug into DungeonScene's combat loop |
| Generalized `RoomEnemySpawner` | Takes `List<(string id, Vector2 pos)>` instead of hardcoded farm points | Refactor/clone `EnemySpawner.SpawnAll` |

### Alternatives Considered
| Instead of | Could Use | Why we didn't |
|---|---|---|
| 7 TMX files (D-01) | Single big TMX with camera-locked rooms | D-01 locks this. Also: per-TMX is simpler collision/spawn/trigger scoping |
| Custom chest overlay | Ship new UI | `ChestScene` exists and matches spec exactly |
| Rewriting combat update in DungeonScene | Extract `CombatLoop` helper | Saves ~200 lines of drift from FarmScene |
| Generic `IBoss` abstraction | Reuse `BossEntity` directly for dungeon boss | v1 has one boss; YAGNI. Phase 6+ can generalize if needed |

**Installation:** No new NuGet packages required. `[VERIFIED: .csproj inspection]`

## Architecture Patterns

### Recommended Project Structure
```
src/
├── Scenes/
│   └── DungeonScene.cs             # NEW — one class for all 7 rooms, takes roomId ctor arg
├── World/
│   ├── DungeonDoor.cs              # NEW — Entity w/ open/closed sprite, toggles CollisionBox
│   └── DungeonState.cs             # NEW — per-run flags (cleared rooms, chest-opened set, boss-defeated)
├── Data/
│   └── DungeonRegistry.cs          # NEW — static config: rooms, spawns, chest tiles, door targets
├── Combat/
│   ├── EnemySpawner.cs             # REFACTOR — accept spawn list param, stop hardcoding farm coords
│   └── CombatLoop.cs               # NEW (optional) — extracted from FarmScene.OnPreUpdate
├── Core/
│   ├── GameState.cs                # EXTEND — add DungeonState snapshot fields (v8 save)
│   └── GameStateSnapshot.cs        # FIX — include Chests/Resources/DungeonState in SaveNow()
└── Scenes/
    └── VillageScene.cs             # EXTEND — add "enter_dungeon" trigger case
```

```
assets/Maps/
├── dungeon_r1.tmx   # room 1 (entrance)
├── dungeon_r2.tmx   # room 2 (main)
├── dungeon_r3.tmx   # room 3 (main) + branch to r3a (optional)
├── dungeon_r3a.tmx  # optional chest room
├── dungeon_r4.tmx   # room 4 (main) + branch to r4a
├── dungeon_r4a.tmx  # optional chest room
└── dungeon_boss.tmx # boss room
```

Example names — exact names are Claude's discretion per D-case.

### Pattern 1: Parameterized Scene (room ID as ctor arg)
**What:** One `DungeonScene` class; instance-per-room driven by `RoomId` passed into constructor.
**When to use:** When all rooms share update/draw logic but differ only in map + spawn table + exits.
**Example:**
```csharp
// Mirrors VillageScene pattern (src/Scenes/VillageScene.cs:20)
public class DungeonScene : GameplayScene
{
    private readonly DungeonRoomData _room;

    public DungeonScene(ServiceContainer services, string roomId, string fromScene)
        : base(services, fromScene)
    {
        _room = DungeonRegistry.Get(roomId);
    }

    protected override string MapPath => _room.TmxPath;
    protected override string SceneName => $"Dungeon:{_room.Id}";

    protected override bool HandleTrigger(string triggerName)
    {
        // Exits are named per room: "exit_r1_to_r2", "exit_r3_to_r3a", "exit_boss_to_farm"
        if (!_room.Exits.TryGetValue(triggerName, out var target)) return false;
        if (target.RequiresCleared && !Services.Dungeon.IsCleared(_room.Id)) return false;

        if (target.LeaveDungeon)
            Services.SceneManager.TransitionTo(new FarmScene(Services, "Dungeon"));
        else
            Services.SceneManager.TransitionTo(new DungeonScene(Services, target.RoomId, _room.Id));
        return true;
    }
}
```

### Pattern 2: Room-Cleared Event
**What:** Detect empty enemy list once per frame; fire `OnRoomCleared` once; sync doors + persistent flag.
**When to use:** D-02/D-05 — main rooms gate on clear.
**Example:**
```csharp
// In DungeonScene.OnPreUpdate after enemy-removal pass:
if (!_clearedThisEntry && _enemies.Count == 0 && _room.HasGatedExit)
{
    _clearedThisEntry = true;
    Services.Dungeon.MarkCleared(_room.Id);
    foreach (var door in _doors)
        if (door.TargetRoom == _room.Id) door.Open();
    Console.WriteLine($"[DungeonScene] Room {_room.Id} cleared!");
}
```

### Pattern 3: Door as Entity (sprite swap + collision gate)
**What:** `DungeonDoor : Entity` with `IsOpen` property. When closed, `CollisionBox` blocks the player (returned via `GetSolids()`) and the trigger fires `return false`. When open, collision is empty and trigger routes to next scene.
**When to use:** D-04 / D-05 sprite-swap gates.
```csharp
// Pattern: ChestInstance animation model (src/World/ChestInstance.cs:30-44)
public class DungeonDoor : Entity
{
    public bool IsOpen { get; private set; }
    public override Rectangle CollisionBox => IsOpen ? Rectangle.Empty : _closedBounds;
    public void Open() { IsOpen = true; /* swap frame to open */ }
}
```

### Pattern 4: Run-Scoped State Service
**What:** `DungeonState` lives on `ServiceContainer` (like `Quest`, `Inventory`). Reset on entry/death.
```csharp
public class DungeonState
{
    public HashSet<string> ClearedRooms { get; } = new();
    public HashSet<string> OpenedChestIds { get; } = new();
    public bool BossDefeated { get; set; }
    public int RunSeed { get; private set; }

    public void BeginRun()
    {
        ClearedRooms.Clear();
        OpenedChestIds.Clear();
        BossDefeated = false;
        RunSeed = new Random().Next();
    }
}
```

### Anti-Patterns to Avoid
- **Duplicating FarmScene.OnPreUpdate into DungeonScene.** Extract the 100-line combat/projectile/enemy/boss loop into a `CombatLoop` helper that both scenes call. Otherwise melee-hit, knockback, separation, and loot-drop logic will drift.
- **Storing dungeon state on `FarmScene` fields.** FarmScene is unloaded on transition — private state evaporates. Use `ServiceContainer` or `GameState`.
- **Silent Chests/Resources drop in SaveNow.** `GameStateSnapshot.SaveNow` currently **omits** `Chests` and `Resources` (verified by reading the source). If DungeonScene triggers F5 or overlay-close save, chest contents and cleared-door state will vanish. Fix this before shipping Phase 5.
- **Per-room `EnemySpawner` using hardcoded farm coords.** Current `EnemySpawner.SpawnPoints` is a `static readonly` hardcoded array pointing at farm tiles. Refactor to accept an injected list or replace by reading spawn objects from the TMX.
- **Respawning FarmScene player on death from DungeonScene without scene transition.** Farm respawn currently relies on being in FarmScene (see lines 429-434). From DungeonScene, death must `Services.Dungeon.Reset()` + `TransitionTo(new FarmScene(...))` + heal player.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Chest overlay UI | New drag-and-drop window | `ChestScene` | Already implements D-08/D-09 including drag, double-click quick-transfer, Take All / Send All / Sort buttons |
| Chest data model | New `TreasureChest` class | `ChestInstance` + `ChestManager` + `ChestRegistry` | Already supports animation, save/load, `GetChestAtFacingTile` |
| Trigger zones | Custom Point-In-Rect + edge-detection | `TriggerZone` + `TileMap.Triggers` | Loaded automatically from TMX "Triggers" object group; `GameplayScene` dispatches on intersect |
| Scene transitions with fade | Manual screen fade code | `Services.SceneManager.TransitionTo(newScene)` | Used by Village/Castle/Shop already |
| Player-vs-enemy melee hitbox | New collision loop | `MeleeAttack.GetHitbox` + `HasHit` + `RecordHit` | Already in FarmScene.OnPreUpdate; extract, don't duplicate |
| Loot randomization | `Random` + list + if-else chain | `LootTable.Roll(Random)` | Independent DropChance rolls already tested in Phase 3 |
| Quest completion | Custom flag | `Services.Quest.Complete()` | Phase 4 exposes this as the placeholder hook for Phase 5 (D-12) |
| Save migration | Custom version checks | `SaveManager.MigrateIfNeeded` + bump `SaveVersion = 8` | Pattern established; add dungeon fields to `GameState` |
| Player death respawn | Full restart | `Player.HP = Player.MaxHP; Player.Position = farm_spawn` | Pattern from FarmScene:429-434. Just pair with scene transition + DungeonState.Reset |
| Boss entity | Generic enemy with high HP | `BossEntity` | Ships with wind-up telegraph, summon-phase minions, `GetBossLoot(killedBefore)` scaling — perfect for DNG-04 |

**Key insight:** Phase 5 is 70% gluing existing systems together. The risk is duplicating combat logic rather than extracting it. Budget 1 task for a `CombatLoop` refactor before building DungeonScene.

## Runtime State Inventory

N/A — Phase 5 is greenfield additive. No renames or migrations beyond a save-version bump (v7 → v8) for new `DungeonState` fields.

## Common Pitfalls

### Pitfall 1: GameStateSnapshot silently drops Chests/Resources on non-Farm save
**What goes wrong:** F5 or `SaveNow` called from DungeonScene writes a save with `Chests = new List<>()`, erasing any open-chest tracking.
**Why it happens:** `GameStateSnapshot.SaveNow` was written for FarmScene exits and hardcodes certain fields; `Chests` / `Resources` are never assigned from live managers because the snapshot doesn't know what scene owns them.
**How to avoid:** Either (a) extend `SaveNow` to accept optional chest/resource managers, or (b) route save through the active scene's `BuildCurrentStateSnapshot()` like `FarmScene` does. Recommended: (a) — add `ServiceContainer.ActiveChestManager` and read in `SaveNow`.
**Warning signs:** Re-entering a room after F5 and chests are reset; dungeon progress lost on manual save.

### Pitfall 2: EnemySpawner still hardcodes farm coordinates
**What goes wrong:** Reusing `EnemySpawner` for dungeon rooms spawns enemies at farm positions (400,200), etc.
**Why it happens:** `SpawnPoints` is `static readonly`; `SpawnAll()` ignores any input.
**How to avoid:** Refactor: `SpawnAll(List<(string, Vector2)> points)` or new `RoomEnemySpawner`. TMX can carry spawn objects in an "EnemySpawns" object group; parse like `TriggerZone`.
**Warning signs:** Enemies appear offscreen or in walls in dungeon rooms.

### Pitfall 3: Player death in dungeon doesn't leave the dungeon
**What goes wrong:** FarmScene's respawn code (`Player.HP = MaxHP; Position = TileCenterWorld(10,10)`) only runs when player is in FarmScene.
**Why it happens:** The check lives inside `FarmScene.OnPreUpdate`; in DungeonScene the player dies and remains in the dungeon.
**How to avoid:** In `DungeonScene.OnPreUpdate`, detect `!Player.IsAlive` → call `Services.Dungeon.Reset()` → heal → `SceneManager.TransitionTo(new FarmScene(Services, "DungeonDeath"))`.
**Warning signs:** Black-screen-of-death, player stuck at 0 HP, enemies keep attacking corpse.

### Pitfall 4: Boss door open but boss never spawned
**What goes wrong:** Boss room entered before `BossEntity` is instantiated; player walks through empty room, fighting nothing.
**Why it happens:** Boss spawn in `FarmScene.OnLoad` uses `_spawner.SpawnBoss()` unconditionally. Dungeon boss must spawn only on first entry per run, not if already defeated.
**How to avoid:** In `DungeonScene.OnLoad` (boss variant): `if (!Services.Dungeon.BossDefeated) _boss = _spawner.SpawnBoss(bossTile);`.
**Warning signs:** Boss room feels empty; MainQuest.Complete never fires.

### Pitfall 5: Chest contents regenerate every room entry
**What goes wrong:** Player opens chest, takes Item A, leaves room. Returns → chest has fresh loot.
**Why it happens:** `LootTable.Roll` is called on scene load without idempotency.
**How to avoid:** D-10 specifies contents generated on **dungeon entry**. Roll once in `DungeonState.BeginRun()` and store per-chest contents in `DungeonState.ChestContents[chestId]`. Reload from that map on room re-entry.
**Warning signs:** Infinite loot farming by re-entering optional rooms.

### Pitfall 6: Room-cleared flag not reset on death
**What goes wrong:** Player clears room 1, dies in room 2. On respawn, re-enters dungeon — room 1 door is already "open" because flag persists.
**Why it happens:** Cleared-room set is per-run but `DungeonState.Reset()` not wired into the death path.
**How to avoid:** Death handler MUST call `Services.Dungeon.BeginRun()` (not `Reset()` only) before scene transition. Enforce via unit test.
**Warning signs:** Skip past earlier rooms after death; enemies absent on re-entry.

### Pitfall 7: TMX polygon collision ignores DungeonDoor entity
**What goes wrong:** Door drawn as sprite but player walks through it because there's no collision geometry in the TMX.
**Why it happens:** Doors are dynamic entities, not TMX polygons; `TileMap.CheckCollision` only checks TMX data.
**How to avoid:** Expose doors via `GetSolids()` in DungeonScene, which `PlayerEntity.Update` already consults (see FarmScene:440-449 for pattern with chests + enemies as solids).
**Warning signs:** Player walks through closed doors visually; no progression gate.

## Code Examples

### Room registry (static config — mirrors `CropRegistry` / `ChestRegistry`)
```csharp
// Source: pattern from src/Data/ChestRegistry.cs
public static class DungeonRegistry
{
    public static readonly Dictionary<string, DungeonRoomData> Rooms = new()
    {
        ["r1"] = new DungeonRoomData
        {
            Id = "r1",
            TmxPath = "assets/Maps/dungeon_r1.tmx",
            Spawns = new() { ("Skeleton", new Vector2(200, 160)), ("Skeleton", new Vector2(280, 240)) },
            Chests = new(), // no chest in main rooms
            Exits = new()
            {
                ["exit_r1_to_village"] = new(LeaveDungeon: true),
                ["exit_r1_to_r2"] = new(RoomId: "r2", RequiresCleared: true),
            },
            HasGatedExit = true,
        },
        ["r3a"] = new DungeonRoomData
        {
            Id = "r3a",
            TmxPath = "assets/Maps/dungeon_r3a.tmx",
            Spawns = new(), // optional rooms — may be empty per D-06
            Chests = new() { ("chest_r3a", new Point(5, 4), "chest_wood") },
            Exits = new() { ["exit_r3a_to_r3"] = new(RoomId: "r3") },
            HasGatedExit = false,
        },
        // ... r2, r3, r4, r4a, boss
    };
    public static DungeonRoomData Get(string id) => Rooms[id];
}
```

### Chest seeding on dungeon entry (D-10)
```csharp
// Called from Village → "enter_dungeon" trigger handler, BEFORE first DungeonScene loads
public void BeginDungeonRun(ServiceContainer services, Random? rng = null)
{
    var dungeon = services.Dungeon;
    dungeon.BeginRun(); // clears all per-run state

    rng ??= new Random(dungeon.RunSeed);
    var chestLoot = new LootTable(new List<LootDrop>
    {
        new LootDrop("Health_Potion", 1.0f), // guaranteed
        new LootDrop("Iron_Sword",    0.25f),
        new LootDrop("Mana_Crystal",  0.4f),
    });

    foreach (var roomEntry in DungeonRegistry.Rooms)
    foreach (var (chestId, _, _) in roomEntry.Value.Chests)
    {
        dungeon.ChestContents[chestId] = chestLoot.Roll(rng);
    }
}
```

### Death respawn from DungeonScene (pattern from FarmScene:429-434)
```csharp
// In DungeonScene.OnPreUpdate, after combat update:
if (!Player.IsAlive)
{
    Console.WriteLine($"[DungeonScene:{_room.Id}] Player died — resetting dungeon run");
    Services.Dungeon.BeginRun(); // resets cleared rooms, opened chests, boss flag
    Player.HP = Player.MaxHP;
    Services.SceneManager.TransitionTo(new FarmScene(Services, "DungeonDeath"));
    return true;
}
```

### Boss victory → quest completion (D-14)
```csharp
// DungeonScene.OnPreUpdate (boss-room variant) after boss death handling
if (_boss != null && !_boss.IsAlive)
{
    Services.Dungeon.BossDefeated = true;
    var loot = _boss.GetBossLoot(priorKill: false);
    foreach (var (id, qty) in loot)
        SpawnItemDrop(id, qty, _boss.Position);

    Services.Quest?.Complete();   // trips MainQuest → Complete (Phase 4 D-12 hook)
    GameStateSnapshot.SaveNow(Services);
    _boss = null;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `FarmScene` directly constructed scenes in Phase 1 | `GameplayScene` base + `SceneManager.TransitionTo` | Phase 1 | Dungeon must use the base class, not reinvent |
| Legacy `WeaponId/ArmorId` save fields | `Equipment` dictionary | Save v5 | Dungeon additions bump to v8 cleanly |
| No chest system | `ChestInstance/Manager/Scene` | Save v6 (Phase, recent) | **Matches D-07..D-10 with zero new UI work** |
| `GameStateSnapshot.SaveNow` silently dropped Chests/Resources | (currently still broken) | N/A | **Fix during Phase 5 or dungeon persistence fails** |

**Deprecated/outdated:**
- `Game1.cs` as coordinator: now a thin shell; all logic is in scenes (ARCH-05 complete).
- Direct `SaveManager.Save(state)` calls outside FarmScene: prefer `GameStateSnapshot.SaveNow(services)`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `BossEntity` ctor takes arbitrary Vector2 and works outside FarmScene | Standard Stack / Code Examples | Medium — may need to decouple from farm-specific tuning (summon positions); verify during planning by reading BossEntity in full |
| A2 | `TileMap` TMX loader handles maps with no `Farmland` layer (dungeon rooms won't have farming) | Architecture | Low — `IsFarmZone` returns bool; missing layer likely just returns false, but planner should verify with a minimal dungeon TMX |
| A3 | `PlayerEntity` respawn (HP reset + position teleport) is idempotent across scenes | Pitfalls / Code Examples | Low — Player lives on ServiceContainer, survives scene swaps by design |
| A4 | Minimap rendering (`MinimapRenderer`) won't break on a dungeon-sized TMX | Standard Stack | Low-medium — not required for DNG criteria; can disable minimap in DungeonScene if it misbehaves |
| A5 | Phase 5 bumps `SaveVersion` from 7 to 8 | Save Integration | Low — pattern established (v3→v4→v5→v6→v7); `SaveManager.MigrateIfNeeded` already chains migrations |
| A6 | No existing dungeon tileset art (placeholder tiles OK for MVP) | Environment | Confirmed — `assets/Sprites/Scenario/` has only a tree sheet; `assets/Maps/` has no dungeon_*.tmx |

## Open Questions (RESOLVED)

1. **Where does the player exit after defeating the boss — farm or village?** RESOLVED: village at castle door (Plan 01 Task 2 / Plan 03 Task 1) so the player can talk to King for quest-complete dialogue and exercise the Phase 4 NPC-04 branch.

2. **Do optional rooms need their own enemies, or is the chest alone the reward?** RESOLVED: mixed — r3a empty (chest only, quick-pick), r4a has 2 Skeletons + chest (harder = better loot). Encoded in DungeonRegistry (Plan 02 Task 2).

3. **Minimum viable tileset — ship placeholders or block on art?** RESOLVED: ship placeholder (`Buildings/3_Props_and_Buildings_16x16.png` + flat-color floor, colored-rectangle doors). Art pass deferred. Not a blocker (Plan 02 Task 1/2).

4. **Enemy count per room — balance target?** RESOLVED: 2–3 per main room, scaling up toward boss (r1: 2 Skeletons; r4: 1 Golem + 1 DarkMage). Values tuned in DungeonRegistry (Plan 01 Task 2 / Plan 02 Task 2).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Build | ✓ | (project-confirmed) | — |
| MonoGame.Framework.DesktopGL 3.8.* | Runtime | ✓ | via .csproj | — |
| TiledCS 3.3.3 | TMX parsing | ✓ | via .csproj | — |
| Tiled map editor | Authoring 7 TMX files | Unknown (developer machine) | — | Hand-edit XML if needed, but strongly prefer Tiled |
| Dungeon tileset PNG | Room visuals | ✗ | — | Reuse `Buildings/3_Props_and_Buildings_16x16.png` + solid-color floor |
| Door sprite (open/closed frames) | D-04 visual swap | ✗ | — | Draw colored rectangle (red closed, green open) as placeholder |
| Boss room unique enemy sprite | DNG-04 | ✗ | — | Reuse `BossEntity` placeholder rect (scaled red) from Phase 3 |

**Missing dependencies with no fallback:**
- None — every missing art asset has a placeholder path.

**Missing dependencies with fallback:**
- Dungeon tileset → reuse existing Buildings sheet + flat floor
- Door sprite → colored rectangle with frame swap

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | **None currently — no test project exists** (`[VERIFIED: ls repo root, no *.Tests folder, no tests/ dir]`) |
| Config file | N/A |
| Quick run command | N/A — must install first |
| Full suite command | `dotnet test` (once test project exists) |

### Wave 0 Gaps (REQUIRED — no test infra exists)
- [ ] Add test project: `tests/stardew_medieval_v3.Tests.csproj` referencing `xunit` + `xunit.runner.visualstudio` + main project. Command: `dotnet new xunit -o tests/stardew_medieval_v3.Tests && dotnet add tests/*.csproj reference stardew_medieval_v3.csproj && dotnet sln add tests/*.csproj`
- [ ] `tests/Dungeon/DungeonStateTests.cs` — run reset, cleared-room tracking, chest-open tracking, save roundtrip
- [ ] `tests/Dungeon/DungeonRegistryTests.cs` — room exits resolve, boss room requires all main rooms cleared, no orphan trigger names
- [ ] `tests/Dungeon/RoomClearedTests.cs` — empty-enemy-list detection fires event exactly once
- [ ] `tests/Dungeon/LootRollTests.cs` — chest loot deterministic given seed (uses existing `LootTable.Roll(Random)`)
- [ ] `tests/Save/SaveV7ToV8MigrationTests.cs` — v7 save loads cleanly with no DungeonState fields; default values sensible

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DNG-01 | 7 rooms load and connect via triggers | unit (registry validation) | `dotnet test --filter DungeonRegistryTests` | Wave 0 |
| DNG-01 | Enter village → dungeon → navigate r1..boss | integration / manual UAT | manual — scene transitions not headless-testable in MonoGame | manual |
| DNG-02 | All-enemies-dead → door opens (state flag flips) | unit | `dotnet test --filter RoomClearedTests` | Wave 0 |
| DNG-02 | Closed door blocks player (collision) | integration / manual | manual — requires game loop | manual |
| DNG-03 | Chest contents rolled deterministically from LootTable | unit | `dotnet test --filter LootRollTests` | Wave 0 |
| DNG-03 | Drag-and-drop player↔chest transfers items | manual UAT | already passes per ChestScene existing | manual |
| DNG-03 | Chest state persists across room re-entry until reset | unit | `dotnet test --filter DungeonStateTests.ChestPersistence` | Wave 0 |
| DNG-04 | Boss defeated → `MainQuest.Complete()` fires | unit | `dotnet test --filter DungeonStateTests.BossCompletesQuest` | Wave 0 |
| DNG-04 | Boss defeated → BossDefeated flag saved | unit | same as above (save-roundtrip assertion) | Wave 0 |
| Death | Dying in dungeon respawns at farm + resets run | unit (state only) + manual UAT | `dotnet test --filter DungeonStateTests.DeathResetsRun` | Wave 0 |
| Save | v7 save migrates to v8 without crash | unit | `dotnet test --filter SaveV7ToV8MigrationTests` | Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter FullyQualifiedName~Dungeon` (targets only dungeon tests, < 10s)
- **Per wave merge:** `dotnet test` (full suite)
- **Phase gate:** Full suite green + manual UAT playthrough (7 rooms, die once, re-enter, kill boss) documented in VERIFICATION.md

## Project Constraints (from CLAUDE.md)

Extracted for planner compliance:
- Engine: **MonoGame 3.8 DesktopGL** — no change.
- Language: **C# 12 / .NET 8** — no change.
- Maps: **Tiled (.tmx/.tsx) via TiledCS** — Phase 5 stays in this pipeline.
- Resolution: **960×540** — dungeon rooms must fit this camera / design scope.
- Assets: **Pixel art medieval** — placeholder tiles acceptable but must look coherent with existing sprites.
- Platform: **Windows PC only**.

Code conventions (must follow):
- One public class per file; PascalCase filenames.
- `_camelCase` private fields; `PascalCase` public members.
- `TryXxx` / `IsXxx` naming for booleans (e.g., `TryOpen`, `IsCleared`).
- `On` prefix for event handlers (`OnRoomCleared`, `OnBossDefeated`).
- `[ModuleName]` prefixed `Console.WriteLine` for all logging (e.g., `[DungeonScene]`, `[DungeonState]`).
- Nullable reference types enabled — use `null!` forgiving operator for late-init fields, `?` for optional.
- One statement per line; Allman braces; 4-space indent.
- Triple-slash `/// <summary>` on every public class and method.
- Services injected via constructor (composition root is `FarmScene.OnLoad` + `ServiceContainer`).
- Events pattern: `OnX` public event, `?.Invoke(...)` with null-coalescing.
- `GSD Workflow Enforcement` — must start file-changing work via a GSD command; Phase 5 execution uses `/gsd-execute-phase`.

## Sources

### Primary (HIGH confidence — all verified by direct file reads in this session)
- `src/Core/GameplayScene.cs` — base scene lifecycle, trigger dispatch, global input, F5 save
- `src/Scenes/FarmScene.cs` — canonical combat update loop, chest prompt, enemy/boss management, save snapshot builder
- `src/Scenes/VillageScene.cs` — trigger-to-scene transition pattern
- `src/Scenes/ChestScene.cs` — drag-and-drop chest overlay implementing D-08/D-09 verbatim
- `src/World/ChestInstance.cs` + `ChestManager.cs` — entity + manager + save model
- `src/World/TriggerZone.cs` — record for named AABB zones
- `src/Combat/EnemySpawner.cs` — reveals hardcoded farm coords (refactor target)
- `src/Combat/CombatManager.cs` + `ProjectileManager.cs` + `BossEntity.cs` + `EnemyData.cs` — reusable combat primitives
- `src/Combat/LootTable.cs` — independent-roll drop rolling
- `src/Core/GameState.cs` — save schema at v7
- `src/Core/GameStateSnapshot.cs` — discovered Chests/Resources omission bug
- `src/Quest/MainQuest.cs` — `Complete()` hook exposed by Phase 4 (D-12)
- `src/Core/ServiceContainer.cs` — service injection pattern
- `.planning/phases/05-dungeon/05-CONTEXT.md` — all D-01..D-14 decisions
- `.planning/phases/04-world-npcs/04-CONTEXT.md` — scene/trigger patterns
- `.planning/REQUIREMENTS.md` — DNG-01..04 acceptance criteria
- `.planning/ROADMAP.md` — Phase 5 goal and dependencies
- `CLAUDE.md` — stack + conventions

### Secondary (MEDIUM)
None — everything cross-referenced against source.

### Tertiary (LOW)
None.

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — all claims verified against current source
- Architecture: **HIGH** — patterns lifted directly from existing VillageScene / FarmScene / ChestScene
- Pitfalls: **HIGH** — identified from actual code inspection (e.g., GameStateSnapshot omission is observable in source)
- Validation: **HIGH for unit coverage plan**, **MEDIUM for integration** (no test infra → Wave 0 must bootstrap one)
- Asset strategy: **HIGH** — directory listings confirm what's missing

**Research date:** 2026-04-14
**Valid until:** 2026-05-14 (30-day horizon; project is active and stable)

## RESEARCH COMPLETE
