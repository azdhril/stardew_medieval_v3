---
phase: 06-progression-polish
plan: 03
subsystem: ui
tags: [nineslice, hud, xp-bar, pixel-art, monogame]

# Dependency graph
requires:
  - phase: 06-01
    provides: ProgressionManager with XP/Level tracking
  - phase: 06-02
    provides: LevelUpBanner, DeathBanner, auto-save timer
provides:
  - NineSlice panels for clock/day, gold, and quest tracker HUD elements
  - XP progress bar above hotbar with level label
  - Icon-decorated HUD labels (clock icon, gold coin icon)
  - Clean status bars with no numeric text overlays
  - UITheme eagerly loaded in FarmScene for HUD rendering
affects: [ui, hud, visual-polish]

# Tech tracking
tech-stack:
  added: []
  patterns: [NineSlice panels for HUD elements, UITheme shared across HUD and overlays]

key-files:
  created: []
  modified:
    - src/UI/HUD.cs
    - src/UI/UITheme.cs
    - src/Scenes/FarmScene.cs
    - src/Scenes/CastleScene.cs
    - src/Scenes/ShopScene.cs

key-decisions:
  - "UITheme eagerly loaded in FarmScene (not lazy) so HUD NineSlice panels render from frame 1"
  - "Reused PanelTitle texture as PanelSmall for HUD panels (consistent with established visual style)"
  - "XP bar positioned 6px above hotbar at full hotbar width for visual alignment"
  - "Removed all bar numeric text (HP/MP/STA) per plan -- bars are visually expressive at 960x540"
  - "DrawQuestTracker theme parameter is optional (null default) preserving backward compat for callers without theme"

patterns-established:
  - "HUD dependency injection via setter methods (SetProgression, SetTheme, SetInventory) for optional services"
  - "NineSlice panels on HUD elements with flat-rect fallbacks when theme unavailable"

requirements-completed: [HUD-01, HUD-04]

# Metrics
duration: 6min
completed: 2026-04-16
---

# Phase 06 Plan 03: HUD Polish Summary

**NineSlice panels for clock/gold/quest tracker, XP bar above hotbar with level label, removed bar text overlays -- HUD now matches pixel-art medieval visual language**

## Performance

- **Duration:** 6 min
- **Started:** 2026-04-16T23:43:49Z
- **Completed:** 2026-04-16T23:49:41Z
- **Tasks:** 1 of 2 (Task 2 is checkpoint:human-verify -- pending)
- **Files modified:** 5

## Accomplishments
- Replaced plain-text clock/day display with NineSlice panel + clock icon
- Replaced plain gold text with NineSlice panel + coin icon
- Added XP progress bar above hotbar with yellow fill and "Lv X" label
- Removed numeric text overlays from HP/MP/STA bars (visually clean)
- Upgraded quest tracker background to NineSlice panel (with flat-rect fallback)
- Removed "F" text from fireball cooldown indicator
- Added 5 new texture properties to UITheme (GoldIcon, ClockIcon, XPBarBg, XPBarFill, PanelSmall)
- Wired UITheme eagerly in FarmScene for HUD rendering from first frame

## Task Commits

Each task was committed atomically:

1. **Task 1: XP bar + Level label + remove bar text + clock/gold NineSlice panels** - `897ea45` (feat)

**Task 2: Visual verification of complete Phase 6 systems** -- PENDING (checkpoint:human-verify)

## Files Created/Modified
- `src/UI/HUD.cs` - Complete HUD polish: NineSlice panels for clock/gold/quest, XP bar, removed bar text, ProgressionManager/UITheme wiring
- `src/UI/UITheme.cs` - Added GoldIcon, ClockIcon, XPBarBg, XPBarFill, PanelSmall texture properties and PanelSmallInsets
- `src/Scenes/FarmScene.cs` - Eagerly initializes UITheme and wires Progression + Theme to HUD
- `src/Scenes/CastleScene.cs` - Pass Services.Theme to DrawQuestTracker for NineSlice panel
- `src/Scenes/ShopScene.cs` - Pass Services.Theme to DrawQuestTracker for NineSlice panel

## Decisions Made
- Used PanelTitle.png (existing asset) as PanelSmall for HUD panels -- visually consistent with established UI
- UITheme loaded eagerly in FarmScene rather than lazily in overlay scenes, since HUD draws every frame
- DrawQuestTracker signature extended with optional UITheme parameter (null default) so CastleScene/ShopScene can pass theme without breaking existing callers
- XP bar uses Style1 progress sprites (yellow fill) matching the medieval gold aesthetic

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Restored source files from base commit in worktree**
- **Found during:** Task 1 (reading source files)
- **Issue:** Worktree was created from an older branch, missing ProgressionManager, ServiceContainer.Progression slot, and other files from base commit f6224a3
- **Fix:** Ran `git checkout f6224a3 --` on all source files except the two being actively modified (HUD.cs, UITheme.cs)
- **Files modified:** src/Scenes/FarmScene.cs, src/Core/ServiceContainer.cs, src/Progression/, and other dependency files
- **Verification:** dotnet build succeeds, all 52 tests pass
- **Committed in:** 897ea45 (part of task commit -- only task-modified files staged)

**2. [Rule 2 - Missing Critical] Constructor parameter for InventoryManager**
- **Found during:** Task 1 (comparing with base commit)
- **Issue:** Plan's HUD constructor didn't include InventoryManager parameter, but base commit already passes _inventory in FarmScene
- **Fix:** Added `InventoryManager? inventory = null` optional parameter to HUD constructor (matching base commit pattern)
- **Verification:** FarmScene `new HUD(... _inventory)` call compiles correctly

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 missing critical)
**Impact on plan:** Both fixes necessary for compilation. No scope creep.

## Issues Encountered
None beyond the worktree sync issue documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Task 2 (checkpoint:human-verify) awaits visual verification of complete Phase 6 deliverables
- All code changes are committed and build/tests pass
- Visual verification should confirm: NineSlice panels, XP bar, clean bars, level-up feedback, death UX

---
*Phase: 06-progression-polish*
*Completed: 2026-04-16*
