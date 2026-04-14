---
phase: 05-dungeon
plan: 03
subsystem: dungeon-boss-quest-victory
tags: [dungeon, boss, quest, victory]
requires:
  - "Plan 05-01 DungeonState/DungeonScene/CombatLoop/EnemySpawner.SpawnBoss"
  - "Plan 05-02 TMX object-group API (Map.GetObjectGroup) + DungeonDoor + chest seeding"
  - "BossEntity + MainQuest.Complete + GameStateSnapshot.SaveNow + ItemDropEntity"
provides:
  - "assets/Maps/dungeon_boss.tmx (boss arena with BossSpawn + gated exit)"
  - "BossSpawnGate static helper encoding the 'once per milestone' contract"
  - "DungeonScene boss-room branch: spawn on first entry, skip on re-entry"
  - "DungeonScene boss-victory handler: loot + quest + door-open + save"
  - "Persistent BossDefeated semantics (NOT reset by BeginRun)"
  - "VillageScene castle_door spawn mapping for boss->village return (D-14)"
  - "BossVictoryTests (4) + boss-room registry test + boss TMX cross-check"
affects:
  - "src/World/DungeonState.cs (BeginRun no longer clears BossDefeated)"
  - "src/Scenes/DungeonScene.cs (boss spawn/victory/drops integrated)"
  - "src/Scenes/VillageScene.cs (castle_door spawn anchor added)"
  - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonStateTests.cs (test renamed + flipped)"
  - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonRegistryTests.cs (boss TMX check unskipped)"
tech-stack:
  added: []
  patterns:
    - "Testable gate helper (BossSpawnGate.ShouldSpawn) extracted from scene load logic"
    - "One-shot victory handler guarded by _bossVictoryHandled flag"
    - "TMX object-group fallback (map center if BossSpawn missing) per T-05-10"
    - "Save-via-GameStateSnapshot.SaveNow on victory so milestone persists even if player quits before leaving"
key-files:
  created:
    - "assets/Maps/dungeon_boss.tmx"
    - "src/Combat/BossSpawnGate.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/BossVictoryTests.cs"
  modified:
    - "src/Scenes/DungeonScene.cs"
    - "src/Scenes/VillageScene.cs"
    - "src/World/DungeonState.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonStateTests.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonRegistryTests.cs"
decisions:
  - "BossDefeated is a persistent milestone, NOT reset by DungeonState.BeginRun (D-14 'derrotar o boss = dungeon completa'). Locked via BossVictoryTests.BeginRun_DoesNotClear_BossDefeatedMilestone and DungeonStateTests.BeginRun_ClearsRunFlags_ButPreservesBossDefeatedMilestone."
  - "castle_door -> (208,128) in VillageScene.Spawns reuses the existing Castle-return position instead of authoring a new Spawn object in village.tmx. D-14 says 'near castle door'; same tile is exactly that."
  - "ReadBossSpawn fallback is the map center (240,160) so T-05-10 (missing BossSpawn group) is mitigated without a crash."
  - "Extracted BossSpawnGate helper instead of inlining !Dungeon.BossDefeated in OnLoad so unit tests can cover the gate contract directly."
  - "SpawnItemDrop became a real method in DungeonScene (mirroring FarmScene) instead of stubbed; CombatLoopContext now uses it for both regular enemy drops and boss loot."
metrics:
  duration: "~45min"
  tasks_completed: 2
  files_created: 3
  files_modified: 5
  test_count: 22
  tests_passing: 22
  completed: 2026-04-14
---

# Phase 05 Plan 03: Dungeon Boss + Victory + Return-to-Village Summary

Closes the dungeon loop: boss room map authored, BossEntity spawns on first entry per milestone, victory drops loot + completes MainQuest + opens the exit door + persists the save, and the exit routes the player to the village castle door so the King's quest-complete dialogue (NPC-04) is the natural next beat.

## What Was Built

### Task 1 — Map + boss spawn + victory handler (commit 5a84b5a)

