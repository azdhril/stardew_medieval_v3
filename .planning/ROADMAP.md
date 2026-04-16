# Roadmap: Stardew Medieval

## Overview

Transform a working farming prototype into a complete farming+combat hybrid RPG. The build order follows dependency chains: architecture foundation enables items/inventory, which enables combat, which enables dungeon content. World structure and NPCs run in parallel once architecture exists. Progression and polish tie everything together into the core loop: farm, explore, fight, loot, evolve.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Architecture Foundation** - Refactor Game1 into SceneManager, create Entity base class, restructure GameState
- [x] **Phase 2: Items & Inventory** - Unified item system, inventory grid, hotbar, equipment, farming polish
- [x] **Phase 3: Combat** - Melee and magic attacks, HP system, enemy AI, boss encounter
- [x] **Phase 3.1: Verification Backfill & Metadata Sync** - INSERTED. Produce 02/03-VERIFICATION.md, sync STATE/ROADMAP/REQUIREMENTS drift
- [ ] **Phase 4: World & NPCs** - Map transitions, village with shop and King, dialogue system, quest
- [x] **Phase 4.1: Shop UX & Save Gap Closure** - INSERTED. Save persistence blocker + shop mouse nav + scroll-wheel regression fix
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
**Plans:** 3 plans
**UI hint**: yes

Plans:
- [x] 02-01-PLAN.md — InventoryManager data layer, SpriteAtlas, HotbarRenderer, mouse input (Wave 1)
- [x] 02-02-PLAN.md — Inventory UI overlay with grid tab, equipment tab, and item interaction (Wave 2)
- [x] 02-03-PLAN.md — ItemDropEntity with magnetism, farming fixes, harvest-to-inventory integration (Wave 3)

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
**Plans:** 3 plans

Plans:
- [x] 03-01-PLAN.md — Player combat core: melee attack, fireball projectile, HP bars, i-frames (Wave 1)
- [x] 03-02-PLAN.md — Enemy types (Skeleton, Dark Mage, Golem), AI FSM, loot, FarmScene integration (Wave 2)
- [x] 03-03-PLAN.md — Skeleton King boss fight with telegraphed attacks, summon phases, unique loot (Wave 3)

### Phase 3.1: Verification Backfill & Metadata Sync (INSERTED)
**Goal**: Formally verify Phase 2 and Phase 3 deliverables and resync planning metadata that drifted during execution -- promotes 15 requirements from `partial` to `satisfied` and aligns STATE/ROADMAP/REQUIREMENTS with reality
**Depends on**: Phase 3
**Requirements**: INV-01, INV-02, INV-03, INV-04, INV-05, FARM-01, FARM-02, FARM-03, HUD-02, CMB-01, CMB-02, CMB-03, CMB-04, CMB-05, CMB-06 (verification only — implementation already landed in phases 2 and 3)
**Gap Closure**: Closes 15 `partial` requirements from `.planning/v1.0-MILESTONE-AUDIT.md`
**Success Criteria** (what must be TRUE):
  1. `.planning/phases/02-items-inventory/02-VERIFICATION.md` exists with goal-backward analysis covering INV-01..05, FARM-01..03, HUD-02
  2. `.planning/phases/03-combat/03-VERIFICATION.md` exists with goal-backward analysis covering CMB-01..06 (UAT 15/15 already passed)
  3. `STATE.md` reflects phases 1–3 complete (not "Phase 03 executing, 0%")
  4. ROADMAP Progress table marks Phase 2 and Phase 3 as `Complete` with correct plan counts
  5. REQUIREMENTS.md traceability flips ARCH-01..05, INV-01..05, FARM-01..03, HUD-02, CMB-01..06 from `Pending` to `Satisfied` and checkboxes from `[ ]` to `[x]`
**Plans:** 3 plans

Plans:
- [x] 03.1-01-PLAN.md — Write 02-VERIFICATION.md (Wave 1)
- [x] 03.1-02-PLAN.md — Write 03-VERIFICATION.md (Wave 1)
- [x] 03.1-03-PLAN.md — Sync STATE/ROADMAP/REQUIREMENTS metadata (Wave 2)

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
**Plans:** 7 plans
**UI hint**: yes

