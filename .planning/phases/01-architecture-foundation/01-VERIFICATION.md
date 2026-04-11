---
phase: 01-architecture-foundation
verified: 2026-04-10T22:30:00Z
status: gaps_found
score: 3/4 must-haves verified
overrides_applied: 0
gaps:
  - truth: "A test entity (e.g. dummy NPC) can be spawned using the Entity base class with position, sprite, and collision"
    status: failed
    reason: "Entity base class exists and PlayerEntity inherits it correctly, but no second concrete Entity subclass (dummy NPC or test entity) was created to demonstrate that the class can be extended beyond the player. The roadmap SC requires proving extensibility by actually spawning a non-player entity."
    artifacts:
      - path: "Core/Entity.cs"
        issue: "File exists and is substantive -- abstract class is correct. No concrete non-player entity inherits from it."
    missing:
      - "A concrete Entity subclass (e.g. Scenes/TestScene.cs or a dedicated DummyNpc.cs) that instantiates and draws an entity with Position, SpriteSheet, and CollisionBox to prove Phase 3 enemies can extend Entity"
human_verification:
  - test: "Boot the game and observe the farm scene"
    expected: "Visual output is identical to pre-refactor: player walks with WASD, can till/plant/water/harvest, HUD shows stamina/time, day/night cycle visible"
    why_human: "No automated rendering checks -- regression can only be confirmed visually"
  - test: "Press T in farm scene, then press B to return"
    expected: "Screen fades to black, shows dark blue 'Test Scene - Press B to go back', fades again and returns to farm with all state preserved"
    why_human: "Fade animation and scene state preservation require a running game to confirm"
  - test: "Load a v2 save file (SaveVersion=2, no Inventory/Gold/XP fields)"
    expected: "Game loads without crash; Inventory=empty, Gold=0, XP=0, Level=1, CurrentScene=Farm set as defaults; day/farm state preserved"
    why_human: "Requires an actual v2 save file and observing console output for migration log message"
---

# Phase 1: Architecture Foundation Verification Report

