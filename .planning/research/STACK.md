# Technology Stack

**Project:** Stardew Medieval v3 - Medieval Fantasy RPG/Farming Game
**Researched:** 2026-04-10
**Confidence Note:** WebSearch/WebFetch/Bash were unavailable. Recommendations based on training data (cutoff May 2025). MonoGame ecosystem is stable and slow-moving, so recommendations are HIGH confidence unless noted. Verify exact latest versions on NuGet before installing.

## Current Stack (Keep As-Is)

| Technology | Version | Purpose | Status |
|------------|---------|---------|--------|
| C# 12 | .NET 8.0 | Game logic | KEEP |
| MonoGame.Framework.DesktopGL | 3.8.* | Engine | KEEP |
| MonoGame.Content.Builder.Task | 3.8.* | Asset pipeline | KEEP |
| TiledCS | 3.3.3 | Tiled map loading | KEEP |
| System.Text.Json | built-in | Save/load serialization | KEEP |

## Recommended Additions

### MonoGame.Extended (HIGH confidence)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| MonoGame.Extended | 4.0.* | Core utilities: sprite animations, collections, math helpers | De facto standard extension library for MonoGame. Provides AnimatedSprite, TextureAtlas, ObjectPool, and other utilities that every 2D game needs. Eliminates boilerplate for animation state machines. |
| MonoGame.Extended.Tiled | 4.0.* | Enhanced Tiled map rendering | Better Tiled integration than raw TiledCS -- provides animated tiles, object layer helpers, and efficient culled rendering. **Evaluate against current TiledCS setup**: if TiledCS is working well and you only need collision polygons, keep TiledCS and skip this. If you need animated tiles or richer Tiled features, consider migrating. |

**Rationale:** MonoGame.Extended is the most widely-used companion library to MonoGame. It is maintained by the community and provides battle-tested utilities. Version 4.x targets .NET 8+ and MonoGame 3.8+.

**Confidence:** HIGH -- MonoGame.Extended has been the standard companion library for years. Version 4.0 was a major rewrite targeting modern .NET. Verify exact latest version on NuGet.

### No External ECS -- Roll Your Own (HIGH confidence)

| Decision | Rationale |
|----------|-----------|
| Do NOT add an ECS framework (Arch, DefaultEcs, LeoECS, etc.) | The project already has a clear layered architecture with explicit systems. An ECS adds conceptual overhead and forces restructuring all existing code. For a single-player 2D RPG with < 500 active entities, a simple inheritance + composition approach is more readable and maintainable. |

**What to build instead:**
- `Entity` base class with `Position`, `Velocity`, `IsActive`, `Update()`, `Draw()`
- `EntityManager` that holds lists and handles add/remove/query
- Component-like data classes (e.g., `HealthComponent`, `CombatStats`) composed into entities
- This is "poor man's ECS" -- gives you the data separation benefits without the framework overhead

**Confidence:** HIGH -- Stardew Valley itself uses simple OOP, not ECS. For this scope, ECS is over-engineering.

### No External GUI Library -- Custom Pixel Art UI (HIGH confidence)

| Decision | Rationale |
|----------|-----------|
| Do NOT add Myra, GeonBit.UI, or Apos.Gui | These libraries are designed for editor-style UIs (buttons, textboxes, panels). A pixel art RPG needs custom-drawn UI that matches the game's art style: hand-drawn inventory slots, health bars, dialogue boxes with 9-slice sprites. Generic UI widgets will look out of place and fight against pixel-perfect rendering. |

**What to build instead:**
- `UIElement` base class with `Draw(SpriteBatch)`, `Update()`, `Bounds`
- 9-slice sprite renderer for dialogue boxes and panels
- `InventoryGrid` widget with custom slot rendering
- `DialogueBox` with typewriter text effect
- `HealthBar` / `StaminaBar` with sprite-based rendering
- All UI rendered in screen-space (no camera transform), already the pattern in the existing HUD

**Confidence:** HIGH -- Every successful pixel art game (Stardew Valley, Celeste, Undertale) uses custom UI. Generic UI libraries are for tools, not games.

