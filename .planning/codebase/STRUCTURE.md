# Codebase Structure

**Analysis Date:** 2026-04-10

## Directory Layout

```
stardew_medieval_v3/
├── Program.cs              # Entry point (3 lines)
├── Game1.cs                # Main game coordinator (256 lines)
├── stardew_medieval_v3.csproj  # Project file, dependencies
│
├── src/Core/                   # Fundamental game systems
│   ├── GameState.cs        # Persistent game data model
│   ├── SaveManager.cs      # JSON save/load with versioning
│   ├── TimeManager.cs      # In-game clock, day/night cycle
│   ├── InputManager.cs     # Keyboard state with edge detection
│   └── Camera.cs           # 2D camera with smooth follow
│
├── src/World/                  # Static world representation
│   └── TileMap.cs          # Tiled TMX loader, collision, rendering
│
├── src/Player/                 # Player entity
│   ├── PlayerEntity.cs     # Position, movement, animation, collision
│   └── PlayerStats.cs      # Stamina tracking
│
├── src/Farming/                # Crop and grid management
│   ├── GridManager.cs      # Cell dictionary, tilling/watering
│   ├── CropManager.cs      # Planting, growth lifecycle
│   ├── CropInstance.cs     # Mutable crop state (day count, wilted)
│   ├── CropData.cs         # Static crop definition (stages, yield)
│   ├── CellData.cs         # Single farm cell state
│   └── ToolController.cs   # Tool selection, input dispatch
│
├── src/Data/                   # Game data registries
│   └── CropRegistry.cs     # Static crop definitions + texture loading
│
├── src/UI/                     # User interface
│   └── HUD.cs              # Stamina bar, clock, controls hint
│
├── assets/                # Game assets (sprites, maps)
│   ├── Maps/               # Tiled TMX files
│   │   └── test_farm.tmx   # Main game map
│   ├── Sprites/
│   │   ├── src/Player/         # player_spritesheet.png (4x4 grid)
│   │   ├── Crops/          # 20+ crop growth sheets
│   │   └── Farm/           # (directory exists, no files yet)
│   └── bin/                # Content pipeline output
│
├── bin/                    # Build output (Debug/net8.0/)
├── obj/                    # Build intermediate files
├── .vscode/                # Visual Studio Code settings
├── .config/                # .NET tooling config
│
└── Icon.ico, Icon.bmp      # Application icons
```

## Directory Purposes

**Program.cs:**
- Purpose: Application entry point
- Contains: Three lines instantiating and running `Game1`
- Key files: N/A (entry point)

**src/Core/:**
- Purpose: Fundamental game systems (time, input, camera, persistence)
- Contains: Stateful managers with event systems
- Key files: `TimeManager.cs` (day cycle), `SaveManager.cs` (JSON persistence), `InputManager.cs` (keyboard polling)

**src/World/:**
- Purpose: Static game world (map layout, collision, rendering)
- Contains: Single `TileMap` class handling Tiled TMX parsing and collision detection
- Key files: `TileMap.cs` (polygon-based collision, layer rendering, farm zone detection)

**src/Player/:**
- Purpose: Player character entity
- Contains: Movement, animation, stamina
- Key files: `PlayerEntity.cs` (position, movement with collision), `PlayerStats.cs` (stamina bar state)

**src/Farming/:**
- Purpose: Grid-based farming mechanics
- Contains: Cell management, crop lifecycle, tool input dispatch
- Key files: 
  - `GridManager.cs` (cell dictionary, tilling/watering, overlay rendering)
  - `CropManager.cs` (planting, day-tick growth evaluation)
  - `ToolController.cs` (tool selection, action dispatch to grid/crop/harvest)

**src/Data/:**
- Purpose: Game content registries
- Contains: Crop definitions with texture loading
- Key files: `CropRegistry.cs` (static dictionary of 18+ crop types)

**src/UI/:**
- Purpose: Screen-space user interface
- Contains: HUD rendering (stamina, time, controls)
- Key files: `HUD.cs` (text, bars, hints in screen coordinates)

**assets/:**
- Purpose: Game assets (sprites, maps)
- Contains: Tiled TMX files, PNG spritesheets
- Key files: `assets/Maps/test_farm.tmx` (main tilemap), `assets/Sprites/Crops/*.png` (growth stage sheets)

## Key File Locations

**Entry Points:**
- `Program.cs`: Creates Game1 instance and calls Run()
- `Game1.cs`: MonoGame Game subclass, coordinates all systems

