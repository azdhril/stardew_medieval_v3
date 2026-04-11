using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Single enemy class driven by EnemyData stats and EnemyAI FSM behavior.
/// Extends Entity base class for HP, knockback, flash, and collision support.
/// Renders as a colored placeholder rectangle per D-25/D-05.
/// </summary>
public class EnemyEntity : Entity
{
    private readonly EnemyData _data;
    private readonly EnemyAI _ai;
    private bool _meleeAttackReady;
    private Texture2D? _pixel;

    /// <summary>Static enemy type data (stats, loot, visual info).</summary>
    public EnemyData Data => _data;

    /// <summary>AI FSM for state inspection.</summary>
    public EnemyAI AI => _ai;

    /// <summary>True when the enemy has a melee attack ready to deal damage.</summary>
    public bool IsMeleeAttackReady => _meleeAttackReady;

    /// <summary>
    /// Collision box centered on Position with dimensions from EnemyData.
    /// </summary>
    public override Rectangle CollisionBox
    {
        get
        {
            return new Rectangle(
                (int)Position.X - _data.Width / 2,
                (int)Position.Y - _data.Height / 2,
                _data.Width,
                _data.Height);
        }
    }

    /// <summary>
    /// Create a new enemy entity with data-driven stats at the given spawn position.
    /// </summary>
    /// <param name="data">Enemy type definition with all stats.</param>
    /// <param name="spawnPosition">World position to spawn at.</param>
    public EnemyEntity(EnemyData data, Vector2 spawnPosition)
    {
        _data = data;
        _ai = new EnemyAI(spawnPosition);
        Position = spawnPosition;
        HP = data.MaxHP;
        MaxHP = data.MaxHP;
        Defense = 0f;
        FrameWidth = data.Width;
        FrameHeight = data.Height;
    }

    /// <summary>
    /// Update enemy AI, movement, knockback, flash, and attack logic.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    /// <param name="playerPos">Current player world position for AI calculations.</param>
    /// <param name="projectiles">Projectile manager for ranged attack spawning.</param>
    public void Update(float deltaTime, Vector2 playerPos, ProjectileManager projectiles)
    {
        if (!IsAlive) return;

        // Update AI state machine
        _ai.Update(deltaTime, Position, playerPos, _data);

        // Move based on AI direction
        Vector2 moveDir = _ai.GetMoveDirection(Position, playerPos, _data);
        if (moveDir != Vector2.Zero)
        {
            Position += moveDir * _data.MoveSpeed * deltaTime;
        }

        // Update knockback and flash from Entity base
        UpdateKnockback(deltaTime);
        UpdateFlash(deltaTime);

        // Handle attack readiness
        if (_ai.IsAttackReady)
        {
            if (_data.IsRanged)
            {
                // Ranged: spawn enemy projectile toward player
                projectiles.SpawnEnemyProjectile(Position, playerPos, _data.ProjectileSpeed, _data.AttackDamage);
                Console.WriteLine($"[EnemyEntity] {_data.Id} fired projectile at player");
            }
            else
            {
                // Melee: flag for FarmScene to check collision
                _meleeAttackReady = true;
            }
        }
    }

    /// <summary>
    /// Consume the melee attack flag after damage has been applied.
    /// Called by FarmScene after checking collision with player.
    /// </summary>
    public void ConsumeMeleeAttack()
    {
        _meleeAttackReady = false;
    }

    /// <summary>
    /// Apply damage with knockback resistance from EnemyData.
    /// Per D-02/D-18: effective knockback = 32px * (1 - KnockbackResistance).
    /// </summary>
    /// <param name="amount">Raw damage amount.</param>
    /// <returns>True if damage was applied.</returns>
    public override bool TakeDamage(float amount)
    {
        return base.TakeDamage(amount);
    }

    /// <summary>
    /// Apply knockback with resistance scaling.
    /// Per D-18: Golem has 0.75 resistance = only 8px knockback from 32px base.
    /// </summary>
    /// <param name="direction">Knockback direction (normalized).</param>
    /// <param name="baseDistance">Base knockback distance in pixels.</param>
    public void ApplyKnockbackWithResistance(Vector2 direction, float baseDistance)
    {
        float effectiveDistance = baseDistance * (1f - _data.KnockbackResistance);
        if (effectiveDistance > 0.5f)
        {
            ApplyKnockback(direction, effectiveDistance);
        }
    }

    /// <summary>
    /// Draw the enemy as a colored placeholder rectangle.
    /// Per D-25/D-05: uses PlaceholderColor, flashes white on hit.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in world-space.</param>
    /// <param name="pixel">1x1 white texture for drawing rectangles.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsAlive) return;

        _pixel = pixel;
        Color drawColor = IsFlashing ? Color.White : _data.PlaceholderColor;

        var rect = new Rectangle(
            (int)Position.X - _data.Width / 2,
            (int)Position.Y - _data.Height / 2,
            _data.Width,
            _data.Height);

        spriteBatch.Draw(pixel, rect, drawColor);
    }

    /// <summary>
    /// Override base Draw to do nothing (we use the pixel-based Draw overload).
    /// </summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        // Use Draw(spriteBatch, pixel) instead
    }
}
