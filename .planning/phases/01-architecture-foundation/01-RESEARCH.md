# Phase 1: Architecture Foundation - Research

**Researched:** 2026-04-10
**Domain:** MonoGame scene management, entity architecture, game state refactoring
**Confidence:** HIGH

## Summary

This phase refactors the existing monolithic `Game1.cs` into a scene-based architecture with shared entity behavior and extensible game state. The codebase is small (~15 source files, ~1200 lines total), well-structured, and builds cleanly on .NET 8.0 + MonoGame 3.8.4.1. All decisions are locked in CONTEXT.md -- the implementation path is clear.

The main challenge is decomposition without regression: Game1.cs currently owns all initialization, update, and draw coordination. Extracting this into SceneManager + Scene subclasses requires careful transfer of responsibilities while preserving the exact same visual and behavioral output. The save migration path is straightforward thanks to the existing `MigrateIfNeeded()` pattern.

**Primary recommendation:** Execute in dependency order: Entity base class first (no dependencies), then ServiceContainer + Scene abstractions, then SceneManager with fade transitions, then GameState expansion + ItemDefinition/ItemRegistry, and finally the FarmScene extraction from Game1.cs. This order minimizes broken intermediate states.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Scenes as abstract class (`abstract class Scene`) with virtual methods (LoadContent, Update, Draw, UnloadContent). Scenes inherit and override.
- **D-02:** Shared services via `ServiceContainer` passed in Scene constructor. Groups InputManager, TimeManager, Camera, SpriteBatch, etc.
- **D-03:** Scene transition via fade to black (fade out 0.3-0.5s -> black screen -> swap scene -> fade in 0.3-0.5s). SceneManager controls states: None -> FadingOut -> Loading -> FadingIn -> None.
- **D-04:** SceneManager with stack (push/pop). Active scene on top receives Update. Draw renders bottom to top. Allows menus and overlays as stacked scenes (e.g. PauseScene over FarmScene).
- **D-05:** Simple inheritance with abstract `Entity` class. Subclasses: PlayerEntity, EnemyEntity, NPCEntity, ItemDrop.
- **D-06:** Entity base includes: Position, CollisionBox, Facing, Velocity/Movement, Animation (SpriteSheet, FrameIndex, AnimationTimer), HP/IsAlive. Justification: Phases 3-5 need these capabilities in multiple entities -- putting them in base now avoids guaranteed refactor.
- **D-07:** Full ItemDefinition structure in Phase 1: Id, Name, Type (enum: Crop/Seed/Tool/Weapon/Armor/Consumable/Loot), Rarity (Common/Uncommon/Rare), StackLimit, SpriteId, Stats (Dictionary<string, float>). Later phases only populate data, don't change structure.
- **D-08:** Item definitions in JSON (`items.json`) loaded via static `ItemRegistry`. CropRegistry migrates to this unified system.
- **D-09:** Save compatibility via version + defaults. Increment CURRENT_SAVE_VERSION. Extend MigrateIfNeeded() with v->v+1 migration. New fields get safe defaults.
- **D-10:** New GameState fields: Inventory (empty List<ItemStack>), Gold/XP/Level (zeroed), CurrentScene (string), QuestState (enum), Equipment slots (WeaponId/ArmorId optional), HotbarSlots (List<string?> with 8 slots). All with safe defaults.

### Claude's Discretion
- Internal refactoring order (which files first)
- Exact names of intermediate methods/properties
- How to organize fade transition logic internally in SceneManager

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ARCH-01 | SceneManager manages transition between scenes (Farm, Village, Dungeon) with fade in/out | D-01, D-03, D-04 define the exact architecture. Fade state machine pattern documented below. |
| ARCH-02 | Entity base class with position, sprite, collision, shared by src/Player/Enemy/NPC | D-05, D-06 define the full property set. PlayerEntity provides extraction reference. |
| ARCH-03 | Unified ItemDefinition model for crops, tools, weapons, armor, consumables, loot | D-07, D-08 define structure and JSON loading. CropRegistry migration path documented. |
| ARCH-04 | GameState restructured for inventory, XP, quest state, gold, current scene | D-09, D-10 define exact fields and migration strategy. SaveManager pattern already exists. |
| ARCH-05 | Game1.cs refactored to delegate logic to scenes instead of coordinating directly | All decisions combined. Game1 becomes thin shell delegating to SceneManager. |
</phase_requirements>

