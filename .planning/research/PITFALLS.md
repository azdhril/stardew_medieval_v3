# Domain Pitfalls

**Domain:** MonoGame medieval farming RPG with combat, dungeons, inventory (brownfield)
**Researched:** 2026-04-10
**Confidence:** HIGH (based on direct codebase analysis + established MonoGame/gamedev patterns)

## Critical Pitfalls

Mistakes that cause rewrites or major issues.

### Pitfall 1: God Object Game1.cs Becomes Unmanageable

**What goes wrong:** Game1.cs is already the sole coordinator for 10+ subsystem fields. Adding combat, enemies, dungeons, inventory, NPCs, dialogue, map transitions, and loot will push it to 500+ lines of wiring code. Every new system adds more fields, more Update() calls, more Draw() ordering concerns, and more event subscriptions. Developers start adding "just one more thing" to Game1 and suddenly it's untestable, merge-conflict-prone, and impossible to reason about.

**Why it happens:** MonoGame's architecture naturally funnels everything through the Game class. The current pattern (direct field references, manual Update/Draw ordering) has no abstraction for managing systems independently.

**Consequences:**
- Adding combat requires touching Game1 in 5+ places (field, Initialize, LoadContent, Update, Draw, OnDayAdvanced)
- Draw ordering bugs (player drawn behind enemies, HUD behind overlay)
- Impossible to unit test system interactions without instantiating the entire game
- Parallel development on different features causes constant merge conflicts

**Prevention:**
- Introduce a lightweight GameSystem/Scene abstraction BEFORE adding new systems
- Each system (Combat, Dungeon, Inventory, NPC) registers itself with Update/Draw priorities
- Game1 becomes a thin shell: iterate registered systems, nothing else
- Use a service locator or dependency injection container so systems find each other without Game1 wiring

**Detection:** Game1.cs exceeds 300 lines, or adding a new system requires editing more than 2 methods in Game1.

**Phase:** Must be addressed in the FIRST phase (architecture refactor), before any new system is built.

---

### Pitfall 2: No Entity/Component Architecture Before Adding Enemies

**What goes wrong:** PlayerEntity is a monolithic class with position, movement, animation, collision, and stats baked in. When enemies are added, developers copy-paste PlayerEntity patterns, creating EnemyEntity with duplicated movement/animation/collision logic. Then NPCs need the same but without combat. Then projectiles need position + collision but no animation. The result is a tangled inheritance hierarchy or massive code duplication.

**Why it happens:** The farming-only codebase needed exactly one entity (player). The "just add another entity class" approach works for 1-2 types but collapses at 5+.

**Consequences:**
- Duplicate collision code across Player, Enemy, NPC, Projectile
- Bug fixes must be applied in N places
- Different entity types drift in behavior (enemy collision works differently than player)
- Adding a new entity type (boss, pet, thrown item) requires substantial boilerplate

**Prevention:**
- Introduce a minimal component system: Position, Sprite, Collider, Health, AI as composable pieces
- Does NOT need to be a full ECS framework (overkill for this scale). Simple composition with interfaces is sufficient
- Extract collision logic from PlayerEntity into a shared Collider component
- Extract animation into a shared SpriteAnimator component
- Player, Enemy, NPC become assemblies of components, not inheritance trees

**Detection:** You're copy-pasting movement or collision code from PlayerEntity into a new class.

**Phase:** Must happen before src/Combat/Enemy systems. Part of the architecture refactor phase.

---

### Pitfall 3: Inventory System Built Without Unified Item Model

**What goes wrong:** Crops are harvested with `Console.WriteLine` and disappear. Seeds are a ToolType enum. When inventory is added, developers create an Item class for inventory but crops remain a separate CropData system, tools remain an enum, equipment becomes yet another model. Three different "item" representations exist with no shared identity. Converting between them requires mapping code everywhere.

**Why it happens:** The current codebase has three disconnected concepts that should be one: CropData (what you grow), ToolType enum (what you use), and the future Item (what you carry). They were designed independently because farming didn't need inventory.

**Consequences:**
- "Wheat seed" exists as CropData.Name AND as an inventory Item AND as ToolType.Seeds -- which is canonical?
- Harvesting creates an item that has no connection to the crop it came from
- Equipment (sword, armor) is a completely separate system from tools (hoe, watering can)
- Shop system needs to convert between all representations
- Save format must store items in 3 different ways

