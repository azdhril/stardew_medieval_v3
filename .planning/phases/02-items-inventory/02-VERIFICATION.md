---
phase: 02-items-inventory
type: verification
date: 2026-04-12
requirements_verified: [INV-01, INV-02, INV-03, INV-04, INV-05, FARM-01, FARM-02, FARM-03, HUD-02]
method: goal-backward (truths -> artifacts -> evidence)
---

# Phase 2 Verification: Items & Inventory

## Summary

All 9 requirements assigned to Phase 2 are **Satisfied**. Implementation landed across plans 02-01, 02-02, 02-03 (see corresponding SUMMARY files). This document provides goal-backward evidence mapping each requirement to concrete source artifacts. All cited file paths were verified to exist in the repository as of 2026-04-12.

| Requirement | Status | Primary Evidence |
|-------------|--------|------------------|
| INV-01 | Satisfied | src/Inventory/InventoryManager.cs (20-slot array, TryAdd stacking) |
| INV-02 | Satisfied | src/UI/HotbarRenderer.cs + src/Scenes/FarmScene.cs (number key handling) |
| INV-03 | Satisfied | src/Inventory/InventoryManager.cs equipment slots + src/Inventory/EquipmentData.cs |
| INV-04 | Satisfied | src/Data/Rarity.cs + src/UI/InventoryGridRenderer.cs (rarity tinting) |
| INV-05 | Satisfied | src/Entities/ItemDropEntity.cs (magnetism + pickup) |
| FARM-01 | Satisfied | src/Player/PlayerEntity.cs GetFootPosition + src/Farming/ToolController.cs |
| FARM-02 | Satisfied | src/Farming/GridManager.cs (real crop growth spritesheets) |
| FARM-03 | Satisfied | src/Farming/ToolController.cs TryHarvest -> ItemDropEntity -> InventoryManager |
| HUD-02 | Satisfied | src/Scenes/InventoryScene.cs + src/UI/InventoryGridRenderer.cs + I-key binding |

## Per-Requirement Evidence

### INV-01: Inventory grid com 20 slots, suporte a stacking de consumiveis

**Truth:** Player has a 20-slot inventory; consumable items stack within a single slot rather than occupying separate slots.

**Evidence:**
- Source: `src/Inventory/InventoryManager.cs` — `public const int SlotCount = 20;` backing array `_slots = new ItemStack?[SlotCount]`; `TryAdd(string itemId, int quantity)` uses two-pass algorithm (first pass merges into existing stacks with matching itemId, second pass fills empty slots)
- Source: `src/UI/InventoryGridRenderer.cs` — 5x4 grid rendering of 20 slots with quantity labels
- Source: `src/Data/ItemStack.cs` — stack quantity model consumed by InventoryManager
- Summary: `02-01-SUMMARY.md` (requirements-completed lists INV-01); `02-02-SUMMARY.md` (grid UI)

**Status:** Satisfied

### INV-02: Hotbar com 8 slots acessiveis por number keys (1-8)

**Truth:** 8-slot hotbar renders at screen bottom; pressing keys 1-8 selects the active hotbar slot which drives tool/weapon/consumable usage.

**Evidence:**
- Source: `src/UI/HotbarRenderer.cs` — draws 8 numbered slots at screen bottom with item icons and active-slot highlight
- Source: `src/Inventory/InventoryManager.cs` — `ActiveHotbarIndex` property, `GetActiveHotbarItem()`, hotbar shares slots 0-7 with the main inventory (no duplication)
- Source: `src/Scenes/FarmScene.cs` — number key handling routes to `InventoryManager.ActiveHotbarIndex`
- Source: `src/Core/InputManager.cs` — keyboard edge-detection for number keys
- Summary: `02-01-SUMMARY.md` (requirements-completed lists INV-02)

**Status:** Satisfied

### INV-03: Equipment slots separados (arma, armadura) que afetam combat stats

**Truth:** Player has distinct weapon/armor equipment slots separate from inventory, and equipping changes derived combat stats (attack/defense).

**Evidence:**
- Source: `src/Data/EquipSlot.cs` — enum defining equipment slot types (weapon, armor)
- Source: `src/Inventory/InventoryManager.cs` — `_equipment` dictionary keyed by `EquipSlot`, `GetAllEquipment()`, equip/unequip methods with swap-back behavior (old item returns to the slot the new one came from)
- Source: `src/Inventory/EquipmentData.cs` — `GetEquipmentStats(IReadOnlyDictionary<EquipSlot, string>)` returns `(float attack, float defense)` derived from equipped ItemDefinition stat bags
- Source: `src/UI/InventoryGridRenderer.cs` — `DrawEquipment()` renders equipment slots and combined ATK/DEF stat display
- Summary: `02-02-SUMMARY.md` (requirements-completed lists INV-03)

**Status:** Satisfied

### INV-04: Sistema de raridade de itens (common/uncommon/rare) com cores distintas e stat multipliers

**Truth:** Items carry a rarity tier; UI displays rarity via distinct colors; stat values can scale per rarity.

**Evidence:**
- Source: `src/Data/Rarity.cs` — `public enum Rarity { Common, Uncommon, Rare }`
- Source: `src/Data/ItemDefinition.cs` — Rarity field on each item definition (loaded from `src/Data/items.json`)
- Source: `src/UI/InventoryGridRenderer.cs` — rarity color tint drawn as semi-transparent overlay per slot (green 0.3 alpha for Uncommon, gold 0.3 alpha for Rare)
- Source: `src/Data/items.json` — test items include all three rarities (Cabbage=Common, Cosmic Carrot=Uncommon, Flame Blade=Rare)
- Summary: `02-02-SUMMARY.md` (requirements-completed lists INV-04)

