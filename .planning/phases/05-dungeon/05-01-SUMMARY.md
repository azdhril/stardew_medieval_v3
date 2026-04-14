---
phase: 05-dungeon
plan: 01
subsystem: dungeon-infrastructure
tags: [dungeon, scene, combat, save, testing]
requires:
  - "Scene stack with GameplayScene base and SceneManager transitions (Phase 04)"
  - "FarmScene combat composition: CombatManager, ProjectileManager, EnemySpawner, ChestManager"
  - "Save pipeline with GameState/SaveManager/MigrateIfNeeded and JSON roundtrip"
provides:
  - "xunit test project (tests/stardew_medieval_v3.Tests) with [Trait(\"Category\",\"quick\")] filter"
  - "DungeonState singleton (ClearedRooms, OpenedChestIds, ChestContents, BossDefeated, RunSeed, IsRunActive)"
  - "DungeonRegistry + DungeonRoomData covering r1/r2/r3/r3a/r4/r4a/boss with typed ExitData"
  - "Parameterized DungeonScene that drives a room by id, gates exits on clear, resets run on death"
  - "CombatLoop.Update helper + CombatLoopContext extracted from FarmScene"
  - "EnemySpawner accepting injected spawn lists (no hardcoded coords); SpawnBoss takes a Vector2"
  - "Save schema v8 with DungeonStateSnapshot and v7->v8 migration"
  - "GameStateSnapshot.SaveNow populating Chests, Resources, and Dungeon from services with prior-fallback"
affects:
  - "FarmScene.OnPreUpdate (combat block replaced by CombatLoop.Update)"
  - "FarmScene.OnLoad (wires Services.ChestManager, Services.ResourceManager, Services.Dungeon)"
  - "Save file format (bumped CURRENT_SAVE_VERSION 7 -> 8)"
tech-stack:
  added:
    - "xunit 2.5.3 + Microsoft.NET.Test.Sdk 17.8.0 + xunit.runner.visualstudio 2.5.3"
  patterns:
    - "Per-scene per-instance combat composition (no shared singletons leaking state)"
    - "Run-scoped singleton on ServiceContainer.Dungeon (mirrors Quest/Player lifetime)"
    - "Helper + Context struct to share combat tick between scenes without inheritance"
    - "DTO snapshot pattern (HashSet/Dict <-> List) for JSON roundtrip"
    - "Prior-fallback in snapshot build so services absent in one scene don't erase persisted data"
key-files:
  created:
    - "tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonStateTests.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonRegistryTests.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/RoomClearedTests.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/LootRollTests.cs"
    - "tests/stardew_medieval_v3.Tests/Save/SaveV7ToV8MigrationTests.cs"
    - "src/World/DungeonState.cs"
    - "src/Data/DungeonRegistry.cs"
    - "src/Data/DungeonRoomData.cs"
    - "src/Scenes/DungeonScene.cs"
    - "src/Combat/CombatLoop.cs"
  modified:
    - "stardew_medieval_v3.csproj (exclude tests/** from main compile globs)"
    - "src/Combat/EnemySpawner.cs (injected spawn list; SpawnBoss takes position)"
    - "src/Scenes/FarmScene.cs (use CombatLoop; wire Services.ChestManager/ResourceManager/Dungeon; DungeonDeath spawn branch)"
    - "src/Core/ServiceContainer.cs (add ChestManager, ResourceManager, Dungeon slots)"
    - "src/Core/GameState.cs (SaveVersion=8; Dungeon field)"
    - "src/Core/GameStateSnapshot.cs (SaveNow populates Chests, Resources, Dungeon with prior-fallback)"
    - "src/Core/SaveManager.cs (CURRENT_SAVE_VERSION=8; v7->v8 migration)"
decisions:
  - "Parameterized DungeonScene (one class, room id constructor arg) instead of 7 subclasses - keeps registry-driven authoring as source of truth"
  - "Run-scoped singleton on ServiceContainer (not a static) so tests and future save-load can swap instances"
  - "Helper + context struct for CombatLoop (not a GameplayScene base method) - FarmScene and DungeonScene stay independent, no surprise inheritance coupling"
  - "DungeonState RunSeed seeds per-scene _lootRng so re-entry produces identical drops (D-10 idempotency)"
  - "GetSaveData() API used (not BuildSaveModel) - plan had a name drift, implementation follows actual code"
  - "Test project compile-globs excluded in main csproj (<Compile Remove=\"tests/**\"/>) to prevent xunit Fact/Trait leaking into game compile"
metrics:
  duration: "~3h (session-reconstructed)"
  tasks_completed: 3
  files_created: 11
  files_modified: 7
  test_count: 14
  tests_passing: 14
  completed: 2026-04-14
---

# Phase 05 Plan 01: Dungeon Infrastructure Summary

Dungeon infrastructure shell: test project, run-scoped DungeonState singleton, 7-room DungeonRegistry, parameterized DungeonScene, extracted CombatLoop helper, and save schema v8 with the latent Chests/Resources drop bug fixed.

## What Was Built

### Task 1 - Test project bootstrap (commit 6aef83d)

