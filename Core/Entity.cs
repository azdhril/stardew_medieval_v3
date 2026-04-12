using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Abstract base class for all game entities (player, enemies, NPCs, item drops).
/// Provides position, movement, HP, animation, collision, and combat fundamentals.
/// </summary>
public abstract class Entity
{
    // Position & movement
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Direction FacingDirection { get; set; } = Direction.Down;

    // HP (per D-06: HP/IsAlive in base for enemies/NPCs in Phases 3-5)
    public float HP { get; set; } = 100f;
    public float MaxHP { get; set; } = 100f;
    public bool IsAlive => HP > 0;

    // Defense stat for damage calculation
    public float Defense { get; set; } = 0f;

    // Knockback fields (per D-02: lerp, no physics)
    private Vector2 _knockbackTarget;
    private float _knockbackTimer;
    private const float KnockbackDuration = 0.2f;

    // Flash fields for hit feedback
    protected float _flashTimer;
    protected const float FlashDuration = 0.1f;

    /// <summary>True when the entity is flashing from a recent hit.</summary>
    public bool IsFlashing => _flashTimer > 0;

    // Animation (per D-06: SpriteSheet, FrameIndex, AnimationTimer)
    protected Texture2D? SpriteSheet { get; set; }
    protected int FrameWidth { get; set; }
    protected int FrameHeight { get; set; }
    protected int FrameIndex { get; set; }
    protected float AnimationTimer { get; set; }
    protected float FrameTime { get; set; } = 0.15f;

    /// <summary>
    /// Center point of the collision box. Use this for targeting instead of Position,
    /// which is the sprite center (much higher than the feet-level collision box).
    /// </summary>
    public Vector2 CollisionCenter
    {
        get
        {
            var box = CollisionBox;
            return new Vector2(box.X + box.Width / 2f, box.Y + box.Height / 2f);
        }
    }

    /// <summary>
    /// Full-body hitbox covering the entire sprite. Used for combat damage checks
    /// (enemy attacks, projectiles hitting the player). The player should need to
    /// dodge with their whole body, not just their feet.
    /// </summary>
    public virtual Rectangle HitBox
    {
        get
        {
            int fw = FrameWidth > 0 ? FrameWidth : 16;
            int fh = FrameHeight > 0 ? FrameHeight : 16;
            int w = (int)(fw * 0.3f);  // 30% of frame width (narrower)
            int h = (int)(fh * 0.7f);
            return new Rectangle(
                (int)Position.X - w / 2,
                (int)(Position.Y + fh * 0.05f) - h / 2,  // shifted down 20%
                w, h);
        }
    }

    /// <summary>
    /// Collision box at the entity's feet. Used for movement/terrain collision only.
    /// </summary>
    public virtual Rectangle CollisionBox
    {
        get
        {
            int w = 10, h = 6;
            int fh = FrameHeight > 0 ? FrameHeight : 16;
            return new Rectangle(
                (int)Position.X - w / 2,
                (int)Position.Y + fh / 2 - h - 2 - (int)(fh * 0.05f),
                w, h);
        }
    }

    /// <summary>
    /// Update entity state. Override for entity-specific logic.
    /// </summary>
    public virtual void Update(float deltaTime) { }

    /// <summary>
    /// Draw the entity. Override for entity-specific rendering.
    /// </summary>
    public virtual void Draw(SpriteBatch spriteBatch) { }

    /// <summary>
    /// Apply damage to this entity. Reduces HP, triggers flash.
    /// Per D-06: minimum 1 damage if amount is greater than 0.
    /// </summary>
    /// <param name="amount">Raw damage amount.</param>
    /// <returns>True if damage was applied.</returns>
    public virtual bool TakeDamage(float amount)
    {
        if (amount <= 0 || !IsAlive) return false;

        // Minimum 1 damage if amount > 0 (per D-06)
        float finalDamage = Math.Max(1f, amount);
        HP = Math.Max(0f, HP - finalDamage);
        _flashTimer = FlashDuration;
        return true;
    }

    /// <summary>
    /// Apply knockback: lerp toward a target position over KnockbackDuration.
    /// Per D-02: lerp-based, no physics simulation.
    /// </summary>
    /// <param name="direction">Normalized direction of knockback.</param>
    /// <param name="distance">Distance in pixels to knock back.</param>
    public void ApplyKnockback(Vector2 direction, float distance)
    {
        if (direction != Vector2.Zero)
            direction.Normalize();
        _knockbackTarget = Position + direction * distance;
        _knockbackTimer = KnockbackDuration;
    }

    /// <summary>
    /// Update knockback lerp. Call from subclass Update methods.
    /// </summary>
    protected void UpdateKnockback(float deltaTime)
    {
        if (_knockbackTimer > 0)
        {
            _knockbackTimer -= deltaTime;
            float t = 1f - Math.Max(0f, _knockbackTimer / KnockbackDuration);
            Position = Vector2.Lerp(Position, _knockbackTarget, t);
        }
    }

    /// <summary>
    /// Update flash timer. Call from subclass Update methods.
    /// </summary>
    protected void UpdateFlash(float deltaTime)
    {
        if (_flashTimer > 0)
            _flashTimer -= deltaTime;
    }

    /// <summary>
    /// Convert a Direction enum to a unit Vector2.
    /// </summary>
    public static Vector2 DirectionToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up => new Vector2(0, -1),
            Direction.Down => new Vector2(0, 1),
            Direction.Left => new Vector2(-1, 0),
            Direction.Right => new Vector2(1, 0),
            _ => Vector2.Zero
        };
    }
}
