# Phase 3: Combat - Research

**Researched:** 2026-04-11
**Domain:** Real-time 2D combat system in MonoGame (melee, ranged, enemy AI, boss fights)
**Confidence:** HIGH

## Summary

This phase adds a complete combat system to the existing MonoGame game: melee sword attacks, a fireball projectile, HP bars, three enemy types with FSM AI, and a boss fight. The codebase already provides strong foundations -- `Entity` base class with HP/MaxHP/IsAlive/Velocity/CollisionBox, `EquipmentData` for combined attack/defense stats, `InputManager` with LMB/RMB edge detection, and HP bar sprite assets. The primary work is building on these existing systems rather than creating new infrastructure.

MonoGame's built-in `Rectangle.Intersects()` is sufficient for all collision needs (melee hitbox vs enemy collision box, projectile vs enemy, enemy attack vs player). No external physics library is needed. The DummyNpc pattern establishes the model for placeholder sprite enemies (colored rectangles with collision boxes). The `ItemDropEntity` pattern demonstrates entity lifecycle management in scenes (list iteration, removal on collection).

**Primary recommendation:** Use composition over inheritance for enemy behavior (single `EnemyEntity` class with data-driven stats + shared FSM), Rectangle-based collision for everything, and integrate combat into FarmScene by adding enemy list management alongside existing item drop patterns.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Melee attack via Left Click, swing sword in FacingDirection, ~90 degree arc covering 3 tiles ahead. Stardew Valley style.
- D-02: Knockback ~32px opposite to player direction. Set position directly with lerp, no physics.
- D-03: Slash animation overlay (arc sprite), ~0.3s duration, 2-3 frames.
- D-04: Cooldown per weapon (Iron_Sword: 0.5s, Steel_Sword: 0.4s, Flame_Blade: 0.35s). No combo system.
- D-05: Hit feedback: enemy flashes white ~0.1s (tint flash). No hitstop, no screenshake.
- D-06: Damage = weapon.damage + equipment_attack_bonus. Enemy defense subtracts (minimum 1 damage).
- D-07: One spell: Fireball. Travels in FacingDirection (not mouse-aimed). 200px/s, 300px range, despawns on hit or max range.
- D-08: RMB = magic, LMB = melee (when weapon equipped).
- D-09: Fireball cooldown: 2s. No mana system. Cooldown indicator on HUD.
- D-10: Fireball visual: 8x8 or 16x16 sprite, static with rotation or 2-frame animation.
- D-11: Fireball damage: 15 fixed. No equipment scaling.
- D-12: Player HP uses Entity.HP/MaxHP (already exists). MaxHP=100. HP bar on HUD using UI_StatusBar_Fill_HP.png.
- D-13: Enemy HP bars above enemy when HP < MaxHP. Red bar on gray background. Hidden at full HP.
- D-14: Enemy attacks damage player. Death = respawn at farm with full HP. Death penalty deferred to Phase 6.
- D-15: i-frames: 1s after taking damage. Player blinks, immune to damage.
- D-16: Skeleton (melee rusher): 60px/s, detect 120px, attack 24px range, 10 dmg, 40 HP. Drops bones + gold chance.
- D-17: Dark Mage (ranged caster): 30px/s, detect 160px, keep 100px distance, shoots every 3s, 12 dmg projectile, 30 HP. Drops mana crystal + gold chance.
- D-18: Golem (slow tank): 20px/s, detect 80px, 120 HP, 20 dmg, 2s attack cooldown, 8px knockback resistance. Drops stone + rare chance.
- D-19: AI FSM: Idle -> Chase -> Attack -> Return. Distance-based transitions.
- D-20: 3-5 enemies spawn at hardcoded positions on farm. Respawn on day advance. Relocated to dungeon in Phase 5.
- D-21: Boss: Skeleton King, 2x sprite size, 300 HP. Fixed spawn on farm (temporary).
- D-22: Boss telegraphs: (1) Wide slash with 1s red flash wind-up, 25 dmg. (2) Summon 2 skeleton minions at each 30% HP threshold.
- D-23: Boss drops Flame_Blade (guaranteed first kill) + gold bonus.
- D-24: Boss HP bar at bottom of screen (RPG-style boss bar with name).
- D-25: V1 placeholder sprites: colored rectangles (skeleton=white, mage=purple, golem=brown, boss=red large). Same as DummyNpc approach.

