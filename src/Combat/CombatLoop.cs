using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Per-frame inputs to <see cref="CombatLoop.Update"/>. Carries everything the
/// shared combat tick needs without coupling the helper to a specific scene type.
/// </summary>
public class CombatLoopContext
{
    /// <summary>The active player entity.</summary>
    public required PlayerEntity Player { get; init; }

    /// <summary>Live enemy list. Dead entries are removed in place.</summary>
    public required List<EnemyEntity> Enemies { get; init; }

    /// <summary>Optional boss reference. Cleared (set null) by the scene after death.</summary>
    public BossEntity? Boss { get; set; }

    /// <summary>Per-scene projectile manager.</summary>
    public required ProjectileManager Projectiles { get; init; }

    /// <summary>Per-scene combat manager (owns melee / fireball state).</summary>
    public required CombatManager Combat { get; init; }

    /// <summary>RNG for enemy loot rolls.</summary>
    public required Random LootRng { get; init; }

    /// <summary>Callback to spawn an item drop at a world position. Scene-owned.</summary>
    public required Action<string, int, Vector2> SpawnItemDrop { get; init; }

    /// <summary>Optional callback fired when any enemy (or boss) is killed. Used for XP/gold drops.</summary>
    public Action<EnemyEntity>? OnEnemyKilled { get; init; }

    /// <summary>Optional callback fired once the boss dies (returns boss loot list).</summary>
    public Action<BossEntity>? OnBossDefeated { get; init; }

    /// <summary>If true, boss loot uses the "first kill" guarantee.</summary>
    public bool BossFirstKill { get; init; }

    /// <summary>Optional slash effect trigger (run on melee swing start).</summary>
    public Action<Vector2, Direction>? OnMeleeSwingStart { get; init; }

    /// <summary>TileMap for enemy/boss collision. Null = no collision constraint.</summary>
    public TileMap? Map { get; init; }

    /// <summary>A* pathfinder for enemy navigation. Null = direct line movement.</summary>
    public Pathfinder? Pathfinder { get; init; }
}

