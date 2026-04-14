# Architecture Patterns

**Domain:** 2D Medieval Fantasy RPG/Farming (MonoGame)
**Researched:** 2026-04-10
**Confidence:** HIGH (based on established MonoGame/XNA patterns and direct codebase analysis)

## Current State Assessment

The existing codebase uses a **layered coordinator pattern** where `Game1.cs` instantiates all systems and orchestrates Update/Draw. This works well for farming-only scope but will buckle under combat + enemies + dungeons + inventory + NPCs + map transitions unless we introduce two key architectural additions:

1. **Scene/Screen Manager** -- to handle map transitions and game states (playing, inventory open, dialogue, paused)
2. **Entity system with shared interfaces** -- to unify player, enemies, NPCs, and projectiles under common Update/Draw/collision contracts

The goal is to **extend the existing architecture, not rewrite it**. Game1 remains the coordinator but delegates to scenes. Existing systems (farming, time, save) remain untouched.

## Recommended Architecture

```
Game1.cs (coordinator)
  |
  +-- src/Core/ (unchanged: InputManager, TimeManager, Camera, SaveManager, GameState)
  |
  +-- SceneManager (NEW)
  |     |
  |     +-- FarmScene        (existing farming + player, wraps current Game1 logic)
  |     +-- VillageScene     (NPCs, shops, castle)
  |     +-- DungeonScene     (rooms, enemies, combat, loot)
  |     +-- BossRoomScene    (boss fight, special arena)
  |
  +-- src/Entities/ (NEW: shared base for all game objects)
  |     +-- Entity (base: Position, Velocity, CollisionBox, Update, Draw)
  |     +-- PlayerEntity (REFACTORED: extends Entity, adds combat stats)
  |     +-- Enemy (extends Entity, adds AI + health + loot table)
  |     +-- NPC (extends Entity, adds dialogue tree)
  |     +-- Projectile (extends Entity: spells, thrown items)
  |
  +-- src/Combat/ (NEW)
  |     +-- CombatManager (hit detection, damage calc, cooldowns)
  |     +-- AttackData (weapon stats, hitbox shapes, cooldowns)
  |     +-- DamageCalculator (attack vs defense, level scaling)
  |
  +-- AI/ (NEW)
  |     +-- AIBehavior (base: Update returns movement/action intent)
  |     +-- PatrolBehavior (wander within zone)
  |     +-- ChaseBehavior (pursue target within range)
  |     +-- AttackBehavior (attack when in range)
  |     +-- BossAI (phase-based behavior)
  |
  +-- src/Inventory/ (NEW)
  |     +-- InventoryManager (slot array, add/remove/stack)
  |     +-- Item (base: ID, name, icon, stackable, type)
  |     +-- Equipment (extends Item: weapon/armor with stats)
  |     +-- LootTable (weighted random drops per enemy type)
  |
  +-- Dialogue/ (NEW)
  |     +-- DialogueManager (drives conversation flow)
  |     +-- DialogueTree (nodes with text + choices + conditions)
  |     +-- DialogueData (loaded from JSON)
  |
  +-- Dungeon/ (NEW)
  |     +-- DungeonManager (room graph, current room, transitions)
  |     +-- Room (enemy spawns, loot, exits, Tiled map reference)
  |     +-- DungeonGenerator (optional: procedural room selection)
  |
  +-- Progression/ (NEW)
  |     +-- LevelSystem (XP thresholds, stat gains per level)
  |     +-- PlayerProgression (current XP, level, allocated stats)
  |
  +-- src/UI/ (EXTENDED)
  |     +-- HUD (existing, extended with health bar + XP + active weapon)
  |     +-- InventoryScreen (grid UI, drag equip)
  |     +-- DialogueBox (text rendering, choice buttons)
  |     +-- ShopScreen (buy/sell interface)
  |     +-- BossHealthBar (screen-top boss HP)
  |
  +-- src/Farming/ (unchanged)
  +-- src/World/ (unchanged, TileMap reused per scene)
  +-- src/Data/ (extended: ItemRegistry, EnemyRegistry, DialogueRegistry)
```

## Component Boundaries

