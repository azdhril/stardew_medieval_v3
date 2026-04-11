---
phase: 01-architecture-foundation
reviewed: 2026-04-10T12:00:00Z
depth: standard
files_reviewed: 17
files_reviewed_list:
  - Core/Direction.cs
  - Core/Entity.cs
  - Core/Scene.cs
  - Core/ServiceContainer.cs
  - Core/SceneManager.cs
  - Core/GameState.cs
  - Core/SaveManager.cs
  - Data/ItemDefinition.cs
  - Data/ItemRegistry.cs
  - Data/ItemStack.cs
  - Data/ItemType.cs
  - Data/Rarity.cs
  - Data/items.json
  - Player/PlayerEntity.cs
  - Scenes/FarmScene.cs
  - Scenes/TestScene.cs
  - Game1.cs
findings:
  critical: 0
  warning: 5
  info: 4
  total: 9
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-04-10T12:00:00Z
**Depth:** standard
**Files Reviewed:** 17
**Status:** issues_found

## Summary

The Phase 01 architecture foundation is well-structured. The Scene/SceneManager stack pattern, Entity base class, ServiceContainer, and ItemRegistry are clean and follow project conventions. No critical issues were found. There are several warnings around missing error handling, a potential resource leak in SceneManager, and a method signature that hides the base class virtual. The info items are minor code quality observations.

## Warnings

### WR-01: PlayerEntity.Update hides base Entity.Update without override keyword

**File:** `Player/PlayerEntity.cs:29`
**Issue:** `PlayerEntity` declares `public void Update(float deltaTime, Vector2 input, TileMap map)` with a different signature than `Entity.Update(float deltaTime)`. This is fine as an overload, but the base `Entity.Update(float deltaTime)` virtual method is never overridden and remains callable. If anyone calls `((Entity)player).Update(dt)`, it will silently do nothing. This is a latent bug surface as more entity types are added and polymorphic Update calls are introduced.
**Fix:** Override the base method to delegate, or make the design intention explicit:
```csharp
public override void Update(float deltaTime)
{
    // No-op: PlayerEntity requires the overload with input and map.
    // This prevents silent no-op if called polymorphically.
    throw new InvalidOperationException("Use Update(float, Vector2, TileMap) for PlayerEntity.");
}
```

### WR-02: SceneManager._pixel Texture2D is never disposed

**File:** `Core/SceneManager.cs:32`
**Issue:** `SceneManager.Initialize` creates a `Texture2D` for the fade overlay (`_pixel`), but there is no `Dispose` method on `SceneManager`. When the game shuts down, this texture leaks. While minor for a single-instance object, it sets a bad precedent as more managed GPU resources are added.
**Fix:** Implement `IDisposable` on `SceneManager` and dispose the pixel texture:
```csharp
public class SceneManager : IDisposable
{
    // ... existing code ...
    public void Dispose()
    {
        _pixel?.Dispose();
    }
}
```
Then call `_sceneManager.Dispose()` in `Game1.UnloadContent()` or `Dispose(bool)`.

### WR-03: ItemRegistry.Initialize silently swallows all load errors

**File:** `Data/ItemRegistry.cs:39-41`
**Issue:** If `items.json` is missing, malformed, or has invalid enum values, the catch block logs to console but leaves `_items` empty. All subsequent `Get()` calls return null. Downstream code that assumes items exist (e.g., inventory, hotbar) will silently fail with null references. Given the project's "fail-secure" philosophy from CLAUDE.md, a load failure in a core registry should be more visible.
**Fix:** At minimum, track and expose whether initialization succeeded:
```csharp
public static bool IsInitialized { get; private set; }

public static void Initialize(string jsonPath = "Data/items.json")
{
    _items.Clear();
    IsInitialized = false;
    try
    {
        // ... existing load code ...
        IsInitialized = _items.Count > 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ItemRegistry] CRITICAL: Failed to load items: {ex.Message}");
    }
}
```

### WR-04: GameState.HotbarSlots initialization creates fixed-size list that can drift

**File:** `Core/GameState.cs:30`
**Issue:** `HotbarSlots` is initialized as `new List<string?>(new string?[8])` which creates an 8-element list. However, since it is a `List<T>` (not an array), nothing prevents serialization/deserialization from producing lists of different sizes. If a save file has fewer or more than 8 slots, the game could index out of bounds or behave unexpectedly. The migration code (SaveManager line 87) also uses `new List<string?>(new string?[8])` but does not enforce the size on existing saves that already have a `HotbarSlots` value.
**Fix:** Add validation after deserialization or use a fixed-size array:
```csharp
// Option A: Validate after load
while (state.HotbarSlots.Count < 8) state.HotbarSlots.Add(null);
if (state.HotbarSlots.Count > 8) state.HotbarSlots.RemoveRange(8, state.HotbarSlots.Count - 8);

// Option B: Use array instead
public string?[] HotbarSlots { get; set; } = new string?[8];
```

### WR-05: FarmScene auto-save in OnDayAdvanced does not persist new v3 fields

**File:** `Scenes/FarmScene.cs:212-223`
**Issue:** The `GameState` created during auto-save sets `DayNumber`, `Season`, `StaminaCurrent`, `PlayerX/Y`, `GameTime`, `FarmCells`, and `CurrentScene` -- but omits `Inventory`, `Gold`, `XP`, `Level`, `QuestState`, `WeaponId`, `ArmorId`, and `HotbarSlots`. These will serialize as their default values (empty list, 0, null), meaning every auto-save wipes out any inventory/gold/xp progress. This is a data loss bug once those systems are in use.
**Fix:** Populate all v3 fields when constructing the save state:
```csharp
var state = new GameState
{
    // ... existing fields ...
    Inventory = /* player inventory */,
    Gold = /* player gold */,
    XP = /* player xp */,
    Level = /* player level */,
    // etc.
};
```
Even if these systems are not yet active, the save should at minimum preserve the values loaded from a previous save rather than overwriting them with defaults.

## Info

### IN-01: ItemRegistry uses hardcoded relative path default

**File:** `Data/ItemRegistry.cs:19`
**Issue:** `Initialize` defaults to `"Data/items.json"` which is relative to the working directory. This works when running from the project root but may break if the working directory differs (e.g., running from `bin/Debug/`).
**Fix:** Consider resolving relative to the assembly location, or pass the path explicitly from the caller that knows the content root.

### IN-02: Entity.CollisionBox uses magic numbers for dimensions

**File:** `Core/Entity.cs:37`
**Issue:** `int w = 10, h = 6;` are magic numbers for the collision box size. These are not documented and make it hard to understand the intended collision area relative to the sprite.
**Fix:** Extract to named constants:
```csharp
protected virtual int CollisionWidth => 10;
protected virtual int CollisionHeight => 6;
```

### IN-03: SceneManager.Draw allocates array every frame

**File:** `Core/SceneManager.cs:136`
**Issue:** `_scenes.ToArray()` allocates a new array every frame to reverse Stack iteration order. This is a minor GC pressure concern but not a correctness issue.
**Fix:** Consider using a `List<Scene>` internally instead of `Stack<Scene>`, or cache the array when the stack changes.

### IN-04: Unused import in SceneManager

**File:** `Core/SceneManager.cs:3`
**Issue:** `using System.Linq;` is imported but no LINQ methods are used in this file.
**Fix:** Remove the unused import.

---

_Reviewed: 2026-04-10T12:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
