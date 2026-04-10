---
phase: 01-architecture-foundation
plan: 01
subsystem: core-types
tags: [entity, item-system, architecture, foundation]
dependency_graph:
  requires: []
  provides: [Entity-base-class, Direction-enum, ItemDefinition-model, ItemRegistry, items-json]
  affects: [PlayerEntity, future-enemies, future-NPCs, future-inventory]
tech_stack:
  added: []
  patterns: [abstract-base-class, static-registry, JSON-data-loading, enum-extraction]
key_files:
  created:
    - Core/Entity.cs
    - Core/Direction.cs
    - Data/ItemType.cs
    - Data/Rarity.cs
    - Data/ItemDefinition.cs
    - Data/ItemStack.cs
    - Data/ItemRegistry.cs
    - Data/items.json
  modified:
    - Player/PlayerEntity.cs
    - stardew_medieval_v3.csproj
decisions:
  - "Entity base class uses protected properties for animation fields so subclasses access them directly"
  - "ItemRegistry uses JsonStringEnumConverter for readable JSON enum values"
  - "Cosmic Carrot and Prickly Pear marked as Uncommon rarity; all others Common"
  - "CropRegistry left untouched -- coexists with ItemRegistry until Phase 2 integration"
metrics:
  duration: 3m
  completed: "2026-04-10T19:08:38Z"
  tasks_completed: 2
  tasks_total: 2
  files_created: 8
  files_modified: 2
---

# Phase 01 Plan 01: Foundational Type System Summary

Entity base class with HP/animation/collision and unified ItemDefinition/ItemRegistry loading 45 items from JSON.

## What Was Done

### Task 1: Entity Base Class and Direction Extraction

- Created `Core/Direction.cs` extracting the `Direction` enum from the bottom of `PlayerEntity.cs` into its own file under the `stardew_medieval_v3.Core` namespace
- Created `Core/Entity.cs` abstract base class with: Position, Velocity, FacingDirection, HP/MaxHP/IsAlive, SpriteSheet/FrameWidth/FrameHeight/FrameIndex/AnimationTimer/FrameTime (protected), virtual CollisionBox, virtual Update/Draw
- Refactored `Player/PlayerEntity.cs` to inherit from `Entity`, removing all duplicated fields. PlayerEntity keeps its own `Update(float, Vector2, TileMap)` signature (does not override Entity.Update) and overrides `Draw`

### Task 2: ItemDefinition Model and ItemRegistry

- Created `Data/ItemType.cs` enum: Crop, Seed, Tool, Weapon, Armor, Consumable, Loot
- Created `Data/Rarity.cs` enum: Common, Uncommon, Rare
- Created `Data/ItemDefinition.cs` with Id, Name, Type, Rarity, StackLimit, SpriteId, Stats dictionary
- Created `Data/ItemStack.cs` for inventory slot references
- Created `Data/ItemRegistry.cs` static registry with `Initialize(jsonPath)`, `Get(id)`, `GetByType(type)`, `All` property
- Created `Data/items.json` with 45 items: 21 crop seed/crop pairs (42 items) + 3 tools (Hoe, Watering Can, Scythe)
- Updated `stardew_medieval_v3.csproj` to copy items.json to output directory

## Verification

- `dotnet build` passes with zero errors and zero warnings
- All 45 items counted in items.json
- PlayerEntity compiles correctly inheriting from Entity
- No regression: CropRegistry untouched, existing game behavior preserved

## Deviations from Plan

None -- plan executed exactly as written.

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | a7d67a3 | feat(01-01): create Entity base class and extract Direction enum |
| 2 | 94edace | feat(01-01): add ItemDefinition model, enums, ItemRegistry, and items.json |

## Self-Check: PASSED

All created files exist, both commits verified in git log.