| Component | Responsibility | Communicates With | Does NOT Touch |
|-----------|---------------|-------------------|----------------|
| **SceneManager** | Manages active scene, transitions, scene stack | Game1 (owner), all Scenes | Game logic directly |
| **Scene (base)** | Owns a TileMap + entity list, handles own Update/Draw | SceneManager, Entities, UI | Other scenes directly |
| **FarmScene** | Wraps existing farming logic into scene contract | GridManager, CropManager, ToolController | Combat, Dungeon |
| **DungeonScene** | Manages rooms, enemy spawning, combat context | DungeonManager, CombatManager, Enemies | Farming |
| **CombatManager** | Hit detection, damage application, combat events | Entities (player + enemies), DamageCalculator | AI decisions, movement |
| **AIBehavior** | Decides enemy intent (move/attack/flee) | Enemy (owner), reads player position | CombatManager directly |
| **InventoryManager** | Item storage, stacking, equipment slots | PlayerEntity (stats from equipment), UI | Combat, AI |
| **DialogueManager** | Drives conversation state machine | DialogueTree (data), UI (DialogueBox) | Combat, Inventory |
| **LevelSystem** | XP gain, level-up, stat allocation | PlayerProgression, events to UI | Combat directly |
| **DungeonManager** | Room graph, room transitions within dungeon | Rooms, SceneManager (for dungeon exit) | Village, Farm |

### Key Boundary Rules

1. **Scenes own entities.** Each scene maintains its own list of entities. Entities do not exist outside a scene.
2. **Combat is scene-local.** CombatManager lives inside DungeonScene (and potentially VillageScene for future PvE events). FarmScene has no combat.
3. **Inventory is global.** InventoryManager persists across scenes (owned by Game1 or a GameSession object). Player takes items between farm, village, and dungeon.
4. **Dialogue is modal.** When DialogueManager is active, it captures input. The scene pauses entity updates but keeps rendering.
5. **AI reads, does not write.** AI behaviors produce intents (MoveDirection, AttackTarget). The entity/combat system executes them. AI never directly modifies another entity.

## Data Flow

### Combat Flow (most complex new system)

```
1. Enemy.AIBehavior.Update()
   -> Reads: player position, own health, distance
   -> Produces: AIIntent { MoveDirection, ShouldAttack, TargetPosition }

2. Enemy.Update() applies AIIntent
   -> Moves enemy (with collision check against TileMap)
   -> If ShouldAttack: calls CombatManager.RegisterAttack(enemy, attackData)

3. Player presses attack key
   -> InputManager detects press
   -> Scene calls CombatManager.RegisterAttack(player, equippedWeaponData)

4. CombatManager.ProcessAttacks() (called once per frame)
   -> For each registered attack:
      -> Generate hitbox from attacker position + facing + weapon reach
      -> Check overlap against all entities in scene
      -> For each hit: DamageCalculator.Calculate(attacker, defender, weapon)
      -> Apply damage to defender.Health
      -> Fire OnDamageDealt event (UI listens for floating damage numbers)
      -> If defender.Health <= 0: Fire OnEntityDied event

5. OnEntityDied (enemy):
   -> LootTable.Roll() -> produces Item list
   -> Drop items in world (or add directly to inventory)
   -> Award XP to player via LevelSystem.AddXP()
```

### Map Transition Flow

```
1. Player walks into transition zone (Tiled object layer "Transitions")
   -> TileMap detects player CollisionBox overlaps transition object
   -> Transition object has properties: targetMap, targetX, targetY

2. Scene fires OnTransitionRequested(targetMap, targetPosition)

3. SceneManager receives event:
   -> If targetMap == current scene's map: just teleport player (room transition in dungeon)
   -> If targetMap is different scene type:
      a. Call currentScene.OnExit() (cleanup, save state)
      b. Push/swap scene: SceneManager.SetScene(new TargetScene(...))
      c. New scene loads TileMap, spawns entities, places player at targetPosition
      d. Call newScene.OnEnter()

4. Shared state (inventory, player stats, time) persists because it lives above scenes
```

### Inventory + Equipment Flow