## Standard Stack

### Core (already in project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.4.1 | Game engine | [VERIFIED: dotnet list package] |
| MonoGame.Content.Builder.Task | 3.8.4.1 | Content pipeline | [VERIFIED: dotnet list package] |
| TiledCS | 3.3.3 | Tiled map parsing | [VERIFIED: dotnet list package] |
| System.Text.Json | built-in | JSON serialization | Already used by SaveManager [VERIFIED: source code] |

### Supporting (no new packages needed)
This phase requires **zero new NuGet packages**. All functionality is built with MonoGame primitives and standard .NET APIs.

| Capability | Implementation | Rationale |
|------------|---------------|-----------|
| Scene management | Custom abstract class | MonoGame has no built-in scene system [VERIFIED: MonoGame API] |
| Fade transitions | SpriteBatch + Texture2D overlay | `_pixel` texture already exists in Game1.cs [VERIFIED: source line 41] |
| JSON item loading | System.Text.Json | Already used for saves, consistent serializer [VERIFIED: SaveManager.cs] |
| Entity hierarchy | C# abstract class | Standard OOP, no framework needed |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom SceneManager | MonoGame.Extended ScreenManager | Adds dependency for simple use case; locked decision is custom |
| System.Text.Json | Newtonsoft.Json | STJ already in use, no reason to add dependency |

**Installation:** No new packages required.

## Architecture Patterns

### Recommended Project Structure (new/modified files)
```
src/Core/
  Entity.cs             # NEW - abstract base class
  ServiceContainer.cs   # NEW - groups shared services
  SceneManager.cs       # NEW - stack-based scene management
  Scene.cs              # NEW - abstract scene base
  GameState.cs          # MODIFIED - expanded fields
  SaveManager.cs        # MODIFIED - v2->v3 migration
src/Data/
  ItemDefinition.cs     # NEW - unified item model
  ItemRegistry.cs       # NEW - static JSON loader (replaces CropRegistry)
  ItemType.cs           # NEW - enum (Crop/Seed/Tool/Weapon/Armor/Consumable/Loot)
  Rarity.cs             # NEW - enum (Common/Uncommon/Rare)
  items.json            # NEW - item definitions (crops initially)
src/Farming/
  CropData.cs           # MODIFIED - references ItemDefinition
src/Scenes/
  FarmScene.cs          # NEW - extracted from Game1.cs
  TestScene.cs          # NEW - placeholder for transition testing
Game1.cs                # MODIFIED - thin shell delegating to SceneManager
```

### Pattern 1: Scene Stack with State Machine Transitions
**What:** SceneManager owns a `Stack<Scene>` and a `TransitionState` enum (None, FadingOut, Loading, FadingIn). Push/pop operations trigger the fade state machine. [ASSUMED - standard pattern for MonoGame games]
**When to use:** All scene changes go through SceneManager.
**Example:**
```csharp
// Source: derived from D-03, D-04 locked decisions
public class SceneManager
{
    private readonly Stack<Scene> _scenes = new();
    private TransitionState _state = TransitionState.None;
    private float _fadeAlpha;
    private const float FadeDuration = 0.4f; // seconds
    private Action? _pendingAction; // the push/pop to execute at black screen

    public void TransitionTo(Scene newScene)
    {
        _pendingAction = () =>
        {
            while (_scenes.Count > 0)
                _scenes.Pop().UnloadContent();
            _scenes.Push(newScene);
            newScene.LoadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
    }

    public void Push(Scene overlay)
    {
        _pendingAction = () =>
        {
            _scenes.Push(overlay);
            overlay.LoadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
    }

    public void Pop()
    {
        _pendingAction = () =>
        {
            if (_scenes.Count > 0)
                _scenes.Pop().UnloadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
    }

    public void Update(float dt)
    {
        switch (_state)
        {
            case TransitionState.FadingOut:
                _fadeAlpha += dt / FadeDuration;
                if (_fadeAlpha >= 1f)
                {
                    _fadeAlpha = 1f;
                    _pendingAction?.Invoke();
                    _pendingAction = null;
                    _state = TransitionState.FadingIn;
                }
                break;
            case TransitionState.FadingIn:
                _fadeAlpha -= dt / FadeDuration;
                if (_fadeAlpha <= 0f)
                {
                    _fadeAlpha = 0f;
                    _state = TransitionState.None;
                }
                break;
            case TransitionState.None:
                if (_scenes.Count > 0)
                    _scenes.Peek().Update(dt);
                break;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        // Draw all scenes bottom-to-top
        foreach (var scene in _scenes.Reverse())
            scene.Draw(sb);

        // Draw fade overlay
        if (_fadeAlpha > 0f)
            sb.Draw(_pixel, fullScreenRect, Color.Black * _fadeAlpha);
    }
}
```

