---
phase: 05-dungeon
plan: 02
subsystem: dungeon-rooms
tags: [dungeon, tmx, door, chest, trigger, loot-seed]
requires:
  - "Plan 05-01 DungeonState + DungeonRegistry + DungeonScene shell + CombatLoop"
  - "ChestInstance/ChestManager/ChestScene from Phase 04"
  - "TiledCS TMX parser + TileMap.Triggers + existing Collision polygon loader"
provides:
  - "DungeonDoor entity with IsOpen-gated CollisionBox and sprite-swap/fallback draw"
  - "DungeonChestSeeder static class (deterministic ChestContents roll per RunSeed)"
  - "TileMap.GetObjectGroup(name) generic API returning TmxObject (name/bounds/point/properties)"
  - "Six playable dungeon room TMXs: r1, r2, r3, r3a, r4, r4a (boss TMX deferred to Plan 03)"
  - "village.tmx enter_dungeon trigger routing to DungeonScene(r1, \"village\") via BeginDungeonRun"
  - "DungeonScene upgrades: TMX-driven enemy spawns / doors / chests / player entry spawn"
affects:
  - "VillageScene.HandleTrigger (adds enter_dungeon case + BeginDungeonRun helper)"
  - "VillageScene.GetSpawn (new Dungeon/dungeon_entrance spawn anchors)"
  - "DungeonScene.OnLoad (enemy/door/chest parsing, chest content hydration, pre-open cleared-room doors)"
  - "DungeonScene.GetSolids (now includes DungeonDoor entities)"
  - "DungeonScene.OnDrawWorld (renders chest manager + door fallback rects)"
  - "DungeonScene.HandleTrigger (boss door additionally checks r1-r4 cleared)"
  - "DungeonState.RunSeed (now has public setter — test seam)"
tech-stack:
  added: []
  patterns:
    - "TMX object groups as authoring source of truth: EnemySpawns/Doors/ChestSpawns/Spawn"
    - "Registry + TMX fallback: DungeonScene reads TMX group first, falls back to DungeonRegistry if missing (Pitfall 7 tolerance)"
    - "Idempotent chest seeding: ChestContents rolled once on BeginRun, read on each room entry"
    - "Door-as-Entity pattern: DungeonDoor extends Entity so GetSolids()/player-collision path is reused verbatim (no new collision code)"
    - "Test seams on save-state types (public setter on RunSeed) — trade strict invariant for testability where risk is low"
key-files:
  created:
    - "src/World/DungeonDoor.cs"
    - "src/World/DungeonChestSeeder.cs"
    - "assets/Maps/dungeon_tileset.tsx"
    - "assets/Maps/dungeon_r1.tmx"
    - "assets/Maps/dungeon_r2.tmx"
    - "assets/Maps/dungeon_r3.tmx"
    - "assets/Maps/dungeon_r3a.tmx"
    - "assets/Maps/dungeon_r4.tmx"
    - "assets/Maps/dungeon_r4a.tmx"
  modified:
    - "assets/Maps/village.tmx (+ enter_dungeon trigger @ 880,240 32x32)"
    - "src/World/TileMap.cs (+ GetObjectGroup + TmxObject record)"
    - "src/World/DungeonState.cs (RunSeed public setter)"
    - "src/Scenes/VillageScene.cs (+ enter_dungeon case + BeginDungeonRun helper + Dungeon spawn anchors)"
    - "src/Scenes/DungeonScene.cs (TMX-driven enemies/doors/chests + chest interact flow + boss gate + pre-open doors on re-entry)"
    - "tests/stardew_medieval_v3.Tests/Dungeon/LootRollTests.cs (3 real tests: determinism, seed variance, chest coverage)"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonRegistryTests.cs (+ EveryExit_HasMatchingTriggerInSourceTmx cross-check)"
decisions:
  - "Map dimensions: main rooms 30x17 (480x272 px), optional rooms 16x12 (256x192). Fits the 960x540 base with camera zoom and matches RESEARCH §Environment recommendation"
  - "Outer-wall collision as four rectangular objects (not per-tile polygons) — cheap, matches existing castle/village pattern"
  - "Doors placed INSIDE the walkable area in front of the exit trigger — avoids cutting gaps in the wall ring, keeps the player physically blocked by the door sprite itself (not a wall hole)"
  - "Boss-all-cleared check done in DungeonScene.HandleTrigger, NOT in DungeonRegistry — keeps the registry purely declarative"
  - "Doors open on room clear inside DungeonScene (iterate _doors, call Open) rather than subscribing to OnRoomCleared externally — reduces one level of indirection and keeps door lifetime tied to scene lifetime"
  - "Chest TMX objects carry chestId property only; spriteId defaults to 'chest_wood' via ChestRegistry. Avoids duplicating variant info in both TMX and registry"
  - "DungeonDoor.Draw handles sprite path; DrawFallback handles colored-rect path and receives a pixel texture from the scene. Two methods keep each path straightforward instead of smuggling pixel refs through Entity base"
  - "Test-friendly DungeonChestSeeder.Seed(DungeonState) overload so tests don't need a full MonoGame GraphicsDevice/SpriteBatch to instantiate ServiceContainer"
