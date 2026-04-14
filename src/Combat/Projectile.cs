using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Lightweight projectile data class representing a single projectile in flight.
/// Per D-07/D-10: tracks position, velocity, damage, distance traveled, and ownership.
/// </summary>
public class Projectile
{
    /// <summary>Current world position of the projectile.</summary>
    public Vector2 Position;

    /// <summary>Velocity vector (direction * speed) in pixels/second.</summary>
    public Vector2 Velocity;

    /// <summary>Damage dealt on hit.</summary>
    public float Damage;

    /// <summary>Maximum travel distance in pixels before expiring (per D-07: 300px for fireball).</summary>
    public float MaxDistance;

    /// <summary>Total distance traveled so far.</summary>
    public float DistanceTraveled;

    /// <summary>True when the projectile should be removed.</summary>
    public bool IsExpired;

    /// <summary>True if owned by player (damages enemies), false if enemy-owned (damages player).</summary>
    public bool IsPlayerOwned;

    /// <summary>Render color (orange for player fireball, purple for mage projectile).</summary>
    public Color Color;

    /// <summary>Collision hitbox centered on position (8x8 per D-10).</summary>
    public Rectangle Hitbox => new Rectangle(
        (int)Position.X - 4, (int)Position.Y - 4, 8, 8);

    /// <summary>
    /// Create a new projectile.
    /// </summary>
    /// <param name="startPos">Starting world position.</param>
    /// <param name="velocity">Direction * speed vector.</param>
    /// <param name="damage">Damage on hit.</param>
    /// <param name="maxDistance">Max travel distance before expiring.</param>
    /// <param name="isPlayerOwned">True if player-owned.</param>
    /// <param name="color">Render color.</param>
    public Projectile(Vector2 startPos, Vector2 velocity, float damage,
        float maxDistance, bool isPlayerOwned, Color color)
    {
        Position = startPos;
        Velocity = velocity;
        Damage = damage;
        MaxDistance = maxDistance;
        IsPlayerOwned = isPlayerOwned;
        Color = color;
        DistanceTraveled = 0f;
        IsExpired = false;
    }

    /// <summary>
    /// Update position and check distance limit.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        DistanceTraveled += Velocity.Length() * deltaTime;

        if (DistanceTraveled >= MaxDistance)
            IsExpired = true;
    }
}