### Pattern 2: ServiceContainer (Dependency Bag)
**What:** Simple container class grouping shared services passed to Scene constructors. Not a DI framework -- just a struct/class holding references. [ASSUMED - common MonoGame pattern]
**When to use:** Every Scene receives this in its constructor.
**Example:**
```csharp
// Source: derived from D-02 locked decision
public class ServiceContainer
{
    public required GraphicsDevice GraphicsDevice { get; init; }
    public required SpriteBatch SpriteBatch { get; init; }
    public required InputManager Input { get; init; }
    public required TimeManager Time { get; init; }
    public required Camera Camera { get; init; }
    public required ContentManager Content { get; init; }
    public required SceneManager SceneManager { get; init; }
}
```

### Pattern 3: Entity Base Class with Template Method
**What:** Abstract Entity defines common state and virtual Update/Draw. Subclasses override specifics. [VERIFIED: matches D-05, D-06]
**When to use:** All game objects that have position, sprite, collision.
**Example:**
```csharp
// Source: derived from D-05, D-06, extracted from PlayerEntity.cs patterns
public abstract class Entity
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Direction FacingDirection { get; set; } = Direction.Down;
    public float HP { get; set; } = 100f;
    public float MaxHP { get; set; } = 100f;
    public bool IsAlive => HP > 0;

    // Animation
    protected Texture2D? SpriteSheet { get; set; }
    protected int FrameWidth { get; set; }
    protected int FrameHeight { get; set; }
    protected int FrameIndex { get; set; }
    protected float AnimationTimer { get; set; }
    protected float FrameTime { get; set; } = 0.15f;

    public virtual Rectangle CollisionBox
    {
        get
        {
            int w = 10, h = 6;
            return new Rectangle(
                (int)Position.X - w / 2,
                (int)Position.Y + FrameHeight / 2 - h - 2,
                w, h);
        }
    }

    public virtual void Update(float deltaTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
}
```

### Pattern 4: Save Migration Chain
**What:** Increment version, add sequential migration methods. [VERIFIED: SaveManager.cs already implements this]
**When to use:** Every GameState expansion.
**Example:**
```csharp
// Source: existing SaveManager.cs pattern
private static void MigrateIfNeeded(GameState state)
{
    if (state.SaveVersion < 2)
    {
        // v1 -> v2: farm cells added
        state.SaveVersion = 2;
    }
    if (state.SaveVersion < 3)
    {
        // v2 -> v3: inventory, gold, xp, scene, quest, equipment, hotbar
        state.Inventory ??= new();
        state.Gold = 0;
        state.XP = 0;
        state.Level = 1;
        state.CurrentScene = "Farm";
        state.QuestState = 0; // None
        state.WeaponId = null;
        state.ArmorId = null;
        state.HotbarSlots ??= new List<string?>(new string?[8]);
        state.SaveVersion = 3;
    }
}
```