metrics:
  duration: "~1.5h"
  tasks_completed: 2
  files_created: 9
  files_modified: 7
  test_count: 17
  tests_passing: 17
  completed: 2026-04-14
---

# Phase 05 Plan 02: Dungeon Rooms Summary

Six playable dungeon rooms with gated doors, optional chest rooms, deterministic loot seeding per run, and a village → dungeon entry that works round-trip — everything the dungeon needs except the boss arena (Plan 03).

## What Was Built

### Task 1 — DungeonDoor + village entry + chest seeder scaffold (commit 995ffaa)

- **`src/World/DungeonDoor.cs`**: `Entity` subclass with `IsOpen` (private-set), `Open()`/`Close()` (both idempotent with logging), and `CollisionBox` that returns `Rectangle.Empty` when open so the existing `GetSolids()` collision path automatically stops blocking. `Draw(SpriteBatch)` handles the sprite-swap path (column 0 closed / column 1 open of a 2-frame sheet); `DrawFallback(SpriteBatch, Texture2D pixel)` is a second entry point used by `DungeonScene` to render a red (closed) / green (open) tinted rectangle when no sprite sheet is provided. Two draw methods avoid smuggling the scene's 1x1 pixel texture through `Entity`.
- **`src/World/DungeonChestSeeder.cs`**: `Seed(ServiceContainer)` + test-friendly `Seed(DungeonState)` overload. Builds a fresh `Random` from `DungeonState.RunSeed` and rolls a small `LootTable` (`Health_Potion`/`Iron_Sword`/`Mana_Crystal`/`Bones`) per chest declared in `DungeonRegistry`, writing results into `DungeonState.ChestContents`. D-10 idempotency: called once on dungeon entry, never re-rolled on room re-entry.
- **`assets/Maps/dungeon_tileset.tsx`**: minimal Tiled tileset referencing `Sprites/Buildings/3_Props_and_Buildings_16x16.png` (512x2240 @ 16px = 4480 tiles, 32 cols).
- **`assets/Maps/village.tmx`**: added `<object name="enter_dungeon" type="transition" x="880" y="240" width="32" height="32"/>` in the existing `Triggers` group.
- **`src/Scenes/VillageScene.cs`**: `HandleTrigger` now routes `enter_dungeon` through `BeginDungeonRun(Services)` → `TransitionTo(new DungeonScene(Services, "r1", "village"))`. `BeginDungeonRun` ensures `Services.Dungeon` exists, calls `BeginRun()` for a fresh `RunSeed`, and seeds chest contents. Two new spawn anchors (`Dungeon`, `dungeon_entrance`) land returning players at `(864, 280)` just outside the cave entrance.

### Task 2 — Six TMX rooms + TMX object-group API + DungeonScene consumer + tests (commit cfb5792)

- **`src/World/TileMap.cs`**: new `GetObjectGroup(string groupName)` that returns `List<TmxObject>`. `TmxObject` is a record with `Name`, `Bounds` (Rectangle world-px), `Point` (Vector2 object-center), and a case-insensitive `Properties` dict. Missing groups return an empty list (Pitfall 7: scene falls back gracefully).
- **Six TMX rooms**:
  - `dungeon_r1.tmx` (30x17): N gated door → r2, S trigger → village (exit). 2 skeletons.
  - `dungeon_r2.tmx` (30x17): N gated door → r3, S trigger → r1. 3 skeletons.
  - `dungeon_r3.tmx` (30x17): N gated door → r4, E ungated trigger → r3a, S trigger → r2. 1 skeleton + 1 dark mage.
  - `dungeon_r3a.tmx` (16x12): optional. W ungated trigger → r3. 1 chest `dungeon_r3a_chest`. No enemies.
  - `dungeon_r4.tmx` (30x17): N gated door → boss (additionally requires r1–r4 cleared), E ungated trigger → r4a, S trigger → r3. 1 golem + 1 dark mage.
  - `dungeon_r4a.tmx` (16x12): optional. W ungated trigger → r4. 1 chest `dungeon_r4a_chest` + 2 skeletons.
  Each TMX has: `Ground` tile layer (all tile 1), `Collision` group (outer ring), `Triggers`, optional `Doors`, optional `EnemySpawns`, optional `ChestSpawns`, and a `Spawn` group with `from_<prev>` markers so `GetSpawn(fromScene)` lands the player at the right threshold.
