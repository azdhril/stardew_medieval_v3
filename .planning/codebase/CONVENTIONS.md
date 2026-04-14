# Coding Conventions

**Analysis Date:** 2026-04-10

## Naming Patterns

**Files:**
- Class files use PascalCase: `GameState.cs`, `TimeManager.cs`, `PlayerEntity.cs`
- Organized by feature/domain in directories: `src/Core/`, `src/Farming/`, `src/Player/`, `src/UI/`, `src/World/`, `src/Data/`
- One public class per file as standard

**Functions/Methods:**
- Public methods use PascalCase: `Update()`, `LoadContent()`, `TrySpendStamina()`, `GetSourceRect()`
- Private methods use PascalCase: `AdvanceDay()`, `TryMove()`, `DrawFarmZoneHint()`
- Boolean-returning methods use `Try` or `Is` prefix: `TryTill()`, `TryWater()`, `IsRipe`, `IsWilted`
- Event handlers use `On` prefix: `OnDayAdvanced`, `OnHourPassed`, `OnStaminaChanged`, `OnDayAdvanced()`

**Variables:**
- Local variables use camelCase: `deltaTime`, `dayText`, `fill`, `cell`, `move`
- Private fields use underscore prefix + camelCase: `_graphics`, `_spriteBatch`, `_input`, `_player`, `_cropManager`, `_frameWidth`
- Constants use UPPER_SNAKE_CASE: `CURRENT_SAVE_VERSION`, `TileSize` (as static const)
- Public properties use PascalCase: `Position`, `Stats`, `FacingDirection`, `MaxStamina`, `CurrentStamina`, `DayNumber`

**Types:**
- Classes: PascalCase - `Game1`, `TimeManager`, `GridManager`, `CropData`
- Enums: PascalCase - `Direction` (with enum values: `Down`, `Left`, `Right`, `Up`)
- Interfaces: PascalCase with I prefix (not used yet, but follows .NET convention)
- Namespaces: PascalCase with domain hierarchy - `stardew_medieval_v3.Core`, `stardew_medieval_v3.Farming`, `stardew_medieval_v3.Player`

## Code Style

**Formatting:**
- No explicit formatter used (checked csproj: no Prettier, ESLint, or .editorconfig)
- Standard C# conventions: 4-space indentation
- Opening braces on same line (Allman style): `public class TimeManager { ... }`
- One statement per line

**Linting:**
- No linting configuration detected
- Nullable reference types enabled in `.csproj`: `<Nullable>enable</Nullable>`
- Type hints are comprehensive throughout

**Null-coalescing:**
- Uses null-coalescing operator: `string ampm = h < 12 ? "AM" : "PM"`
- Property initialization with null-forgiving operator: `private SpriteBatch _spriteBatch = null!`
- Conditional null access: `OnDayAdvanced?.Invoke()`

## Import Organization

**Order (by file analysis):**
1. System namespaces: `using System;`, `using System.Collections.Generic;`, `using System.IO;`
2. Framework/Third-party: `using Microsoft.Xna.Framework;`, `using Microsoft.Xna.Framework.Graphics;`
3. Project namespaces (relative to current module): `using stardew_medieval_v3.Core;`, `using stardew_medieval_v3.Farming;`

**Path Aliases:**
- No custom path aliases used
- Fully qualified namespaces throughout
- File organization structure serves as implicit "aliasing"

## Error Handling

**Patterns:**
- Try-catch for initialization/loading operations:
  ```csharp
  // From SaveManager.cs
  try
  {
      var json = File.ReadAllText(SavePath);
      var state = JsonSerializer.Deserialize<GameState>(json);
      if (state != null)
      {
          MigrateIfNeeded(state);
          Console.WriteLine($"[SaveManager] Loaded day {state.DayNumber}");
      }
      return state;
  }
  catch (Exception ex)
  {
      Console.WriteLine($"[SaveManager] Load failed: {ex.Message}");
      return null;
  }
  ```

- Boolean return for failure scenarios (graceful degradation):
  ```csharp
  // From GridManager.cs
  public bool TryTill(Point tile, PlayerStats stats)
  {
      if (!_map.IsFarmZone(tile.X, tile.Y))
      {
          Console.WriteLine("[GridManager] Cannot till: not in farm zone");
          return false;
      }
      ...
      return true;
  }
  ```

- Null-returns for optional results:
  ```csharp
  // From SaveManager.cs
  public static GameState? Load()
  {
      if (!File.Exists(SavePath))
          return null;
      ...
  }
  ```

- Check-then-act pattern:
  ```csharp
  // From CropManager.cs
  var cell = _grid.GetCell(tile);
  if (cell == null || !cell.IsTilled)
  {
      Console.WriteLine("[CropManager] Cannot plant: not tilled");
      return false;
  }
  ```

