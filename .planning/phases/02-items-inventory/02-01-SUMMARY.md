---
phase: 02-items-inventory
plan: 01
subsystem: inventory
tags: [monogame, inventory, hotbar, spritesheet, equipment]

# Dependency graph
requires:
  - phase: 01-architecture-foundation
    provides: Scene architecture, ServiceContainer, ItemRegistry, GameState, InputManager
provides:
  - InventoryManager with 20 slots, stacking, equipment, save/load
  - SpriteAtlas for item icon rendering from spritesheet
  - HotbarRenderer with 8-slot visual at screen bottom
  - Mouse input tracking in InputManager
  - Weapon, armor, and consumable item definitions
affects: [02-items-inventory plan 02, 02-items-inventory plan 03, combat-system]

# Tech tracking
tech-stack:
  added: []
  patterns: [SpriteAtlas for icon mapping, InventoryManager as pure data class, HotbarRenderer screen-space UI]

key-files:
  created:
    - Inventory/InventoryManager.cs
    - Inventory/EquipmentData.cs
    - Data/SpriteAtlas.cs
    - UI/HotbarRenderer.cs
  modified:
    - Core/InputManager.cs
    - Core/ServiceContainer.cs
    - Scenes/FarmScene.cs
    - UI/HUD.cs
    - Data/items.json

key-decisions:
  - "SpriteAtlas uses grid-based registration with fallback rectangle for missing sprites"
  - "Hotbar is first 8 inventory slots (shared data, not separate storage)"
  - "Equipment swap: equipping puts old equipment back in the same slot"

patterns-established:
  - "SpriteAtlas.CreateDefault() for centralized sprite registration"
  - "InventoryManager as pure data class consumed by multiple UI renderers"
  - "ServiceContainer.Inventory for cross-scene inventory access"

requirements-completed: [INV-01, INV-02]

# Metrics
duration: 4min
completed: 2026-04-11
---

# Phase 02 Plan 01: Inventory Data Layer & Hotbar Summary

**20-slot InventoryManager with stacking/equipment/save-load, SpriteAtlas for item icons, 8-slot HotbarRenderer at screen bottom, mouse input tracking, and weapon/armor/consumable test items**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-11T02:17:31Z
- **Completed:** 2026-04-11T02:21:27Z
- **Tasks:** 2
- **Files modified:** 9

## Accomplishments
- InventoryManager managing 20 slots with TryAdd (two-pass stacking), RemoveAt, MoveItem (merge or swap), equipment equip/unequip, and GameState save/load
- SpriteAtlas mapping all item SpriteIds to 16x16 grid positions in the pickup items spritesheet
- HotbarRenderer drawing 8 numbered slots at screen bottom with item icons, quantity labels, and active slot highlighting
- Mouse state tracking (position, left/right click detection) added to InputManager for Plan 02
- 7 new item definitions: 3 weapons, 3 armor, 1 consumable with stat bags

## Task Commits

Each task was committed atomically:

1. **Task 1: InventoryManager, EquipmentData, SpriteAtlas, mouse input, and test items** - `2eb0999` (feat)
2. **Task 2: HotbarRenderer and FarmScene integration** - `a2dca95` (feat)

## Files Created/Modified
- `Inventory/InventoryManager.cs` - Pure data class: 20 slots, hotbar index, equipment, stacking, save/load
- `Inventory/EquipmentData.cs` - Static equipment stat calculator from weapon/armor definitions
- `Data/SpriteAtlas.cs` - SpriteId to Rectangle mapping for item icon spritesheet
- `UI/HotbarRenderer.cs` - 8-slot hotbar at screen bottom with slot textures and item icons
- `Core/InputManager.cs` - Added mouse state tracking (position, click detection)
- `Core/ServiceContainer.cs` - Added nullable Inventory property for cross-scene access
- `Scenes/FarmScene.cs` - Inventory creation, hotbar integration, save/load, test items, number key handling
- `UI/HUD.cs` - Removed controls hint text (replaced by hotbar)
- `Data/items.json` - Added weapons (Iron/Steel/Flame Sword), armor (Leather/Iron/Dragon), Health Potion

## Decisions Made
- SpriteAtlas uses grid-based registration with fallback to (0,0,16,16) for unregistered sprites
- Hotbar slots 0-7 are the first 8 inventory slots (shared data, no duplication)
- Equipment swap puts old equipment back in the same slot the new one came from
- Test items (Cabbage x5, Iron Sword) added before save-load check so fresh games have items visible

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Copied untracked source files from main repo to worktree**
- **Found during:** Task 1 (pre-build verification)
- **Issue:** Many source files (InputManager.cs, TimeManager.cs, Camera.cs, HUD.cs, Farming/, World/, Player/PlayerStats.cs, etc.) exist in main repo working directory but were never committed to git. Worktree only has tracked files, so build would fail.
- **Fix:** Copied all untracked source files and content assets from main repo to worktree
- **Files modified:** 15+ untracked files copied (not committed as new -- they are pre-existing code)
- **Verification:** dotnet build passes before any plan changes

**2. [Rule 3 - Blocking] Installed dotnet-mgcb tool and copied tools manifest**
- **Found during:** Task 1 (pre-build verification)
- **Issue:** MonoGame Content Builder tool not available in worktree, causing build failure
- **Fix:** Copied .config/dotnet-tools.json from main repo and ran dotnet tool restore
- **Verification:** dotnet build passes with content pipeline

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both auto-fixes were environment setup for the worktree. No code changes, no scope creep.

## Issues Encountered
- Spritesheet path in plan says `Content/Sprites/Farming/Pickup_Items.png` but actual location is `Content/Sprites/Items/Pickup_Items.png`. Used correct path in implementation.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- InventoryManager ready for Plan 02 (inventory UI overlay with drag-and-drop)
- SpriteAtlas ready for Plan 02 (item rendering in grid)
- Mouse input tracking ready for Plan 02 (click/drag detection)
- ServiceContainer.Inventory ready for InventoryScene cross-scene access
- I-key handler commented out, ready to uncomment when InventoryScene is created

---
*Phase: 02-items-inventory*
*Completed: 2026-04-11*