- Created `tests/stardew_medieval_v3.Tests` xunit project with `Microsoft.NET.Test.Sdk 17.8.0`, `xunit 2.5.3`, `xunit.runner.visualstudio 2.5.3`, and `<ProjectReference>` back to the main csproj.
- Added `<Compile Remove="tests/**"/>` (and matching `None`/`EmbeddedResource` removes) to `stardew_medieval_v3.csproj` so the default `**/*.cs` glob does NOT pull test files into the main project (which lacks xunit refs).
- Scaffolded 6 test files (5 under `Dungeon/`, 1 under `Save/`) all marked `[Trait("Category","quick")]` so `dotnet test --filter Category=quick` runs them.
- Initial commit left `RoomClearedTests` and `LootRollTests` as `Assert.True(true)` stubs; Task 3 fills RoomClearedTests with real assertions.

### Task 2 - DungeonState + save v7->v8 + SaveNow fix (commit e3d5d3b)

- **DungeonState** (`src/World/DungeonState.cs`): run-scoped singleton with `ClearedRooms` (HashSet), `OpenedChestIds` (HashSet), `ChestContents` (Dict<string,List<string>>), `BossDefeated`, `RunSeed`, `IsRunActive`. `BeginRun()` clears all flags and assigns fresh RunSeed. `ToSnapshot`/`LoadFromSnapshot` handle DTO roundtrip.
- **DungeonRoomData** + **ExitData** record (`src/Data/DungeonRoomData.cs`): typed room config with Spawns, Chests, Exits dictionary, and `HasGatedExit`/`IsBossRoom`/`IsOptional` flags.
- **DungeonRegistry** (`src/Data/DungeonRegistry.cs`): static 7-room table (r1, r2, r3, r3a, r4, r4a, boss). Boss exit per D-14: `new ExitData(LeaveDungeon: true, TargetScene: "village", TargetTrigger: "castle_door")`.
- **ServiceContainer** (`src/Core/ServiceContainer.cs`): added `ChestManager?`, `ResourceManager?`, `Dungeon?` (DungeonState) slots.
- **Save schema bump**: `GameState.SaveVersion` default -> 8, `GameState.Dungeon` field added, `SaveManager.CURRENT_SAVE_VERSION` -> 8 with `case 7:` migration adding `state.Dungeon ??= new DungeonStateSnapshot();`.
- **Pitfall 1 fix**: `GameStateSnapshot.SaveNow` now populates `Chests`, `Resources`, and `Dungeon` from services with prior-snapshot fallback so saves in FarmScene (no dungeon) don't erase dungeon flags and dungeon saves don't drop chest state.

### Task 3 - CombatLoop + EnemySpawner refactor + DungeonScene shell (commit e62ffc4)

- **Pitfall 2 fix - EnemySpawner** (`src/Combat/EnemySpawner.cs`): removed hardcoded `SpawnPoints` array. `SpawnAll(IEnumerable<(string,Vector2)>, List<EnemyEntity>)`, `Respawn(points, enemies)`, `SpawnBoss(Vector2 position)`. FarmScene now owns its own `FarmSpawnPoints` static array and `FarmBossSpawn` constant.
- **CombatLoop** (`src/Combat/CombatLoop.cs`): static `Update(deltaTime, ctx)` running projectile tick, melee vs enemies, enemy AI/attack/death/loot, boss telegraph + summon-phase + melee + loot. `CombatLoopContext` carries Player/Enemies/Boss/Projectiles/Combat/LootRng/SpawnItemDrop + optional `OnBossDefeated`/`BossFirstKill`/`OnMeleeSwingStart`. FarmScene's ~100-line inline combat block replaced by a single `CombatLoop.Update(...)` call.
- **DungeonScene** (`src/Scenes/DungeonScene.cs`): inherits `GameplayScene`. Constructor `(ServiceContainer, string roomId, string fromScene)` seeds `_lootRng` from `Services.Dungeon?.RunSeed`. `OnLoad` creates per-scene `CombatManager`, `ProjectileManager`, `ChestManager`, and spawns via `_spawner.SpawnAll(_room.Spawns, _enemies)`. `OnPreUpdate` drives combat input + `CombatLoop.Update`, handles death (`BeginRun()` + heal + transition to `FarmScene("DungeonDeath")`), and fires one-shot `OnRoomCleared` when `HasGatedExit && _enemies.Count==0 && (_boss==null || !_boss.IsAlive)`. `HandleTrigger` looks up `_room.Exits[name]`, blocks on `RequiresCleared && !IsCleared`, and transitions to VillageScene/FarmScene (LeaveDungeon) or next DungeonScene (intra-dungeon).
- **FarmScene** wired: `Services.ChestManager = _chestManager; Services.ResourceManager = _resourceManager; Services.Dungeon ??= new DungeonState();` in `OnLoad`. Player respawn branch for `fromScene == "DungeonDeath"` placed at FarmBossSpawn.
- **RoomClearedTests** filled with real assertions: MarkCleared idempotency, BeginRun reset, r3a/r4a are optional AND non-gated, r1 has `RequiresCleared` exit and responds to MarkCleared.