- **`assets/Maps/dungeon_boss.tmx`** (30x20): Ground layer, Collision rectangles (four walls), one `Triggers` rectangle `exit_boss_to_village` at the south wall, one `Doors` rectangle `boss_exit` (initially closed) directly in front of the trigger, one `BossSpawn` point at (240, 160) with property `bossId=skeleton_king`, and one `Spawn` point `from_r4` at (240, 48) for arrival from r4. The door is physically blocking so the player cannot reach the exit trigger until the victory handler opens it.
- **`src/Combat/BossSpawnGate.cs`**: 1-method static helper `ShouldSpawn(DungeonState?) => state == null || !state.BossDefeated`. Extracted so tests can exercise the contract without a full MonoGame harness. xmldoc captures the D-14/D-13 reasoning for why `BossDefeated` is a milestone (not a per-run flag).
- **`src/Scenes/DungeonScene.cs`**:
  - Imports `stardew_medieval_v3.Entities` for `ItemDropEntity`; adds `List<ItemDropEntity> _itemDrops` and `bool _bossVictoryHandled` fields.
  - `OnLoad`: new boss-room branch at the end. On first entry (`BossSpawnGate.ShouldSpawn == true`) reads `ReadBossSpawn()` and calls `_spawner.SpawnBoss(pos)`. On re-entry after victory, opens all doors and sets `_bossVictoryHandled = true` so the victory block never re-fires.
  - `ReadBossSpawn()`: parses the `BossSpawn` TMX object group, prefers Point objects, falls back to rectangle-center, falls back to map-center (240,160) with a warning log (T-05-10 mitigation).
  - `OnPreUpdate`: real `SpawnItemDrop` now wired into `CombatLoopContext` (replaces Plan 02's log-only stub). Added victory handler after `CombatLoop.Update`: when `_room.IsBossRoom && !_bossVictoryHandled && _boss != null && !_boss.IsAlive`, it drops `GetBossLoot(bossAlreadyKilled: !firstKill)` as `ItemDropEntity`s, sets `Services.Dungeon.BossDefeated = true`, calls `Services.Quest.Complete()`, opens every door, saves via `GameStateSnapshot.SaveNow`, and nulls `_boss`.
  - `OnPostUpdate`: drives `ItemDropEntity.UpdateWithPlayer` + pickup cleanup (mirrors FarmScene).
  - `OnDrawWorld`: renders drops after chests/doors.
  - New public `SpawnItemDrop(itemId, quantity, pos)` method mirroring FarmScene's pattern; uses `Services.Atlas` with a null-guard warning.
- **`src/World/DungeonState.cs`**: `BeginRun()` no longer clears `BossDefeated`. xmldoc explicitly documents the D-14 reasoning and the `BossSpawnGate` dependency. This is the load-bearing change — it lets the boss stay defeated across subsequent dungeon re-entries (after death-reset, after normal re-entry, after load).
- **`src/Scenes/VillageScene.cs`**: added `["castle_door"] = new Vector2(208, 128)` to `Spawns`. When the boss exit fires `TransitionTo(new VillageScene(Services, "castle_door"))` (via the existing `ExitData.TargetTrigger` path in `DungeonScene.HandleTrigger`), `GetSpawn` resolves to the castle door position.
- **`tests/.../DungeonStateTests.cs`**: `BeginRun_ClearsAllFlags` renamed to `BeginRun_ClearsRunFlags_ButPreservesBossDefeatedMilestone` and the assertion flipped to `Assert.True(state.BossDefeated)`. Test now locks the correct semantics.

### Task 2 — BossVictoryTests + registry cross-check (commit e297d3f)

- **`tests/.../BossVictoryTests.cs`** (4 tests):
  - `OnBossDeath_QuestCompleted`: exercises the victory handler's observable side effects (MainQuest.State == Complete, DungeonState.BossDefeated == true).
  - `BossVictory_Persists_AcrossSaveRoundtrip`: hermetic `JsonSerializer` roundtrip through `GameState.Dungeon` (no disk I/O), plus `LoadFromSnapshot` hydration confirming the flag survives.
  - `ReEntry_AfterDefeat_DoesNotRespawnBoss`: covers `BossSpawnGate.ShouldSpawn` returning false post-victory, staying false after another `BeginRun`, and returning true for a null state (dev tooling safety).
  - `BeginRun_DoesNotClear_BossDefeatedMilestone`: duplicate/stricter coverage of the D-14 invariant directly on `DungeonState` so a future refactor that forgets the contract goes red.
- **`tests/.../DungeonRegistryTests.cs`**: added `BossRoom_RequiresAllMainRoomsCleared` locking r4's `exit_r4_to_boss` gate + confirming r1..r4 are registered. Removed the Plan 03 skip from `EveryExit_HasMatchingTriggerInSourceTmx` so `dungeon_boss.tmx` now participates in the cross-check (which passes because the TMX authored in Task 1 contains the `exit_boss_to_village` trigger declared in the registry).

All tests marked `[Trait("Category", "quick")]`.

## Test Results

```
dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --nologo
Aprovado!  Falha: 0, Aprovado: 22, Ignorado: 0, Total: 22
```

22 tests passing:
- `DungeonStateTests` (4): BeginRun preserves BossDefeated; ChestContents roundtrip; boss kill flips BossDefeated; death-reset semantics.
- `DungeonRegistryTests` (5): 7 rooms load; boss exit config; no orphan triggers; every exit has a matching TMX trigger (boss now included); boss-room-requires-main-rooms.
- `RoomClearedTests` (4): unchanged from Plan 01.
- `LootRollTests` (3): unchanged from Plan 02.
- `BossVictoryTests` (4): Plan 03, described above.
- `SaveV7ToV8MigrationTests` (2): unchanged from Plan 01.

`dotnet build` clean (1 pre-existing `GameplayScene.cs:177` warning, out of scope per Rule 4 boundary).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Critical functionality] `DungeonState.BeginRun` was clearing `BossDefeated`**
- **Found during:** Task 1 (re-reading existing DungeonState code before extending DungeonScene).
- **Issue:** Plan 01's `BeginRun` implementation included `BossDefeated = false;`, which directly contradicts the Plan 03 Task 2 decision and the D-14 milestone semantics. Without fixing this, any death-reset or room-transition that triggers `BeginRun` (e.g. defensive load-with-no-active-run branch in `DungeonScene.OnLoad`) would wipe the milestone and allow boss respawn.
- **Fix:** Removed the `BossDefeated = false;` line; added an xmldoc block explaining why it's intentional; updated the existing `DungeonStateTests.BeginRun_ClearsAllFlags` test (renamed + flipped assertion). Locked with a new `BossVictoryTests.BeginRun_DoesNotClear_BossDefeatedMilestone` test.
- **Files modified:** `src/World/DungeonState.cs`, `tests/.../DungeonStateTests.cs`.
- **Commit:** 5a84b5a.