/// <summary>
/// Shared combat tick: enemy AI/attack/death, melee hitbox vs enemies and boss,
/// projectile updates, boss telegraph + summon-phase + loot. Extracted from
/// FarmScene.OnPreUpdate so DungeonScene can reuse the exact same logic without
/// drift. Scenes still own their own _enemies/_boss lists and item-drop spawning.
/// </summary>
public static class CombatLoop
{
    /// <summary>
    /// Run one combat frame against the supplied context. Mutates ctx.Enemies
    /// and ctx.Boss in place; spawns drops via ctx.SpawnItemDrop.
    /// </summary>
    public static void Update(float deltaTime, CombatLoopContext ctx)
    {
        // Build the enemy-as-Entity list once for projectile collisions (boss included).
        var enemiesAsEntities = new List<Entity>(ctx.Enemies);
        if (ctx.Boss != null && ctx.Boss.IsAlive)
            enemiesAsEntities.Add(ctx.Boss);
        ctx.Projectiles.Update(deltaTime, enemiesAsEntities, ctx.Player);

        // Player melee hitbox vs enemies
        if (ctx.Combat.Melee.IsSwinging)
        {
            var hitbox = ctx.Combat.Melee.GetHitbox(ctx.Player.Position, ctx.Player.FacingDirection);
            foreach (var enemy in ctx.Enemies)
            {
                if (!enemy.IsAlive) continue;
                if (hitbox.Intersects(enemy.HitBox) && !ctx.Combat.Melee.HasHit(enemy))
                {
                    float damage = ctx.Combat.CalculateMeleeDamage();
                    enemy.TakeDamage(damage);
                    ctx.Combat.OnPlayerMeleeHit(ctx.Player);
                    ctx.Combat.Melee.RecordHit(enemy);

                    var knockDir = enemy.Position - ctx.Player.Position;
                    if (knockDir != Vector2.Zero) knockDir.Normalize();
                    enemy.ApplyKnockbackWithResistance(knockDir, 32f);

                    Console.WriteLine($"[CombatLoop] Melee hit {enemy.Data.Name} for {damage:F0} damage");
                }
            }
        }

        // Enemy AI / attack / death
        for (int i = ctx.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = ctx.Enemies[i];
            enemy.Update(deltaTime, ctx.Player.Position, ctx.Projectiles, ctx.Map, ctx.Pathfinder);

            if (enemy.IsMeleeAttackReady)
            {
                if (enemy.GetMeleeAttackHitbox().Intersects(ctx.Player.HitBox))
                    ctx.Combat.TryPlayerTakeDamage(ctx.Player, enemy.Data.AttackDamage);
                enemy.ConsumeMeleeAttack();
            }

            if (!enemy.IsAlive)
            {
                ctx.OnEnemyKilled?.Invoke(enemy);

                var drops = enemy.Data.Loot.Roll(ctx.LootRng);
                foreach (var (itemId, quantity) in drops)
                {
                    ctx.SpawnItemDrop(itemId, quantity, enemy.Position);
                    Console.WriteLine($"[CombatLoop] {enemy.Data.Name} dropped {quantity}x {itemId}");
                }
                ctx.Enemies.RemoveAt(i);
            }
        }

        // Boss (mirrors FarmScene boss tick — telegraph, summon phases, melee, loot).
        if (ctx.Boss != null && ctx.Boss.IsAlive)
        {
            ctx.Boss.Update(deltaTime, ctx.Player.Position, ctx.Projectiles, ctx.Map, ctx.Pathfinder);

            var minions = ctx.Boss.CheckSummonPhase();
            if (minions != null)
                ctx.Enemies.AddRange(minions);

            if (ctx.Boss.IsBossSlashReady)
            {
                var slashHitbox = ctx.Boss.GetBossSlashHitbox();
                if (slashHitbox.Intersects(ctx.Player.HitBox))
                {
                    ctx.Combat.TryPlayerTakeDamage(ctx.Player, ctx.Boss.Data.AttackDamage);
                    Console.WriteLine("[CombatLoop] Boss slash hit player!");
                }
            }

            if (ctx.Combat.Melee.IsSwinging)
            {
                var hitbox = ctx.Combat.Melee.GetHitbox(ctx.Player.Position, ctx.Player.FacingDirection);
                if (hitbox.Intersects(ctx.Boss.HitBox) && !ctx.Combat.Melee.HasHit(ctx.Boss))
                {
                    float damage = ctx.Combat.CalculateMeleeDamage();
                    ctx.Boss.TakeDamage(damage);
                    ctx.Combat.OnPlayerMeleeHit(ctx.Player);
                    ctx.Combat.Melee.RecordHit(ctx.Boss);

                    var knockDir = ctx.Boss.Position - ctx.Player.Position;
                    if (knockDir != Vector2.Zero) knockDir.Normalize();
                    ctx.Boss.ApplyKnockbackWithResistance(knockDir, 32f);

                    Console.WriteLine($"[CombatLoop] Melee hit Skeleton King for {damage:F0} damage");
                }
            }

            if (!ctx.Boss.IsAlive)
            {
                ctx.OnEnemyKilled?.Invoke(ctx.Boss);

                var bossLoot = ctx.Boss.GetBossLoot(ctx.BossFirstKill == false);
                foreach (var (itemId, quantity) in bossLoot)
                {
                    ctx.SpawnItemDrop(itemId, quantity, ctx.Boss.Position);
                    Console.WriteLine($"[CombatLoop] Skeleton King dropped {quantity}x {itemId}");
                }
                ctx.OnBossDefeated?.Invoke(ctx.Boss);
                ctx.Boss = null;
                Console.WriteLine("[CombatLoop] Skeleton King defeated!");
            }
        }
    }
}
