---
phase: 01-architecture-foundation
plan: 03
subsystem: architecture
tags: [scene-management, game-state, save-migration, monogame]

requires:
  - phase: 01-01
    provides: Entity base class, ItemStack, ItemRegistry, Direction enum
  - phase: 01-02
    provides: Scene abstract class, ServiceContainer, SceneManager with fade transitions

provides:
  - Expanded GameState with inventory, gold, XP, level, scene, quest, equipment, hotbar fields
  - Save migration v2->v3 with safe defaults for all new fields
  - FarmScene containing all farm gameplay extracted from Game1
  - TestScene placeholder for transition testing
  - Game1 refactored to thin shell delegating to SceneManager

affects: [farming, combat, inventory, quests, dungeon, ui]

tech-stack:
  added: []
  patterns: [scene-based-architecture, service-container-injection, save-migration-chain]

key-files:
  created:
    - src/Scenes/FarmScene.cs
    - src/Scenes/TestScene.cs
  modified:
    - src/Core/GameState.cs
    - src/Core/SaveManager.cs
    - Game1.cs

key-decisions:
  - "GameState v3 uses safe defaults (gold=0, level=1, scene=Farm) so old saves load without data loss"
  - "FarmScene subscribes/unsubscribes to TimeManager events in LoadContent/UnloadContent for clean lifecycle"
  - "InputManager stays in Game1 (updated before any scene) while TimeManager is scene-controlled"
  - "Game1 is under 60 lines of logic with zero gameplay code"

patterns-established:
  - "Scene lifecycle: LoadContent -> Update/Draw loop -> UnloadContent"
  - "Service access via Services property inherited from Scene base class"
  - "Save migration chain: each version block checks SaveVersion < N and upgrades incrementally"

requirements-completed: [ARCH-04, ARCH-05]

duration: 60min
completed: 2026-04-10
---

# Phase 01 Plan 03: GameState Expansion, FarmScene Extraction, Game1 Thin Shell

**Expanded GameState with v3 fields (inventory/gold/XP/equipment), extracted all farm gameplay into FarmScene, and refactored Game1 to a thin shell delegating to SceneManager with fade transitions.**

## Performance

- **Duration:** ~60 min
- **Started:** 2026-04-10T20:56:20Z
- **Completed:** 2026-04-10T21:56:21Z
- **Tasks:** 2/3 (Task 3 is human-verify checkpoint)
- **Files modified:** 5

## Accomplishments

### Task 1: Expand GameState and save migration v2->v3
- Added 9 new fields to GameState: Inventory, Gold, XP, Level, CurrentScene, QuestState, WeaponId, ArmorId, HotbarSlots
- Updated SaveVersion default from 2 to 3
- Added v2->v3 migration block in SaveManager.MigrateIfNeeded with safe defaults
- Old saves load transparently: inventory=empty, gold=0, XP=0, level=1, scene="Farm"
- Commit: `57ade36`

### Task 2: Extract FarmScene, create TestScene, refactor Game1
- Created `src/Scenes/FarmScene.cs` with all farm gameplay logic extracted from Game1 (tilemap, player, farming, HUD, day/night cycle, save/load)
- Created `src/Scenes/TestScene.cs` as a placeholder scene with dark blue background and text, proving transitions work
- Refactored `Game1.cs` to thin shell: creates ServiceContainer + SceneManager, pushes FarmScene, delegates Update/Draw
- Game1 went from ~255 lines to ~80 lines (under 60 lines of logic)
- Scene transitions: T key to TestScene, B key to return to FarmScene, with fade-to-black effect
- Commit: `bd7d7cf`

### Task 3: Human verification checkpoint
- Awaiting human verification that game boots identically and scene transitions work

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

- `dotnet build` passes with 0 errors, 0 warnings
- All acceptance criteria verified via grep checks
- Game1.cs contains no TileMap, PlayerEntity, GridManager, or CropManager references
- FarmScene contains all gameplay logic with proper event subscription/unsubscription

## Self-Check: PASSED

- All 5 files found in worktree
- Commits 57ade36 and bd7d7cf verified
- Build passes with 0 errors
- Awaiting human verification (Task 3 checkpoint)
