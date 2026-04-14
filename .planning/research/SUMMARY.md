# Research Summary

**Project:** Stardew Medieval v3
**Domain:** Medieval Fantasy RPG/Farming Hybrid (MonoGame C#)
**Date:** 2026-04-10

## Executive Summary

Stardew Medieval v3 is a brownfield MonoGame project with working farming mechanics that needs to become a complete farming+combat hybrid RPG. The recommended approach is to extend the existing architecture with a SceneManager and Entity base class first, wrap existing farming logic into a FarmScene, then build Combat, Dungeon, Items, and Dialogue on that foundation — no external ECS, physics, or GUI framework needed at this scale.

Two non-negotiable architectural changes before any new feature work:
1. **SceneManager** — Game1 cannot coordinate farm+village+dungeon without it
2. **Unified ItemDefinition** — crops/tools/weapons must share one identity system

## Stack Recommendation

**Keep existing:** MonoGame 3.8 + TiledCS 3.3.3 + .NET 8 + System.Text.Json

**Optional addition:** MonoGame.Extended 4.x for animation utilities (verify version on NuGet)

**Do NOT add:** ECS frameworks, GUI libraries, physics engines, dialogue engines, DI containers. All are over-engineering at this scale (<100 entities).

**Hand-roll:** Entity system (inheritance + composition), A* pathfinding (PriorityQueue), pixel art UI (9-slice panels), combat hitboxes (AABB separate from world collision), enemy AI (state machines).

**Data-driven:** Extend existing JSON pattern (CropRegistry, SaveManager) to items, enemies, spells, loot tables, dialogue, quests. All in `assets/Data/*.json`.

## Table Stakes (v1 Must-Have)

16 features required for the core loop to feel complete:

1. Inventory system with grid slots (20 bag + 8 hotbar)
2. Hotbar with quick-select (number keys)
3. Melee combat with directional attack + knockback
4. At least 1 ranged magic attack (projectile)
5. Enemy AI (3 types: melee rusher, ranged caster, slow tank)
6. HP system for player and enemies
7. Death consequence (lose 50% gold, respawn at farm)
8. 1 dungeon with 5-8 rooms
9. Boss fight (telegraphed attacks, unique loot)
10. Loot drops from enemies (ground items)
11. Item rarity (common/uncommon/rare)
12. XP and leveling (10-15 levels)
13. Graphical HUD (HP, stamina, hotbar, time)
14. Map transitions (farm ↔ village ↔ dungeon)
15. Village with shop NPC + King NPC with quest
16. Save/load extended to all new systems

## Key Differentiator

**"False hero" narrative** — costs almost nothing in code, gives the game a unique voice. Lean into it in every NPC interaction.

## Critical Pitfalls

1. **Game1.cs god object** — must refactor into SceneManager before adding any systems
2. **No entity abstraction** — PlayerEntity is monolithic, need Entity base for enemies/NPCs
3. **Three disconnected item concepts** — CropData, ToolType, future items will collide; unify first
4. **Single-map assumption** — Camera, collision, save all assume one map; scene management required
5. **Combat vs world collision** — different problems, don't reuse TileMap polygons for hitboxes
6. **Fragile save system** — flat GameState with manual migration breaks as features grow

## Suggested Build Order

Architecture research and pitfall analysis converge on this phase ordering:

1. **Architecture Foundation** — SceneManager, Entity base, GameState restructuring (resolves 5/6 critical pitfalls)
2. **Items & Inventory** — Unified ItemDefinition, inventory grid, hotbar, equipment
3. **Combat Core** — Melee + magic, AABB hitboxes, HP system, 3 enemy types with AI
4. **World Structure** — Map transitions (farm/village/dungeon), village with shop + King
5. **Dungeon & Boss** — 5-8 rooms, boss encounter, loot tables, quest completion
6. **Progression & Polish** — XP/leveling, graphical HUD, save integration, balance tuning

## Confidence

| Area | Level | Reason |
|------|-------|--------|
| Stack | HIGH | Stable MonoGame ecosystem, direct codebase analysis |
| Features | MEDIUM-HIGH | Well-established genre conventions (Stardew, Rune Factory, Tibia) |
| Architecture | HIGH | Grounded in direct codebase analysis, standard MonoGame patterns |
| Pitfalls | HIGH | Derived from actual code patterns and their failure modes |

## Open Questions

- MonoGame.Extended exact 4.x version (verify on NuGet before installing)
- Pixel art assets available for enemies, dungeon tilesets, UI elements
- Boss AI phases — needs design input during implementation
- Equipment visual appearance on player sprite (overlays vs palette swaps)

---
*Research completed: 2026-04-10*
