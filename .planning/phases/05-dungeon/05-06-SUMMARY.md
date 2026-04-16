---
phase: 05-dungeon
plan: 06
subsystem: dungeon-chest-one-time-loot
tags: [dungeon, persistence, chest, gap-closure, uat, tdd]
requires:
  - "Plan 05-02 DungeonState + DungeonChestSeeder (this plan extends Seed and refines BeginRun)"
  - "Existing GameStateSnapshot.SaveNow path (no save schema change needed — ToSnapshot already round-trips OpenedChestIds)"
  - "Existing DungeonScene.OnLoad hydration branch (IsChestOpened path already renders chests as opened/empty)"
provides:
  - "OpenedChestIds survives BeginRun — dungeon chest loot is one-time-per-save"
  - "DungeonChestSeeder skips opened chests (writes empty content list instead of re-rolling)"
  - "Save-before-wipe on death so crash-after-death does not forget opened chests"
  - "Closes 05-UAT Gap 2 (dungeon chest persistence)"
affects:
  - "src/World/DungeonState.cs (BeginRun no longer clears OpenedChestIds; xmldoc updated to document new invariant)"
  - "src/World/DungeonChestSeeder.cs (Seed loop guards on IsChestOpened)"
  - "src/Scenes/DungeonScene.cs (GameStateSnapshot.SaveNow called BEFORE BeginRun on death)"
  - "tests/stardew_medieval_v3.Tests/Dungeon/LootRollTests.cs (+2 new tests)"
  - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonStateTests.cs (BeginRun assertion renamed + updated)"
tech-stack:
  added: []
  patterns:
    - "TDD RED→GREEN: 2 failing tests written first, then DungeonState.BeginRun + DungeonChestSeeder.Seed modified to flip them green"
    - "Seeder writes empty-list (not skip) for opened chests — keeps ChestContents keys consistent across save snapshots and documents intent"
    - "Save-before-wipe belt-and-suspenders: in-memory preservation plus immediate disk flush guards against quit-after-death"
key-files:
  created:
    - ".planning/phases/05-dungeon/05-06-SUMMARY.md"
  modified:
    - "src/World/DungeonState.cs"
    - "src/World/DungeonChestSeeder.cs"
    - "src/Scenes/DungeonScene.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/LootRollTests.cs"
    - "tests/stardew_medieval_v3.Tests/Dungeon/DungeonStateTests.cs"
decisions:
  - "DungeonChestSeeder writes `new List<string>()` (explicit empty list) for opened chests rather than skipping — per plan rationale: keeps every registered chest as a key in ChestContents so save snapshots stay consistent and any future consumer that treats missing keys as 'not yet seeded' still works correctly."
  - "SaveNow BEFORE BeginRun even though BeginRun now preserves OpenedChestIds — pure belt-and-suspenders. In-memory fix handles the happy path; the SaveNow guards the quit-after-death path (Alt+F4 / power loss). Matches the cheap I/O pattern boss-victory already uses."
  - "Renamed pre-existing DungeonStateTests.BeginRun_ClearsRunFlags_ButPreservesBossDefeatedMilestone → ..._ButPreservesBossDefeatedAndOpenedChests and updated its assertion from `Assert.Empty(OpenedChestIds)` to `Assert.Contains(\"chest_a\", OpenedChestIds)`. Reason: the old test encoded the OLD invariant (OpenedChestIds cleared on BeginRun) which directly conflicts with the new spec — renaming + updating is more truthful than writing a second test that contradicts the first."
metrics:
  duration: 25min
  completed: 2026-04-16
  tasks: 3
  commits: 2
requirements: [DNG-03, SAV-01]
---

# Phase 05 Plan 06: Dungeon Chest One-Time Loot Summary

Closed 05-UAT Gap 2 so dungeon chests stay permanently opened across death, re-entry, and save/load. Three source changes (DungeonState.BeginRun, DungeonChestSeeder.Seed, DungeonScene death branch) plus a TDD RED→GREEN test pair and a rename+update of one pre-existing test that encoded the old invariant.

## What Changed

### Task 1 — TDD: Persist OpenedChestIds + skip in seeder (commit `b795d02`)

**RED (tests written first, confirmed failing):**
- `LootRollTests.Seed_SkipsOpenedChests_WritesEmptyContents` — seed a DungeonState with `OpenedChestIds = {"dungeon_r3a_chest"}`, assert post-seed that r3a_chest is present in ChestContents with an empty list while r4a_chest has ≥1 item.
- `LootRollTests.BeginRun_PreservesOpenedChestIds` — add "foo", "bar" to OpenedChestIds, call BeginRun, assert both survive and that ClearedRooms/ChestContents are still reset with a fresh RunSeed.