**2. [Rule 2 - Critical functionality] `SpawnItemDrop` in `DungeonScene` was a log-only stub from Plan 02**
- **Found during:** Task 1 (wiring boss loot).
- **Issue:** Plan 02 had left the `CombatLoopContext.SpawnItemDrop` callback as a `Console.WriteLine` with comment `// Item drop entities for dungeon kills are a later-plan concern`. Boss loot (and all dungeon enemy loot) needs a real drop entity or the victory step is pointless.
- **Fix:** Added `DungeonScene.SpawnItemDrop(string, int, Vector2)` mirroring `FarmScene`'s method — constructs `ItemDropEntity` with `Services.Atlas`, appends to `_itemDrops`. Wired the callback to this method. Added `OnPostUpdate` (pickup physics) and `OnDrawWorld` render pass for drops. Null-guarded `Services.Atlas` with a warning log.
- **Files modified:** `src/Scenes/DungeonScene.cs`.
- **Commit:** 5a84b5a.

### Scope clarifications (not deviations)

- **`castle_door` routing:** Plan Task 1 step 5 listed two options — add a new `Spawn` object in `village.tmx` or map the existing `Castle` spawn. Chose the map-dict approach because (a) the existing Castle-return tile (208, 128) is already visually the castle door and (b) requires no TMX edits so Plan 02's village.tmx is untouched.

## Known Stubs

None. The `LootRollTests` stub from Plan 01 was filled in by Plan 02; no new stubs introduced in Plan 03.

## Threat Model Outcome

- **T-05-08 (save-file edit):** accepted as designed. Unchanged.
- **T-05-09 (Quest.Complete fires but save fails):** mitigated via `GameStateSnapshot.SaveNow` call in the victory handler, which funnels through `SaveManager.Save`'s existing error handling. Failure logs to console; the `BossDefeated` runtime flag stays true and the next successful save will persist it. Acceptable for single-player.
- **T-05-10 (BossSpawn object group missing):** mitigated via `ReadBossSpawn()` map-center fallback with warning log. Scene does not crash; boss just spawns at a safe default.

## Self-Check: PASSED

- `assets/Maps/dungeon_boss.tmx`: FOUND
- `src/Combat/BossSpawnGate.cs`: FOUND
- `tests/stardew_medieval_v3.Tests/Dungeon/BossVictoryTests.cs`: FOUND
- `src/Scenes/DungeonScene.cs`: FOUND (boss branch + victory handler + SpawnItemDrop)
- `src/Scenes/VillageScene.cs`: FOUND (castle_door spawn)
- `src/World/DungeonState.cs`: FOUND (BossDefeated preserved in BeginRun)
- Commit 5a84b5a: FOUND
- Commit e297d3f: FOUND
- 22/22 tests passing
- Build clean (1 pre-existing warning, out of scope)

## Phase 5 Acceptance

With Plan 03 complete, all four Phase 5 success criteria are met:
1. **5-8 rooms** — 7 rooms (r1-r4, r3a, r4a, boss) authored and registered. ✓
2. **Gated progression** — main rooms require clear to unlock north door; boss room requires all 4 main cleared. ✓
3. **Treasure chests** — 2 chests in optional rooms, drag-and-drop via ChestScene overlay. ✓
4. **Boss defeat completes objective** — boss victory flips MainQuest to Complete, persists to save, and routes player to castle door for NPC-04 dialogue. ✓