### Claude's Discretion
- Internal structure of CombatSystem/AttackManager
- EnemyEntity organization (inheritance vs composition)
- Projectile implementation (Entity subclass vs separate system)
- Collision detection approach (rectangle overlap vs circle)
- Spawn system internal structure
- Exact hitbox sizes for attacks

### Deferred Ideas (OUT OF SCOPE)
- Mana system (v2)
- Combo system (v2+)
- Dodge roll / evade (v2+)
- Parry / shield block (v2+)
- Screenshake and hitstop (v2+)
- Real enemy sprites (art pass)
- Additional spells beyond Fireball (v2+)
- Magic damage scaling with equipment (v2+)
- A* pathfinding for enemies (v2+)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CMB-01 | Melee directional sword attack with knockback | D-01 through D-06: Rectangle hitbox in facing direction, knockback via position lerp, damage from EquipmentData |
| CMB-02 | Ranged magic projectile with cooldown | D-07 through D-11: Fireball entity with velocity, collision check, 2s cooldown timer, fixed 15 damage |
| CMB-03 | HP system with visible bars for player and enemies | D-12 through D-15: Entity.HP already exists, HUD HP bar using existing sprites, enemy bars rendered in world space |
| CMB-04 | 3 enemy types with distinct AI | D-16 through D-18: Single EnemyEntity class with data-driven stats, different behavior via EnemyType enum |
| CMB-05 | Enemy AI state machine (Idle/Chase/Attack/Return) | D-19: Simple FSM with distance-based transitions, no pathfinding needed |
| CMB-06 | Boss fight with telegraphed attacks and unique loot | D-21 through D-24: BossEntity extending EnemyEntity, two attack patterns, boss HP bar at screen bottom |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Engine**: MonoGame 3.8 DesktopGL -- no engine changes
- **Language**: C# 12 / .NET 8.0
- **Resolution**: 960x540 base
- **Tile size**: 16px (TileMap.TileSize = 16)
- **Naming**: PascalCase methods, _camelCase private fields, [ModuleName] console logging
- **Pattern**: Entity subclass pattern (PlayerEntity, DummyNpc established)
- **Events**: Delegate-based (Action/event) for decoupled communication
- **Error handling**: Boolean returns for failable operations (Try* prefix)
- **Comments**: XML doc comments on all public members

## Standard Stack

### Core (already in project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.* | Game engine -- Rectangle, Vector2, SpriteBatch, Color tinting | Already in project [VERIFIED: csproj] |
| System.Text.Json | built-in | Serialization for enemy/loot data if needed | Already used for saves [VERIFIED: codebase] |

### Supporting (no new packages needed)
This phase requires **zero new NuGet packages**. MonoGame's built-in math (Vector2, Rectangle, MathHelper) and rendering (SpriteBatch, Color) cover all combat needs.

| Built-in | Purpose | Why Sufficient |
|----------|---------|----------------|
| Rectangle.Intersects() | All collision detection (hitbox vs hitbox) | Fast AABB check, standard for 2D tile games [VERIFIED: MonoGame API] |
| Vector2.Distance() | Range checks (detection, attack range, projectile max range) | Already used in ItemDropEntity magnetism [VERIFIED: codebase] |
| MathHelper.Lerp() | Knockback smooth movement | Already used in project [VERIFIED: codebase] |
| Color.White * alpha / Color tint | Hit flash, i-frame blink | SpriteBatch.Draw accepts tint parameter [VERIFIED: codebase] |
| MathHelper.Clamp() | HP clamping, position bounds | Already used extensively [VERIFIED: codebase] |

**No external physics, collision, or AI libraries needed.** [VERIFIED: codebase complexity analysis]

## Architecture Patterns

### Recommended Project Structure
```
Combat/
    CombatManager.cs       # Coordinates attacks, damage, i-frames for player
    MeleeAttack.cs         # Melee hitbox creation, slash visual, cooldown
    Projectile.cs          # Fireball entity (and future projectiles)
    ProjectileManager.cs   # Manages active projectiles lifecycle
Enemies/
    EnemyEntity.cs         # Single enemy class with data-driven stats
    EnemyData.cs           # Static enemy type definitions (like CropData pattern)
    EnemySpawner.cs        # Manages spawn positions, respawn on day advance
    EnemyAI.cs             # FSM logic (Idle/Chase/Attack/Return)
    BossEntity.cs          # Boss-specific behavior extending EnemyEntity
    LootTable.cs           # Drop definitions per enemy type
UI/
    (modify existing HUD.cs)  # Add player HP bar, magic cooldown indicator
    EnemyHealthBar.cs      # World-space HP bar drawn above enemies
    BossHealthBar.cs       # Screen-space boss HP bar at bottom
```

