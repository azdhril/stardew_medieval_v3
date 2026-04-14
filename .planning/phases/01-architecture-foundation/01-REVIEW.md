---
phase: 01-architecture-foundation
reviewed: 2026-04-10T12:00:00Z
depth: standard
files_reviewed: 18
files_reviewed_list:
  - src/Core/Direction.cs
  - src/Core/Entity.cs
  - src/Core/GameState.cs
  - src/Core/SaveManager.cs
  - src/Core/Scene.cs
  - src/Core/SceneManager.cs
  - src/Core/ServiceContainer.cs
  - src/Data/ItemDefinition.cs
  - src/Data/ItemRegistry.cs
  - src/Data/ItemStack.cs
  - src/Data/ItemType.cs
  - src/Data/Rarity.cs
  - src/Data/items.json
  - src/Entities/DummyNpc.cs
  - Game1.cs
  - src/Player/PlayerEntity.cs
  - src/Scenes/FarmScene.cs
  - src/Scenes/TestScene.cs
findings:
  critical: 0
  warning: 6
  info: 4
  total: 10
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-04-10T12:00:00Z
**Depth:** standard
**Files Reviewed:** 18
**Status:** issues_found

## Summary

The Phase 01 architecture foundation is well-structured. The Scene/SceneManager stack pattern, Entity base class, ServiceContainer, and data layer (ItemRegistry, ItemDefinition, ItemStack) are clean and follow project conventions consistently. XML doc comments are thorough, naming follows the established PascalCase/underscore-prefix patterns, and error handling in I/O paths (SaveManager, ItemRegistry) uses the check-then-act and try-catch patterns documented in conventions.

No critical (security/crash) issues were found. Six warnings identify bugs that will manifest as the game grows: a method hiding issue in PlayerEntity, auto-save data loss for v3 fields, silent item registry failures, a GPU texture leak, mutable registry exposure, and hotbar size drift. Four info items note minor code quality improvements.

## Warnings

### WR-01: PlayerEntity.Update hides base Entity.Update without override keyword

**File:** `src/Player/PlayerEntity.cs:29`
**Issue:** `PlayerEntity` declares `public void Update(float deltaTime, Vector2 input, TileMap map)` with a different signature than the virtual `Entity.Update(float deltaTime)`. The base class method is hidden (not overridden). If a `PlayerEntity` is ever referenced as `Entity` and `Update(dt)` is called polymorphically, the player-specific logic (movement, animation, collision) will be silently skipped. The compiler should emit CS0108 about this. As more entity types are added and update loops iterate over `List<Entity>`, this will become a real bug.
**Fix:** Override the base method to make the design intention explicit:
```csharp
// Option A: Override base and delegate
public override void Update(float deltaTime)
{
    // PlayerEntity requires explicit input/map context.
    // This override prevents silent no-op in polymorphic calls.
}

// Option B: If hiding is intentional, use 'new' keyword
public new void Update(float deltaTime, Vector2 input, TileMap map) { ... }
```

### WR-02: FarmScene auto-save omits all v3 GameState fields (data loss)

**File:** `src/Scenes/FarmScene.cs:212-223`
**Issue:** The `GameState` constructed during `OnDayAdvanced` auto-save sets v2 fields (`DayNumber`, `Season`, `StaminaCurrent`, `PlayerX/Y`, `GameTime`, `FarmCells`) and `CurrentScene`, but omits `Inventory`, `Gold`, `XP`, `Level`, `QuestState`, `WeaponId`, `ArmorId`, and `HotbarSlots`. These serialize as their default values (empty list, 0, null). Once inventory/gold/xp systems are active, every auto-save will wipe that progress to defaults. This is a latent data loss bug.
**Fix:** Populate all v3 fields when constructing the save state. Even if these systems are not yet active, preserve values loaded from a previous save:
```csharp
var state = new GameState
{
    // ... existing v2 fields ...
    CurrentScene = "Farm",
    Inventory = _currentInventory,  // or new() if not yet tracked
    Gold = _currentGold,
    XP = _currentXP,
    Level = _currentLevel,
    // etc.
};
```

### WR-03: ItemRegistry.Initialize silently swallows all load errors

