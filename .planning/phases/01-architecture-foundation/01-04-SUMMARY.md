---
phase: 01-architecture-foundation
plan: 04
subsystem: entities
tags: [monogame, entity, npc, inheritance, collision]

# Dependency graph
requires:
  - phase: 01-architecture-foundation (plan 03)
    provides: Entity base class, TestScene, scene management
provides:
  - DummyNpc concrete entity proving Entity base class extensibility
  - SC-3 gap closure (non-player entity spawned with position, visual, collision)
  - ARCH-02 requirement fully satisfied
affects: [02-core-gameplay, entity-system, npc-system]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Entity subclass pattern: inherit Entity, override CollisionBox/Update/Draw"
    - "Placeholder rendering: 1x1 pixel texture with color tint for pre-sprite entities"
    - "Patrol behavior: timer-based direction flip with constant velocity"

key-files:
  created:
    - src/Entities/DummyNpc.cs
  modified:
    - src/Scenes/TestScene.cs

key-decisions:
  - "Used 1x1 pixel texture with color tint instead of real sprite sheet (sprites come in Phase 3-4)"
  - "Patrol movement at 30px/sec with 2-second direction flip for visible but non-distracting behavior"

patterns-established:
  - "Entity subclass pattern: constructor receives Texture2D + startPosition, sets FrameWidth/FrameHeight, overrides CollisionBox/Update/Draw"
  - "Debug collision box rendering: semi-transparent colored rectangle drawn over entity for visual debugging"

requirements-completed: [ARCH-02]

# Metrics
duration: 8min
completed: 2026-04-10
---

# Phase 01 Plan 04: DummyNpc Entity (SC-3 Gap Closure) Summary

**DummyNpc entity proving Entity base class extensibility with patrol movement and collision box in TestScene**

## Performance

- **Duration:** 8 min
- **Started:** 2026-04-10
- **Completed:** 2026-04-10
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- DummyNpc concrete entity created, inheriting from Entity base class with position, visual rendering, and collision box
- Horizontal patrol movement (30px/sec, 2-second direction flip) proves Update() override works
- SC-3 verification gap closed: non-player entity successfully spawned in TestScene
- ARCH-02 requirement moves from PARTIAL to SATISFIED

## Task Commits

Each task was committed atomically:

1. **Task 1: Criar DummyNpc entity e integrar no TestScene** - `be7f7a6` (feat)
2. **Task 2: Verificacao visual do DummyNpc no TestScene** - checkpoint:human-verify (approved)

**Plan metadata:** [this commit] (docs: complete plan)

## Files Created/Modified
- `src/Entities/DummyNpc.cs` - Concrete Entity subclass with patrol movement, colored rectangle rendering, and collision box override
- `src/Scenes/TestScene.cs` - Modified to instantiate, update, and draw DummyNpc; added label for entity test

## Decisions Made
- Used 1x1 pixel texture with ForestGreen color tint for DummyNpc rendering (real sprites deferred to Phase 3-4)
- CollisionBox sized at 12x8 pixels centered at entity feet (standard NPC collision footprint)
- Patrol speed of 30px/sec chosen for visible but non-distracting horizontal movement

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Entity extensibility proven; future NPC/enemy entities can follow DummyNpc pattern
- All SC gaps from Phase 01 verification are now closed
- Phase 01 architecture foundation is complete and ready for Phase 02

---
*Phase: 01-architecture-foundation*
*Completed: 2026-04-10*