### Pattern 1: Data-Driven Enemy Types (recommended over inheritance)
**What:** Single `EnemyEntity` class parameterized by `EnemyData` record, similar to how `CropInstance` wraps `CropData`. [ASSUMED]
**When to use:** All three normal enemy types (Skeleton, Dark Mage, Golem)
**Why over inheritance:** Three enemy types differ only in stats and AI parameters (speeds, ranges, damage), not in fundamental behavior. A single class with different data avoids class explosion. Boss IS different enough to warrant its own subclass.
```csharp
// Source: pattern derived from existing CropData/CropInstance [VERIFIED: codebase]
public record EnemyData(
    string Id,
    string Name,
    Color PlaceholderColor,
    int Width,
    int Height,
    float MaxHP,
    float MoveSpeed,
    float DetectionRange,
    float AttackRange,
    float AttackDamage,
    float AttackCooldown,
    float KnockbackResistance,  // 0 = full knockback, 1 = immune
    bool IsRanged,
    float ProjectileSpeed,      // only for ranged
    LootTable Loot
);
```

### Pattern 2: Simple FSM for Enemy AI
**What:** Enum-based state machine with distance-based transitions. No abstract State classes needed for 4 simple states. [ASSUMED]
**When to use:** All enemies including boss
```csharp
// Source: standard game AI pattern [ASSUMED]
public enum EnemyState { Idle, Chase, Attack, Return }

// In EnemyEntity.Update():
float distToPlayer = Vector2.Distance(Position, playerPos);
_state = _state switch
{
    EnemyState.Idle when distToPlayer < _data.DetectionRange => EnemyState.Chase,
    EnemyState.Chase when distToPlayer < _data.AttackRange => EnemyState.Attack,
    EnemyState.Chase when distToPlayer > _data.DetectionRange * 1.5f => EnemyState.Return,
    EnemyState.Attack when distToPlayer > _data.AttackRange * 1.2f => EnemyState.Chase,
    EnemyState.Return when Vector2.Distance(Position, _spawnPos) < 4f => EnemyState.Idle,
    _ => _state
};
```

### Pattern 3: Rectangle Hitbox for Melee Attack
**What:** Create a temporary Rectangle in the player's facing direction, check intersection with all enemy collision boxes. [ASSUMED]
**When to use:** Every melee attack (LMB)
```csharp
// Source: standard MonoGame collision pattern [ASSUMED]
public Rectangle GetMeleeHitbox(Vector2 playerPos, Direction facing)
{
    // ~3 tiles wide (48px) x 1.5 tiles deep (24px) arc approximation
    int w = 48, h = 24;
    return facing switch
    {
        Direction.Up    => new Rectangle((int)playerPos.X - w/2, (int)playerPos.Y - h - 8, w, h),
        Direction.Down  => new Rectangle((int)playerPos.X - w/2, (int)playerPos.Y + 8, w, h),
        Direction.Left  => new Rectangle((int)playerPos.X - h - 8, (int)playerPos.Y - w/2, h, w),
        Direction.Right => new Rectangle((int)playerPos.X + 8, (int)playerPos.Y - w/2, h, w),
        _ => Rectangle.Empty
    };
}
```

### Pattern 4: Projectile as Lightweight Entity
**What:** Projectile class with position, velocity, lifetime, collision check. Managed by ProjectileManager (list with removal). [ASSUMED]
**When to use:** Fireball, enemy mage projectiles
```csharp
// Source: pattern from ItemDropEntity lifecycle [VERIFIED: codebase]
public class Projectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Damage;
    public float MaxDistance;
    public float DistanceTraveled;
    public bool IsExpired;
    public bool IsPlayerOwned; // true = damages enemies, false = damages player
    public Rectangle Hitbox => new((int)Position.X - 4, (int)Position.Y - 4, 8, 8);
}
```

