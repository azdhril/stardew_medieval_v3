---
phase: 04-world-npcs
plan: 02
subsystem: world-topology
tags: [scenes, tilemaps, transitions, spawns, services]
requirements: [WLD-01, WLD-02, WLD-03, WLD-04]
dependency_graph:
  requires:
    - "World/TileMap.Triggers (Plan 04-01)"
    - "World/TriggerZone record (Plan 04-01)"
    - "Core/SceneManager.TransitionTo (fade pipeline)"
    - "Core/ServiceContainer (DI bag)"
  provides:
    - "Scenes/VillageScene.cs"
    - "Scenes/CastleScene.cs"
    - "Scenes/ShopScene.cs"
    - "Content/Maps/{village,castle,shop}.tmx"
    - "Services.Player shared slot (used by Plans 04-03, 04-04)"
    - "Services.GameState shared slot"
    - "FarmScene(fromScene) ctor overload"
  affects:
    - "Plan 04-03 (King NPC lives in CastleScene)"
    - "Plan 04-04 (Shopkeeper lives in ShopScene)"
tech-stack:
  added: []
  patterns:
    - "Scene-per-map with Services.Player shared instance (brownfield extension)"
    - "Trigger name -> scene-switch dispatch via Rectangle.Intersects"
    - "Per-entry spawn dictionaries keyed by fromScene string"
key-files:
  created:
    - "Content/Maps/village.tmx"
    - "Content/Maps/castle.tmx"
    - "Content/Maps/shop.tmx"
    - "Scenes/VillageScene.cs"
    - "Scenes/CastleScene.cs"
    - "Scenes/ShopScene.cs"
  modified:
    - "Core/ServiceContainer.cs"
    - "Scenes/FarmScene.cs"
    - "Content/Maps/test_farm.tmx"
decisions:
  - "Shared player via Services.Player so state survives scene transitions (WLD-04)"
  - "Farm-tileset used as placeholder art for all 3 new maps per research A2"
  - "Trigger->scene dispatch uses Rectangle.Intersects on CollisionBox (no separate proximity check)"
metrics:
  duration: "~30 min"
  completed: "2026-04-12"
  tasks: 3
  files_touched: 9
---

# Phase 04 Plan 02: Village / Castle / Shop Scenes Summary

Deliver the navigable world topology (WLD-01..04) as a vertical slice: the player can now walk Farm -> Village -> Castle/Shop -> Village -> Farm with fade-to-black hops and per-entry spawn placement. Shells only for Castle/Shop interiors -- their NPCs and Shop UI land in Plans 04-03 and 04-04 respectively.

## TMX Files Authored

| File | Size | Tileset | Collision | Triggers |
|------|------|---------|-----------|----------|
| `Content/Maps/village.tmx` | 60x34 (960x544) | farm_tileset.tsx, tile 74 grass | N/E/S perimeter walls; west edge open | `exit_to_farm` (x=0, y=240, 16x64), `door_castle` (x=192, y=96, 32x16), `door_shop` (x=720, y=96, 32x16) |
| `Content/Maps/castle.tmx` | 40x30 (640x480) | farm_tileset.tsx, tile 74 | Full perimeter + south wall gap at x=192..224 | `exit_to_village` (x=192, y=464, 32x16) |
| `Content/Maps/shop.tmx` | 40x30 (640x480) | farm_tileset.tsx, tile 74 | Same as castle | `exit_to_village` (x=192, y=464, 32x16) |
| `Content/Maps/test_farm.tmx` (modified) | 40x30 (pre-existing) | (unchanged) | (unchanged) | NEW: `enter_village` (x=624, y=208, 16x64) |

## Spawn-Point Dicts Per Scene

```csharp
// VillageScene
["Farm"]   = (48, 270)
["Castle"] = (208, 128)
["Shop"]   = (736, 128)

// CastleScene
["Village"] = (208, 416)

// ShopScene
["Village"] = (208, 416)

// FarmScene (switch on _fromScene)
"Village" => (896, 272)
_         => existing (tile(10,10) or save-loaded position)
```

