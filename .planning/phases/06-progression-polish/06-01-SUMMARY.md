---
phase: 06-progression-polish
plan: 01
subsystem: progression
tags: [xp, leveling, gold-drops, save-migration, combat-rewards]

requires:
  - phase: 05-dungeon
    provides: CombatLoop, EnemyEntity, BossEntity, ItemDropEntity, InventoryManager, save v8
provides:
  - XPTable with exponential curve (50*1.22^(level-1))
  - ProgressionManager (AwardXP, OnLevelUp, RollGold, LoadFromState, SaveToState)
  - Gold_Coin currency item with magnetism pickup via AddGold
  - CombatLoop OnEnemyKilled callback for both enemies and boss
  - Save v9 migration (MaxHP, MaxStamina, BaseDamageBonus derived from Level)
  - GameStateSnapshot v9 field persistence
  - CombatManager.DamageBonus wired from ProgressionManager
  - InventoryManager.ForceRemoveEquipment for death penalty
affects: [06-02-death-penalty, 06-03-hud-polish]

tech-stack:
  added: []
  patterns: [event-driven stat push on level-up, currency item type bypasses inventory slots]

key-files:
  created:
    - src/Progression/XPTable.cs
    - src/Progression/ProgressionManager.cs
    - tests/stardew_medieval_v3.Tests/Progression/XPTableTests.cs
    - tests/stardew_medieval_v3.Tests/Progression/ProgressionManagerTests.cs
    - tests/stardew_medieval_v3.Tests/Save/SaveV8ToV9MigrationTests.cs
  modified:
    - src/Core/GameState.cs
    - src/Core/SaveManager.cs
    - src/Core/ServiceContainer.cs
    - src/Core/GameStateSnapshot.cs
    - src/Data/ItemType.cs
    - src/Data/items.json
    - src/Data/SpriteAtlas.cs
    - src/Entities/ItemDropEntity.cs
    - src/Combat/CombatLoop.cs
    - src/Combat/CombatManager.cs
    - src/Inventory/InventoryManager.cs
    - src/Scenes/FarmScene.cs
    - src/Scenes/DungeonScene.cs
    - tests/stardew_medieval_v3.Tests/Save/SaveV7ToV8MigrationTests.cs

key-decisions:
  - "CombatManager.DamageBonus as settable property (minimal coupling vs constructor injection)"
  - "OnLevelUp event handler syncs DamageBonus to CombatManager in both scenes"
  - "Gold_Coin uses ItemType.Currency to bypass inventory slot, calls AddGold directly"

patterns-established:
  - "Currency items bypass TryAdd and call AddGold on pickup"
  - "OnEnemyKilled callback pattern for cross-cutting kill rewards"
  - "ProgressionManager follows same create-once-reuse pattern as Inventory/Quest in ServiceContainer"

requirements-completed: [PRG-01, PRG-02, PRG-03, SAV-01, SAV-02]

duration: 11min
completed: 2026-04-16
---

# Phase 06 Plan 01: XP/Leveling + Gold Drops + Save v9 Summary

**Exponential XP curve with level-up stat pushes, gold coin drops from all enemy kills via OnEnemyKilled callback, and save v8-to-v9 migration persisting MaxHP/MaxStamina/BaseDamageBonus**

## Performance

- **Duration:** 11 min
- **Started:** 2026-04-16T23:13:30Z
- **Completed:** 2026-04-16T23:24:32Z
- **Tasks:** 3
- **Files modified:** 18 (5 created, 13 modified)

## Accomplishments
- XPTable with exponential curve (50 * 1.22^(level-1)), capped at level 100 with int.MaxValue
- ProgressionManager tracks XP, awards per enemy kill, triggers level-up with +10 MaxHP, +5 MaxStamina, +1 DamageBonus, full HP/Stamina restore
- Gold_Coin as Currency item type: drops from every enemy kill with +/-30% variance, magnetism pickup calls AddGold directly (no inventory slot consumed)
- CombatLoop.OnEnemyKilled fires for both regular enemies AND boss death, wired in FarmScene and DungeonScene
- Save v9 migration derives progression stats from Level for existing saves
- CombatManager.DamageBonus synced from ProgressionManager on level-up
- 20 unit tests covering XP curve, save migration, progression logic, and gold rolling

