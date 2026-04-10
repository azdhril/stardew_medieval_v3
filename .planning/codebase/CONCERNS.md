# Codebase Concerns

**Analysis Date:** 2025-02-27

## Tech Debt

### 1. Verbose Console Logging in Production Code
- **Issue**: 34 direct Console.WriteLine() calls scattered throughout gameplay systems
- **Files**: 
  - `Core/SaveManager.cs` (4 calls)
  - `Core/GameState.cs` (implicit via SaveManager)
  - `Core/TimeManager.cs` (implicit via callers)
  - `Farming/GridManager.cs` (8 calls)
  - `Farming/CropManager.cs` (7 calls)
  - `Farming/ToolController.cs` (9 calls)
  - `World/TileMap.cs` (3 calls)
  - `Data/CropRegistry.cs` (1 call)
  - `Game1.cs` (2 calls)
- **Impact**: 
  - Makes debugging difficult (mixed with game logs)
  - No log level control - all messages visible
  - Performance impact on older systems
  - Cannot disable logging in release builds
- **Fix approach**: Introduce abstraction layer (ILogger interface) with null/filtering implementations for production

### 2. Null-Forgiving Operator Overuse
- **Issue**: 17 declarations using `null!` operator suppress null-safety checks
- **Files**: 
  - `Game1.cs` (8 fields: _spriteBatch, _input, _time, _camera, _map, _player, _gridManager, _cropManager, _toolController, _hud, _pixel)
  - `Farming/GridManager.cs` (2 fields: _tilledTexture, _wateredTexture)
  - `Player/PlayerEntity.cs` (1 field: _spriteSheet)
  - `UI/HUD.cs` (2 fields: _font, _pixel)
  - `World/TileMap.cs` (1 field: _map)
- **Impact**: 
  - Hides real null-reference exceptions that occur at runtime
  - Breaks null-safety contract of C# 8.0 nullable reference types
  - Fields SHOULD be initialized in `Initialize()` or `LoadContent()` - no guarantee they're set
  - If LoadContent fails, fields remain null and crash on use
- **Fix approach**: Use lazy initialization property with backing fields, or guarantee initialization order with assertions

### 3. Lack of Null Checks After Texture/Asset Loading
- **Issue**: `LoadTexture()` in `CropRegistry.cs` (line 194) returns null on failure but callers don't validate
- **Files**: `Data/CropRegistry.cs` (22-181)
- **Scenario**: If a crop spritesheet fails to load (missing file, corrupted), `GrowthSheet` property remains null
- **Impact**: 
  - `GridManager.DrawCrops()` line 204 checks `crop.Data.GrowthSheet == null` but this should never happen if crops load
  - Any missing PNG causes silent failure - crop registered but invisible
  - No feedback to player that assets are missing
- **Fix approach**: Either throw exception on missing assets (fail-fast), or use fallback textures with warning

### 4. Save File Migration Incomplete
- **Issue**: Version migration in `SaveManager.cs` only handles v1→v2, no forward compatibility
- **Files**: `Core/SaveManager.cs` (65-73)
- **Problem**: 
  - If save format changes in future (e.g., v2→v3), old logic doesn't handle it
  - Only one migration path exists
  - New fields added to GameState have no default handling
- **Fix approach**: Implement proper versioned migration chain with per-version upgrade functions

### 5. Crop Data Hardcoded in Code, Not Data-Driven
- **Issue**: All 23 crop definitions hardcoded in `CropRegistry.Initialize()` 
- **Files**: `Data/CropRegistry.cs` (22-178)
- **Impact**: 
  - Adding new crop requires code change + recompilation
  - Cannot validate crop data (no schema)
  - Inconsistent growth timings (all 1 day per stage, but DaysToWilt varies)
- **Fix approach**: Move crop definitions to JSON/CSV, load at startup with validation

## Known Bugs