**Prevention:**
- Design a single `ItemDefinition` registry FIRST, before building inventory UI
- All game objects that can be held, dropped, sold, or used are Items: crops, seeds, tools, weapons, potions
- CropData references an ItemDefinition for its yield; ToolType becomes an Item with a "tool" behavior
- Item has composable behaviors: Plantable, Equippable, Consumable, Sellable
- This is the foundational data model -- get it right before building inventory slots

**Detection:** You have more than one way to identify "the same thing" in code (string name, enum value, registry key).

**Phase:** Must be designed in the inventory/item system phase, BEFORE combat items or shop.

---

### Pitfall 4: Map Transitions Without Scene/State Management

**What goes wrong:** The game currently has one map (test_farm.tmx). Adding farm-to-village-to-dungeon transitions seems simple ("just load a different TMX") but breaks everything: player position must be preserved per-map, enemies must be spawned/despawned, farming state is map-specific, camera bounds change, collision polygons change, and the save system stores only one map's state.

**Why it happens:** Single-map games have implicit assumptions baked in everywhere. Camera.Bounds is set once. GridManager assumes one farm. TileMap is one instance. GameState has one PlayerX/PlayerY.

**Consequences:**
- Loading a new map without unloading the old one leaks textures (MonoGame doesn't garbage-collect GPU resources)
- Player appears at wrong position when transitioning back
- Farm crops stop growing while in dungeon (no background simulation)
- Save file has no concept of "which map am I on"
- Enemy/NPC state is lost on transition

**Prevention:**
- Design a Scene/Map manager that handles: load, unload, transition, state preservation
- Each map has its own persistent state (farm cells, enemy positions, NPC states)
- GameState gets a `CurrentMapId` field and per-map state dictionaries
- Use "transition zones" (Tiled object layer) that define entry/exit points between maps
- Explicitly Dispose() textures and map data when leaving a scene

**Detection:** You're about to add a second TileMap instance or a `LoadMap(string)` method without a scene manager.

**Phase:** Must be built as infrastructure before the dungeon phase. Part of map/world system refactor.

---

### Pitfall 5: Combat Feels Wrong Because of Collision System Mismatch

**What goes wrong:** The current collision system uses circle-polygon intersection designed for smooth wall sliding. Combat needs hitbox-vs-hurtbox collision (rectangle or circle overlap for sword swings, projectile hits). Developers try to reuse the TileMap collision system for combat, resulting in attacks that miss when they look like hits, or attacks that hit through walls.

**Why it happens:** Wall collision and combat collision are fundamentally different problems. Wall collision is continuous (slide along surfaces). Combat collision is discrete (did this attack frame overlap an enemy this tick?). Mixing them creates confusion.

**Consequences:**
- Sword swing hitbox clips through walls and hits enemies in the next room
- Player can't attack enemies standing adjacent because collision repels them
- Projectile collision doesn't account for entity movement between frames (tunneling)
- No concept of "attack frames" vs "recovery frames" for melee feel

**Prevention:**
- Keep TileMap collision for world/wall interactions only
- Create a separate combat collision system: simple AABB or circle overlap checks
- Combat hitboxes are temporary (exist only during attack frames), separate from entity collision boxes
- Add wall-line-of-sight check for attacks (raycast from attacker to target, blocked by wall = no hit)
- Define attack as a state machine: windup -> active (hitbox active) -> recovery -> idle

**Detection:** You're passing TileMap into a combat method, or enemies can be hit through walls.

**Phase:** Combat system phase. Design hitbox system independently from world collision.

---

### Pitfall 6: Save System Breaks Silently When New Systems Are Added

**What goes wrong:** GameState is a flat class with fields for every piece of data. Adding inventory (list of items), combat stats (level, XP, HP), dungeon progress (which rooms cleared), NPC relationships, and quest state means GameState becomes a god object. Worse, every new field breaks backward compatibility -- old saves missing the field either crash or silently default to wrong values.

**Why it happens:** The current save system serializes GameState directly to JSON. Adding `public List<InventoryItem> Inventory { get; set; } = new()` works for new games but existing saves don't have that field. The migration system only handles v1->v2.

**Consequences:**
- Players lose save progress when updating the game
- Testing requires manually crafting save files for every combination of features
- GameState becomes 50+ fields with no organization
- Migration chain becomes a maintenance nightmare

**Prevention:**
- Restructure GameState into nested sub-states: `PlayerState`, `FarmState`, `InventoryState`, `QuestState`, `DungeonState`
- Each sub-state has its own version and migration logic
- Use JSON serializer options that ignore missing fields (with sensible defaults)
- Build a proper migration chain: v2->v3->v4, each step transforms only its changed fields
- Add round-trip save/load tests for every new feature

**Detection:** GameState has more than 15 fields, or you're adding a migration that touches more than 3 fields.

**Phase:** Should be refactored during the infrastructure/architecture phase, before new persistent systems are added.

## Moderate Pitfalls

### Pitfall 7: Dungeon Generation Without Spatial Partitioning

**What goes wrong:** Dungeons with 50+ enemies per room cause frame drops because collision detection is O(n*m) -- every enemy checks against every other enemy and the player every frame. The current TileMap collision already iterates ALL polygons per check.

**Prevention:**
- Implement spatial hashing or a simple grid partition for entity collision queries
- Only check entities within 2-3 tiles of each other
- Dungeon rooms should cap at 10-15 active enemies; spawn in waves instead of all at once
- Profile early: if Update() exceeds 2ms with 20 enemies, the architecture needs work

**Phase:** Must be in place before dungeon content is authored. Part of the combat/entity infrastructure.

---

### Pitfall 8: Tool/Weapon System Collision With Existing Keybinds

**What goes wrong:** Current tools use hardcoded keys (H=Hoe, G=WateringCan, R=Seeds, F=Hands, E=interact, P=sleep, Tab=cycle). Adding combat means: attack key, block key, spell key, potion key, inventory toggle, equipment screen. Keys quickly run out and conflict. Players can't remember 15 different keys.

**Prevention:**
- Move to a hotbar-based system where number keys 1-9 select the active item (tool OR weapon)
- E remains interact, left-click becomes "use active item" (farm tool or attack)
- Separate action contexts: farming mode vs combat mode, or unified "use" that depends on equipped item
- Implement input rebinding before adding more actions

**Detection:** You're assigning a new letter key to a new action.

**Phase:** Should be addressed when building the inventory/hotbar system, before combat keybinds.

---

### Pitfall 9: AI Pathfinding in Tiled Maps Without Navigation Data

**What goes wrong:** Enemies need to chase the player but the map only has collision polygons (for wall sliding), not a navigation graph. Developers implement naive "move toward player" AI that gets stuck on walls, oscillates in corners, or walks through obstacles.

**Prevention:**
- Generate a tile-based walkability grid from the TileMap collision data at load time
- Use A* on the tile grid for pathfinding (well-understood, performant for grid maps)
- Add a "last known position" mechanic so enemies path to where the player was, not teleport-chase
- Keep patrol/idle behavior simple: random walk within a radius, with wall checks

**Phase:** Required for the creature/enemy AI phase. Build pathfinding infrastructure before enemy behaviors.

---

### Pitfall 10: Dialogue/Quest System Coupled to Specific NPCs

**What goes wrong:** The King NPC quest is hardcoded: talk to King, get quest, kill dungeon boss, return. When adding more quests or NPCs, each one requires new code paths. Dialogue trees become switch statements. Quest state is scattered across multiple classes.

**Prevention:**
- Data-drive dialogue from JSON/YAML (NPC says X, player chooses Y, state changes to Z)
- Quest system tracks conditions and objectives generically: "kill N of type X", "deliver item Y to NPC Z", "reach location W"
- NPCs reference dialogue trees and quest triggers by ID, not by hardcoded logic
- Keep quest state in a central QuestManager, not in individual NPC objects

**Phase:** NPC/dialogue phase. Design the data format before implementing the first quest.

---

### Pitfall 11: Mixing Screen-Space and World-Space UI

**What goes wrong:** The game already has a HUD in screen space. Adding floating health bars over enemies, damage numbers, interaction prompts ("Press E") requires world-space UI that moves with the camera. Developers either put world-space UI in the wrong SpriteBatch.Begin() call (ignoring camera transform) or calculate screen positions manually and get it wrong at different zoom levels.

**Prevention:**
- Establish clear render passes: (1) world tiles, (2) world entities, (3) world UI (health bars, damage numbers -- camera-transformed), (4) screen overlay (day/night), (5) screen UI (HUD, menus, inventory)
- Create a WorldUI helper that handles camera projection for floating text/bars
- Always test UI at zoom levels other than default (the current Zoom=3 will hide bugs)

**Phase:** HUD/UI refactor phase, before combat adds floating health bars.

---

### Pitfall 12: Texture Atlas Neglect Causes Content Pipeline Pain

**What goes wrong:** The codebase loads individual PNG files via `Texture2D.FromStream()`, bypassing MonoGame's Content Pipeline. Each crop has its own spritesheet. Adding enemies, NPCs, items, UI elements, and effects means hundreds of individual texture loads. GPU draw call count skyrockets because each texture swap is a draw call break.

**Prevention:**
- Pack sprites into texture atlases (tools: TexturePacker, or build-time MonoGame Content Pipeline)
- Group by render pass: all world entities in one atlas, all UI in another
- This doesn't need to happen immediately but should be planned before 100+ sprites exist
- At minimum, batch draws by texture (all enemies using the same sheet drawn together)

**Phase:** Can be deferred to optimization phase, but atlas structure should be designed early to avoid rework.

## Minor Pitfalls

### Pitfall 13: Float Accumulation in Game Timers

**What goes wrong:** TimeManager uses float for GameTime, accumulated each frame. Combat timers (attack cooldown, buff duration, enemy spawn delay) added as more floats will compound precision issues over long sessions.

**Prevention:** Use `double` for time accumulation. For short-duration timers (attack cooldown), float is fine. For anything measuring "total game time elapsed," use double or integer frame counting.

**Phase:** Quick fix during any refactor that touches TimeManager.

---

### Pitfall 14: Hardcoded Crop Data Pattern Repeated for Items/Enemies

**What goes wrong:** CropRegistry.Initialize() hardcodes 23 crop definitions in C# code. If the same pattern is used for items, enemies, spells, and NPCs, adding content requires recompilation for every change.

**Prevention:** Move ALL game data to JSON/YAML files loaded at runtime. Crop definitions, item stats, enemy stats, spell definitions, dialogue trees, quest definitions -- all data-driven. Create a generic DataRegistry<T> pattern used by all content types.

**Phase:** Should be one of the first infrastructure changes. Massive time savings for content iteration.

---

### Pitfall 15: No Debug/Cheat Tools for Testing

**What goes wrong:** Testing combat balance, dungeon progression, and inventory requires playing through the game normally. Developers waste hours reaching test scenarios.

**Prevention:**
- Add a debug console or cheat keys (F1-F12) for: spawn enemy, give item, set level, teleport to map, toggle god mode, show collision boxes, show pathfinding
- Make debug tools a first-class system, not an afterthought
- Gate behind a `#if DEBUG` preprocessor directive

**Phase:** Build alongside the first testable system (combat). Pays for itself immediately.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Architecture Refactor | Pitfall 1 (God Game1), Pitfall 2 (No ECS), Pitfall 6 (Save bloat) | Refactor BEFORE adding features. This phase is the foundation. |
| Item/Inventory System | Pitfall 3 (No unified item model), Pitfall 8 (Keybind collision) | Design ItemDefinition registry first. Hotbar replaces letter keys. |
| Combat System | Pitfall 5 (Collision mismatch), Pitfall 7 (No spatial partition) | Separate combat hitboxes from world collision. Profile with 20 enemies early. |
| Dungeon/Maps | Pitfall 4 (Map transitions), Pitfall 9 (No pathfinding) | Scene manager + tile walkability grid before content authoring. |
| NPCs/Quests | Pitfall 10 (Hardcoded quests) | Data-driven dialogue and quest conditions. Generic objective system. |
| src/UI/HUD | Pitfall 11 (Screen vs world space UI) | Clear render pass ordering. WorldUI helper for floating elements. |
| Content Pipeline | Pitfall 12 (Texture explosion), Pitfall 14 (Hardcoded data) | Atlas planning early. JSON data loading for all content types. |

## Sources

- Direct codebase analysis: Game1.cs, PlayerEntity.cs, ToolController.cs, GameState.cs, GridManager.cs
- CONCERNS.md analysis document (2025-02-27)
- ARCHITECTURE.md analysis document (2026-04-10)
- Established MonoGame game architecture patterns (training data, MEDIUM confidence)
- Common 2D RPG development patterns from Stardew Valley and similar games (training data, MEDIUM confidence)

---

*Pitfalls audit: 2026-04-10*
