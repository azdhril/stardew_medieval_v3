---
phase: 03-combat
plan: 02
subsystem: combat
tags: [enemy-ai, fsm, loot, spawner, monogame]

# Dependency graph
requires:
  - phase: 03-01
    provides: "Entity base class, CombatManager, ProjectileManager, MeleeAttack, SlashEffect, EnemyHealthBar"
provides:
  - "Three enemy types (Skeleton, DarkMage, Golem) with data-driven stats"
  - "EnemyAI FSM with Idle/Chase/Attack/Return states"
  - "EnemyEntity extending Entity with AI integration and placeholder rendering"
  - "LootTable with probability-based drop rolling"
  - "EnemySpawner with hardcoded positions and day-advance respawn"
  - "Full combat loop wired in FarmScene (melee, projectile, death, loot, respawn)"
affects: [03-combat, 04-world-npcs]

# Tech tracking
tech-stack:
  added: []
  patterns: [data-driven-enemies, fsm-ai, loot-tables]

key-files:
  created:
    - Combat/EnemyData.cs
    - Combat/EnemyAI.cs
    - Combat/EnemyEntity.cs
    - Combat/LootTable.cs
    - Combat/EnemySpawner.cs
  modified:
    - Data/items.json
    - Scenes/FarmScene.cs

key-decisions:
  - "Single EnemyEntity class driven by EnemyData records rather than subclasses per enemy type"
  - "EnemyAI GetMoveDirection takes EnemyData param for ranged kiting behavior"
  - "ApplyKnockbackWithResistance method on EnemyEntity handles resistance scaling"

patterns-established:
  - "Data-driven enemies: EnemyData record defines all stats, EnemyEntity is generic"
  - "FSM AI pattern: EnemyState enum + EnemyAI class with Update/GetMoveDirection"
  - "LootTable: probability-based drops with Roll(Random) for deterministic testing"

requirements-completed: [CMB-04, CMB-05]

# Metrics
duration: 4min
completed: 2026-04-11
---

# Phase 03 Plan 02: Enemy Types and Combat Loop Summary

**Three data-driven enemy types (Skeleton/DarkMage/Golem) with FSM AI, loot drops, and full combat loop wired in FarmScene**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-11T18:34:02Z
- **Completed:** 2026-04-11T18:38:02Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Three enemy types with distinct behaviors: Skeleton (fast melee rusher, 40HP), DarkMage (ranged kiter, 30HP, 3s cooldown projectiles), Golem (slow tank, 120HP, 75% knockback resistance)
- Finite State Machine AI with Idle/Chase/Attack/Return states and distance-based transitions
- Full combat loop in FarmScene: player melee hits enemies, enemies attack player (melee or ranged), death rolls loot drops, player respawns on death
- Loot system with probability-based drops (Bones, Mana_Crystal, Stone_Chunk items added)
- Enemy respawning on day advance via EnemySpawner

## Task Commits

Each task was committed atomically:

1. **Task 1: EnemyData + EnemyAI + EnemyEntity + LootTable + loot items** - `594afdb` (feat)
2. **Task 2: EnemySpawner + FarmScene integration** - `8ef531a` (feat)

## Files Created/Modified
- `Combat/EnemyData.cs` - Static enemy type definitions (Skeleton, DarkMage, Golem) as records with EnemyRegistry
- `Combat/EnemyAI.cs` - FSM AI with Idle/Chase/Attack/Return states, ranged kiting behavior
- `Combat/EnemyEntity.cs` - Entity subclass with data-driven stats, AI integration, placeholder rectangle rendering
- `Combat/LootTable.cs` - LootDrop record and LootTable class with probability-based Roll method
- `Combat/EnemySpawner.cs` - Spawn positions management and day-advance respawning
- `Data/items.json` - Added Bones, Mana_Crystal, Stone_Chunk loot items
- `Scenes/FarmScene.cs` - Full combat loop wiring: enemies, melee/projectile checks, death/loot, player respawn

## Decisions Made
- Used single EnemyEntity class driven by EnemyData records (not subclasses) for simplicity and data-driven design
- Added ApplyKnockbackWithResistance method to EnemyEntity rather than overriding base ApplyKnockback, keeping Entity base clean
- EnemyAI.GetMoveDirection takes EnemyData parameter to support ranged kiting behavior without storing data reference in AI
- Melee attacks that miss (player moves out of range) are consumed to prevent delayed hits

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build produces 30 errors from untracked files (TileMap, TimeManager, Camera, GridManager, CropManager) that are not in git history. These are pre-existing and unrelated to this plan's changes. All new files compile without errors.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Enemy system complete, ready for Plan 03 (combat balancing/polish) or Phase 04 (world/NPCs)
- Enemies render as colored placeholder rectangles; sprite art can be added later
- Spawn positions are hardcoded; future enhancement could use Tiled map objects

## Self-Check: PASSED

- All 5 created files exist on disk
- Both task commits verified (594afdb, 8ef531a)
- All acceptance criteria counts match expected values

---
*Phase: 03-combat*
*Completed: 2026-04-11*