### Anti-Patterns to Avoid
- **God class Scene:** Don't put everything from Game1 into FarmScene. Farming systems (GridManager, CropManager, ToolController) remain separate -- FarmScene just coordinates them. [ASSUMED]
- **Circular dependency Scene <-> SceneManager:** Scene should receive SceneManager via ServiceContainer, not hold a direct reference to request transitions. Use the ServiceContainer reference. [ASSUMED]
- **Skipping UnloadContent:** Scenes MUST unload textures/resources when popped. MonoGame doesn't garbage collect GPU resources automatically. [VERIFIED: MonoGame framework behavior]
- **Blocking during fade:** Don't call LoadContent during FadingOut. The state machine should: FadeOut -> execute action at full black -> FadeIn. Loading happens at the black screen moment. [ASSUMED - standard practice]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization | Custom parser | System.Text.Json | Already in use, handles nullables, polymorphism [VERIFIED: SaveManager.cs] |
| Fade transition math | Custom easing curves | Linear lerp on alpha | Simple fade to black doesn't need easing for MVP [ASSUMED] |
| Entity collision | Physics engine | Existing TileMap.CheckCollision pattern | Already works for player, extend to entities [VERIFIED: PlayerEntity.cs] |
| Item definition validation | Manual checks | Default values + nullable types | C# nullable reference types already enabled in .csproj [VERIFIED: csproj] |

**Key insight:** This phase is pure refactoring + data model expansion. No new capabilities are needed from external libraries. The existing MonoGame + .NET primitives cover everything.

## Common Pitfalls

### Pitfall 1: Breaking the Draw Order
**What goes wrong:** After refactoring, visual elements render in wrong order (crops under map, HUD under world, etc.)
**Why it happens:** Game1.cs currently has a specific draw order: map layers -> grid overlays -> crops -> player -> farm zone hint -> darkness overlay -> HUD. Moving to scenes can scramble this.
**How to avoid:** FarmScene.Draw() must replicate the exact same SpriteBatch begin/end sequence with the same parameters (SamplerState.PointClamp for world, separate batch for HUD). Copy the draw order verbatim.
**Warning signs:** Visual glitches, z-fighting, blurry sprites (wrong SamplerState).

### Pitfall 2: Lost Event Subscriptions
**What goes wrong:** TimeManager.OnDayAdvanced stops triggering crop growth after scene refactoring.
**Why it happens:** Game1.cs subscribes in Initialize(). If FarmScene recreates managers or doesn't subscribe, events break silently.
**How to avoid:** FarmScene must subscribe its OnDayAdvanced handler in LoadContent and unsubscribe in UnloadContent. Document which events each scene owns.
**Warning signs:** Day advances but crops don't grow, stamina doesn't restore.

### Pitfall 3: Save/Load Context Mismatch
**What goes wrong:** Loading a save with CurrentScene="Farm" but the SceneManager hasn't pushed FarmScene yet.
**Why it happens:** Save loading happens before scene initialization in current code.
**How to avoid:** Game1 loads save data first, then tells SceneManager which scene to push based on `state.CurrentScene`. Scene's LoadContent receives the state.
**Warning signs:** Null reference on load, player position reset, wrong scene on boot.

### Pitfall 4: Stack Overflow with Scene Stack
**What goes wrong:** Scenes accumulate on stack without being popped.
**Why it happens:** Push without pop for overlays (pause menus, etc.)
**How to avoid:** TransitionTo() clears the stack before pushing. Push() is only for overlays that will be popped. Add a debug log showing stack depth.
**Warning signs:** Memory growth, draw calls increasing, multiple scene instances updating.

### Pitfall 5: CropRegistry Migration Data Loss
**What goes wrong:** After migrating CropRegistry to ItemRegistry, save files reference crop names that ItemRegistry doesn't recognize.
**Why it happens:** CropRegistry uses "Cabbage", "Carrot" etc. as keys. If ItemRegistry uses different IDs, old saves break.
**How to avoid:** ItemDefinition.Id for crops MUST match existing CropRegistry keys exactly (e.g., "Cabbage", "Carrot"). Verify with a round-trip test: save -> load -> verify crops intact.
**Warning signs:** Crops disappearing after load, null crop references.

### Pitfall 6: Direction Enum Duplication
**What goes wrong:** `Direction` enum is currently in `src/Player/PlayerEntity.cs`. Moving to Entity base creates a conflict or duplication.
**Why it happens:** Enum is defined in the same file as PlayerEntity per current conventions.
**How to avoid:** Move `Direction` enum to its own file in `src/Core/` or keep in Entity.cs. Update all `using` statements. Do this as the very first step.
**Warning signs:** Compilation errors about ambiguous Direction type.

## Code Examples