## Test Results

```
dotnet test --filter "FullyQualifiedName~Dungeon|FullyQualifiedName~Save"
Aprovado!  Falha: 0, Aprovado: 14, Ignorado: 0, Total: 14
```

14 tests passing:
- `DungeonStateTests` (4): BeginRun clears all flags, ChestContents persist across re-entry, boss kill flips BossDefeated, BeginRun resets after death.
- `DungeonRegistryTests` (3): all 7 rooms load, boss exit targets village with TargetTrigger, no orphan trigger names.
- `RoomClearedTests` (4): MarkCleared idempotency, BeginRun reset, optional rooms are non-gated, gated room has RequiresCleared exit.
- `SaveV7ToV8MigrationTests` (2): v7 JSON gets default DungeonState via reflection into private `MigrateIfNeeded`, v8 roundtrips ClearedRooms/OpenedChestIds/BossDefeated/RunSeed/ChestContents.
- `LootRollTests` (1): still a stub - real assertions pending Plan 02 when loot is actually rolled from chests.

`dotnet build` clean (1 pre-existing warning in `GameplayScene.cs:177`, out of scope per Rule 4 boundary).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Test files pulled into main csproj compile**
- **Found during:** Task 1 (first `dotnet test` run)
- **Issue:** Main csproj default `**/*.cs` glob compiled `tests/**/*.cs`, so xunit `Fact`/`Trait` attributes appeared unrecognized in main project build.
- **Fix:** Added `<ItemGroup><Compile Remove="tests/**"/><None Remove="tests/**"/><EmbeddedResource Remove="tests/**"/></ItemGroup>` to `stardew_medieval_v3.csproj`.
- **Commit:** 6aef83d

**2. [Rule 2 - Critical functionality] Plan referenced `BuildSaveModel()`; actual API is `GetSaveData()`**
- **Found during:** Task 2 (ChestManager/ResourceManager save-roundtrip wiring)
- **Issue:** Plan text used an invented method name. Actual managers expose `GetSaveData()` / `LoadSaveData()`.
- **Fix:** Used the real API throughout `GameStateSnapshot.SaveNow`.
- **Commit:** e3d5d3b

**3. [Rule 2 - Critical functionality] FarmScene never wired ChestManager/ResourceManager/Dungeon onto ServiceContainer**
- **Found during:** Task 2 (verifying Pitfall 1 fix)
- **Issue:** The new prior-fallback snapshot logic requires services to be findable via ServiceContainer. FarmScene was creating `_chestManager`/`_resourceManager` locally but never assigning them.
- **Fix:** Added `Services.ChestManager = _chestManager; Services.ResourceManager = _resourceManager; Services.Dungeon ??= new DungeonState();` in `FarmScene.OnLoad`.
- **Commit:** e3d5d3b (rolled into Task 2) and e62ffc4 (DungeonScene side).

**4. [Rule 1 - Bug] `GetBossLoot(bool bossAlreadyKilled)` semantics inversion**
- **Found during:** Task 3 (CombatLoop extraction)
- **Issue:** `BossEntity.GetBossLoot` takes `bool bossAlreadyKilled`; context exposes `BossFirstKill` for caller intuitiveness.
- **Fix:** `ctx.Boss.GetBossLoot(ctx.BossFirstKill == false)` at the call site.
- **Commit:** e62ffc4

Threat model T-05-01 (save tampering / schema drift) mitigated via the `MigrateIfNeeded` case-7 branch + `SaveV7ToV8MigrationTests`.

## Known Stubs

`tests/stardew_medieval_v3.Tests/Dungeon/LootRollTests.cs` is still `Assert.True(true)`. Real assertions depend on Plan 02 wiring loot rolls through `DungeonState.ChestContents` on BeginRun. Intentional: loot roll pathway does not exist until Plan 02 seeds chest contents.

## Handoff to Plan 02

- DungeonScene ctor + OnLoad + HandleTrigger are production-ready; Plan 02 adds DungeonDoor trigger in FarmScene that `TransitionTo(new DungeonScene(Services, "r1", "farm"))` after calling `Services.Dungeon.BeginRun()`.
- `_chestManager` is instantiated but not seeded - Plan 02 reads `_room.Chests` and populates from `Services.Dungeon.ChestContents` (frozen on BeginRun).
- `SpawnItemDrop` callback currently logs-and-skips inside DungeonScene; Plan 02 wires real `ItemDropEntity` spawning (see Plan 02 task list).
- `OnRoomCleared` event is already firing - Plan 02 subscribers can unlock doors visually.

## Self-Check: PASSED

- tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj: FOUND
- src/World/DungeonState.cs: FOUND
- src/Data/DungeonRegistry.cs: FOUND
- src/Data/DungeonRoomData.cs: FOUND
- src/Scenes/DungeonScene.cs: FOUND
- src/Combat/CombatLoop.cs: FOUND
- Commit 6aef83d: FOUND
- Commit e3d5d3b: FOUND
- Commit e62ffc4: FOUND
- 14/14 tests passing
