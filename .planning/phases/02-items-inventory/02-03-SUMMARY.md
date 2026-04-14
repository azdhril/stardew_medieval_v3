---
phase: 02-items-inventory
plan: 03
subsystem: farming
tags: [monogame, item-drops, magnetism, farming, harvest, inventory]

# Dependency graph
requires:
  - phase: 02-items-inventory
    plan: 01
    provides: InventoryManager, SpriteAtlas, ItemRegistry, item definitions
  - phase: 02-items-inventory
    plan: 02
    provides: InventoryScene overlay, equipment system
provides:
  - ItemDropEntity with bounce spawn and magnetic pickup
  - Harvest-to-inventory loop via item drops
  - FARM-01 fix (facing tile targeting from foot position)
  - Scythe tool for harvesting
  - GetFootPosition() on PlayerEntity for accurate tile calculations
affects: [combat-system, dungeon-loot, shop-system]

# Tech tracking
tech-stack:
  added: []
  patterns: [Entity-based world items with physics, Foot-based tile targeting, Spawn callback delegation]

key-files:
  created:
    - src/Entities/ItemDropEntity.cs
  modified:
    - src/Scenes/FarmScene.cs
    - src/Farming/ToolController.cs
    - src/Player/PlayerEntity.cs
    - src/Inventory/InventoryManager.cs
    - src/UI/HotbarRenderer.cs

key-decisions:
  - "GetTilePosition uses foot position (CollisionBox center) instead of sprite center for accurate tile targeting"
  - "Consumable slots reduced from 2 (Q/E) to 1 (Q only) — E kept free for interact actions"
  - "0.5s pickup immunity delay after spawn so bounce animation is visible before magnetism activates"
  - "Both Hands and Scythe can harvest ripe crops (Scythe on C key)"
  - "FARM-02 was already implemented — crops already rendered with real growth stage spritesheets"

patterns-established:
  - "GetFootPosition() pattern: gameplay tile calculations use foot position, not sprite center"
  - "Spawn callback pattern: FarmScene.SpawnItemDrop passed as Action delegate to ToolController"
  - "Entity lifecycle pattern: ItemDropEntity managed in List with reverse-iteration removal on IsCollected"

requirements-completed: [INV-05, FARM-01, FARM-02, FARM-03]

# Metrics
duration: 45min
completed: 2026-04-11
---

# Phase 02 Plan 03: Item Drops & Farming Integration Summary

**ItemDropEntity with bounce/magnetism pickup, foot-based facing tile fix, and harvest-to-inventory loop**

## Performance

- **Duration:** ~45 min (including human verification and bug fixes)
- **Completed:** 2026-04-11
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments
- ItemDropEntity with bounce animation on spawn, magnetic pull at 56px range, and real item sprites
- Farming actions now target the tile in front of the player's feet (not sprite center/head level)
- Harvesting ripe crops with Scythe/Hands spawns item drops that flow into inventory
- Consumable hotkey simplified to Q-only, freeing E for actions

## Task Commits

Each task was committed atomically:

1. **Task 1: ItemDropEntity with bounce spawn and magnetic pickup** - `c6a65e0` (feat)
2. **Task 2: Farming fixes and harvest-to-drop integration** - `949d41d` (feat)
3. **Task 3: Human verification fixes** - `88635de` + `a75ef50` (fix)

## Files Created/Modified
- `src/Entities/ItemDropEntity.cs` - World-space item entity with bounce, magnetism, and pickup
- `src/Farming/ToolController.cs` - Scythe tool, harvest spawns ItemDropEntity, facing tile fix
- `src/Player/PlayerEntity.cs` - GetFootPosition() for foot-based tile calculations
- `src/Scenes/FarmScene.cs` - Item drop list management, uses foot position for magnetism
- `src/Inventory/InventoryManager.cs` - ConsumableSlotCount reduced to 1
- `src/UI/HotbarRenderer.cs` - Consumable key labels updated to Q-only

## Decisions Made
- GetFacingTile was targeting head-level tiles because GetTilePosition used sprite center (Position). Fixed by adding GetFootPosition() that uses CollisionBox center — all tile calculations now foot-based.
- Item drops were instantly collected because player Position was too close to facing tile. Fixed with foot position + 0.5s pickup immunity delay.
- FARM-02 (real crop sprites) was already working — GridManager.DrawCrops() already used growth spritesheets. No changes needed.

## Deviations from Plan

### Auto-fixed Issues

**1. Facing tile at head level instead of feet**
- **Found during:** Task 3 (human verification)
- **Issue:** GetTilePosition() used Position (sprite center = head), making actions target tiles at head height
- **Fix:** Added GetFootPosition() using CollisionBox center; GetTilePosition() now uses it
- **Files modified:** src/Player/PlayerEntity.cs
- **Verification:** User confirmed tilling targets correct tile relative to feet
- **Committed in:** 88635de

**2. Item drops instantly disappearing**
- **Found during:** Task 3 (human verification)
- **Issue:** Magnetism used player sprite center which was within pickup range of facing tile
- **Fix:** FarmScene passes GetFootPosition() to UpdateWithPlayer; added 0.5s PickupDelay
- **Files modified:** src/Entities/ItemDropEntity.cs, src/Scenes/FarmScene.cs
- **Verification:** User confirmed bounce animation visible and magnetism works
- **Committed in:** a75ef50

---

**Total deviations:** 2 auto-fixed during human verification
**Impact on plan:** Both fixes essential for correct gameplay feel. No scope creep.

## Issues Encountered
- Console.WriteLine output not visible in MonoGame Windows builds — used file-based debug logging to diagnose item drop issues
- Initial test crops were all wilted (ripe=True wilted=True), which follow the "clear wilted" path instead of harvest path. Required planting fresh crops to test the harvest flow.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Item system complete: items flow from farm → drops → inventory → equipment
- Ready for Phase 3 (Combat): weapon/armor items can be equipped, damage system can reference equipment stats
- ItemDropEntity pattern reusable for enemy loot drops in combat/dungeon phases

---
*Phase: 02-items-inventory*
*Completed: 2026-04-11*