Plans:
- [x] 04-01-PLAN.md — Foundation contracts: MainQuest + NpcEntity + TriggerZone + Save v5 + Gold API (Wave 1)
- [x] 04-02-PLAN.md — World scenes & maps: VillageScene/CastleScene/ShopScene + TMX + FarmScene edge trigger (Wave 2)
- [x] 04-03-PLAN.md — Dialogue system + King NPC + HUD quest tracker + F9 debug key (Wave 3)
- [x] 04-04-PLAN.md — Shop UI: ShopPanel + Toast + Shopkeeper NPC + ShopStock (Wave 4)
- [x] 04-05-PLAN.md — Gap closure: save persistence (FarmScene first-entry guard) (Wave 5) [blocker]
- [x] 04-06-PLAN.md — Gap closure: shop mouse support (tabs + rows + action button + close) (Wave 5)
- [ ] 04-07-PLAN.md — Gap closure: shop scroll wheel + scrollbar + partial-stack sell (Wave 6)

### Phase 04.1: Shop UX & Save Gap Closure (INSERTED)

**Goal:** Close three outstanding gaps from 04-UAT: save persistence blocker (Gold, quest state, and inventory must round-trip through SaveManager); full mouse navigation of the shop (tabs, list hover/click, buttons, close); and the scroll-wheel regression in ShopPanel where follow-selection resets `_scrollOffset` every frame.
**Requirements**: HUD-05 (save round-trip), NPC-03/NPC-04 (shop UX completeness)
**Depends on:** Phase 4
**Plans:** 3/3 plans complete

Plans:
- [x] 04.1-01-PLAN.md -- ShopPanel scroll-wheel regression fix (gate follow-selection on selection-changed) (Wave 1)
- [x] 04.1-02-PLAN.md -- Shop mouse navigation hardening (tabs, rows, close X, click-outside, action button, qty widget) (Wave 2)
- [x] 04.1-03-PLAN.md -- GameStateSnapshot + SaveNow on shop close + F5 manual save (Wave 1)

### Phase 5: Dungeon
**Goal**: Players can enter and progress through a complete dungeon experience from entrance to boss room
**Depends on**: Phase 3, Phase 4
**Requirements**: DNG-01, DNG-02, DNG-03, DNG-04
**Success Criteria** (what must be TRUE):
  1. Player can enter the dungeon from the village and navigate through 5-8 connected rooms
  2. Clearing all enemies in a room opens the door to the next room (visible gate/barrier change)
  3. Optional side rooms contain treasure chests with randomized loot
  4. The final room contains the boss; defeating it completes the dungeon objective
**Plans:** 4 plans

Plans:
- [x] 05-01-PLAN.md — Dungeon infrastructure: test project bootstrap + DungeonState/Registry + CombatLoop extraction + EnemySpawner refactor + GameStateSnapshot fix + save v7→v8 (Wave 1)
- [x] 05-02-PLAN.md — 6 room TMXs + DungeonDoor entity + village entry trigger + idempotent chest seeding (Wave 2)
- [x] 05-03-PLAN.md — Boss room TMX + spawn guard + victory handler (quest complete + village return) + BossVictoryTests (Wave 3)
- [ ] 05-04-PLAN.md — Gap closure: fix pitch-black dungeon rendering in r2/r3/r3a/r4/r4a/boss + defensive Clear color (Wave 1)

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
| 1. Architecture Foundation | 4/4 | Complete | - |
| 2. Items & Inventory | 3/3 | Complete | - |
| 3. Combat | 3/3 | Complete | - |
| 3.1. Verification Backfill & Metadata Sync | 3/3 | Complete | - |
| 4. World & NPCs | 0/4 | Not started | - |
| 5. Dungeon | 3/4 | In progress (gap closure) | - |
| 6. Progression & Polish | 0/3 | Not started | - |