### Pattern 5: Knockback via Position Lerp (not physics)
**What:** When hit, set a knockback target position and lerp toward it over ~0.2s. No velocity-based physics. [ASSUMED]
**When to use:** All enemy knockback from player attacks
```csharp
// Per D-02: simple lerp, no physics
private Vector2 _knockbackTarget;
private float _knockbackTimer;
private const float KnockbackDuration = 0.2f;

public void ApplyKnockback(Vector2 direction, float distance)
{
    _knockbackTarget = Position + direction * distance;
    _knockbackTimer = KnockbackDuration;
}

// In Update:
if (_knockbackTimer > 0)
{
    _knockbackTimer -= deltaTime;
    float t = 1f - (_knockbackTimer / KnockbackDuration);
    Position = Vector2.Lerp(Position, _knockbackTarget, t);
}
```

### Pattern 6: Tint Flash for Hit Feedback
**What:** When damaged, set a flash timer. During flash, draw with `Color.White` (full white tint) instead of normal color. [VERIFIED: MonoGame SpriteBatch API]
**When to use:** Enemy hit feedback (D-05), also works for player damage
```csharp
// Source: standard MonoGame tint technique [VERIFIED: MonoGame API]
// SpriteBatch.Draw already accepts Color parameter
Color drawColor = _flashTimer > 0 ? Color.White : _normalColor;
spriteBatch.Draw(SpriteSheet, destRect, srcRect, drawColor);
```
Note: For placeholder rectangles (colored rects), "flash white" means temporarily drawing the rect as white instead of its normal color.

### Pattern 7: I-Frames via Timer
**What:** After player takes damage, set invulnerability timer to 1.0s. During i-frames, skip damage application and blink sprite (toggle visibility every 0.1s). [ASSUMED]
```csharp
private float _iFrameTimer;
private const float IFrameDuration = 1.0f;

public bool TryTakeDamage(float amount)
{
    if (_iFrameTimer > 0) return false;
    HP = MathHelper.Clamp(HP - amount, 0, MaxHP);
    _iFrameTimer = IFrameDuration;
    return true;
}

// In Draw: blink effect
bool visible = _iFrameTimer <= 0 || ((int)(_iFrameTimer * 10) % 2 == 0);
if (visible) { /* draw normally */ }
```

### Anti-Patterns to Avoid
- **Over-engineering the FSM:** Do NOT use abstract State classes, State Pattern with entry/exit methods, or a generic FSM framework. Four states with distance checks in a switch is sufficient. [ASSUMED]
- **Physics-based knockback:** Do NOT add a physics system or use Entity.Velocity for knockback with deceleration. Lerp to target position is simpler and matches Stardew Valley feel. [ASSUMED]
- **Separate scene for combat:** Do NOT create a CombatScene. Combat happens in FarmScene (and later DungeonScene). Enemies are entities managed in the scene like item drops. [VERIFIED: CONTEXT.md D-20]
- **Complex hitbox shapes:** Do NOT implement circle or polygon collision. Rectangle AABB is standard for this type of game and the decision context allows either approach. Rectangles are simpler and MonoGame has built-in support. [ASSUMED]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| AABB collision | Custom intersection math | `Rectangle.Intersects()` | Built into MonoGame, handles edge cases [VERIFIED: MonoGame API] |
| Distance checks | Manual Pythagorean | `Vector2.Distance()` | Built-in, optimized [VERIFIED: MonoGame API] |
| Smooth interpolation | Custom easing functions | `MathHelper.Lerp()`, `Vector2.Lerp()` | Built-in [VERIFIED: MonoGame API] |
| HP clamping | Manual min/max | `MathHelper.Clamp()` | Already used throughout codebase [VERIFIED: codebase] |
| Direction vectors | Switch with manual x/y | Helper method returning unit Vector2 per Direction | Create once, reuse everywhere |
| Timer management | Complex timer classes | Simple `float` fields with deltaTime subtraction | Pattern already established (AnimationTimer, PaceTimer in DummyNpc) [VERIFIED: codebase] |

**Key insight:** MonoGame's `Microsoft.Xna.Framework` namespace provides all the math primitives needed for 2D combat. The game already uses these patterns for farming and item drops.

## Common Pitfalls