```
1. Enemy dies -> LootTable.Roll() -> Items
2. Items added to InventoryManager.AddItem(item)
   -> Finds first compatible slot (stackable? merge. else first empty)
   -> Fires OnInventoryChanged event
   -> UI updates slot display

3. Player opens inventory (Tab key)
   -> Game enters InventoryOpen state (scene pauses, UI captures input)
   -> Player clicks weapon/armor -> InventoryManager.Equip(item, slot)
   -> Equipment stats applied to PlayerEntity.CombatStats
   -> OnEquipmentChanged event fires
   -> HUD updates weapon icon display

4. Save: InventoryManager.GetSaveData() returns list of {itemId, count, slot}
   -> Merged into GameState alongside existing farm data
```

### Dialogue Flow

```
1. Player presses interact key facing NPC
   -> Scene checks: any NPC entity within interact range + facing direction?
   -> If yes: DialogueManager.StartDialogue(npc.DialogueTreeId)

2. DialogueManager enters active state
   -> Scene.IsDialogueActive = true (pauses entity updates, keeps rendering)
   -> DialogueBox.Show(currentNode.Text, currentNode.Choices)

3. Player selects choice (or presses continue for linear dialogue)
   -> DialogueManager.Advance(choiceIndex)
   -> If node has action: execute (give quest, open shop, etc.)
   -> If more nodes: show next
   -> If end: DialogueManager.EndDialogue()

4. Scene resumes normal updates
```

## Patterns to Follow

### Pattern 1: Scene Interface

**What:** All game areas implement a common IScene interface. Game1 delegates to the active scene.
**When:** Always. This is the backbone of map transitions.

```csharp
public interface IScene
{
    void LoadContent(GraphicsDevice device, ContentManager content);
    void Update(float deltaTime, InputManager input);
    void Draw(SpriteBatch spriteBatch, Camera camera);
    void OnEnter(SceneTransitionData data);  // player position, etc.
    void OnExit();
    string MapName { get; }
}

public class SceneManager
{
    private IScene _activeScene;
    private readonly Dictionary<string, Func<IScene>> _sceneFactories = new();

    public void TransitionTo(string sceneName, SceneTransitionData data)
    {
        _activeScene?.OnExit();
        _activeScene = _sceneFactories[sceneName]();
        _activeScene.LoadContent(_device, _content);
        _activeScene.OnEnter(data);
    }
}
```

### Pattern 2: Entity Base Class (not full ECS)

**What:** Simple inheritance hierarchy for game objects. Not a full Entity-Component-System (overkill for this scope).
**When:** For anything that exists in the world with position + collision + rendering.
**Why not ECS:** The game has <100 entities per scene. ECS adds complexity without performance benefit at this scale. A class hierarchy with composition for behaviors (AI) is simpler and fits the existing codebase style.

```csharp
public abstract class Entity
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsActive { get; set; } = true;
    public abstract Rectangle CollisionBox { get; }

    public virtual void Update(float deltaTime, TileMap map) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
}
```

### Pattern 3: AI as Composable Behaviors

**What:** Enemies have an `IAIBehavior` that produces intents. Swap behaviors for different enemy types or states.
**When:** All enemy AI.

```csharp
public interface IAIBehavior
{
    AIIntent Update(Enemy self, Vector2 playerPos, float deltaTime);
}

public struct AIIntent
{
    public Vector2 MoveDirection;
    public bool ShouldAttack;
    public Vector2? TargetPosition;
}

// Enemy uses it:
public class Enemy : Entity
{
    public IAIBehavior Behavior { get; set; }
    public float Health { get; set; }
    public LootTable Loot { get; set; }

    public override void Update(float dt, TileMap map)
    {
        var intent = Behavior.Update(this, _playerRef, dt);
        // Apply intent...
    }
}
```

### Pattern 4: Registry Pattern for Game Data

**What:** Extend the existing `CropRegistry` pattern to items, enemies, and dialogue. Static registries loaded at startup, keyed by string ID.
**When:** For all static game data (item definitions, enemy templates, dialogue trees).