## Task Commits

Each task was committed atomically (TDD: test then feat):

1. **Task 1: XPTable + Save v9 + GameState fields** - `0b9e795` (test: failing) + `2405b04` (feat: XPTable, GameState v9, SaveManager migration)
2. **Task 2: ProgressionManager + CombatLoop + Gold_Coin + pickup** - `b95056d` (test: failing) + `2ed4093` (feat: ProgressionManager, CombatLoop OnEnemyKilled, Currency item type, ItemDropEntity pickup, ForceRemoveEquipment)
3. **Task 3: Wire into FarmScene and DungeonScene** - `02935b6` (feat: scene wiring, damage bonus sync)

## Files Created/Modified
- `src/Progression/XPTable.cs` - Exponential XP curve, enemy XP lookup dictionary
- `src/Progression/ProgressionManager.cs` - XP tracking, level-up stat push, gold rolling, save/load
- `src/Core/GameState.cs` - MaxHP, MaxStamina, BaseDamageBonus properties (v9)
- `src/Core/SaveManager.cs` - CURRENT_SAVE_VERSION=9, v8->v9 migration block
- `src/Core/ServiceContainer.cs` - Progression slot
- `src/Core/GameStateSnapshot.cs` - v9 field persistence in SaveNow
- `src/Data/ItemType.cs` - Currency enum value
- `src/Data/items.json` - Gold_Coin item definition
- `src/Data/SpriteAtlas.cs` - RegisterGoldCoin method
- `src/Entities/ItemDropEntity.cs` - Currency type check, AddGold on pickup
- `src/Combat/CombatLoop.cs` - OnEnemyKilled callback in CombatLoopContext, invoked for enemies and boss
- `src/Combat/CombatManager.cs` - DamageBonus property added to melee damage calculation
- `src/Inventory/InventoryManager.cs` - ForceRemoveEquipment for death penalty
- `src/Scenes/FarmScene.cs` - ProgressionManager creation, save load, Gold_Coin sprite registration, OnEnemyKilled wiring
- `src/Scenes/DungeonScene.cs` - OnEnemyKilled wiring, DamageBonus sync

## Decisions Made
- CombatManager.DamageBonus as a settable property rather than constructor injection -- keeps CombatManager decoupled from ProgressionManager, synced via OnLevelUp event handler
- Gold_Coin uses ItemType.Currency to bypass inventory slot system entirely, calling AddGold directly -- prevents gold from consuming player inventory space
- OnEnemyKilled callback invoked on both regular enemy death AND boss death paths in CombatLoop -- ensures boss kill awards XP and gold coins (research pitfall 5)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Updated SaveV7ToV8MigrationTests to expect v9 after full migration chain**
- **Found during:** Task 2 (after adding v9 migration, existing v7->v8 test expected SaveVersion==8 but migration chain now goes to v9)
- **Issue:** V7Save_LoadsWithDefaultDungeonState asserted SaveVersion==8, but v9 migration now runs on any save < 9
- **Fix:** Updated assertion to expect SaveVersion==9 and added v9 field checks (MaxHP=100, MaxStamina=100, BaseDamageBonus=0)
- **Files modified:** tests/stardew_medieval_v3.Tests/Save/SaveV7ToV8MigrationTests.cs
- **Verification:** All 44 tests pass
- **Committed in:** 2ed4093 (part of Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug fix)
**Impact on plan:** Necessary correction for existing test after save version bump. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ProgressionManager ready for death penalty system (Plan 02) to use ForceRemoveEquipment and gold loss
- XP/Level/Gold values available in ServiceContainer for HUD polish (Plan 03) to display
- OnLevelUp event available for visual effects (flash, sound) in future plans

## Self-Check: PASSED

All 6 key files verified present. All 5 task commits verified in git log.

---
*Phase: 06-progression-polish*
*Completed: 2026-04-16*