### Pitfall 1: Melee Hitting Multiple Times Per Swing
**What goes wrong:** A single sword swing damages the same enemy every frame for the attack duration (~0.3s at 60fps = 18 hits).
**Why it happens:** Hitbox check runs every frame while attack is active.
**How to avoid:** Track which enemies were hit during the current swing in a HashSet. Clear the set when a new attack starts. Only apply damage on first contact per enemy per swing.
**Warning signs:** Enemies dying in one hit regardless of damage values.

### Pitfall 2: Knockback Through Walls
**What goes wrong:** Enemy gets knocked into a wall/collision tile and gets stuck or teleported.
**Why it happens:** Knockback target position is set without checking tile collision.
**How to avoid:** After computing knockback target, validate against TileMap collision. Clamp knockback distance to the nearest valid position. Or simply skip wall-check for v1 since enemies are on the open farm. Flag for Phase 5 dungeon integration.
**Warning signs:** Enemies stuck in walls after being hit.

### Pitfall 3: Player Attacking During Scene Transitions
**What goes wrong:** LMB press triggers melee attack while inventory/pause overlay is open.
**Why it happens:** Input is processed before checking if overlay is active.
**How to avoid:** Check scene state / overlay active flag before processing combat input. FarmScene already handles this for hotbar (returns early when overlay pushed). Apply same pattern to combat input.
**Warning signs:** Hearing attack sounds or seeing slash effects behind menus.

### Pitfall 4: Enemy Update Order Causing Frame Lag
**What goes wrong:** Enemies from list beginning attack first, creating unfair advantage patterns.
**Why it happens:** Sequential list iteration means first enemies in list always process first.
**How to avoid:** For v1 with 3-5 enemies this is negligible. Document as known limitation. If it matters later, shuffle update order.
**Warning signs:** Not visible with < 10 enemies.

### Pitfall 5: Boss HP Threshold Triggers Firing Multiple Times
**What goes wrong:** Boss spawns infinite minions because the "30% HP lost" check fires every frame while HP is in range.
**Why it happens:** Using `currentHP < threshold` without tracking that the threshold was already triggered.
**How to avoid:** Track triggered thresholds with a set/counter. E.g., `_spawnPhase` integer that increments when each threshold is crossed: phase 0 at 300 HP, phase 1 at 210 (70%), phase 2 at 120 (40%). Only spawn minions when `_spawnPhase` advances.
**Warning signs:** Dozens of skeletons spawning when boss is at a threshold boundary.

### Pitfall 6: Projectile Not Despawning at Map Edges
**What goes wrong:** Fireball travels forever if it doesn't hit anything before map boundary.
**Why it happens:** Only checking max distance, not map bounds.
**How to avoid:** Check both `DistanceTraveled >= MaxDistance` AND position outside map bounds. Also despawn if hitting tile collision.
**Warning signs:** Projectiles accumulating at map edges, performance degradation.

### Pitfall 7: LMB Conflict with ToolController
**What goes wrong:** LMB both triggers melee attack AND farming tool action.
**Why it happens:** ToolController currently uses E key for actions, but adding LMB combat creates input conflict.
**How to avoid:** When a weapon is equipped on active hotbar slot, LMB = melee attack (combat). When no weapon equipped (or farming tool selected), LMB = existing farming tool action. Check active hotbar item type to determine LMB behavior. This is a critical integration point.
**Warning signs:** Tilling soil while trying to attack, or attacking while trying to farm.

## Code Examples

### Example 1: Direction to Vector2 Helper
```csharp
// Utility method needed throughout combat system
// Source: derived from existing FacingDirection usage [VERIFIED: codebase]
public static Vector2 DirectionToVector(Direction dir) => dir switch
{
    Direction.Up    => new Vector2(0, -1),
    Direction.Down  => new Vector2(0, 1),
    Direction.Left  => new Vector2(-1, 0),
    Direction.Right => new Vector2(1, 0),
    _ => Vector2.Zero
};
```