**File:** `src/Data/ItemRegistry.cs:38-41`
**Issue:** If `items.json` is missing, malformed, or has invalid enum values, the catch block logs to console but leaves `_items` empty. All subsequent `Get()` calls return null, and `GetByType()` returns empty lists. Downstream code that assumes items exist will silently fail with null references. Per the project conventions, load failures should be visible.
**Fix:** Track and expose load state so callers can react:
```csharp
public static bool IsInitialized { get; private set; }

// In Initialize, after successful load:
IsInitialized = _items.Count > 0;
Console.WriteLine($"[ItemRegistry] Loaded {_items.Count} items");
```

### WR-04: FarmScene leaks player spritesheet texture on unload

**File:** `src/Scenes/FarmScene.cs:50-51`
**Issue:** `LoadTexture` creates a `Texture2D` from a file stream and passes it to `_player.LoadContent()`, but the texture reference is not stored for disposal. When `FarmScene.UnloadContent()` runs, only `_pixel` is disposed. The player spritesheet leaks GPU memory across scene transitions.
**Fix:** Store the texture and dispose it in `UnloadContent`:
```csharp
private Texture2D _playerTexture = null!;

// In LoadContent:
_playerTexture = LoadTexture(device, "...");
_player.LoadContent(_playerTexture);

// In UnloadContent:
_playerTexture?.Dispose();
```

### WR-05: ItemRegistry.All exposes mutable internal dictionary

**File:** `src/Data/ItemRegistry.cs:66-67`
**Issue:** `public static Dictionary<string, ItemDefinition> All => _items;` returns a direct reference to the internal mutable dictionary. Any caller can add, remove, or modify entries at runtime, breaking the registry's role as a read-only lookup. Per CLAUDE.md, game data registries should not be modifiable by runtime code.
**Fix:** Return a read-only view:
```csharp
public static IReadOnlyDictionary<string, ItemDefinition> All => _items;
```

### WR-06: GameState.HotbarSlots size can drift after deserialization

**File:** `src/Core/GameState.cs:30`
**Issue:** `HotbarSlots` is `List<string?>` initialized with 8 elements, but nothing prevents JSON deserialization from producing lists of different sizes. A corrupted or hand-edited save with fewer or more than 8 slots could cause index-out-of-bounds errors in hotbar UI code. The migration code (SaveManager line 87) only null-coalesces but does not validate size.
**Fix:** Validate after deserialization in `MigrateIfNeeded`:
```csharp
// Ensure exactly 8 slots
while (state.HotbarSlots.Count < 8) state.HotbarSlots.Add(null);
if (state.HotbarSlots.Count > 8) state.HotbarSlots.RemoveRange(8, state.HotbarSlots.Count - 8);
```

## Info

### IN-01: ItemRegistry uses hardcoded relative path default

**File:** `src/Data/ItemRegistry.cs:19`
**Issue:** `Initialize` defaults to `"src/Data/items.json"` which is relative to the working directory. This works when running from the project root but may break if the working directory differs (e.g., running from `bin/Debug/net8.0/`).
**Fix:** Consider resolving relative to the assembly location, or always pass the path explicitly from the caller.

### IN-02: Entity.CollisionBox uses magic numbers

**File:** `src/Core/Entity.cs:37`
**Issue:** `int w = 10, h = 6;` are undocumented magic numbers for the default collision box dimensions.
**Fix:** Extract to named virtual properties for clarity and easy overriding:
```csharp
protected virtual int DefaultCollisionWidth => 10;
protected virtual int DefaultCollisionHeight => 6;
```

### IN-03: SceneManager.Draw allocates array every frame

**File:** `src/Core/SceneManager.cs:136`
**Issue:** `_scenes.ToArray()` allocates a new array every frame to reverse the Stack iteration order. This creates minor GC pressure each frame.
**Fix:** Consider caching the array and rebuilding only when the stack changes, or using a `List<Scene>` internally.

### IN-04: Unused using directive in SceneManager

**File:** `src/Core/SceneManager.cs:3`
**Issue:** `using System.Linq;` is imported but no LINQ methods are used in this file.
**Fix:** Remove the unused import.

---

_Reviewed: 2026-04-10T12:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
