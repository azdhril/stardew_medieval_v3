# Roadmap: Stardew Medieval

## Overview

Transform a working farming prototype into a complete farming+combat hybrid RPG. The build order follows dependency chains: architecture foundation enables items/inventory, which enables combat, which enables dungeon content. World structure and NPCs run in parallel once architecture exists. Progression and polish tie everything together into the core loop: farm, explore, fight, loot, evolve.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Architecture Foundation** - Refactor Game1 into SceneManager, create Entity base class, restructure GameState
- [ ] **Phase 2: Items & Inventory** - Unified item system, inventory grid, hotbar, equipment, farming polish
- [ ] **Phase 3: Combat** - Melee and magic attacks, HP system, enemy AI, boss encounter
- [ ] **Phase 4: World & NPCs** - Map transitions, village with shop and King, dialogue system, quest
- [ ] **Phase 5: Dungeon** - Multi-room dungeon with progression, treasure, and boss room
- [ ] **Phase 6: Progression & Polish** - XP/leveling, gold economy, graphical HUD, save system, death penalty

## Phase Details

### Phase 1: Architecture Foundation
**Goal**: The codebase supports multiple scenes, shared entity behavior, and extensible game state -- unblocking all feature work
**Depends on**: Nothing (first phase)
**Requirements**: ARCH-01, ARCH-02, ARCH-03, ARCH-04, ARCH-05
**Success Criteria** (what must be TRUE):
  1. Game boots into a FarmScene that behaves identically to the current game (no regression)
  2. Player can transition between at least two placeholder scenes (Farm and a test scene) with fade in/out
  3. A test entity (e.g. dummy NPC) can be spawned using the Entity base class with position, sprite, and collision
  4. GameState serializes and deserializes the new structure (inventory placeholder, scene, gold) without breaking existing saves
**Plans:** 4 plans

Plans:
- [x] 01-01-PLAN.md — Entity base class + ItemDefinition/ItemRegistry (Wave 1)
- [x] 01-02-PLAN.md — Scene abstract class, ServiceContainer, SceneManager with fade transitions (Wave 1)
- [x] 01-03-PLAN.md — GameState expansion, save migration, FarmScene extraction, Game1 refactor (Wave 2)
- [x] 01-04-PLAN.md — Gap closure: DummyNpc entity to prove Entity extensibility (Wave 1)

### Phase 2: Items & Inventory
**Goal**: Players can collect, manage, and equip items through a complete inventory system, and farming produces items that flow into inventory
**Depends on**: Phase 1
**Requirements**: INV-01, INV-02, INV-03, INV-04, INV-05, FARM-01, FARM-02, FARM-03, HUD-02
**Success Criteria** (what must be TRUE):
  1. Player can open inventory (I key), see 20-slot grid with items, and drag/click to rearrange
  2. Player can select hotbar slots (1-8 keys) to switch active tool/weapon/consumable
  3. Player can equip a weapon and armor into dedicated slots and see stat changes reflected
  4. Harvesting a crop removes it from the field and adds it to inventory (no more overlay-only feedback)
  5. Items on the ground are pulled toward the player magnetically when within pickup range
**Plans**: TBD
**UI hint**: yes

Plans:
- [ ] 02-01: TBD
- [ ] 02-02: TBD
- [ ] 02-03: TBD

### Phase 3: Combat
**Goal**: Players can fight enemies with melee and magic in real-time combat with visible feedback
**Depends on**: Phase 2
**Requirements**: CMB-01, CMB-02, CMB-03, CMB-04, CMB-05, CMB-06
**Success Criteria** (what must be TRUE):
  1. Player can swing a sword in the direction they face, hitting enemies with visible knockback
  2. Player can cast at least one ranged spell that travels as a projectile and damages enemies on hit
  3. Both player and enemies display HP bars that decrease when taking damage
  4. Three distinct enemy types exist (melee rusher, ranged caster, slow tank) with visibly different behaviors
  5. A boss enemy performs telegraphed attacks (visible wind-up) and drops unique loot on death
**Plans**: TBD

Plans:
- [ ] 03-01: TBD
- [ ] 03-02: TBD
- [ ] 03-03: TBD

### Phase 4: World & NPCs
**Goal**: Players can navigate between farm, village, and dungeon entrance, interact with NPCs, buy items, and receive the main quest
**Depends on**: Phase 1
**Requirements**: WLD-01, WLD-02, WLD-03, WLD-04, NPC-01, NPC-02, NPC-03, NPC-04, HUD-03, HUD-05
**Success Criteria** (what must be TRUE):
  1. Player can walk to the edge of the farm map and transition to the village (and back) with a fade-to-black effect
  2. Village map contains a castle and a shop, each accessible via door triggers
  3. Player can talk to the King NPC and receive the main quest ("clear the dungeon") via a styled dialogue box with portrait
  4. Player can open the shop UI, see items with prices, and buy/sell items using gold
  5. NPC dialogue changes based on quest state (no quest, quest active, quest complete)
**Plans**: TBD
**UI hint**: yes

Plans:
- [ ] 04-01: TBD
- [ ] 04-02: TBD
- [ ] 04-03: TBD

### Phase 5: Dungeon
**Goal**: Players can enter and progress through a complete dungeon experience from entrance to boss room
**Depends on**: Phase 3, Phase 4
**Requirements**: DNG-01, DNG-02, DNG-03, DNG-04
**Success Criteria** (what must be TRUE):
  1. Player can enter the dungeon from the village and navigate through 5-8 connected rooms
  2. Clearing all enemies in a room opens the door to the next room (visible gate/barrier change)
  3. Optional side rooms contain treasure chests with randomized loot
  4. The final room contains the boss; defeating it completes the dungeon objective
**Plans**: TBD

Plans:
- [ ] 05-01: TBD
- [ ] 05-02: TBD
- [ ] 05-03: TBD

### Phase 6: Progression & Polish
**Goal**: The full gameplay loop is connected end-to-end: farm, fight, level up, buy gear, complete the quest -- and it all saves
**Depends on**: Phase 5
**Requirements**: PRG-01, PRG-02, PRG-03, PRG-04, HUD-01, HUD-04, SAV-01, SAV-02
**Success Criteria** (what must be TRUE):
  1. Killing enemies grants XP; accumulating enough XP triggers a level-up with visible HP/damage/stamina increases
  2. Gold drops from enemies and is earned from selling crops; gold is spent at the shop
  3. Dying causes the player to lose 10% gold and possibly one item, then respawn at the farm
  4. The HUD displays HP bar, stamina bar, hotbar icons, clock/day, and active quest tracker -- all graphical (no raw text)
  5. Save/load preserves all state: inventory, equipment, XP, level, gold, quest progress, current scene
**Plans**: TBD
**UI hint**: yes

Plans:
- [ ] 06-01: TBD
- [ ] 06-02: TBD
- [ ] 06-03: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6
Note: Phase 4 depends only on Phase 1 (not Phase 2/3), so it could run after Phase 1 in theory, but sequential execution is simpler.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Architecture Foundation | 3/4 | Gap closure planned | - |
| 2. Items & Inventory | 0/3 | Not started | - |
| 3. Combat | 0/3 | Not started | - |
| 4. World & NPCs | 0/3 | Not started | - |
| 5. Dungeon | 0/3 | Not started | - |
| 6. Progression & Polish | 0/3 | Not started | - |