- **`src/Scenes/DungeonScene.cs`**:
  - `OnLoad` parses TMX `EnemySpawns` (with `enemyId` property) and falls back to `DungeonRegistry.Rooms[id].Spawns` if the group is empty — keeps the plan's both-paths-work contract.
  - Parses `Doors` (`doorId`/`targetRoomId` properties), instantiates `DungeonDoor`, and pre-opens doors whose source room is already cleared this run (so backtracking through a cleared room doesn't re-block the door).
  - Parses `ChestSpawns` (`chestId` property), instantiates `ChestInstance`, and hydrates `Container` from `Services.Dungeon.ChestContents[chestId]`. Variant defaults to `chest_wood`.
  - `OnPreUpdate` wires the chest-open flow mirroring FarmScene: face chest → `E` → `BeginOpen` → push `ChestScene` overlay → on close, `BeginClose` + `GameStateSnapshot.SaveNow`. `Services.Dungeon.MarkChestOpened(chestId)` fires when the overlay opens so re-entry shows the opened sprite even if the player leaves items inside.
  - `GetSolids` now includes `_doors` so `PlayerEntity.Update` collision-check blocks closed doors automatically (Pitfall 7 fix).
  - `OnDrawWorld` renders `_chestManager.Draw(sb)` and `door.DrawFallback(sb, Pixel)` for each door.
  - Room-clear block now iterates `_doors` and calls `Open()` — no event subscription needed since doors are scene-local.
  - `HandleTrigger` now also blocks the boss door until all main rooms (r1–r4) are cleared. When leaving the dungeon, uses `exit.TargetTrigger` as the `fromScene` arg so `VillageScene.GetSpawn("dungeon_entrance")` lands correctly.
  - Death path additionally calls `DungeonChestSeeder.Seed(Services)` so the fresh run has fresh chest contents (D-13).
- **`src/World/DungeonState.cs`**: `RunSeed` now has a public setter (test seam) so LootRollTests can construct deterministic states without invoking the random `BeginRun`.
- **`tests/.../LootRollTests.cs`**: three real tests — `Seed_IsDeterministic_ForSameSeed` (two equal seeds produce identical `ChestContents`), `Seed_DiffersForDifferentSeeds` (at least one chest differs between two seeds), `Seed_PopulatesAllRegisteredChests` (every chestId in the registry appears as a key).
- **`tests/.../DungeonRegistryTests.cs`**: new `EveryExit_HasMatchingTriggerInSourceTmx` parses each (non-boss) room TMX with `System.Xml.Linq` and asserts every `Exits` key exists as a `<object name=...>` inside a `Triggers` object group. `FindRepoRoot` walks up from the test `AppContext.BaseDirectory` until it finds a dir containing `assets/Maps`.

## Test Results

```
dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Dungeon"
Aprovado!  Falha: 0, Aprovado: 17, Ignorado: 0, Total: 17
```

17/17 passing. Plan 02 added 4 tests (3 in LootRoll + 1 in DungeonRegistry). `dotnet build` clean (1 pre-existing warning in `GameplayScene.cs:177`, out of scope per Rule 4 boundary).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking] Plan snippet used `d.ItemId` on `LootTable.Roll` result**
- **Found during:** Task 2 (writing `DungeonChestSeeder.Seed`)
- **Issue:** Plan text has `table.Roll(rng).Select(d => d.ItemId)` but `LootTable.Roll` returns `List<(string itemId, int quantity)>` tuples, not `LootDrop` records.
- **Fix:** Used `.Select(d => d.itemId)` (tuple element name).
- **Commit:** 995ffaa

