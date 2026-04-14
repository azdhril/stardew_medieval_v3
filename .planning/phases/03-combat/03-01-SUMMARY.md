---
phase: 03-combat
plan: 01
subsystem: combat
tags: [monogame, melee, projectile, combat, hp-bar, i-frames, knockback]

requires:
  - phase: 02-items-inventory
    provides: InventoryManager, EquipmentData, ItemRegistry, HotbarRenderer

provides:
  - CombatManager coordinating player attacks, damage, and i-frames
  - MeleeAttack with hitbox, cooldown, and multi-hit prevention
  - Projectile and ProjectileManager for fireball and enemy projectiles
  - SlashEffect visual overlay for melee swings
  - EnemyHealthBar (world-space) and BossHealthBar (screen-space)
  - Entity base class combat methods (TakeDamage, ApplyKnockback, DirectionToVector)

affects: [03-02 enemies, 03-03 dungeon-boss, combat balancing]

tech-stack:
  added: []
  patterns: [combat-manager-pattern, entity-combat-base, projectile-lifecycle, hit-tracking-hashset]

key-files:
  created:
    - src/Combat/CombatManager.cs
    - src/Combat/MeleeAttack.cs
    - src/Combat/Projectile.cs
    - src/Combat/ProjectileManager.cs
    - src/Combat/SlashEffect.cs
    - src/UI/EnemyHealthBar.cs
    - src/UI/BossHealthBar.cs
  modified:
    - src/Core/Entity.cs
    - src/Player/PlayerEntity.cs
    - src/UI/HUD.cs
    - src/Scenes/FarmScene.cs

key-decisions:
  - "Fireball request uses consume-flag pattern to decouple CombatManager from ProjectileManager"
  - "Entity.TakeDamage enforces minimum 1 damage when amount > 0 (per D-06)"
  - "PlayerEntity.IFrameTimer set by CombatManager to avoid exposing combat internals to entity"

patterns-established:
  - "Combat component pattern: CombatManager owns MeleeAttack, coordinates with ProjectileManager via flags"
  - "Hit tracking: HashSet<Entity> per swing prevents multi-hit (per Research Pitfall 1)"
  - "Knockback via lerp (no physics): simple, predictable, per D-02"

requirements-completed: [CMB-01, CMB-02, CMB-03]

duration: 7min
completed: 2026-04-11
---

# Phase 03 Plan 01: Player Combat Core Summary

**Melee sword attack (LMB), fireball projectile (RMB), damage/defense calculation, i-frames with blink, knockback, and HP bar rendering for player/enemies/boss**

## Performance

- **Duration:** 7 min
- **Started:** 2026-04-11T18:21:58Z
- **Completed:** 2026-04-11T18:29:01Z
- **Tasks:** 3
- **Files modified:** 11

## Accomplishments
- Entity base class extended with TakeDamage, ApplyKnockback, DirectionToVector, knockback lerp, flash timer, and Defense property
- Complete melee attack system: hitbox generation (48x24px), per-weapon cooldowns, multi-hit prevention via HashSet
- Projectile system: fireball at 200px/s with 300px max range and 15 fixed damage, enemy projectile spawning
- CombatManager coordinates all combat: LMB melee (requires weapon in hotbar), RMB fireball (2s cooldown), i-frames (1s), damage calculation with equipment stats
- PlayerEntity blinks during i-frames and flashes red on hit
- HUD displays player HP bar (red) and fireball cooldown indicator with progress fill
- EnemyHealthBar (world-space, hidden at full HP) and BossHealthBar (screen-space with name text) created
- FarmScene wired up with full combat integration: CombatManager, ProjectileManager, SlashEffect

## Task Commits

Each task was committed atomically:

1. **Task 1: Entity combat methods + MeleeAttack + CombatManager** - `335c80f` (feat)
2. **Task 2: Projectile system + Fireball + slash visual** - `892918c` (feat)
3. **Task 3: HP bars + magic cooldown indicator** - `01271b2` (feat)

## Files Created/Modified
- `src/Combat/CombatManager.cs` - Coordinates player attacks, damage calc, i-frames, fireball cooldown
- `src/Combat/MeleeAttack.cs` - Melee hitbox, swing timer, cooldown, hit tracking
- `src/Combat/Projectile.cs` - Projectile data (position, velocity, damage, lifetime, ownership)
- `src/Combat/ProjectileManager.cs` - Spawns/updates/collision-checks all projectiles
- `src/Combat/SlashEffect.cs` - Visual slash overlay with fade animation
- `src/UI/EnemyHealthBar.cs` - World-space HP bar drawn above enemies
- `src/UI/BossHealthBar.cs` - Screen-space boss HP bar at bottom center
- `src/Core/Entity.cs` - Added TakeDamage, ApplyKnockback, DirectionToVector, Defense, knockback, flash
- `src/Player/PlayerEntity.cs` - Added IFrameTimer, blink rendering, knockback/flash updates
- `src/UI/HUD.cs` - Added player HP bar and fireball cooldown indicator
- `src/Scenes/FarmScene.cs` - Wired CombatManager, ProjectileManager, SlashEffect into game loop

## Decisions Made
- Used consume-flag pattern for fireball requests: CombatManager sets flag, FarmScene reads and spawns via ProjectileManager. Avoids tight coupling.
- Entity.TakeDamage enforces minimum 1 damage (per D-06) to prevent zero-damage scenarios from high defense.
- IFrameTimer is a public property on PlayerEntity set by CombatManager, keeping combat logic centralized while allowing the entity to render blink effect.
- Reordered FarmScene.LoadContent to create InventoryManager before HUD so combat dependencies are available.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Reordered FarmScene.LoadContent initialization order**
- **Found during:** Task 3 (HUD modification)
- **Issue:** HUD was instantiated before InventoryManager and CombatManager, but new HUD constructor requires both
- **Fix:** Moved ItemRegistry.Initialize() and InventoryManager creation before HUD and CombatManager creation
- **Files modified:** src/Scenes/FarmScene.cs
- **Verification:** dotnet build succeeds with 0 errors
- **Committed in:** 01271b2 (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Initialization reorder was necessary for constructor dependency. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Combat foundation complete: melee, projectile, damage, i-frames, HP bars all functional
- Ready for Plan 02 (enemy AI) to create enemies that interact with these systems
- ProjectileManager.Update accepts IReadOnlyList<Entity> for enemy collision, currently passed empty array
- EnemyHealthBar and BossHealthBar are static utilities ready to be called from enemy Draw methods

## Self-Check: PASSED

All 7 created files exist. All 3 task commits verified. Build succeeds with 0 errors.

---
*Phase: 03-combat*
*Completed: 2026-04-11*