### Pathfinding: A* Implementation (MEDIUM confidence)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Custom A* on tile grid | N/A (hand-rolled) | Enemy AI navigation | The game already has a tile grid via TileMap. A* on a 2D grid is ~100 lines of C#. External pathfinding libraries (RoyT.AStar, etc.) add dependencies for trivial functionality. Tile-based A* with the existing collision data is straightforward. |

**Implementation approach:**
- `Pathfinder` static class with `FindPath(Point start, Point goal, TileMap map) -> List<Point>`
- Use `TileMap.IsWalkable(x, y)` (already have collision data from Tiled object layers)
- Priority queue via `PriorityQueue<T, int>` (built into .NET 8)
- Cache paths per-enemy, recalculate every N frames (not every frame)

**Confidence:** MEDIUM -- RoyT.AStar is a popular NuGet option if you want drop-in. But for tile grids it is genuinely trivial to implement. Training data may be stale on RoyT version.

### assets/Asset Management (HIGH confidence)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| MonoGame Content Pipeline (MGCB) | 3.8.* | Sprite fonts, effects | Already in use. Continue using for .spritefont and any .fx shader files. |
| Direct PNG loading via Texture2D.FromStream | N/A | Sprite sheets, tilesets | Already the pattern in Game1.LoadTexture(). Good for hot-reload during dev. Keep this for game art assets. |
| JSON files via System.Text.Json | built-in | Game data (items, enemies, dialogue, quests) | Already used for saves. Extend the pattern: item definitions, enemy stats, dialogue trees, quest data all as JSON loaded at startup. No need for a database. |

### Sprite Animation (MEDIUM confidence)

| Decision | Rationale |
|----------|-----------|
| Use MonoGame.Extended AnimatedSprite OR roll custom | MonoGame.Extended provides `AnimatedSprite` and `SpriteSheet` classes that handle frame timing, atlas regions, and animation state. If you add MonoGame.Extended, use these. If not, implement a simple `AnimationController` (~80 lines) with frame timing and spritesheet region lookup. |

**What the animation system needs:**
- Per-entity animation state (Idle, Walk, Attack, Hit, Death) x direction (Up, Down, Left, Right)
- Frame timing with configurable FPS per animation
- Sprite atlas region lookup (row/column in spritesheet)
- Attack animations with "active frame" callbacks (for hitbox timing)

### Audio (HIGH confidence)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| MonoGame built-in (SoundEffect, Song) | 3.8.* | SFX and music | MonoGame's audio API is sufficient for a 2D RPG. SoundEffect for short clips (sword swing, crop harvest), Song for background music. No need for FMOD or other audio middleware at this scale. |

**Pattern:**
- `AudioManager` singleton with `PlaySFX(string name)`, `PlayMusic(string name)`
- Preload all SFX as `SoundEffect` instances at startup
- Use `SoundEffectInstance` for looping ambient sounds
- Volume control per category (music, sfx, ambient)

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| ECS | Custom entity system | Arch ECS, DefaultEcs | Over-engineering for < 500 entities. Forces full rewrite of existing systems. |
| GUI | Custom pixel art UI | Myra, GeonBit.UI | Generic widget look clashes with pixel art. Custom UI matches art style. |
| Pathfinding | Hand-rolled A* | RoyT.AStar NuGet | A* on tile grid is trivial in C#. Avoids dependency for ~100 lines of code. |
| Physics | Simple AABB + circle collision | Aether.Physics2D, Farseer | 2D RPG needs simple hitbox checks, not realistic physics simulation. Existing polygon collision is already more than enough. |
| Tiled loading | Keep TiledCS 3.3.3 | MonoGame.Extended.Tiled | TiledCS works. Only migrate if you need animated tiles or run into TiledCS limitations. |
| Serialization | System.Text.Json | Newtonsoft.Json, MessagePack | Built-in, fast, already in use. No reason to add another serializer. |
| Dialogue | Custom JSON-based system | Yarn Spinner, Ink | Yarn Spinner has a C# runtime but it is designed for Unity. For simple branching dialogue (NPC says X, player picks response), a JSON-driven custom system is simpler and gives full control. |
| State machines | Custom `enum` + `switch` | Stateless library | Game state machines (enemy AI, player states) are simple enough that a library adds no value. `enum` + `switch` is idiomatic for game dev. |

