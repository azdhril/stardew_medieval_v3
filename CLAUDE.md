<!-- GSD:project-start source:PROJECT.md -->
## Project

**Stardew Medieval**

Um jogo top-down medieval fantasy que combina simulação de fazenda (estilo Stardew Valley) com exploração de dungeons e combate RPG (estilo Tibia). O jogador é um falso herói que precisa cuidar de sua fazenda, evoluir como guerreiro, e atender missões do Rei — tudo isso em um mundo relaxante mas desafiador. Feito em C#/MonoGame para facilitar desenvolvimento assistido por IA.

**Core Value:** O loop central deve ser satisfatório: cuidar da fazenda → explorar/lutar → voltar com loot → evoluir → desbloquear mais conteúdo. Se esse ciclo não for divertido, nada mais importa.

### Constraints

- **Engine**: MonoGame 3.8 DesktopGL — sem trocar engine
- **Linguagem**: C# 12 / .NET 8.0 — manter stack existente
- **Mapas**: Tiled (.tmx/.tsx) via TiledCS — manter pipeline de mapas
- **Resolução**: 960x540 base — manter configuração atual
- **Assets**: Pixel art medieval — estilo visual consistente
- **Plataforma v1**: Windows PC only
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# 12 - Entire application codebase, all game systems and logic
- MGCB (MonoGame Content Builder) - Content pipeline for sprites, fonts, and assets
- XML - Tiled map format (.tmx, .tsx files)
- JSON - Game state serialization and save files
## Runtime
- .NET 8.0 - Target framework with forward compatibility enabled via RollForward=Major
- Windows (DesktopGL) - DesktopGL profile for MonoGame
- NuGet - .NET package management
- Lockfile: Implicit (managed by .NET project file)
## Frameworks
- MonoGame.Framework.DesktopGL 3.8.* - Game engine for 2D graphics, input handling, and game loop
- MonoGame.Content.Builder.Task 3.8.* - Content pipeline build integration
- dotnet-mgcb 3.8.4.1 - Command-line tool for building game content (mgcb command)
## Key Dependencies
- TiledCS 3.3.3 - Tiled map format parsing and loading
## Configuration
- Manifest: `app.manifest` - Windows application manifest for DPI awareness and OS compatibility
- `stardew_medieval_v3.csproj` - MSBuild project file
- `Content/Content.mgcb` - MonoGame content project file
## Platform Requirements
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- MonoGame 3.8 templates and tools
- .NET 8.0 Runtime
- Windows 7+ (with DPI awareness support)
- GPU capable of OpenGL support (via DesktopGL)
- 960x540 minimum window resolution
- MonoGame content must be pre-built via MGCB
- Tiled map files (.tmx) with referenced tilesets (.tsx)
- Sprite PNG files for crop growth stages and player animation
- SpriteFont files (.spritefont XML) for text rendering
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- Class files use PascalCase: `GameState.cs`, `TimeManager.cs`, `PlayerEntity.cs`
- Organized by feature/domain in directories: `Core/`, `Farming/`, `Player/`, `UI/`, `World/`, `Data/`
- One public class per file as standard
- Public methods use PascalCase: `Update()`, `LoadContent()`, `TrySpendStamina()`, `GetSourceRect()`
- Private methods use PascalCase: `AdvanceDay()`, `TryMove()`, `DrawFarmZoneHint()`
- Boolean-returning methods use `Try` or `Is` prefix: `TryTill()`, `TryWater()`, `IsRipe`, `IsWilted`
- Event handlers use `On` prefix: `OnDayAdvanced`, `OnHourPassed`, `OnStaminaChanged`, `OnDayAdvanced()`
- Local variables use camelCase: `deltaTime`, `dayText`, `fill`, `cell`, `move`
- Private fields use underscore prefix + camelCase: `_graphics`, `_spriteBatch`, `_input`, `_player`, `_cropManager`, `_frameWidth`
- Constants use UPPER_SNAKE_CASE: `CURRENT_SAVE_VERSION`, `TileSize` (as static const)
- Public properties use PascalCase: `Position`, `Stats`, `FacingDirection`, `MaxStamina`, `CurrentStamina`, `DayNumber`
- Classes: PascalCase - `Game1`, `TimeManager`, `GridManager`, `CropData`
- Enums: PascalCase - `Direction` (with enum values: `Down`, `Left`, `Right`, `Up`)
- Interfaces: PascalCase with I prefix (not used yet, but follows .NET convention)
- Namespaces: PascalCase with domain hierarchy - `stardew_medieval_v3.Core`, `stardew_medieval_v3.Farming`, `stardew_medieval_v3.Player`
## Code Style
- No explicit formatter used (checked csproj: no Prettier, ESLint, or .editorconfig)
- Standard C# conventions: 4-space indentation
- Opening braces on same line (Allman style): `public class TimeManager { ... }`
- One statement per line
- No linting configuration detected
- Nullable reference types enabled in `.csproj`: `<Nullable>enable</Nullable>`
- Type hints are comprehensive throughout
- Uses null-coalescing operator: `string ampm = h < 12 ? "AM" : "PM"`
- Property initialization with null-forgiving operator: `private SpriteBatch _spriteBatch = null!`
- Conditional null access: `OnDayAdvanced?.Invoke()`
## Import Organization
- No custom path aliases used
- Fully qualified namespaces throughout
- File organization structure serves as implicit "aliasing"
## Error Handling
- Try-catch for initialization/loading operations:
- Boolean return for failure scenarios (graceful degradation):
- Null-returns for optional results:
- Check-then-act pattern:
## Logging
- Use `Console.WriteLine()` for all output
- All logs prefixed with `[ModuleName]` to indicate source: `[Game]`, `[CropManager]`, `[SaveManager]`, `[GridManager]`, `[TileMap]`
- Include relevant context: coordinates, counts, state changes
- Informational: "Loaded day {dayNumber}", "Tilled ({x}, {y})"
- Error: "Load failed: {ex.Message}", "Cannot till: not in farm zone"
- Debug: State machine transitions, day advances, animations
## Comments
- Every public class: summary comment above declaration
- Every public method: summary comment explaining purpose, parameters, return value
- Complex algorithms: inline comments (e.g., collision detection, light intensity calculation)
- Non-obvious logic: why, not what
- Used consistently for public APIs
- Three-slash format: `/// <summary>Description</summary>`
- Parameter description not always present but encouraged
## Function Design
- Methods take 0-3 explicit parameters typically
- Dependency injection via constructor for stateful services: `CropManager(GridManager grid, List<CropData> availableCrops)`
- Out/ref parameters not used
- Optional parameters: nullable types used instead (`CropData?`)
- Boolean for operations that can fail: `bool TryTill()`, `bool TryWater()`, `bool AdvanceDay()`
- Nullable reference for optional lookups: `CropData?`, `GameState?`
- Void for event handlers and draw methods
- Tuples for multi-value returns: used in `GridManager.GetAllCells()` returns `IEnumerable<KeyValuePair<>>`
## Module Design
- One public class per file (primary design)
- Supporting types in same file (e.g., `CellData` namespace-internal)
- Helper enums in files with their primary class: `Direction` enum in `PlayerEntity.cs`
- Public static classes for registries: `CropRegistry` is entirely static for central crop data
- No barrel files detected
- Imports are explicit and path-based
- Domain-driven: `stardew_medieval_v3.Core`, `stardew_medieval_v3.Farming`, `stardew_medieval_v3.Player`, `stardew_medieval_v3.UI`, `stardew_medieval_v3.World`, `stardew_medieval_v3.Data`
- Each domain has single responsibility
- Services instantiated in `Game1` (composition root)
## Class Design Patterns
- `TimeManager`: Owns time state, publishes events
- `GridManager`: Owns grid cell state, handles tilling/watering
- `CropManager`: Owns crop lifecycle logic
- `InputManager`: Owns input state, edge detection
- `ToolController`: Owns tool selection and actions
- `CropData`: Immutable definition (init-only properties)
- `CropInstance`: Mutable runtime state
- `GameState`: Serializable DTO
- `CellData`: Simple state container
- `CropRegistry`: Static for global access to crop definitions
- `SaveManager`: Static for save/load operations
- Pattern: Global utilities that don't need instantiation
- `TimeManager` publishes `OnHourPassed`, `OnDayAdvanced` events
- `PlayerStats` publishes `OnStaminaChanged` event
- Pattern: Decoupled state changes from observers
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Monolithic single-file entry point (`Game1.cs`) coordinates all subsystems
- Event-driven communication (day advancement triggers cascading updates)
- Immediate-mode rendering with camera transformation for world space, separate screen-space rendering for UI
- Content assets (sprites, maps) organized in `/Content` directory, loaded via MonoGame Content Pipeline
## Layers
- Purpose: Manages fundamental game state and input
- Location: `Core/`
- Contains: `InputManager`, `TimeManager`, `Camera`, `GameState`, `SaveManager`
- Depends on: MonoGame Framework
- Used by: Game1 (coordinator), all entity systems
- Purpose: Represents the static game world, collision detection, tilemap rendering
- Location: `World/`
- Contains: `TileMap` (loads Tiled TMX files, manages layers, handles collision detection via polygon ray-casting)
- Depends on: `TiledCS` library for TMX parsing, MonoGame for rendering
- Used by: Player movement, camera bounds, grid-based farming
- Purpose: Manages individual game objects with position, animation, and behavior
- Location: `Player/`
- Contains: `PlayerEntity` (position, movement, animation, collision box), `PlayerStats` (stamina management)
- Depends on: Core systems (InputManager, TileMap for collision)
- Used by: Game1 for update/draw, ToolController for farming actions
- Purpose: Grid-based farming mechanics (tilling, watering, crop growth, harvesting)
- Location: `Farming/`
- Contains: `GridManager` (cell state dictionary), `CropManager` (lifecycle), `CropInstance` (individual crop), `CropData` (static crop definition), `ToolController` (input dispatch)
- Depends on: PlayerEntity, GridManager, CropRegistry
- Used by: Game1 for day-advance events, input handling
- Purpose: Central registry of static game data (crop definitions)
- Location: `Data/`
- Contains: `CropRegistry` (static dictionary of all crop types with growth sheets)
- Depends on: MonoGame GraphicsDevice for texture loading
- Used by: CropManager, GridManager during load/instantiation
- Purpose: Non-diegetic screen-space overlay (HUD with stamina, time, controls)
- Location: `UI/`
- Contains: `HUD` (renders text, bars, control hints in screen coordinates)
- Depends on: TimeManager, PlayerStats, ToolController
- Used by: Game1 for final render pass
## Data Flow
- `GameState` class holds all persistent data: day, season, player position, stamina, farm cells
- `SaveManager` handles JSON serialization to LocalApplicationData
- Farm cell state (`CellData`) maps Point(x,y) → {isTilled, isWatered, crop}
- `CropInstance` wraps `CropData` with mutable dayCount and wilted flag
## Key Abstractions
- Purpose: Represents static world geometry and tile-based interaction zones
- Examples: `World/TileMap.cs` — loads .TMX (Tiled) format, extracts collision objects as polygons
- Pattern: Circle-polygon intersection (smooth collision sliding), ray-casting point-in-polygon test
- Purpose: Models crop lifecycle stages
- Examples: `CropData.GetStageIndex()`, `CropInstance.CheckWilt()`, `CropData.IsRipe()`
- Pattern: Day count maps to stages via `DaysPerStage` division; separate threshold for wilt
- Purpose: Sparse storage of farming state (only store non-empty cells)
- Examples: `GridManager._cells` Dictionary<Point, CellData>
- Pattern: Fast O(1) lookup per tile, avoid allocating 1000s of empty cell objects
## Entry Points
- Location: `Program.cs`
- Triggers: Executable entry point
- Responsibilities: Instantiate and run `Game1`
- Location: `Game1.cs` (inherits MonoGame.Game)
- Triggers: MonoGame event loop (Initialize → LoadContent → Update loop → Draw)
- Responsibilities: Instantiate all subsystems, coordinate input/update/draw, manage day-advance cascade, auto-save on sleep
- Location: `TimeManager.OnDayAdvanced` event
- Triggers: Time accumulation reaches 1.0 (one full game day)
- Responsibilities: Trigger cascading updates (crop growth, stamina restore, save)
## Error Handling
- Farming actions return `bool` success flag (e.g., `TryTill()`, `TryWater()`, `TryPlant()`)
- Missing textures or files logged to console, null checks before use
- Collision detection defaults to safe behavior (blocked movement is safer than phase-through)
- Save/load failures handled gracefully (new game if load fails)
## Cross-Cutting Concerns
- `MathHelper.Clamp()` bounds check positions, stamina, stage indices
- Dictionary lookups use `TryGetValue()` pattern with null safety
- Tile coordinate bounds checked against map dimensions
- JSON serialization via `System.Text.Json`
- Version migration support in `SaveManager.MigrateIfNeeded()`
- Save path: `%LOCALAPPDATA%\StardewMedieval\savegame.json`
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