**GREEN (production fixes):**
- **src/World/DungeonState.cs:** removed `OpenedChestIds.Clear();` from `BeginRun()`. Expanded the method's xmldoc summary to document the new "INTENTIONALLY PRESERVED" set (OpenedChestIds, BossDefeated) vs the "RESET" set (ClearedRooms, ChestContents, RunSeed).
- **src/World/DungeonChestSeeder.cs:** inner loop now guards `if (dungeon.IsChestOpened(chestId)) { dungeon.ChestContents[chestId] = new List<string>(); } else { ... roll ... }`. Unopened chests still roll deterministically from `rng = new Random(RunSeed)`.

**Existing test update (bundled in the same commit):**
- **tests/.../DungeonStateTests.cs:** renamed `BeginRun_ClearsRunFlags_ButPreservesBossDefeatedMilestone` → `BeginRun_ClearsRunFlags_ButPreservesBossDefeatedAndOpenedChests` and switched the relevant assertion from `Assert.Empty(state.OpenedChestIds)` to `Assert.Contains("chest_a", state.OpenedChestIds)`. The old assertion encoded the OLD invariant and would have failed forever otherwise.

**Post-GREEN:** all 5 LootRollTests pass, all 24 Dungeon tests pass.

### Task 2 — Save before wiping run state on death (commit `2b2d062`)

- **src/Scenes/DungeonScene.cs:** in the `OnPreUpdate` death branch, added `GameStateSnapshot.SaveNow(Services);` immediately before `Services.Dungeon!.BeginRun();`. Flushes OpenedChestIds to disk so a quit-after-death (Alt+F4 / crash) still remembers them.

**Why save BEFORE BeginRun even though BeginRun now preserves OpenedChestIds:**
The in-memory fix (Task 1) keeps opened chests surviving a normal play session. But if the player dies and quits before the next autosave, the opened-chest set would be lost with the process. A single `SaveNow` at the moment of death is cheap I/O and makes chest persistence identical to the boss-defeat flag (which already flushes mid-combat via its own `SaveNow`).

### Task 3 — Human-verify

**Resume signal:** "todos os testes dos 3 rounds passaram" (2026-04-16, joint verify with 05-05 + out-of-band Camera/ShopPanel fixes).

**Round B steps confirmed passing:**
- Opened a chest, noted contents.
- Deliberately died → respawned at farm.
- Re-entered the same chest → empty.
- Reloaded save (quit → launch → continue) → same chest stayed empty.
- Regression: unopened chest on same run still rolled fresh loot.
- Boss-defeated flag still permanent (not regressed).

## Deviations from Plan

**None within this plan's scope.** The two out-of-band fixes that landed during the joint verify (Camera.Reclamp, ShopPanel viewport-aware port) belong to the 05-05 plan's scope (viewport awareness) — they are documented in `05-05-SUMMARY.md`, not here.

## Anti-Pattern Recorded

If a test asserts a "reset" invariant on a state-management method (like `BeginRun`), any change to what that method preserves vs resets **MUST** be accompanied by updating (or renaming + updating) the test. Don't stack a new test alongside a contradicting old one — the old one will just fail forever and muddy the signal.

## Verification Results

- [x] `src/World/DungeonState.cs` no longer contains `OpenedChestIds.Clear()`.
- [x] `src/World/DungeonChestSeeder.cs` calls `dungeon.IsChestOpened(chestId)` inside the seed loop.
- [x] `src/Scenes/DungeonScene.cs` death branch calls `GameStateSnapshot.SaveNow(Services);` before `BeginRun()`.
- [x] `LootRollTests` file contains 5 `[Fact]` methods total; 2 new tests added.
- [x] `DungeonStateTests.BeginRun_ClearsRunFlags_ButPreservesBossDefeatedAndOpenedChests` passes.
- [x] `dotnet build` clean.
- [x] `dotnet test --filter "FullyQualifiedName~Dungeon"` — 24/24 pass.
- [x] Human verify Round B: approved.

## Handoff

The dungeon chest persistence loop is now:

```
player opens chest  →  MarkChestOpened + ChestContents[id].Clear
player dies         →  SaveNow (persist OpenedChestIds to disk)
                    →  BeginRun    (keeps OpenedChestIds, clears rooms/ChestContents/RunSeed)
                    →  Seed        (writes empty list for opened chest; rolls fresh for others)
player re-enters    →  DungeonScene.OnLoad hydrates chest via IsChestOpened → renders opened/empty
```

This is the same persistence shape the boss-defeated flag already had — both now persist via SaveNow + survive BeginRun.

## Self-Check: PASSED

- Commit `b795d02` — FOUND (Task 1 + test rename/update).
- Commit `2b2d062` — FOUND (Task 2, SaveNow before BeginRun).
- File `.planning/phases/05-dungeon/05-06-SUMMARY.md` — created (this file).