## Logging

**Framework:** Console output (no structured logging library)

**Patterns:**
- Use `Console.WriteLine()` for all output
- All logs prefixed with `[ModuleName]` to indicate source: `[Game]`, `[CropManager]`, `[SaveManager]`, `[GridManager]`, `[TileMap]`
- Include relevant context: coordinates, counts, state changes
- Informational: "Loaded day {dayNumber}", "Tilled ({x}, {y})"
- Error: "Load failed: {ex.Message}", "Cannot till: not in farm zone"
- Debug: State machine transitions, day advances, animations

**Examples:**
```csharp
Console.WriteLine("[Game] === Day {_time.DayNumber} ===");
Console.WriteLine($"[CropManager] Planted {cropData.Name} at ({tile.X}, {tile.Y})");
Console.WriteLine($"[SaveManager] Migrated save from v1 to v2");
Console.WriteLine($"[TileMap] Loaded {Width}x{Height} map, {_map.Layers.Length} layers");
```

## Comments

**When to Comment:**
- Every public class: summary comment above declaration
- Every public method: summary comment explaining purpose, parameters, return value
- Complex algorithms: inline comments (e.g., collision detection, light intensity calculation)
- Non-obvious logic: why, not what

**JSDoc/TSDoc (C# XML Comments):**
- Used consistently for public APIs
- Three-slash format: `/// <summary>Description</summary>`
- Parameter description not always present but encouraged

**Examples:**
```csharp
/// <summary>
/// Central in-game clock. Advances from 6AM to 2AM, then triggers day advance.
/// </summary>
public class TimeManager { ... }

/// <summary>
/// Returns light intensity multiplier based on time of day (0.0 to 1.0).
/// </summary>
public float GetLightIntensity() { ... }

/// <summary>
/// Ray-casting algorithm to test if a point is inside a polygon.
/// </summary>
private static bool PointInPolygon(Vector2 point, Vector2[] polygon) { ... }
```

## Function Design

**Size:** Methods are generally 15-50 lines. Longest method is `TileMap.DrawTileByGid()` (approx 30 lines) and `Game1.Draw()` (55 lines for rendering setup).

**Parameters:**
- Methods take 0-3 explicit parameters typically
- Dependency injection via constructor for stateful services: `CropManager(GridManager grid, List<CropData> availableCrops)`
- Out/ref parameters not used
- Optional parameters: nullable types used instead (`CropData?`)

**Return Values:**
- Boolean for operations that can fail: `bool TryTill()`, `bool TryWater()`, `bool AdvanceDay()`
- Nullable reference for optional lookups: `CropData?`, `GameState?`
- Void for event handlers and draw methods
- Tuples for multi-value returns: used in `GridManager.GetAllCells()` returns `IEnumerable<KeyValuePair<>>`

## Module Design

**Exports:**
- One public class per file (primary design)
- Supporting types in same file (e.g., `CellData` namespace-internal)
- Helper enums in files with their primary class: `Direction` enum in `PlayerEntity.cs`
- Public static classes for registries: `CropRegistry` is entirely static for central crop data

**Barrel Files:**
- No barrel files detected
- Imports are explicit and path-based

**Namespacing Strategy:**
- Domain-driven: `stardew_medieval_v3.Core`, `stardew_medieval_v3.Farming`, `stardew_medieval_v3.Player`, `stardew_medieval_v3.UI`, `stardew_medieval_v3.World`, `stardew_medieval_v3.Data`
- Each domain has single responsibility
- Services instantiated in `Game1` (composition root)

## Class Design Patterns

**Manager Pattern:**
- `TimeManager`: Owns time state, publishes events
- `GridManager`: Owns grid cell state, handles tilling/watering
- `CropManager`: Owns crop lifecycle logic
- `InputManager`: Owns input state, edge detection
- `ToolController`: Owns tool selection and actions

Pattern: Manager classes encapsulate domain state and expose public methods for interaction, private for internal logic.

**Data Objects:**
- `CropData`: Immutable definition (init-only properties)
- `CropInstance`: Mutable runtime state
- `GameState`: Serializable DTO
- `CellData`: Simple state container

Pattern: Separate data definitions (immutable, loaded once) from runtime instances (mutable, per-game).

**Singleton Pattern (Static):**
- `CropRegistry`: Static for global access to crop definitions
- `SaveManager`: Static for save/load operations
- Pattern: Global utilities that don't need instantiation

**Event-Driven Updates:**
- `TimeManager` publishes `OnHourPassed`, `OnDayAdvanced` events
- `PlayerStats` publishes `OnStaminaChanged` event
- Pattern: Decoupled state changes from observers

---

*Convention analysis: 2026-04-10*
