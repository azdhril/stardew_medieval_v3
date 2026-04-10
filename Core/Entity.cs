using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Abstract base class for all game entities (player, enemies, NPCs, item drops).
/// Provides position, movement, HP, animation, and collision fundamentals.
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

    // Animation (per D-06: SpriteSheet, FrameIndex, AnimationTimer)
    protected Texture2D? SpriteSheet { get; set; }
    protected int FrameWidth { get; set; }
    protected int FrameHeight { get; set; }
    protected int FrameIndex { get; set; }
    protected float AnimationTimer { get; set; }
    protected float FrameTime { get; set; } = 0.15f;

    /// <summary>
    /// Collision box at the entity's feet. Subclasses can override for custom dimensions.
    /// </summary>
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

    /// <summary>
    /// Update entity state. Override for entity-specific logic.
    /// </summary>
    public virtual void Update(float deltaTime) { }

    /// <summary>
    /// Draw the entity. Override for entity-specific rendering.
    /// </summary>
    public virtual void Draw(SpriteBatch spriteBatch) { }
}