### Current Game1.cs Responsibilities (what must transfer to FarmScene)
```csharp
// Source: Game1.cs analysis [VERIFIED]
// These responsibilities move from Game1 to FarmScene:
// 1. TileMap loading and drawing (lines 84, 179)
// 2. Player creation and update (lines 64-65, 144)
// 3. CropRegistry initialization (line 91)
// 4. GridManager/CropManager/ToolController setup (lines 94-98)
// 5. Camera setup and follow (lines 101-102, 146)
// 6. HUD setup and draw (lines 105-107, 201-203)
// 7. Day/night overlay draw (lines 190-198)
// 8. Farm zone hint draw (lines 208-225)
// 9. OnDayAdvanced handler (lines 227-248)
// 10. Save/load coordination (lines 110-118, 237-247)

// These stay in Game1:
// 1. GraphicsDeviceManager setup (lines 43-51)
// 2. SceneManager creation and delegation
// 3. Window configuration
// 4. Escape to exit
```

### ItemDefinition Model
```csharp
// Source: derived from D-07 locked decision
public enum ItemType
{
    Crop, Seed, Tool, Weapon, Armor, Consumable, Loot
}

public enum Rarity
{
    Common, Uncommon, Rare
}

public class ItemDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public ItemType Type { get; set; }
    public Rarity Rarity { get; set; } = Rarity.Common;
    public int StackLimit { get; set; } = 99;
    public string SpriteId { get; set; } = "";
    public Dictionary<string, float> Stats { get; set; } = new();
}

public class ItemStack
{
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; } = 1;
}
```