### 1. Potential Index Out of Bounds in Crop Stage Calculation
- **Trigger**: If `DaysPerStage <= 0` set in CropData
- **Files**: `Farming/CropData.cs` (35-40)
- **Symptom**: `GetStageIndex()` returns 0, but `GetSourceRect()` uses stage*16 as X offset - may read wrong column
- **Current safeguard**: `Clamp(stage, 0, StageCount-1)` prevents true crash but silently uses wrong sprite
- **Workaround**: None - uses last growth stage visually
- **Fix**: Assert DaysPerStage > 0 in CropData constructor

### 2. Watering Overlay Persists Across Save/Load
- **Trigger**: Player waters tile on Day 1, saves game, loads game, checks GridManager
- **Files**: `Farming/GridManager.cs` (119-125), `Core/SaveManager.cs`
- **Symptom**: `IsWatered` flag resets daily per design, BUT if player waters tile -> saves -> exits game -> loads, the watered state is lost (expected), but if they water tile -> saves while still playing same day -> loads same day, watered overlay vanishes
- **Impact**: Cosmetic (watered visual state doesn't affect gameplay since crops only check `IsWatered` on growth)
- **Cause**: `GetSaveData()` saves IsWatered but `OnDayAdvanced()` resets it - save happens *after* watering action
- **Fix**: Store "watered today" flag separately, or don't reset watering in same-day reload

### 3. Harvest of Wilted Crops Clears Tilled State Inconsistently
- **Trigger**: Crop wilts, player harvests with Hands tool
- **Files**: `Farming/ToolController.cs` (88-123)
- **Behavior**: Wilted crop harvest (line 100-107) sets IsTilled=false, IsWatered=false. But normal ripe harvest (line 115-123) does the same
- **Issue**: Wilted crops should arguably stay tilled (player didn't clear the ground), but design requires re-tilling
- **Impact**: Stardew-style is correct, but may surprise players
- **Recommendation**: Document in game UI or comments

## Security Considerations

### 1. Save File Located in User AppData (Information Disclosure Risk)
- **Risk**: `C:\Users\{user}\AppData\Local\StardewMedieval\savegame.json` is plaintext
- **Files**: `Core/SaveManager.cs` (14-18)
- **Current mitigation**: None - JSON is world-readable
- **What could go wrong**: 
  - Player inventory visible to any process on system
  - Cheating enablement
  - Privacy (game progress visible to spouse/roommate)
- **Recommendations**: 
  - Optional encryption (low priority for solo indie game)
  - At minimum, file permissions set to current-user-only

### 2. No Input Validation in Tool Actions
- **Risk**: ToolController dispatches actions based on player input without bounds checking
- **Files**: `Farming/ToolController.cs` (64-66)
- **Scenario**: If player hacks memory/modifies input, could target invalid tiles
- **Current safeguard**: `GridManager.TryTill()` and `GridManager.TryWater()` check `IsFarmZone()` and `IsTillable`
- **Recommendation**: Add tile boundary validation before calling action handlers

## Performance Bottlenecks

### 1. Inefficient Collision Detection in TileMap
- **Problem**: `CheckCircleCollision()` iterates ALL collision polygons every frame
- **Files**: `World/TileMap.cs` (171-187)
- **Scenario**: If map has 100+ collision polygons, player movement check is O(n) per update
- **Current impact**: Likely acceptable for Stardew-scale maps but will hurt on complex maps
- **Improvement path**: 
  - Spatial partitioning (quadtree/grid)
  - Precompile bounding boxes for broad-phase
  - Cache "last valid position" to reduce recomputation

### 2. Dictionary Lookups in DrawCrops/DrawOverlays
- **Problem**: `GridManager` iterates ALL cells every draw frame, even off-screen
- **Files**: `Farming/GridManager.cs` (171-222)
- **Current safeguard**: `viewArea.Intersects()` check prevents rendering off-screen cells, but dictionary iteration is still O(n)
- **Improvement path**: 
  - Spatial index of cells by tile position
  - Only iterate cells within view area
  - Current: 1000 cells = 1000 iterations per frame. With optimization: ~50-100

### 3. Time Manager Precision Loss
- **Problem**: `GameTime` is float, accumulated each frame
- **Files**: `Core/TimeManager.cs` (26)
- **Risk**: Over long sessions (1000+ days), float precision loss could cause drift
- **Impact**: Low - 120 second days * 1000 days = 120,000 seconds. Float has ~7 significant digits precision
- **Improvement path**: Use double for time accumulation, or frame counter

## Fragile Areas

### 1. Crop Spritesheet Layout Assumptions
- **Files**: `Farming/CropData.cs`, `Data/CropRegistry.cs`
- **Why fragile**: 
  - Assumes all sprites are 16px wide per stage
  - Assumes height is only 16, 32, or 48px (hard-coded in sprite.Height)
  - SourceY offset requires manual calculation (e.g., Grape sheet row0=0, row1=32)
  - Typo in filename: `Zuchini_Growth_Stages_16x16.png` (should be Zucchini) - line 149
- **Safe modification**: 
  - Add unit tests for `GetSourceRect()` with all crop types
  - Create crop sprite validator tool to verify layout
  - Document expected spritesheet format (rows=variants, columns=stages)
- **Test coverage**: None - no tests verify sprite extraction

### 2. Player-TileMap Coupling
- **Files**: `Player/PlayerEntity.cs`, `World/TileMap.cs`
- **Why fragile**: 
  - `TileMap.WorldToTile()` and `TileToWorld()` are static conversions
  - Player collision box calculation uses hardcoded frame offset: `_frameHeight / 2 - h - 2` (line 38)
  - If player sprite changes height, collision breaks silently
- **Safe modification**: 
  - Create PlayerDimensions config class (spriteWidth, spriteHeight, collisionOffsetY)
  - Load from data, not hardcoded
  - Add debug overlay to show collision box vs visual
- **Test coverage**: None - no collision tests

### 3. HUD Layout Assumes Fixed Screen Resolution
- **Files**: `UI/HUD.cs`
- **Why fragile**: 
  - Hardcoded positions: stamina bar at (12, screenHeight-30)
  - Control text centered on `screenWidth / 2`
  - If screen resizes or aspect ratio changes, layout breaks
  - No responsive layout system
- **Safe modification**: 
  - Use anchor-based positioning (top-left, bottom-right, center)
  - Create UILayout system with safe areas
  - Test on multiple resolutions
- **Test coverage**: None - UI is visual-only

### 4. CropManager State Not Synchronized with GridManager
- **Files**: `Farming/CropManager.cs`, `Farming/GridManager.cs`, `Game1.cs` (OnDayAdvanced)
- **Why fragile**: 
  - Both managers own crop state (CropManager manages selected crop, GridManager owns cells)
  - Order matters in `OnDayAdvanced()`: crops must tick BEFORE watering resets (Game1.cs line 232)
  - No enforced ordering - if called in wrong order, crops don't wilt correctly
  - Comment says "Order matters" but nothing prevents reordering
- **Safe modification**: 
  - Create single `GameTickSystem` that guarantees order
  - Use explicit phase progression (GrowthPhase -> WiltPhase -> ResetPhase)
  - Add assertion to prevent out-of-order calls
- **Test coverage**: None - no day-advance tests

## Scaling Limits

### 1. GridManager Dictionary Grows Unbounded
- **Current capacity**: Only cells that are tilled/have crops stored
- **Limit**: If player creates massive farm (1000x1000 tiles, all tilled), Dictionary has 1M entries
- **Scaling path**: 
  - Implement cell pooling/sparse grid
  - Use sparse storage (only store non-default cells)
  - At 1000x1000 with all tilled, acceptable on modern PC but poor on Switch/mobile

### 2. Texture Memory for Crop Spritesheets
- **Current**: 23 crops registered, ~2-3 textures per crop variant (some shared)
- **Estimate**: ~40-50 textures * 112x96 avg size = ~5-10MB VRAM
- **Limit**: If 100+ crops added, could exceed GPU memory on low-end devices
- **Scaling path**: 
  - Sprite atlasing (pack all crops into single texture)
  - Streaming (load only visible crops)

### 3. Map Size (TileMap Loading)
- **Current**: ~50x50 maps typical, TiledCS loads entire map into memory
- **Limit**: 1000x1000 map = 1M tile entries * 4 bytes = 4MB per layer
- **Scaling path**: 
  - Chunk-based streaming for large maps
  - Lazy loading of TMX

## Dependencies at Risk

### 1. MonoGame.Framework.DesktopGL 3.8.* (Outdated)
- **Risk**: MonoGame 3.8 released in 2021, unmaintained
- **Impact**: 
  - Security fixes unlikely
  - No support for newer OS features (Vulkan, DX12)
  - May not work on future Windows/macOS versions
- **Migration path**: Upgrade to MonoGame 3.9+, or consider FNA (Stardew uses FNA)

### 2. TiledCS 3.3.3 (No Version Constraint)
- **Risk**: Pinned to exact version, but csproj uses `PackageReference Include="TiledCS" Version="3.3.3"`
- **Impact**: 
  - No protection against breaking changes if manually updated
  - May have bugs/vulnerabilities in older version
- **Migration path**: Review TiledCS 3.4+ for compatibility, or lock version explicitly

## Missing Critical Features

### 1. No Input Rebinding
- **Problem**: Keybinds hardcoded in `ToolController.cs` and `InputManager.cs`
- **Blocks**: Accessibility (can't remap for different keyboard layouts or input devices)
- **Implementation**: Create InputBindings config class

### 2. No Pause/Menu System
- **Problem**: Pressing Escape exits game (Game1.cs line 128), no pause menu
- **Blocks**: Saving mid-day, changing settings, pausing gameplay
- **Implementation**: Add GameState enum (Playing, Paused, Menu)

### 3. No Inventory/Item Storage
- **Problem**: Crops harvested but not tracked (ToolController.cs line 116 just logs)
- **Blocks**: Selling items, crafting, inventory limits
- **Implementation**: Create Inventory system with ItemStack

### 4. No Stamina Restoration During Day
- **Problem**: Stamina only restores on day advance
- **Blocks**: Long play sessions = constant fatigue
- **Implementation**: Add bed interaction to restore stamina

## Test Coverage Gaps

### 1. No Unit Tests for Crop Growth Logic
- **What's not tested**: 
  - Crop stage progression (DaysPerStage calculations)
  - Wilting logic (DaysToWilt timing)
  - Save/load state preservation
- **Files**: `Farming/CropData.cs`, `Farming/CropInstance.cs`, `Farming/CropManager.cs`
- **Risk**: Adding new crops could break growth timings silently
- **Priority**: High - growth is core game loop

### 2. No Tests for TileMap Collision
- **What's not tested**: 
  - Polygon collision detection (CircleIntersectsPolygon)
  - Point-in-polygon algorithm
  - Tileset lookup and rendering
- **Files**: `World/TileMap.cs` (entire file)
- **Risk**: Changes to collision code could break player movement
- **Priority**: High - collision is core mechanic

### 3. No Integration Tests for Day Advancement
- **What's not tested**: 
  - Order of OnDayAdvanced() calls
  - Crop state persistence across days
  - Time manager state reset
- **Files**: `Game1.cs` (OnDayAdvanced method), `Core/TimeManager.cs`
- **Risk**: Fragile state transitions, as noted in CropManager fragility section
- **Priority**: Medium - critical but low frequency execution

### 4. No Save/Load Round-Trip Tests
- **What's not tested**: 
  - Save to JSON, load from JSON, compare state
  - Migration logic
  - Corrupted save file handling
- **Files**: `Core/SaveManager.cs`, `Core/GameState.cs`
- **Risk**: Save corruption goes undetected until next load
- **Priority**: High - data loss is unacceptable

### 5. No Rendering Tests
- **What's not tested**: 
  - Sprite rendering at correct position
  - Overlay visibility
  - Clipping of off-screen elements
- **Files**: `UI/HUD.cs`, `Farming/GridManager.cs`, `Player/PlayerEntity.cs`
- **Risk**: Visual bugs (misaligned sprites, overlapping text)
- **Priority**: Medium - visual-only bugs

---

*Concerns audit: 2025-02-27*