**2. [Rule 2 — Critical functionality] Plan's `Gold_Coin` item does not exist in items.json**
- **Found during:** Task 2 (writing seeder's LootTable)
- **Issue:** Plan used four items including `Gold_Coin`, which is absent from `src/Data/items.json`. An invalid item id would silently fail to add to `ChestInstance.Container` (or worse, log-skip in ItemRegistry), reducing chest content to zero items.
- **Fix:** Replaced `Gold_Coin` with existing loot item `Bones` (common loot already used by skeleton drops). Kept the same drop-chance of 0.9 so loot feel is preserved.
- **Commit:** cfb5792

**3. [Rule 2 — Critical functionality] `DungeonState.RunSeed` was private-set, blocking deterministic tests**
- **Found during:** Task 2 (writing LootRollTests)
- **Issue:** `BeginRun()` assigns `RunSeed = new Random().Next()` every time. To test determinism across two runs, tests need to control RunSeed explicitly without touching reflection.
- **Fix:** Changed `public int RunSeed { get; private set; }` → `public int RunSeed { get; set; }`. Low risk: RunSeed has always been a value type with no invariants; the only invariant is "set exactly once per run", which remains observed by all production callers.
- **Commit:** cfb5792

**4. [Rule 2 — Critical functionality] Boss-gated door required ALL main rooms cleared, not just the source room**
- **Found during:** Task 2 (reviewing plan bullet 5 in Task 2 action list)
- **Issue:** Plan specified extending `ExitData` with `RequiresAllMainRoomsCleared`. Adding a second gating flag risked muddying the registry. Instead, the check is localized to `DungeonScene.HandleTrigger`: if `exit.RoomId == "boss"`, verify all of `{r1, r2, r3, r4}` are in `ClearedRooms`.
- **Fix:** Boss check lives in scene logic; registry stays purely declarative (one knob per exit). `r4.exit_r4_to_boss` still has `RequiresCleared=true` (single-room gate), and DungeonScene adds the multi-room gate for the boss target specifically.
- **Commit:** cfb5792 (intentional deviation from plan, documented here per `ExitData` record left untouched).

**5. [Rule 3 — Blocking] Chest overlay flow was not wired in DungeonScene**
- **Found during:** Task 2 (bringing rooms online)
- **Issue:** Plan 01 left chest wiring deferred. Without a chest-open flow in DungeonScene, opening chests in r3a/r4a would do nothing (no overlay push).
- **Fix:** Ported the FarmScene chest-open flow (promptChest → `E` → `BeginOpen` → push `ChestScene` → on close `BeginClose` + save). Also calls `Services.Dungeon.MarkChestOpened(chestId)` when the overlay pushes, so re-entering a room shows the opened sprite even if the player took nothing.
- **Commit:** cfb5792

### Auth gates

None.

## Known Stubs

- **Boss room TMX (`dungeon_boss.tmx`) is deferred to Plan 03.** `DungeonRegistryTests.EveryExit_HasMatchingTriggerInSourceTmx` explicitly skips `room.Id == "boss"` until then.
- **DungeonDoor sprite swap uses the colored-rectangle fallback** (red = closed, green = open). The sprite sheet path works (column 0/1 slicing) but no dungeon door art has been authored yet. This is acceptable per RESEARCH §Environment fallback — visible and functional during bring-up. Door art is deferred to a polish pass / Plan 03.
- **Dungeon enemy drops are still log-and-skip** (`SpawnItemDrop` callback in DungeonScene). Wiring real `ItemDropEntity` pickup for dungeon kills is noted in the Plan 01 handoff and remains for Plan 03 or a polish plan.

## Threat Flags

None. All TMX parsing uses existing `TileMap.GetObjectGroup` code paths and ships with the game (no remote-TMX surface). Chest content hydration goes through `ChestInstance.Container.TryAdd`, which validates item ids via `ItemRegistry.Get` (unknown ids are skipped with a warning — mitigates T-05-06 re-entered from a hand-edited save).

## Handoff to Plan 03

- **Boss room TMX (`dungeon_boss.tmx`)**: author using the same 30x17 / object-group contract. Required groups: `Collision`, `Triggers` (`exit_boss_to_village`), `EnemySpawns` (or leave empty and rely on `DungeonRegistry` spawn-less entry to invoke `_spawner.SpawnBoss` — plan 03 will add the boss-spawn branch), `Spawn` (`from_r4` entry marker).
- **DungeonDoor sprite**: a 2-frame 16×16 sheet is enough. DungeonScene currently calls `DrawFallback` — switch to `Draw` by passing the sheet into the DungeonDoor ctor in `OnLoad` once the asset lands.
- **Boss-cleared → village-exit trigger**: `Services.Dungeon.BossDefeated` is already flipped by `CombatLoop`'s `OnBossDefeated` callback (see DungeonScene ctx), and Plan 01's `exit_boss_to_village` is `LeaveDungeon: true, TargetScene: "village", TargetTrigger: "castle_door"`. Plan 03 just needs to wire the boss-room scene entry (spawn boss if `!BossDefeated`) and authoring the TMX.
- **Dungeon item drops**: `CombatLoopContext.SpawnItemDrop` currently logs. Port the `ItemDropEntity` path from FarmScene to DungeonScene for dungeon-kill drops.

## Self-Check: PASSED

- src/World/DungeonDoor.cs: FOUND
- src/World/DungeonChestSeeder.cs: FOUND
- src/World/TileMap.cs (GetObjectGroup): FOUND
- assets/Maps/dungeon_tileset.tsx: FOUND
- assets/Maps/dungeon_r1.tmx: FOUND
- assets/Maps/dungeon_r2.tmx: FOUND
- assets/Maps/dungeon_r3.tmx: FOUND
- assets/Maps/dungeon_r3a.tmx: FOUND
- assets/Maps/dungeon_r4.tmx: FOUND
- assets/Maps/dungeon_r4a.tmx: FOUND
- Commit 995ffaa: FOUND
- Commit cfb5792: FOUND
- 17/17 dungeon tests passing (13 Plan 01 + 4 Plan 02)
- `dotnet build` clean (pre-existing warning out of scope)