### Expanded GameState
```csharp
// Source: derived from D-10 locked decision + existing GameState.cs
public class GameState
{
    // Existing (v2)
    public int SaveVersion { get; set; } = 3;
    public int DayNumber { get; set; } = 1;
    public int Season { get; set; }
    public float StaminaCurrent { get; set; } = 100f;
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float GameTime { get; set; }
    public List<FarmCellSaveData> FarmCells { get; set; } = new();

    // New (v3)
    public List<ItemStack> Inventory { get; set; } = new();
    public int Gold { get; set; } = 0;
    public int XP { get; set; } = 0;
    public int Level { get; set; } = 1;
    public string CurrentScene { get; set; } = "Farm";
    public int QuestState { get; set; } = 0; // enum as int for JSON compat
    public string? WeaponId { get; set; }
    public string? ArmorId { get; set; }
    public List<string?> HotbarSlots { get; set; } = new(new string?[8]);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Monolithic Game class | Scene-based architecture | Standard since XNA days | Better separation, testability |
| Component-Entity-System (ECS) | Simple inheritance for small games | Always valid for < 50 entity types | Avoids ECS complexity overhead |
| XML config files | JSON for game data | .NET Core+ era | System.Text.Json built-in, no dependency |

**Deprecated/outdated:**
- XNA Content Pipeline XML format: replaced by JSON or direct file loading. Project already uses direct file loading for sprites and TiledCS for maps. [VERIFIED: source code]
- MonoGame.Extended ScreenManager: exists but adds unnecessary dependency for this use case. Custom scene management is standard for MonoGame. [ASSUMED]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Scene stack pattern with state machine transitions is standard for MonoGame | Architecture Patterns | LOW - well-established pattern, but details may vary |
| A2 | ServiceContainer as simple class (not DI framework) is sufficient | Architecture Patterns | LOW - could need refactoring later if services grow complex |
| A3 | Linear fade (no easing) is acceptable for MVP transitions | Don't Hand-Roll | NONE - purely cosmetic preference |
| A4 | MonoGame.Extended ScreenManager is unnecessary for this scope | Standard Stack | LOW - locked decision already excludes it |
| A5 | Blocking during fade (no Update on scenes) is acceptable | Architecture Patterns | LOW - scenes are paused during 0.4s transitions |

## Open Questions (RESOLVED)

1. **items.json location**
   - What we know: D-08 says JSON loaded via ItemRegistry. Config files currently live in `assets/` or `src/Data/`.
   - What's unclear: Exact path. `src/Data/items.json`? `assets/Data/items.json`?
   - RESOLVED: Use `src/Data/items.json` alongside the code, with CopyToOutputDirectory in .csproj. Consistent with content organization. (Implemented in Plan 01-01 Task 2)

2. **CropData relationship to ItemDefinition**
   - What we know: CropRegistry migrates to ItemRegistry (D-08). CropData has growth-specific fields (DaysPerStage, StageCount, GrowthSheet, etc.) that don't belong in generic ItemDefinition.
   - What's unclear: Does CropData become a separate class that references an ItemDefinition? Or does it embed within ItemDefinition.Stats?
   - RESOLVED: Keep CropData as a separate runtime class for growth logic. ItemDefinition for the "Cabbage" crop item. ItemRegistry provides ItemDefinition; CropManager still uses CropData for growth mechanics. Link them by matching Id/Name. CropRegistry coexists temporarily for GPU texture loading until Phase 2. (Implemented in Plan 01-01 Task 2)

3. **TestScene content**
   - What we know: Success criteria requires "two placeholder scenes with transition."
   - What's unclear: What does TestScene look like?
   - RESOLVED: Dark blue background with text label ("Test Scene - Press B to go back"), just enough to prove transitions work. (Implemented in Plan 01-03 Task 2)

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8.0 SDK | Build/run | Yes | 8.0.419 | -- |
| MonoGame 3.8 | Engine | Yes | 3.8.4.1 | -- |
| TiledCS | Map loading | Yes | 3.3.3 | -- |

No missing dependencies. All tools available.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected -- Wave 0 must set up |
| Config file | none -- see Wave 0 |
| Quick run command | `dotnet test --filter "Category=Phase1" --no-build` |
| Full suite command | `dotnet test` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ARCH-01 | SceneManager transitions with fade states | unit | `dotnet test --filter "FullyQualifiedName~SceneManagerTests"` | No - Wave 0 |
| ARCH-02 | Entity base class with position, collision, animation fields | unit | `dotnet test --filter "FullyQualifiedName~EntityTests"` | No - Wave 0 |
| ARCH-03 | ItemDefinition model with all fields, ItemRegistry loads JSON | unit | `dotnet test --filter "FullyQualifiedName~ItemRegistryTests"` | No - Wave 0 |
| ARCH-04 | GameState serializes/deserializes new fields, migration v2->v3 | unit | `dotnet test --filter "FullyQualifiedName~SaveMigrationTests"` | No - Wave 0 |
| ARCH-05 | Game1 delegates to SceneManager, FarmScene behavior matches original | manual-only | Visual regression check: boot game, verify identical behavior | No |

### Sampling Rate
- **Per task commit:** `dotnet test --no-build`
- **Per wave merge:** `dotnet test`
- **Phase gate:** Full suite green + manual visual regression before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] Test project: `dotnet new xunit -n StardewMedieval.Tests` + reference main project
- [ ] `Tests/SceneManagerTests.cs` -- covers ARCH-01 (state machine transitions)
- [ ] `Tests/EntityTests.cs` -- covers ARCH-02 (entity base class properties)
- [ ] `Tests/ItemRegistryTests.cs` -- covers ARCH-03 (JSON load, item lookup)
- [ ] `Tests/SaveMigrationTests.cs` -- covers ARCH-04 (v2->v3 migration, defaults)
- [ ] Note: MonoGame `GraphicsDevice` cannot be instantiated in unit tests without a Game instance. Tests must focus on logic classes (SceneManager state machine, ItemRegistry, SaveManager migration) not rendering.

## Security Domain

security_enforcement: Not applicable -- single-player offline game, no network, no user authentication, no sensitive data handling. Save files are local JSON. Omitting ASVS analysis.

## Sources

### Primary (HIGH confidence)
- Project source code (all .cs files read directly) -- codebase architecture, current patterns, existing APIs
- .csproj file -- verified package versions, target framework, nullable setting
- `dotnet list package` output -- confirmed resolved package versions
- `dotnet build` output -- confirmed clean compilation

### Secondary (MEDIUM confidence)
- CONTEXT.md locked decisions (D-01 through D-10) -- implementation constraints

### Tertiary (LOW confidence)
- MonoGame scene management patterns -- based on training knowledge, not verified against current MonoGame docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - verified via package list and source code, no new packages needed
- Architecture: HIGH - locked decisions are specific, codebase fully mapped, patterns are well-established
- Pitfalls: HIGH - derived from direct source code analysis, identified concrete risks

**Research date:** 2026-04-10
**Valid until:** 2026-05-10 (stable stack, no version changes expected)