All spawn points are fully outside their own scene's trigger zones (mitigates T-04-06 re-entry loop).

## Trigger Name Conventions (unchanged from plan)

| Trigger name | Source map | Target scene |
|--------------|-----------|--------------|
| `enter_village` | test_farm.tmx (east edge) | VillageScene(from="Farm") |
| `exit_to_farm` | village.tmx (west edge) | FarmScene(from="Village") |
| `door_castle` | village.tmx | CastleScene(from="Village") |
| `door_shop` | village.tmx | ShopScene(from="Village") |
| `exit_to_village` | castle.tmx, shop.tmx | VillageScene(from="Castle"|"Shop") |

All names verified via grep at build time.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `9521cc0` | TMX maps: village, castle, shop |
| 2 | `bc41a1a` | VillageScene + CastleScene + ShopScene + ServiceContainer slots |
| 3 | `48c5020` | FarmScene(fromScene) ctor + east-edge trigger + test_farm.tmx Triggers group |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Services.Player slot did not exist**
- **Found during:** Task 2
- **Issue:** Plan interfaces reference `Services.Player!.Position` and `Services.Player.CollisionBox` as if a Player slot existed on ServiceContainer, but it did not. FarmScene owned a private `_player` field with no cross-scene accessor. Without a shared player reference, WLD-04 (state preserved across transitions) is impossible.
- **Fix:** Added `PlayerEntity? Player`, `Texture2D? PlayerSpriteSheet`, and `GameState? GameState` slots to ServiceContainer. FarmScene.LoadContent now initializes Services.Player lazily (only on first entry) and reuses the shared instance on subsequent entries. All three new scenes (Village/Castle/Shop) read Services.Player and re-position it at their scene's spawn point. State (HP, iframes, stamina, inventory) naturally carries across transitions because the same PlayerEntity instance survives.
- **Files modified:** `Core/ServiceContainer.cs`, `Scenes/FarmScene.cs`
- **Commits:** `bc41a1a`, `48c5020`

**2. [Rule 2 - Missing critical functionality] Services.GameState slot did not exist**
- **Found during:** Task 2
- **Issue:** Plan spec said "writes `Services.GameState.CurrentScene = ...` on entry" but ServiceContainer had no GameState slot and FarmScene only materialized a GameState on day-advance. Scenes had no way to update the persisted CurrentScene.
- **Fix:** Added `GameState? GameState` slot. FarmScene publishes `_loadedState` to Services after load. Village/Castle/Shop scenes update `Services.GameState.CurrentScene` on entry. Next save (triggered on day-advance) captures the correct last-active scene.
- **Files modified:** `Core/ServiceContainer.cs`, `Scenes/FarmScene.cs`, plus read in all 3 new scenes.
- **Commits:** `bc41a1a`, `48c5020`

### Not deviations, but worth noting

- Ground tile in all new maps is `74` (grass, same as test_farm.tmx default) rather than tile `1`, since the farm_tileset.tsx uses tile 74 as its default grass cell. Functionally identical (any tile draws fine) but matches the existing pattern.
- `dotnet build` shows zero warnings on every task boundary.

## Known Stubs

ShopScene and CastleScene are intentional shells per plan -- their NPCs and UI are scheduled for Plans 04-03 and 04-04. Player can walk in, see the placeholder grass floor, and walk back out. Documented in plan header ("NOTE: King NPC is added in Plan 03" / "Shell only here").

## Manual Smoke Walkthrough (Task 3 Verify Block)

Per the plan, the Task 3 manual smoke is a **REQUIRED 7-step walkthrough** that needs a human at the keyboard (game window, WASD, visual fade observation). Execution environment is non-interactive (YOLO mode, no GUI session), so the walkthrough is **DEFERRED to the user for final verification**. All prerequisites are satisfied automatically:

