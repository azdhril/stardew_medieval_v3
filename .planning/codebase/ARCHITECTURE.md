# Architecture

**Analysis Date:** 2026-04-10

## Pattern Overview

**Overall:** Layered architecture with MonoGame as the graphics engine. Clear separation between game core systems (input, time, camera), world systems (tilemap, collision), entity systems (player, farming), and UI. Data flows from input → game logic → entity updates → render pipeline.

**Key Characteristics:**
- Monolithic single-file entry point (`Game1.cs`) coordinates all subsystems
- Event-driven communication (day advancement triggers cascading updates)
- Immediate-mode rendering with camera transformation for world space, separate screen-space rendering for UI
- Content assets (sprites, maps) organized in `/Content` directory, loaded via MonoGame Content Pipeline

## Layers

**Core Systems Layer:**
- Purpose: Manages fundamental game state and input
- Location: `Core/`
- Contains: `InputManager`, `TimeManager`, `Camera`, `GameState`, `SaveManager`
- Depends on: MonoGame Framework
- Used by: Game1 (coordinator), all entity systems

**World/Map Layer:**
- Purpose: Represents the static game world, collision detection, tilemap rendering
- Location: `World/`
- Contains: `TileMap` (loads Tiled TMX files, manages layers, handles collision detection via polygon ray-casting)
- Depends on: `TiledCS` library for TMX parsing, MonoGame for rendering
- Used by: Player movement, camera bounds, grid-based farming

**Entity Layer:**
- Purpose: Manages individual game objects with position, animation, and behavior
- Location: `Player/`
- Contains: `PlayerEntity` (position, movement, animation, collision box), `PlayerStats` (stamina management)
- Depends on: Core systems (InputManager, TileMap for collision)
- Used by: Game1 for update/draw, ToolController for farming actions

**Farming System Layer:**
- Purpose: Grid-based farming mechanics (tilling, watering, crop growth, harvesting)
- Location: `Farming/`
- Contains: `GridManager` (cell state dictionary), `CropManager` (lifecycle), `CropInstance` (individual crop), `CropData` (static crop definition), `ToolController` (input dispatch)
- Depends on: PlayerEntity, GridManager, CropRegistry
- Used by: Game1 for day-advance events, input handling

**Data/Registry Layer:**
- Purpose: Central registry of static game data (crop definitions)
- Location: `Data/`
- Contains: `CropRegistry` (static dictionary of all crop types with growth sheets)
- Depends on: MonoGame GraphicsDevice for texture loading
- Used by: CropManager, GridManager during load/instantiation

**UI Layer:**
- Purpose: Non-diegetic screen-space overlay (HUD with stamina, time, controls)
- Location: `UI/`
- Contains: `HUD` (renders text, bars, control hints in screen coordinates)
- Depends on: TimeManager, PlayerStats, ToolController
- Used by: Game1 for final render pass

## Data Flow

**Input → Action:**
1. `InputManager.Update()` reads keyboard state, tracks edge detection
2. `Game1.Update()` calls `ToolController.Update(input)`
3. `ToolController` dispatches farming actions (`TryTill`, `TryWater`, `TryPlant`) or player movement
4. Actions consume stamina via `PlayerStats.TrySpendStamina()`
5. Grid/Crop state updates stored in `GridManager._cells` dictionary

**Day Advancement:**
1. `TimeManager.Update()` accumulates time, detects day boundary (GameTime >= 1.0)
2. `TimeManager.OnDayAdvanced` event fires
3. Subscribers execute in order: `CropManager.OnDayAdvanced()` → `GridManager.OnDayAdvanced()` → `PlayerStats.RestoreStamina()`
4. Crop growth evaluation: watered crops advance stage, ripe crops checked for wilt
5. Watering flags reset at end of day cycle
6. `SaveManager.Save()` persists state to JSON

**Rendering Pipeline:**
1. World space (camera-transformed): tiles, grid overlays, crops, player
2. Day/night overlay (screen-space): darkness computed from `TimeManager.GetLightIntensity()`
3. HUD (screen-space): time display, stamina bar, controls hint

**State Management:**
- `GameState` class holds all persistent data: day, season, player position, stamina, farm cells
- `SaveManager` handles JSON serialization to LocalApplicationData
- Farm cell state (`CellData`) maps Point(x,y) → {isTilled, isWatered, crop}
- `CropInstance` wraps `CropData` with mutable dayCount and wilted flag

## Key Abstractions

**TileMap (Polygon-based Collision):**
- Purpose: Represents static world geometry and tile-based interaction zones
- Examples: `World/TileMap.cs` — loads .TMX (Tiled) format, extracts collision objects as polygons
- Pattern: Circle-polygon intersection (smooth collision sliding), ray-casting point-in-polygon test

**Crop Growth as State Machine:**
- Purpose: Models crop lifecycle stages
- Examples: `CropData.GetStageIndex()`, `CropInstance.CheckWilt()`, `CropData.IsRipe()`
- Pattern: Day count maps to stages via `DaysPerStage` division; separate threshold for wilt

**Grid Cell Dictionary:**
- Purpose: Sparse storage of farming state (only store non-empty cells)
- Examples: `GridManager._cells` Dictionary<Point, CellData>
- Pattern: Fast O(1) lookup per tile, avoid allocating 1000s of empty cell objects

## Entry Points

**Main Application:**
- Location: `Program.cs`
- Triggers: Executable entry point
- Responsibilities: Instantiate and run `Game1`

**Game Coordinator:**
- Location: `Game1.cs` (inherits MonoGame.Game)
- Triggers: MonoGame event loop (Initialize → LoadContent → Update loop → Draw)
- Responsibilities: Instantiate all subsystems, coordinate input/update/draw, manage day-advance cascade, auto-save on sleep

**Day Advancement Event:**
- Location: `TimeManager.OnDayAdvanced` event
- Triggers: Time accumulation reaches 1.0 (one full game day)
- Responsibilities: Trigger cascading updates (crop growth, stamina restore, save)

## Error Handling

**Strategy:** Console logging with fallback returns

**Patterns:**
- Farming actions return `bool` success flag (e.g., `TryTill()`, `TryWater()`, `TryPlant()`)
- Missing textures or files logged to console, null checks before use
- Collision detection defaults to safe behavior (blocked movement is safer than phase-through)
- Save/load failures handled gracefully (new game if load fails)

## Cross-Cutting Concerns

**Logging:** Simple `Console.WriteLine()` with `[ModuleName]` prefix (e.g., `[GridManager]`, `[CropManager]`)

**Validation:** 
- `MathHelper.Clamp()` bounds check positions, stamina, stage indices
- Dictionary lookups use `TryGetValue()` pattern with null safety
- Tile coordinate bounds checked against map dimensions

**Persistence:**
- JSON serialization via `System.Text.Json`
- Version migration support in `SaveManager.MigrateIfNeeded()`
- Save path: `%LOCALAPPDATA%\StardewMedieval\savegame.json`

---

*Architecture analysis: 2026-04-10*
