---
phase: 02-items-inventory
plan: 02
subsystem: inventory-ui
tags: [monogame, inventory, ui, equipment, overlay, rarity]

# Dependency graph
requires:
  - phase: 02-items-inventory
    plan: 01
    provides: InventoryManager, SpriteAtlas, HotbarRenderer, mouse input, item definitions
provides:
  - InventoryScene overlay with grid and equipment tabs
  - InventoryGridRenderer with 20-slot click-to-move and rarity tinting
  - EquipmentRenderer with Tibia-style weapon/armor slots and stat display
  - PopImmediate on SceneManager for instant overlay close
  - I-key binding in FarmScene to open inventory
affects: [02-items-inventory plan 03, combat-system, save-load]

# Tech tracking
tech-stack:
  added: []
  patterns: [Overlay scene via PushImmediate/PopImmediate, Tab-based UI switching, Rarity color tinting]

key-files:
  created:
    - src/Scenes/InventoryScene.cs
    - src/UI/InventoryGridRenderer.cs
    - src/UI/EquipmentRenderer.cs
  modified:
    - src/Core/SceneManager.cs
    - src/Scenes/FarmScene.cs

key-decisions:
  - "Added PopImmediate to SceneManager for instant overlay close (no fade)"
  - "Equipment interaction: select inventory slot first then click equip slot, or click equip slot to unequip to first empty"
  - "Rarity tints as semi-transparent color overlay on slot (green 0.3 alpha for Uncommon, gold 0.3 alpha for Rare)"

patterns-established:
  - "InventoryScene as overlay scene pattern for future menus (shop, crafting)"
  - "Sub-renderer pattern: InventoryGridRenderer and EquipmentRenderer as composable UI components"

requirements-completed: [INV-01, INV-03, INV-04, HUD-02]

# Metrics
duration: 5min
completed: 2026-04-11
---

# Phase 02 Plan 02: Inventory UI Overlay Summary

**InventoryScene overlay with 20-slot grid (click-to-move, rarity color tints), Tibia-style equipment tab (weapon/armor with stat display), tab switching, and I-key open/close from FarmScene**

## Performance

- **Duration:** 5 min
- **Started:** 2026-04-11T03:26:52Z
- **Completed:** 2026-04-11T03:31:22Z
- **Tasks:** 2 of 3 (Task 3 is human-verify checkpoint)
- **Files modified:** 5

## Accomplishments
- InventoryScene overlay drawn on top of game world with semi-transparent dark background
- Two tabs: Items (20-slot grid) and Equipment (Tibia-style silhouette), switched via Tab key
- InventoryGridRenderer rendering 5x4 slot grid with item icons, rarity color tints (green=Uncommon, gold=Rare), and quantity labels
- Click-to-move item interaction: first click selects, second click moves/swaps
- EquipmentRenderer with weapon and armor slots around character silhouette, combined ATK/DEF stat display
- Equipment interaction: equip from selected inventory slot, unequip to first empty slot
- PopImmediate added to SceneManager for instant overlay open/close (no fade transition)
- I-key opens inventory from FarmScene, I or Escape closes it
- Test items added covering all rarities: Cabbage (Common), Cosmic Carrot (Uncommon), Flame Blade (Rare), plus Leather Armor and Health Potion

## Task Commits

Each task was committed atomically:

1. **Task 1: InventoryScene, InventoryGridRenderer, EquipmentRenderer** - `6eb2651` (feat)
2. **Task 2: FarmScene wiring and multi-rarity test items** - `d6f66f3` (feat)

## Files Created/Modified
- `src/Scenes/InventoryScene.cs` - Overlay scene with tab state, click handling, close on I/Escape
- `src/UI/InventoryGridRenderer.cs` - 20-slot grid rendering with selection, rarity colors, quantity text
- `src/UI/EquipmentRenderer.cs` - Tibia-style equipment layout with weapon/armor slots and stat summary
- `src/Core/SceneManager.cs` - Added PopImmediate() for instant scene removal without fade
- `src/Scenes/FarmScene.cs` - Enabled I-key handler, added multi-rarity test items

## Decisions Made
- Added PopImmediate to SceneManager for instant overlay close -- PushImmediate existed but Pop only had fade version
- Equipment interaction uses two-step flow: select inventory slot then click equipment slot to equip; click occupied equipment slot with no selection to unequip to first empty slot
- Rarity tints drawn as semi-transparent color overlays (0.3 alpha) over the slot background, not as border outlines

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added PopImmediate to SceneManager**
- **Found during:** Task 1
- **Issue:** Plan noted SceneManager.Pop() does fade transition. Inventory overlay should close instantly (no delay). PopImmediate did not exist.
- **Fix:** Added PopImmediate() method following same pattern as PushImmediate (pop + unload, no fade state machine)
- **Files modified:** src/Core/SceneManager.cs
- **Commit:** 6eb2651

---

**Total deviations:** 1 auto-fixed (missing critical functionality)
**Impact on plan:** Minimal -- PopImmediate is 5 lines following existing pattern.

## Issues Encountered
None.

## User Setup Required
None.

## Next Phase Readiness
- Task 3 is a human-verify checkpoint awaiting visual confirmation
- All UI components compile and are wired to FarmScene
- Ready for `dotnet run` visual testing

---
*Phase: 02-items-inventory*
*Completed: 2026-04-11 (Tasks 1-2; Task 3 pending checkpoint)*