- [x] 1. Fresh boot spawns player on farm (existing behavior, no code path changed for `_fromScene == "Fresh"`).
- [ ] 2. Walk east onto trigger -> fade -> VillageScene. **Automated precondition met:** trigger `enter_village` exists in test_farm.tmx at (624, 208, 16x64); FarmScene.Update loops over `_map.Triggers` and calls `TransitionTo(new VillageScene(Services, "Farm"))`. Console log on entry: `[VillageScene] Entered from Farm, spawn (48,270)`.
- [ ] 3. Walk west off village -> fade -> Farm at (896, 272). **Automated precondition met:** VillageScene dispatches `exit_to_farm` to `FarmScene(Services, "Village")`; FarmScene LoadContent sets `_player.Position = (896, 272)` for `fromScene == "Village"`.
- [ ] 4. Walk onto castle door. **Automated precondition met:** `door_castle` trigger at (192, 96, 32x16); dispatch to `CastleScene(Services, "Village")`.
- [ ] 5. Exit castle south -> Village at (208, 128). **Automated precondition met:** `exit_to_village` in castle.tmx; VillageScene spawn for `fromScene == "Castle"` is (208, 128).
- [ ] 6. Same flow for shop door / exit.
- [ ] 7. Save + relaunch: CurrentScene persists through save. **Automated precondition met:** Services.GameState is wired and each scene updates CurrentScene on entry. Day-advance triggers `SaveManager.Save(state)` which captures the then-current scene name. (Game always boots to Farm per research Open Q #3 regardless of persisted CurrentScene; the field is used by HUD/quest consumers, not a startup route.)

SceneManager re-entry guard (`if (_state != TransitionState.None) return`) prevents trigger spam during fade. This was verified by code review only.

## Verification Summary

- `dotnet build stardew_medieval_v3.csproj -c Debug --nologo -v q` **exits 0 with 0 warnings** at every task boundary.
- Every grep check from the plan's `<verify>` blocks passes:
  - `name="exit_to_farm"`, `name="door_castle"`, `name="door_shop"` in village.tmx -> 1 each.
  - `name="exit_to_village"` in castle.tmx and shop.tmx -> 1 each.
  - `name="enter_village"` in test_farm.tmx -> 1.
  - `class VillageScene : Scene`, `class CastleScene : Scene`, `class ShopScene : Scene` -> 1 each.
  - `_fromScene|fromScene` in FarmScene.cs -> 5 hits (field + param + switch + log + store).
- Maps copy to `bin/Debug/net8.0/Content/Maps/` on build (verified via ls).

## Interfaces Block Deltas

Plan's `<interfaces>` block referenced `Services.Player` and `Services.GameState` as if they already existed. They did not. Both slots were added to ServiceContainer in this plan's Task 2. Downstream plans (04-03 King NPC dialogue, 04-04 Shop UI) can now rely on these slots as pre-existing contracts.

## Self-Check: PASSED

- Content/Maps/village.tmx: FOUND (contains `name="exit_to_farm"`, `name="door_castle"`, `name="door_shop"`)
- Content/Maps/castle.tmx: FOUND (contains `name="exit_to_village"`)
- Content/Maps/shop.tmx: FOUND (contains `name="exit_to_village"`)
- Scenes/VillageScene.cs: FOUND (contains `class VillageScene : Scene`, 3 spawn entries, 3 trigger cases)
- Scenes/CastleScene.cs: FOUND (contains `class CastleScene : Scene`, `exit_to_village` case)
- Scenes/ShopScene.cs: FOUND (contains `class ShopScene : Scene`, `exit_to_village` case)
- Core/ServiceContainer.cs: modified (contains `Player`, `PlayerSpriteSheet`, `GameState` slots)
- Scenes/FarmScene.cs: modified (contains `_fromScene`, `VillageScene(Services`, `Services.Player = _player`, `enter_village`)
- Content/Maps/test_farm.tmx: modified (contains `name="enter_village"`)
- Commits 9521cc0, bc41a1a, 48c5020: all FOUND in git log
- `dotnet build -c Debug --nologo -v q`: 0 warnings, 0 errors