```csharp
public static class ItemRegistry
{
    private static readonly Dictionary<string, ItemData> _items = new();

    public static void Initialize()
    {
        // Load from JSON or hard-code for MVP
        Register(new ItemData("iron_sword", "Iron Sword", ItemType.Weapon, attack: 5));
        Register(new ItemData("health_potion", "Health Potion", ItemType.Consumable, healAmount: 20));
    }

    public static ItemData Get(string id) => _items[id];
}
```

### Pattern 5: Event-Driven Cross-System Communication

**What:** Systems communicate through C# events, not direct references. Already used for `OnDayAdvanced` and `OnStaminaChanged`.
**When:** Whenever system A needs to notify system B without depending on it.

```csharp
// CombatManager fires events, UI listens
public class CombatManager
{
    public event Action<Entity, Entity, int>? OnDamageDealt;  // attacker, target, amount
    public event Action<Entity>? OnEntityDied;
}

// UI subscribes:
combatManager.OnDamageDealt += (atk, def, dmg) => floatingText.Show(def.Position, $"-{dmg}");
combatManager.OnEntityDied += (entity) => { if (entity is Enemy e) xpPopup.Show(e.XPReward); };
```

## Anti-Patterns to Avoid

### Anti-Pattern 1: God Game1

**What:** Adding all new systems directly into Game1.cs Update/Draw methods.
**Why bad:** Game1 is already 256 lines. Adding combat, AI, inventory, dialogue, NPC, dungeon logic directly would push it past 1000+ lines and make it unmaintainable.
**Instead:** Game1 creates SceneManager. SceneManager delegates to active scene. Each scene manages its own systems. Game1 stays under 150 lines of actual logic.

### Anti-Pattern 2: Full ECS for Small Scale

**What:** Implementing a proper Entity-Component-System (Artemis, DefaultEcs, etc.) for <100 entities.
**Why bad:** ECS shines at 10,000+ entities. For a Stardew-scale game with maybe 30 entities per screen, ECS adds architectural complexity (component queries, system ordering, archetype management) with zero performance benefit. It also clashes with the existing OOP codebase.
**Instead:** Simple class hierarchy with composition for behaviors. `Entity` -> `Enemy`, `NPC`, `Projectile`. AI behaviors as injected strategy objects.

### Anti-Pattern 3: Global Singletons for Everything

**What:** Making InventoryManager, CombatManager, etc. static singletons.
**Why bad:** Hard to test, unclear ownership, implicit dependencies. The existing `CropRegistry` is static but it is read-only data, which is fine. Mutable systems should not be static.
**Instead:** Game1 (or a GameSession object) owns shared systems. Scenes receive them via constructor injection.

### Anti-Pattern 4: Tightly Coupling Combat to Rendering

**What:** Computing hitboxes or damage inside Draw methods, or making combat depend on animation frames.
**Why bad:** Makes combat feel inconsistent, ties gameplay to framerate, impossible to test without rendering.
**Instead:** Combat is purely logic (Update phase). Hitboxes are defined by AttackData, not animation. Visual feedback (hit flash, particles) is triggered by events from CombatManager.

### Anti-Pattern 5: Loading All Maps at Startup

**What:** Pre-loading every TileMap (farm, village, dungeon rooms) during LoadContent.
**Why bad:** Slow startup, high memory usage. Dungeon with 10 rooms = 10 tilemaps loaded for no reason.
**Instead:** Load maps on scene transition. Only one (or two during transition) map in memory at a time.

## Key Integration Points with Existing Code

### PlayerEntity Refactoring (minimal)

PlayerEntity needs combat stats but should not break existing farming. Approach:

```csharp
// Add to PlayerEntity (non-breaking):
public class PlayerEntity : Entity  // now extends Entity base
{
    // Existing: Position, Stats (stamina), FacingDirection, CollisionBox
    // NEW:
    public CombatStats Combat { get; } = new();  // HP, attack, defense, level
    public InventoryManager Inventory { get; set; }  // assigned by Game1
    public Equipment Equipped { get; } = new();  // weapon, armor, accessory slots
}

public class CombatStats
{
    public int MaxHP { get; set; } = 100;
    public int CurrentHP { get; set; } = 100;
    public int BaseAttack { get; set; } = 5;
    public int BaseDefense { get; set; } = 2;
    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;
}
```

### GameState Extension (non-breaking)