**Status:** Satisfied

### INV-05: Itens dropados no chao com magnetismo -- puxa para o player a partir de certa distancia

**Truth:** Dropped items become world entities that accelerate toward the player when within a pickup/magnet radius, and are collected into the inventory when reaching the player.

**Evidence:**
- Source: `src/Entities/ItemDropEntity.cs` — `MagnetRange = 56f`, `PickupRange = 8f`, `MaxMagnetSpeed = 200f`; `PickupDelay = 0.5f` (post-spawn immunity so bounce animation plays before magnet engages); quadratic ease-in pull (`speed = Lerp(40f, MaxMagnetSpeed, t*t)`); `IsCollected` flag flipped when distance <= PickupRange, InventoryManager.TryAdd invoked on collection
- Source: `src/Scenes/FarmScene.cs` — ItemDropEntity list with reverse-iteration removal for collected entities; passes `GetFootPosition()` to magnetism update
- Summary: `02-03-SUMMARY.md` (requirements-completed lists INV-05)

**Status:** Satisfied

### FARM-01: Posicao correta do player ao arar, semear e regar

**Truth:** Till/water/sow actions target the tile in front of the player's feet, not the tile at sprite-center (head) level.

**Evidence:**
- Source: `src/Player/PlayerEntity.cs` — `GetFootPosition()` returns the CollisionBox center (feet), used by `GetTilePosition()` and `GetFacingTile()` so gameplay tile calculations are foot-based
- Source: `src/Farming/ToolController.cs` — `var tile = _player.GetFacingTile();` feeds `_grid.TryTill(tile, _player.Stats)` and `_grid.TryWater(tile, _player.Stats)`
- Summary: `02-03-SUMMARY.md` (requirements-completed lists FARM-01; documents head-to-foot fix in deviations)

**Status:** Satisfied

### FARM-02: Sprites adequados para colheita (substituir overlays coloridos por sprites reais)

**Truth:** Growing and harvested crops render using real pixel-art spritesheets (growth stages) rather than placeholder colored rectangles.

**Evidence:**
- Source: `src/Farming/GridManager.cs` — `DrawCrops()` renders crops using growth-stage spritesheet source rectangles from `CropData`
- Source: `src/Farming/CropData.cs` — `GetStageIndex()` and spritesheet source-rect resolution per growth stage
- Source: `src/Data/CropRegistry.cs` — loads crop growth spritesheet textures from `assets/Sprites/...`
- Summary: `02-03-SUMMARY.md` (requirements-completed lists FARM-02; note: crop sprites were already wired correctly pre-Phase-2 — verification confirms no colored-overlay fallback remains)

**Status:** Satisfied

### FARM-03: Farming integrado ao novo sistema de inventario

**Truth:** Harvesting a ripe crop produces an ItemDefinition-backed item that ends up in `InventoryManager` via the item-drop pickup loop.

**Evidence:**
- Source: `src/Farming/ToolController.cs` — `TryHarvest(Point tile)` spawns an ItemDropEntity at the crop tile center with the crop's yield ItemId (see `Spawned {yieldQty}x {yieldItem} as item drop` log)
- Source: `src/Entities/ItemDropEntity.cs` — on pickup calls `InventoryManager.TryAdd(itemId, quantity)` which either stacks onto an existing slot or fills the first empty slot
- Source: `src/Scenes/FarmScene.cs` — wires the spawn callback (`SpawnItemDrop` Action delegate) through to `ToolController` and owns the drop list
- Summary: `02-03-SUMMARY.md` (requirements-completed lists FARM-03)

**Status:** Satisfied

### HUD-02: Inventory UI abrivel (tecla I ou similar) mostrando grid + equipment slots

**Truth:** Pressing the I key toggles an overlay that displays both the inventory grid and equipment slots.

**Evidence:**
- Source: `src/Scenes/FarmScene.cs` — `if (input.IsKeyPressed(Keys.I))` pushes `new InventoryScene(Services, _inventory, _spriteAtlas, _hotbar)` (line 216-220)
- Source: `src/Scenes/InventoryScene.cs` — overlay scene with semi-transparent dark background; Tab key switches between Items grid and Equipment view; I or Escape closes via `SceneManager.PopImmediate()`
- Source: `src/UI/InventoryGridRenderer.cs` — renders both the 5x4 grid (line ~205 `Grid -> Equipment`) and the equipment layout (`DrawEquipment` at line 288) with combined ATK/DEF summary (line 316)
- Source: `src/Core/SceneManager.cs` — `PopImmediate()` enables instant overlay close without fade
- Summary: `02-02-SUMMARY.md` (requirements-completed lists HUD-02)

**Status:** Satisfied

## Conclusion

All 9 Phase 2 requirements are verified satisfied with concrete code evidence. Every cited path was confirmed to exist in the repository via Glob/Grep prior to writing this document. The Traceability table in `.planning/REQUIREMENTS.md` should flip these 9 rows from `Pending` to `Satisfied` (handled in 03.1-03-PLAN.md metadata-sync step).

Note on file-path corrections vs. the plan template: implementation organized inventory UI rendering into `src/UI/InventoryGridRenderer.cs` (which contains both grid and equipment rendering) and `src/Scenes/InventoryScene.cs` (the overlay), rather than a single `src/UI/InventoryOverlay.cs`. Equipment stat math lives in `src/Inventory/EquipmentData.cs`. The ItemDropEntity lives under `src/Entities/` (shared with other world entities), not `src/Inventory/`. `HotbarRenderer.cs` lives in `src/UI/` following the screen-space-renderer convention. All citations above reflect the real on-disk structure.