## Libraries to Explicitly AVOID

| Library | Why Avoid |
|---------|-----------|
| **Unity packages** (Yarn Spinner Unity, etc.) | Not compatible with MonoGame |
| **Aether.Physics2D / Farseer** | Realistic physics is wrong paradigm for tile-based RPG. You want game-logic hitboxes, not physics bodies with mass/friction. |
| **Any ORM (EF Core, Dapper)** | No database needed. JSON files are the right persistence model for single-player game data. |
| **Dependency injection containers** | Game objects are created/destroyed per-frame. DI containers add latency and complexity with no benefit for game dev. |
| **Logging frameworks (Serilog, NLog)** | Console.WriteLine is fine for a game. Structured logging is enterprise overhead. |
| **FNA** | Alternative MonoGame runtime. Switching mid-project is disruptive with no benefit for this scope. |

## Data-Driven Design: JSON Schema Additions

The existing project uses JSON for saves. Extend this pattern for all game data:

```
assets/Data/
  items.json          # Item definitions (id, name, type, stats, sprite, stackSize)
  enemies.json        # Enemy definitions (id, name, hp, damage, speed, loot table, AI type)
  loot_tables.json    # Drop tables (enemy -> [{itemId, chance, minQty, maxQty}])
  dialogue.json       # NPC dialogue trees (npcId -> [{text, responses: [{text, next}]}])
  quests.json         # Quest definitions (id, description, objectives, rewards)
  spells.json         # Spell definitions (id, name, manaCost, damage, range, projectileSpeed)
```

All loaded at startup into static registries (same pattern as existing `CropRegistry`).

## Installation

```bash
# Add MonoGame.Extended (if adopted)
dotnet add package MonoGame.Extended --version 4.0.*

# Everything else is hand-rolled or already present
# No other NuGet packages needed
```

## Architecture Impact

Adding these systems to the existing codebase means:

1. **New folders to create:**
   - `src/Entities/` -- Base entity system, enemy types, projectiles
   - `src/Combat/` -- Damage calculation, hitboxes, spell system
   - `src/Inventory/` -- Item model, inventory grid, equipment slots
   - `Dialogue/` -- Dialogue parser, dialogue UI
   - `Quests/` -- Quest state machine, quest tracker
   - `Dungeon/` -- Room transitions, dungeon generation/loading
   - `AI/` -- Enemy behavior (patrol, chase, attack), pathfinding

2. **Existing code to extend (not rewrite):**
   - `Game1.cs` -- Add entity manager, scene/map transitions
   - `src/Core/GameState.cs` -- Add inventory, quest progress, player level to save data
   - `src/Core/InputManager.cs` -- Add mouse click handling for UI interaction
   - `src/Player/PlayerEntity.cs` -- Add combat state, equipment, animation states
   - `src/Data/` -- Add ItemRegistry, EnemyRegistry, SpellRegistry alongside CropRegistry

3. **Pattern to adopt: Scene/Map Management**
   - Current: single map loaded in Game1
   - Needed: ability to transition between Farm, Village, Dungeon maps
   - Implementation: `SceneManager` that handles loading/unloading maps and persisting entity state across transitions

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| MonoGame.Extended recommendation | HIGH | Stable, well-known library. Verify exact 4.x version on NuGet. |
| No ECS decision | HIGH | Correct for project scope and existing architecture. |
| No external GUI decision | HIGH | Standard for pixel art games. |
| A* pathfinding approach | HIGH | Trivial algorithm, built-in PriorityQueue in .NET 8. |
| JSON data-driven approach | HIGH | Already proven in the codebase with saves and CropRegistry. |
| Audio approach | HIGH | MonoGame built-in audio is well-documented and sufficient. |
| Exact package versions | LOW | Could not verify against NuGet. Check before installing. |

## Sources

- Training data knowledge of MonoGame ecosystem (cutoff May 2025)
- Direct analysis of existing codebase (.csproj, Game1.cs, architecture)
- MonoGame is a slow-moving, stable ecosystem -- recommendations are unlikely to be stale
- **Could not verify:** exact latest versions of MonoGame.Extended 4.x, RoyT.AStar. Check NuGet.

---

*Stack research: 2026-04-10*