**Phase Goal:** The codebase supports multiple scenes, shared entity behavior, and extensible game state -- unblocking all feature work
**Verified:** 2026-04-10T22:30:00Z
**Status:** gaps_found
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths (Roadmap Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| SC-1 | Game boots into FarmScene that behaves identically to current game (no regression) | ? HUMAN NEEDED | Build passes with 0 errors/warnings. FarmScene.cs contains all gameplay logic (TileMap, Player, GridManager, CropManager, HUD, day/night, save/load). Game1.cs has no gameplay fields. Visual regression requires human verification. |
| SC-2 | Player can transition between at least two placeholder scenes (Farm and test scene) with fade in/out | ✓ VERIFIED | FarmScene.cs:105 calls `Services.SceneManager.Push(new TestScene(Services))` on T key. TestScene.cs:32 calls `Services.SceneManager.Pop()` on B key. SceneManager implements FadingOut->FadingIn state machine with 0.4s FadeDuration. |
| SC-3 | A test entity (e.g. dummy NPC) can be spawned using the Entity base class with position, sprite, and collision | ✗ FAILED | Entity base class exists (Core/Entity.cs) with all D-06 fields. PlayerEntity inherits from it. However, no second concrete Entity subclass was created. No dummy NPC, no non-player entity was added to TestScene or elsewhere. The SC requires proving extensibility by actually spawning a non-player entity. |
| SC-4 | GameState serializes and deserializes the new structure (inventory placeholder, scene, gold) without breaking existing saves | ✓ VERIFIED | GameState.cs has all 9 new v3 fields (Inventory, Gold, XP, Level, CurrentScene, QuestState, WeaponId, ArmorId, HotbarSlots). SaveManager.cs CURRENT_SAVE_VERSION=3, MigrateIfNeeded has `state.SaveVersion < 3` block with safe defaults. Full deserialization test requires human with v2 save. |

**Score:** 2/4 truths verified (+ 1 needs human + 1 failed)

### Plan Must-Haves Cross-Check

All 13 plan-level must-have truths from Plans 01-03 were verified against the actual code and pass. See per-plan details below.

### Deferred Items

None identified. SC-3 is not addressed by any later phase's success criteria (Phase 3 introduces enemies, but SC-3 is about proving Entity extensibility in Phase 1).

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Core/Entity.cs` | Abstract Entity base class | ✓ VERIFIED | Contains abstract class Entity with Position, Velocity, FacingDirection, HP/MaxHP/IsAlive, SpriteSheet/FrameIndex/AnimationTimer (protected), virtual CollisionBox, virtual Update/Draw |
| `Core/Direction.cs` | Direction enum extracted from PlayerEntity | ✓ VERIFIED | `public enum Direction { Down, Left, Right, Up }` in Core namespace |
| `Core/Scene.cs` | Abstract scene base class | ✓ VERIFIED | Contains abstract class Scene with protected ServiceContainer, virtual LoadContent/Update/Draw/UnloadContent |
| `Core/ServiceContainer.cs` | Shared services dependency bag | ✓ VERIFIED | All required fields: GraphicsDevice, SpriteBatch, Input, Time, Camera, Content, SceneManager (setter) |
| `Core/SceneManager.cs` | Stack-based scene manager with fade transitions | ✓ VERIFIED | Stack<Scene>, TransitionTo/Push/Pop/PushImmediate, FadingOut->FadingIn state machine, FadeDuration=0.4f |
| `Core/GameState.cs` | Expanded game state v3 | ✓ VERIFIED | All 9 new fields plus existing v2 fields. SaveVersion default = 3. |
| `Core/SaveManager.cs` | v2->v3 migration | ✓ VERIFIED | CURRENT_SAVE_VERSION=3, `state.SaveVersion < 3` migration block with console log |
| `Data/ItemDefinition.cs` | Unified item model | ✓ VERIFIED | Id, Name, Type, Rarity, StackLimit, SpriteId, Stats (Dictionary<string,float>) |
| `Data/ItemRegistry.cs` | Static registry loading from JSON | ✓ VERIFIED | `public static class ItemRegistry`, Initialize/Get/GetByType/All, JsonStringEnumConverter used |
| `Data/items.json` | 45 item definitions | ✓ VERIFIED | 45 entries (21 crop pairs + 3 tools: Hoe, Watering_Can, Scythe). Contains Cabbage, Prickly_Pear, Hoe. |
| `Scenes/FarmScene.cs` | Farm gameplay scene extracted from Game1 | ✓ VERIFIED | class FarmScene : Scene. Contains _map, _player, _gridManager, _cropManager, _toolController, _hud. Draws map/player/crops/HUD. Event subscribe/unsubscribe. |
| `Scenes/TestScene.cs` | Placeholder scene for transition testing | ✓ VERIFIED | class TestScene : Scene. Dark blue background, text, B key calls Pop(). |
| `Player/PlayerEntity.cs` | Inherits from Entity | ✓ VERIFIED | `public class PlayerEntity : Entity`. No Direction enum inside. No `private Texture2D _spriteSheet`. Uses SpriteSheet/FrameIndex/AnimationTimer inherited. |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| Player/PlayerEntity.cs | Core/Entity.cs | class inheritance | ✓ WIRED | `public class PlayerEntity : Entity` (line 13) |
| Data/ItemRegistry.cs | Data/items.json | JsonSerializer.Deserialize | ✓ WIRED | Initialize() reads file path, uses JsonStringEnumConverter |
| Core/Scene.cs | Core/ServiceContainer.cs | constructor parameter | ✓ WIRED | `protected Scene(ServiceContainer services)` |
| Core/SceneManager.cs | Core/Scene.cs | Stack<Scene> | ✓ WIRED | `private readonly Stack<Scene> _scenes = new()` |
| Game1.cs | Core/SceneManager.cs | delegation in Update/Draw | ✓ WIRED | `_sceneManager.Update(dt)` and `_sceneManager.Draw(_spriteBatch)` |
| Scenes/FarmScene.cs | Core/Scene.cs | class inheritance | ✓ WIRED | `public class FarmScene : Scene` |
| Core/SaveManager.cs | Core/GameState.cs | migration logic | ✓ WIRED | `if (state.SaveVersion < 3)` block in MigrateIfNeeded |

**Notable deviation from Plan 03 spec:** FarmScene uses `Services.SceneManager.Push(new TestScene(Services))` instead of `TransitionTo()`. TestScene uses `Services.SceneManager.Pop()` to return. This is actually the **correct** behavior (preserves FarmScene state on stack) and was committed intentionally (commit `27c3858`). The plan spec was slightly wrong; the implementation is better.

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Scenes/FarmScene.cs | _player, _map, _gridManager | SaveManager.Load() + direct instantiation | Save data loaded from JSON file; map loaded from TMX | ✓ FLOWING |
| Scenes/FarmScene.cs | OnDayAdvanced (auto-save) | GameState populated from live state | Saves DayNumber, PlayerX/Y, StaminaCurrent, FarmCells | ✓ FLOWING |
| Data/ItemRegistry | _items Dictionary | items.json via JsonSerializer | 45 items loaded from file | ✓ FLOWING |

---

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build succeeds with zero errors | `dotnet build --no-restore` | "Compilação com êxito. 0 Aviso(s) 0 Erro(s)" | ✓ PASS |
| items.json has 45 entries | `grep -c '"Id"' Data/items.json` | 45 | ✓ PASS |
| Prickly Pear entry exists | `grep '"Prickly Pear"' Data/items.json` | Found with Uncommon rarity | ✓ PASS |
| Hoe tool entry exists | `grep '"Hoe"' Data/items.json` | Found with Tool type | ✓ PASS |
| Direction enum NOT in PlayerEntity | `grep "enum Direction" Player/PlayerEntity.cs` | Not found | ✓ PASS |
| Game1 has no gameplay fields | `grep "private TileMap\|private PlayerEntity\|GridManager\|CropManager" Game1.cs` | Not found | ✓ PASS |
| No concrete Entity subclass except PlayerEntity | `grep "class.*Entity" **/*.cs` | Only abstract Entity + PlayerEntity : Entity | ✗ FAIL (SC-3 gap) |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| ARCH-01 | Plan 02 | SceneManager gerencia transicao entre scenes com fade in/out | ✓ SATISFIED | SceneManager with stack, TransitionTo/Push/Pop, fade state machine. FarmScene<->TestScene transitions implemented. |
| ARCH-02 | Plan 01 | Entity base class com posicao, sprite, colisao, compartilhada por Player/Enemy/NPC | ~ PARTIAL | Entity base class exists with all required fields. PlayerEntity inherits it. No Enemy/NPC yet (correct for Phase 1), but SC-3 requires at least one non-player entity to prove extensibility. |
| ARCH-03 | Plan 01 | Unified ItemDefinition model para crops, tools, weapons, armor, consumables e loot | ✓ SATISFIED | ItemDefinition with Id/Name/Type/Rarity/StackLimit/SpriteId/Stats. ItemRegistry with 45 items loaded from JSON. ItemType enum covers all categories. |
| ARCH-04 | Plan 03 | GameState reestruturado para suportar inventario, XP, quest state, gold, scene atual | ✓ SATISFIED | All 9 new v3 fields in GameState. Save migration v2->v3 implemented. |
| ARCH-05 | Plan 03 | Game1.cs refatorado para delegar logica para scenes | ✓ SATISFIED | Game1 is 83 lines, zero gameplay fields, delegates Update/Draw entirely to SceneManager. |

---

## Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| Scenes/FarmScene.cs | `private TileMap _map = null!;` + similar null! declarations | INFO | Standard MonoGame pattern -- initialized in LoadContent before use. Not a stub. |
| Scenes/FarmScene.cs | `private Texture2D _pixel = null!;` | INFO | Created in LoadContent via new Texture2D + SetData. Not empty. |

No stub implementations, TODO/FIXME comments, or placeholder returns found in any phase output files.

---

## Human Verification Required

### 1. Farm Scene Visual Regression

**Test:** Run `dotnet run` and play the farm scene for 1-2 minutes
**Expected:** Player moves with WASD, tools switch with keys, tilling/planting/watering works, HUD shows stamina bar and time, day/night darkening visible, sleeping with P advances day and auto-saves
**Why human:** Rendering output, input feel, and visual correctness cannot be verified programmatically

### 2. Scene Transition with Fade

**Test:** In the farm scene, press T. Then press B.
**Expected:** Screen fades to black (~0.4s), shows dark blue background with text "Test Scene - Press B to go back", pressing B fades back to farm scene with farm state preserved (crops/position intact)
**Why human:** Fade animation timing and visual transition require a running game

### 3. Save Migration (v2 to v3)

**Test:** If a v2 save exists (or manually create one with SaveVersion=2 and no Inventory/Gold/XP fields), load the game
**Expected:** Console shows "[SaveManager] Migrated save from v2 to v3". Game loads with farm state intact. Inventory is empty, Gold=0, Level=1.
**Why human:** Requires a v2 save file and observation of console output

---

## Gaps Summary

One gap blocks the roadmap success criteria:

**SC-3 not met:** The Entity base class is complete and correct, and PlayerEntity demonstrates inheritance properly. However, the roadmap requires that "a test entity (e.g. dummy NPC) **can be spawned**" -- implying a second concrete entity is created and rendered in-game. Neither the plans nor the implementation created a non-player entity. The Entity class is a solid foundation, but its extensibility to non-player entities has not been demonstrated in Phase 1 as the roadmap requires.

**Suggested fix:** Add a minimal concrete entity class (e.g. `Player/DummyNpc.cs` with a colored rectangle sprite) and instantiate one in `Scenes/TestScene.cs` or `FarmScene.cs` at a fixed position. This is a small addition (~30 lines) that directly proves SC-3.

---

_Verified: 2026-04-10T22:30:00Z_
_Verifier: Claude (gsd-verifier)_