### Example 2: Damage Calculation
```csharp
// Per D-06: weapon.damage + equipment_attack_bonus - enemy_defense, minimum 1
// Source: CONTEXT.md D-06 + existing EquipmentData [VERIFIED: codebase]
public static float CalculateDamage(InventoryManager inventory, float enemyDefense)
{
    var (attack, _) = EquipmentData.GetEquipmentStats(inventory.GetAllEquipment());

    // Get active weapon damage from hotbar
    var activeItem = inventory.GetActiveHotbarItem();
    float weaponDmg = 0;
    if (activeItem != null)
    {
        var def = ItemRegistry.Get(activeItem.ItemId);
        if (def?.Stats.TryGetValue("damage", out float d) == true)
            weaponDmg = d;
    }

    float totalAttack = weaponDmg + attack; // weapon base + equipment bonus
    float damage = Math.Max(1, totalAttack - enemyDefense);
    return damage;
}
```

### Example 3: Enemy HP Bar in World Space
```csharp
// Per D-13: bar above enemy, visible when HP < MaxHP
// Source: pattern from HUD.cs DrawRect [VERIFIED: codebase]
public static void DrawEnemyHPBar(SpriteBatch sb, Texture2D pixel,
    Vector2 enemyPos, int spriteHeight, float hp, float maxHp)
{
    if (hp >= maxHp) return; // hidden at full HP

    int barWidth = 24;
    int barHeight = 3;
    int x = (int)enemyPos.X - barWidth / 2;
    int y = (int)enemyPos.Y - spriteHeight / 2 - 6;

    // Background (gray)
    sb.Draw(pixel, new Rectangle(x, y, barWidth, barHeight), Color.Gray);
    // Fill (red)
    float fill = hp / maxHp;
    sb.Draw(pixel, new Rectangle(x, y, (int)(barWidth * fill), barHeight), Color.Red);
}
```