```csharp
public class GameState
{
    // Existing fields unchanged
    public int SaveVersion { get; set; } = 3;  // bump version
    // ... existing fields ...

    // NEW:
    public string CurrentScene { get; set; } = "farm";
    public CombatStatsSaveData PlayerCombat { get; set; } = new();
    public List<InventorySlotSaveData> Inventory { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();
}
```

### FarmScene Wrapping (existing logic moved, not rewritten)

The current Game1 Update/Draw logic for farming becomes FarmScene. The existing classes (GridManager, CropManager, ToolController) are unchanged -- they just get called by FarmScene instead of Game1.

## Scalability Considerations

| Concern | Current (farm only) | With combat + dungeon | Future (multiple dungeons) |
|---------|--------------------|-----------------------|---------------------------|
| Entities per scene | 1 (player) | 10-30 (player + enemies) | Same per scene, more scenes |
| Maps in memory | 1 | 1 (active scene) | 1-2 (during transition) |
| Update complexity | O(1) per system | O(n) where n = entities, <30 | Same, n stays small |
| Save file size | ~1KB (farm cells) | ~5KB (+inventory, stats, quests) | ~10KB (more quest state) |
| Content loading | 2s (crop sheets) | 3-4s per scene (enemy sprites) | Same, load per scene |

Performance is not a concern for this game's scale. The architecture optimizes for **developer velocity and maintainability**, not raw throughput.

## Suggested Build Order (Dependencies)

The build order is determined by what each system depends on:

```
Phase 1: Foundation (no dependencies on new systems)
  1. Entity base class + refactor PlayerEntity to extend it
  2. SceneManager + IScene interface
  3. FarmScene (wrap existing Game1 logic, prove scene system works)
  4. CombatStats on PlayerEntity

Phase 2: Combat Core (depends on Entity base)
  5. Enemy entity with health, sprite, collision
  6. CombatManager (hit detection, damage)
  7. AI behaviors (patrol, chase, attack)
  8. Basic melee attack for player (sword hitbox)

Phase 3: World Structure (depends on SceneManager)
  9. Map transitions (Tiled transition zones)
  10. VillageScene (static NPCs, no dialogue yet)
  11. DungeonScene (single room with enemies)
  12. Multi-room dungeon (room graph, door transitions)

Phase 4: Items + Progression (depends on Combat for loot)
  13. Item system + ItemRegistry
  14. InventoryManager (slots, stacking)
  15. LootTable + enemy drops
  16. Equipment (weapon/armor affecting combat stats)
  17. LevelSystem (XP from kills, stat gains)

Phase 5: Interaction + Polish (depends on everything above)
  18. DialogueManager + DialogueBox UI
  19. NPC dialogue trees (King quest-giver, shopkeeper)
  20. Shop system (buy/sell using gold from loot)
  21. Quest system (King's mission as linear quest)
  22. Boss fight (special enemy with phase-based AI)
  23. Magic system (ranged projectile attack)

Phase 6: UI + Save (integrates all systems)
  24. Inventory screen UI
  25. Extended HUD (health, XP, equipped weapon)
  26. Hotbar (tools + potions)
  27. Extended save/load (inventory, combat stats, quest progress, current scene)
```

**Rationale for ordering:**
- Entity base and SceneManager are prerequisites for everything. Build first.
- Combat before inventory because loot depends on killing enemies. No point having inventory with nothing to put in it.
- Dungeon structure before items because items drop in dungeons.
- Dialogue after world structure because NPCs need to exist in scenes first.
- UI last because it integrates all data (health from combat, items from inventory, quests from dialogue).
- Save integration last because it must capture state from all systems.

## Sources

- Direct analysis of existing codebase (Game1.cs, PlayerEntity.cs, TileMap.cs, GameState.cs, etc.)
- MonoGame framework patterns (Game class lifecycle, SpriteBatch rendering, ContentManager)
- Established game architecture patterns: Scene/Screen management, entity hierarchies, behavior composition for AI
- XNA/MonoGame community conventions for 2D RPGs (camera transform for world space, screen space for UI, event-driven day cycles)

---

*Architecture analysis: 2026-04-10*