**Configuration:**
- `stardew_medieval_v3.csproj`: Target framework (net8.0), dependencies (MonoGame, TiledCS)
- `.vscode/launch.json`: Debug configuration for VS Code

**Core Logic:**
- `Game1.cs`: Main update/draw loop, system initialization, day-advance cascade
- `src/Core/TimeManager.cs`: Game clock with hour/day advancement
- `src/Core/SaveManager.cs`: JSON serialization to LocalApplicationData
- `src/World/TileMap.cs`: Tiled TMX parsing, collision detection, layer rendering
- `src/Farming/GridManager.cs`: Cell state management, soil overlay rendering
- `src/Farming/CropManager.cs`: Crop growth, wilt checking, planting
- `src/Player/PlayerEntity.cs`: Movement with collision, animation, interaction

**Testing:**
- No test directory present (testing not yet implemented)

## Naming Conventions

**Files:**
- `PascalCase.cs` for all C# classes (e.g., `PlayerEntity.cs`, `TimeManager.cs`)
- Single class per file, matching filename exactly
- Namespace mirrors directory structure: `stardew_medieval_v3.Farming`, `stardew_medieval_v3.Core`

**Directories:**
- `PascalCase` for all directories (e.g., `src/Core/`, `src/Player/`, `src/Farming/`)
- Plural for content directories (`assets/Sprites/`, `assets/Maps/`)

**Classes:**
- `PascalCase` with descriptive suffix: `Manager`, `Entity`, `Data`, `Controller`, `Registry`
  - Example: `TimeManager`, `PlayerEntity`, `CropData`, `ToolController`, `CropRegistry`

**Methods:**
- `PascalCase` for public methods
- `camelCase` for private methods
- Try* prefix for methods that return bool (e.g., `TryTill()`, `TryWater()`, `TryPlant()`)
- On* prefix for event handler methods (e.g., `OnDayAdvanced()`)

**Properties:**
- `PascalCase` for public properties with get/set/init
- `_camelCase` for private fields (with underscore prefix)

**Enums:**
- `PascalCase` for enum type name: `ToolType`, `Direction`
- `PascalCase` for enum values: `ToolType.Hoe`, `Direction.Down`

## Where to Add New Code

**New Farming Mechanic (e.g., fertilizer, pesticide):**
- Primary code: `src/Farming/FertilizerManager.cs` (follow CropManager pattern)
- Integration: Update `Game1.cs` to instantiate manager and call Update/OnDayAdvanced
- Data: Add properties to `CellData.cs` and `FarmCellSaveData.cs`
- UI: Update `HUD.cs` if status display needed

**New Crop:**
- Add `CropRegistry.Register()` call in `src/Data/CropRegistry.cs`
- Create or reuse spritesheet in `assets/Sprites/Crops/`
- Define `CropData` instance with stages, yield, timing
- No code change needed if using existing mechanics

**New Player Ability (e.g., fishing, mining):**
- Primary code: `src/Player/PlayerAbility.cs` or `src/Core/AbilityManager.cs`
- Input dispatch: Add key binding to `src/Core/InputManager.cs`
- Integration: Call from `Game1.Update()` or `ToolController.Update()`
- Stats: Extend `PlayerStats.cs` with new skill/level properties

**New UI Element (e.g., inventory screen):**
- Implementation: `src/UI/InventoryScreen.cs`
- Integration: Call from `Game1.Draw()` in separate `_spriteBatch.Begin/End` block
- Input: Handle in `Game1.Update()` or delegate to UI class

**Utilities:**
- Shared helpers: `src/Farming/FarmingUtils.cs` or `src/Core/GameUtils.cs`
- Extension methods: Add to matching namespace file or create `Extensions.cs`
- No external lib dependencies in core (game logic only)

## Special Directories

**assets/:**
- Purpose: Game assets (sprites, maps, fonts)
- Generated: No (hand-authored)
- Committed: Yes (essential for gameplay)
- Build process: MonoGame Content Pipeline copies to output directory via `CopyToOutputDirectory="PreserveNewest"`

**bin/ obj/:**
- Purpose: Build artifacts and intermediate files
- Generated: Yes (automatic during build)
- Committed: No (`.gitignore` excludes)

**.vscode/ .config/:**
- Purpose: IDE and tooling configuration
- Generated: No (hand-authored)
- Committed: Yes (shared development environment)

---

*Structure analysis: 2026-04-10*