### Example 4: Integrating Combat into FarmScene Update
```csharp
// Pattern: manage enemies like item drops (list with removal)
// Source: existing FarmScene item drop pattern [VERIFIED: codebase]
private readonly List<EnemyEntity> _enemies = new();

// In Update:
for (int i = _enemies.Count - 1; i >= 0; i--)
{
    _enemies[i].Update(deltaTime, _player.Position);

    // Check if enemy attacks player
    if (_enemies[i].CanAttackPlayer(_player))
        CombatManager.ApplyEnemyDamage(_player, _enemies[i]);

    // Remove dead enemies
    if (!_enemies[i].IsAlive)
    {
        SpawnEnemyLoot(_enemies[i]);
        _enemies.RemoveAt(i);
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate physics library (Box2D ports) | Built-in Rectangle/Vector2 for simple 2D games | Always for MonoGame | No external dependency needed |
| Complex ECS for entity management | Simple OOP Entity hierarchy | Project convention | Matches existing DummyNpc/PlayerEntity/ItemDropEntity pattern |
| Velocity-based knockback with decay | Position lerp knockback | Common in Stardew-style games | Simpler, more predictable, matches D-02 |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Single EnemyEntity class with data-driven stats is better than 3 subclasses | Architecture Pattern 1 | LOW -- can refactor to subclasses if behavior diverges significantly |
| A2 | Enum-based FSM is sufficient (no State Pattern framework) | Architecture Pattern 2 | LOW -- 4 states is trivial to manage with switch |
| A3 | Rectangle hitbox is sufficient for melee arc approximation | Architecture Pattern 3 | LOW -- exact arc detection is a polish item, rectangles feel fine in Stardew |
| A4 | Knockback via lerp (not velocity) matches Stardew feel | Architecture Pattern 5 | LOW -- lerp is simpler and the user explicitly specified "set position directly with lerp" |
| A5 | No wall collision check needed for knockback in v1 farm area | Pitfall 2 | MEDIUM -- enemies could clip through decorative tiles on farm map, flag for Phase 5 |
| A6 | LMB context switching (combat vs farming) based on active hotbar item type | Pitfall 7 | LOW -- user decisions specify LMB=melee and E=farm interaction, but integration needs care |

## Open Questions

1. **Loot item IDs for enemy drops**
   - What we know: D-16 says "bones", D-17 says "mana crystal", D-18 says "stone". These items don't exist in items.json yet.
   - What's unclear: Should new item definitions be added to items.json, or use placeholder strings that get defined in Phase 6 (progression)?
   - Recommendation: Add minimal item definitions to items.json now (type: "Loot", stackable) so drops work end-to-end. Gold drops can be a quantity added to a future gold counter.

2. **ToolController LMB integration**
   - What we know: ToolController currently uses E for all farming actions. Combat uses LMB for melee.
   - What's unclear: Does ToolController need modification, or does combat input bypass it entirely?
   - Recommendation: Combat input (LMB/RMB) is handled by CombatManager, not ToolController. ToolController continues handling E for farming. No conflict since different keys. The only edge case is if future phases add LMB to farming tools.

3. **Enemy spawn positions on farm map**
   - What we know: D-20 says 3-5 enemies at hardcoded positions. Farm map is `test_farm.tmx`.
   - What's unclear: Which coordinates are safe (not in farm zone, not in collision tiles)?
   - Recommendation: Define spawn points as Vector2 constants. Choose positions away from the farm zone by inspecting the map. Alternatively, add an "enemy_spawn" object layer in Tiled.

4. **Boss first-kill tracking**
   - What we know: D-23 says Flame_Blade drops "guaranteed on first kill."
   - What's unclear: How to track "first kill" across sessions. GameState doesn't have boss kill flags yet.
   - Recommendation: Add a `BossKilled` boolean to GameState. Simple and sufficient for v1 single boss.

## Environment Availability

Step 2.6: SKIPPED (no external dependencies identified). This phase is purely code/config changes using existing MonoGame stack.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual gameplay testing (no automated test framework detected in project) |
| Config file | none |
| Quick run command | `dotnet run` + manual testing |
| Full suite command | `dotnet build` (compile check) + manual gameplay |

Note: The project has no pytest/xunit/nunit configuration. CLAUDE.md for the Sentinel project mentions pytest but that is a different project. This MonoGame game has no automated test infrastructure. All validation is manual gameplay testing.

### Phase Requirements Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CMB-01 | LMB swings sword, enemy takes damage and gets knocked back | manual | `dotnet run` + attack enemy | N/A |
| CMB-02 | RMB fires fireball, travels in facing direction, damages enemy | manual | `dotnet run` + cast spell | N/A |
| CMB-03 | Player HP bar on HUD, enemy HP bars above enemies | manual | `dotnet run` + visual check | N/A |
| CMB-04 | Skeleton rushes, Mage shoots from distance, Golem is slow/tanky | manual | `dotnet run` + observe behaviors | N/A |
| CMB-05 | Enemies idle, chase when near, attack in range, return when far | manual | `dotnet run` + approach/retreat from enemies | N/A |
| CMB-06 | Boss telegraphs attacks (red flash), summons minions, drops Flame_Blade | manual | `dotnet run` + boss fight | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (must compile)
- **Per wave merge:** `dotnet run` + manual gameplay verification of new features
- **Phase gate:** Full manual playthrough of all 6 success criteria

### Wave 0 Gaps
- None -- no automated test infrastructure to set up. Manual testing is the validation approach for this game project.

## Security Domain

> Skipped. This is a single-player local game with no network, auth, or data storage beyond local save files. No ASVS categories apply.

## Sources

### Primary (HIGH confidence)
- **Codebase files verified:** Entity.cs, PlayerEntity.cs, InputManager.cs, FarmScene.cs, DummyNpc.cs, ItemDropEntity.cs, InventoryManager.cs, EquipmentData.cs, HUD.cs, items.json, ServiceContainer.cs, Scene.cs, ToolController.cs, PlayerStats.cs
- **MonoGame API:** Rectangle.Intersects, Vector2.Distance, MathHelper.Lerp/Clamp, SpriteBatch.Draw Color tint parameter -- all verified as existing MonoGame 3.8 API via codebase usage
- **Asset verification:** UI_StatusBar_Fill_HP.png, UI_StatusBar_Bg.png, UI_Icon_HP.png confirmed present in Content/Sprites/System/UI Elements/Bars/

### Secondary (MEDIUM confidence)
- Combat design patterns (FSM, hitbox, knockback, i-frames) are standard 2D game development approaches widely documented

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- no new dependencies, all MonoGame built-in verified in codebase
- Architecture: HIGH -- patterns directly extend existing Entity/Scene/ItemDrop patterns in codebase
- Pitfalls: HIGH -- based on common 2D combat implementation issues, all applicable to this specific codebase
- Enemy AI: HIGH -- simple FSM with 4 states, well-understood pattern
- Integration points: HIGH -- FarmScene, InputManager, EquipmentData all inspected and understood

**Research date:** 2026-04-11
**Valid until:** 2026-05-11 (stable -- MonoGame 3.8 is mature, no breaking changes expected)
