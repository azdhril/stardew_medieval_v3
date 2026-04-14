---
phase: 02-items-inventory
reviewed: 2026-04-11T12:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - src/Entities/ItemDropEntity.cs
  - src/Scenes/FarmScene.cs
  - src/Farming/ToolController.cs
  - src/Player/PlayerEntity.cs
  - src/Inventory/InventoryManager.cs
  - src/UI/HotbarRenderer.cs
findings:
  critical: 1
  warning: 4
  info: 2
  total: 7
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-04-11T12:00:00Z
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Reviewed the items and inventory system: item drops with magnet pickup, inventory management with reference-based hotbar, equipment slots, consumable quick-use, hotbar drag-and-drop rendering, and harvest-to-drop integration. The code is generally well-structured with clear separation of concerns. One critical bug was found where test initialization sets a consumable ref at index 1 while `ConsumableSlotCount` is 1 (making index 1 out of bounds and silently failing). Several warnings relate to save data integrity (slot positions lost on save) and potential NaN from `Vector2.Normalize` on zero-length vectors.

## Critical Issues

### CR-01: Test consumable ref set at out-of-bounds index (silent failure)

**File:** `src/Scenes/FarmScene.cs:119`
**Issue:** `_inventory.SetConsumableRef(1, "Bread")` is called, but `InventoryManager.ConsumableSlotCount` is 1, meaning only index 0 is valid. `SetConsumableRef` silently returns `false` for out-of-range indices (line 215 of InventoryManager.cs), so the "Bread" consumable ref is never assigned. This means the second consumable slot shown in the HotbarRenderer comment ("Q/E") does not actually exist -- the system was reduced to 1 slot but this call was not updated.
**Fix:**
```csharp
// Remove the out-of-bounds call at FarmScene.cs:119:
// _inventory.SetConsumableRef(1, "Bread");  // DELETE - index 1 is out of bounds

// If two consumable slots are intended, update InventoryManager.cs:23:
// public const int ConsumableSlotCount = 2;
```

## Warnings

### WR-01: SaveToState loses inventory slot positions (sparse-to-dense compression)

**File:** `src/Inventory/InventoryManager.cs:413-416`
**Issue:** `SaveToState` only adds non-null slots via `state.Inventory.Add(...)`, compressing the 20-slot array into a dense list. On `LoadFromState` (line 371), items are loaded sequentially from index 0. This means if the player has items at slots 0, 5, and 10, after save/load they will be at slots 0, 1, and 2. Hotbar/consumable refs use item IDs (not slot indices) so they still resolve, but the player's manual inventory arrangement is lost on every save/load cycle.
**Fix:**
```csharp
// SaveToState should preserve slot positions by saving all 20 slots:
state.Inventory.Clear();
for (int i = 0; i < SlotCount; i++)
{
    // Add null entries to preserve slot positions
    if (_slots[i] != null)
        state.Inventory.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity });
    else
        state.Inventory.Add(null!); // or use a sentinel/nullable list
}
```

### WR-02: Vector2.Normalize on zero-length vector produces NaN

**File:** `src/Entities/ItemDropEntity.cs:132`
**Issue:** When `dist` is between `PickupRange` (8) and `MagnetRange` (56), `Vector2.Normalize(playerPos - Position)` is called. If the player and item positions are exactly equal (dist ~0 but still > PickupRange due to floating point), `Normalize` on a zero vector returns `(NaN, NaN)`, which would corrupt the item's Position permanently. While unlikely in practice (dist would need to be > 8 and the vector near-zero simultaneously), the pattern is risky.
**Fix:**
```csharp
if (dist <= MagnetRange && dist > 0.01f)
{
    float t = 1f - (dist / MagnetRange);
    float speed = MathHelper.Lerp(40f, MaxMagnetSpeed, t * t);
    Vector2 direction = (playerPos - Position) / dist; // safe normalization using known distance
    Position += direction * speed * deltaTime;
}
```

### WR-03: HotbarRenderer uses direct Mouse.GetState() bypassing InputManager

**File:** `src/UI/HotbarRenderer.cs:85`
**Issue:** The `Update` method calls `Mouse.GetState().LeftButton` directly instead of going through `InputManager`. All other input in the codebase goes through `InputManager` (as seen in FarmScene, ToolController, PlayerEntity). Bypassing it means: (1) inconsistent input frame -- the mouse state read here may differ from what InputManager captured this frame, (2) if InputManager ever adds mouse input filtering or remapping, the hotbar will not respect it.
**Fix:**
```csharp
// Pass mouse button state through InputManager, or at minimum pass it as a parameter:
public void Update(Point mousePos, bool mouseDown, int screenWidth, int screenHeight)
```

### WR-04: HotbarRenderer doc comment says "2 consumable slots (Q/E)" but only 1 exists

**File:** `src/UI/HotbarRenderer.cs:13`
**Issue:** The class summary says "consumable slots (2, Q/E)" but `ConsumableSlotCount` is 1 and only "Q" is in the `consumableKeys` array (line 138). The comment is misleading and suggests the code was partially updated when reducing from 2 to 1 consumable slot.
**Fix:**
```csharp
/// Renders the reference-based hotbar (8 slots) and consumable slot (1, Q)
/// at the bottom of the screen.
```

## Info

### IN-01: Magic number 16 for tile size in harvest position calculation

**File:** `src/Farming/ToolController.cs:125`
**Issue:** `Vector2 worldPos = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8)` uses hardcoded 16 for tile size. The rest of the codebase uses `TileMap.TileSize` constant for this purpose.
**Fix:**
```csharp
Vector2 worldPos = new Vector2(
    tile.X * TileMap.TileSize + TileMap.TileSize / 2,
    tile.Y * TileMap.TileSize + TileMap.TileSize / 2);
```

### IN-02: Static Random instance in ItemDropEntity not thread-safe

**File:** `src/Entities/ItemDropEntity.cs:17`
**Issue:** `private static readonly Random _random = new()` is shared across all instances. While the game currently runs single-threaded (MonoGame game loop), if entity creation ever happens from multiple threads (e.g., async loading), `Random` is not thread-safe and can return corrupted values. Low risk given the MonoGame single-threaded model.
**Fix:** No action needed for current architecture. If multithreading is introduced later, use `Random.Shared` (.NET 6+) which is thread-safe.

---

_Reviewed: 2026-04-11T12:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
