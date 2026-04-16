---
phase: 06-progression-polish
plan: 02
subsystem: progression, ui, scenes
tags: [death-penalty, level-up, auto-save, visual-feedback, toast-queue]
dependency_graph:
  requires: [06-01]
  provides: [death-penalty, level-up-feedback, periodic-save, toast-queue]
  affects: [FarmScene, DungeonScene, GameplayScene, Toast, ServiceContainer]
tech_stack:
  added: []
  patterns: [TDD-red-green, queue-based-toast, screen-space-banners, world-space-particles]
key_files:
  created:
    - src/Progression/DeathPenalty.cs
    - src/UI/LevelUpBanner.cs
    - src/UI/LevelUpParticles.cs
    - src/UI/DeathBanner.cs
    - tests/stardew_medieval_v3.Tests/Progression/DeathPenaltyTests.cs
  modified:
    - src/UI/Toast.cs
    - src/Core/ServiceContainer.cs
    - src/Core/GameplayScene.cs
    - src/Scenes/FarmScene.cs
    - src/Scenes/DungeonScene.cs
decisions:
  - Toast shared via ServiceContainer so death penalty messages survive DungeonScene->FarmScene transitions
  - Banners and particles placed in GameplayScene base class to avoid duplication across FarmScene/DungeonScene
  - DeathBanner triggered by FarmScene for both farm-local and dungeon deaths (since dungeon transitions to farm)
metrics:
  duration: ~10min
  completed: 2026-04-16
  tasks: 3
  files_created: 5
  files_modified: 5
---

# Phase 06 Plan 02: Death Penalty, Level-Up Feedback & Auto-Save Summary

Death penalty with 10% gold loss + probabilistic item removal, level-up golden banner + world-space gold particle burst, "You died" red banner, Toast queue for sequential messages, and periodic 30s auto-save with Stopwatch performance logging.

## Task Results

| # | Task | Commit(s) | Status |
|---|------|-----------|--------|
| 1 | DeathPenalty helper + death path wiring (TDD) | `99ac697` (RED), `c3cbffc` (GREEN) | Done |
| 2 | Periodic auto-save timer + level-up save trigger | `61db72a` | Done |
| 3 | Level-up banner + particles + death banner | `67071cc` | Done |

## Key Implementation Details

### DeathPenalty System (Task 1)
- `DeathPenalty.Apply(InventoryManager, Random)` returns `PenaltyResult(GoldLost, ItemsLost)`
- Gold: `floor(gold * 0.10)` deducted (0 on amounts <10)
- Items: RNG roll determines 15% lose 2, 25% lose 1, 60% lose nothing
- Unified pool includes both inventory slots AND equipped items
- After removal, `PruneBrokenReferences()` clears stale hotbar/consumable refs
- DungeonScene applies penalty BEFORE HP restore, save, and run reset
- FarmScene guards with `FromScene != "DungeonDeath"` to prevent double-apply

### Toast Queue (Task 1)
- Toast.Show now enqueues if a toast is already active
- Messages display sequentially (each completes its 2.2s lifecycle before the next starts)
- Enables death penalty to show "Lost 12 gold" then "Lost: Iron Sword" in sequence

### Auto-Save (Task 2)
- 30-second periodic timer in GameplayScene base (all gameplay scenes get it)
- Stopwatch logging for performance monitoring (D-21: flag if >50ms)
- Level-up triggers immediate save via OnLevelUp subscription
- Post-death save already present in both FarmScene and DungeonScene death paths

### Visual Feedback (Task 3)
- LevelUpBanner: golden "LEVEL UP! Lv X" at top-center, 1.5s with 3-phase fade
- LevelUpParticles: 12-16 gold particles burst from player, world-space, gravity+fade
- DeathBanner: red "You died" at screen center, 1.5s with 3-phase fade
- All three update/draw in GameplayScene base (no per-scene duplication)
- No gameplay pause during any visual effect (D-08)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Toast shared via ServiceContainer**
- **Found during:** Task 1
- **Issue:** Neither FarmScene nor DungeonScene had a Toast instance. Death penalty messages queued in DungeonScene need to survive the scene transition to FarmScene.
- **Fix:** Added `Toast?` property to ServiceContainer; lazily created in GameplayScene.LoadContent. Toast update/draw wired in GameplayScene base.
- **Files modified:** src/Core/ServiceContainer.cs, src/Core/GameplayScene.cs

**2. [Rule 2 - Missing functionality] UI components in GameplayScene base**
- **Found during:** Task 3
- **Issue:** Plan specified per-scene fields for banners/particles, but update/draw logic would be duplicated across FarmScene and DungeonScene.
- **Fix:** Placed all three UI components (LevelUpBanner, LevelUpParticles, DeathBanner) as fields on GameplayScene base class with update in Update() and draw in Draw(). DeathBanner made `protected` so subclasses can trigger `.Show()`.
- **Files modified:** src/Core/GameplayScene.cs

## Verification

- `dotnet build` -- 0 errors, 1 pre-existing warning
- `dotnet test` -- 52 tests pass (8 new DeathPenalty + 44 existing)
- Acceptance criteria: all grep checks pass for every acceptance item

## Self-Check: PASSED

All 5 created files exist on disk. All 4 commit hashes verified in git log.
