using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Manages all active projectiles: spawning, movement, collision detection, and rendering.
/// Handles both player-owned (fireballs) and enemy-owned projectiles.
/// </summary>
public class ProjectileManager
{
    private readonly List<Projectile> _projectiles = new();

    /// <summary>Read-only access to active projectiles.</summary>
    public IReadOnlyList<Projectile> Active => _projectiles;

    /// <summary>
    /// Callback invoked when an enemy projectile hits the player.
    /// Parameters: (float damage). CombatManager hooks this to apply i-frame logic.
    /// </summary>
    public Action<float>? OnPlayerHit;

    /// <summary>
    /// Spawn a player fireball in the facing direction.
    /// Per D-07: 200px/s speed, 300px max range.
    /// Per D-11: 15 fixed damage.
    /// </summary>
    /// <param name="origin">Player position (spawn point).</param>
    /// <param name="facing">Direction to fire.</param>
    public void SpawnFireball(Vector2 origin, Direction facing)
    {
        var dir = Entity.DirectionToVector(facing);
        var velocity = dir * 200f; // per D-07: 200px/s
        var proj = new Projectile(origin, velocity, 15f, 300f, true, Color.Orange);
        _projectiles.Add(proj);
        Console.WriteLine($"[ProjectileManager] Fireball spawned at {origin}, dir={facing}");
    }

    /// <summary>
    /// Spawn an enemy projectile aimed at a target position.
    /// Used by Dark Mage and other ranged enemies.
    /// </summary>
    /// <param name="origin">Enemy position (spawn point).</param>
    /// <param name="targetPos">Target world position to aim at.</param>
    /// <param name="speed">Projectile speed in pixels/second.</param>
    /// <param name="damage">Damage on hit.</param>
    public void SpawnEnemyProjectile(Vector2 origin, Vector2 targetPos, float speed, float damage)
    {
        var diff = targetPos - origin;
        if (diff == Vector2.Zero) return;

        var direction = Vector2.Normalize(diff);
        var velocity = direction * speed;
        var proj = new Projectile(origin, velocity, damage, 400f, false, Color.Purple);
        _projectiles.Add(proj);
    }

    /// <summary>
    /// Update all projectiles: move, check collisions, remove expired.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    /// <param name="enemies">List of enemy entities for player-projectile collision.</param>
    /// <param name="player">Player entity for enemy-projectile collision.</param>
    public void Update(float deltaTime, IReadOnlyList<Entity> enemies, PlayerEntity player)
    {
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var proj = _projectiles[i];
            proj.Update(deltaTime);

            if (proj.IsExpired)
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            if (proj.IsPlayerOwned)
            {
                // Check collision with enemies
                for (int e = 0; e < enemies.Count; e++)
                {
                    var enemy = enemies[e];
                    if (!enemy.IsAlive) continue;

                    if (proj.Hitbox.Intersects(enemy.CollisionBox))
                    {
                        enemy.TakeDamage(proj.Damage);
                        // Knockback away from projectile origin
                        var knockDir = enemy.Position - proj.Position;
                        if (knockDir != Vector2.Zero)
                            knockDir.Normalize();
                        enemy.ApplyKnockback(knockDir, 16f);
                        proj.IsExpired = true;
                        Console.WriteLine($"[ProjectileManager] Fireball hit enemy for {proj.Damage} damage");
                        break;
                    }
                }
            }
            else
            {
                // Enemy projectile: check collision with player
                if (player.IsAlive && proj.Hitbox.Intersects(player.CollisionBox))
                {
                    OnPlayerHit?.Invoke(proj.Damage);
                    proj.IsExpired = true;
                }
            }

            // Remove if expired from collision
            if (proj.IsExpired)
                _projectiles.RemoveAt(i);
        }
    }

    /// <summary>
    /// Draw all active projectiles as simple filled rectangles.
    /// Per D-10: 8x8 placeholder visual.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in world-space.</param>
    /// <param name="pixel">1x1 white texture for drawing.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var proj in _projectiles)
        {
            spriteBatch.Draw(pixel, proj.Hitbox, proj.Color);
        }
    }
}
