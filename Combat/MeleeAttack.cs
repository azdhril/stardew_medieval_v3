using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Handles melee sword swing: hitbox generation, cooldown, swing timer, and hit tracking.
/// Prevents multi-hit per swing via HashSet (per Research Pitfall 1).
/// </summary>
public class MeleeAttack
{
    private float _cooldownTimer;
    private float _swingTimer;
    private const float SwingDuration = 0.3f; // per D-03
    private bool _isSwinging;
    private readonly HashSet<Entity> _hitEntities = new();

    /// <summary>True while the swing animation is active.</summary>
    public bool IsSwinging => _isSwinging;

    /// <summary>Progress of the current swing (0 to 1). Used for visual effects.</summary>
    public float SwingProgress => _swingTimer > 0 ? 1f - (_swingTimer / SwingDuration) : 0f;

    /// <summary>
    /// Attempt to start a melee swing. Fails if on cooldown or already swinging.
    /// Per D-04: cooldowns vary by weapon (Iron_Sword=0.5s, Steel_Sword=0.4s, Flame_Blade=0.35s).
    /// </summary>
    /// <param name="weaponCooldown">Cooldown duration in seconds for the equipped weapon.</param>
    /// <returns>True if swing started successfully.</returns>
    public bool TrySwing(float weaponCooldown)
    {
        if (_cooldownTimer > 0 || _isSwinging)
            return false;

        _isSwinging = true;
        _swingTimer = SwingDuration;
        _cooldownTimer = weaponCooldown;
        _hitEntities.Clear();
        return true;
    }

    /// <summary>
    /// Get the melee hitbox rectangle for the current swing.
    /// Per D-01: approximately 48px wide x 24px deep in the facing direction (3-tile 90-degree arc).
    /// </summary>
    /// <param name="playerPos">Player center position.</param>
    /// <param name="facing">Player facing direction.</param>
    /// <returns>Rectangle representing the attack hitbox in world space.</returns>
    public Rectangle GetHitbox(Vector2 playerPos, Direction facing)
    {
        // Hitbox: narrowed 30% from original 48px
        const int width = 34;
        const int depth = 24;

        return facing switch
        {
            Direction.Up => new Rectangle(
                (int)playerPos.X - width / 2,
                (int)playerPos.Y - depth + 2,   // shifted down ~30%
                width, depth),
            Direction.Down => new Rectangle(
                (int)playerPos.X - width / 2,
                (int)playerPos.Y + 8,
                width, depth),
            Direction.Left => new Rectangle(
                (int)playerPos.X - depth - 4,    // closer to player (~10%)
                (int)playerPos.Y - width / 2,
                depth, width),
            Direction.Right => new Rectangle(
                (int)playerPos.X + 4,            // closer to player (~10%)
                (int)playerPos.Y - width / 2,
                depth, width),
            _ => Rectangle.Empty
        };
    }

    /// <summary>
    /// Update cooldown and swing timers.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        if (_cooldownTimer > 0)
            _cooldownTimer -= deltaTime;

        if (_swingTimer > 0)
        {
            _swingTimer -= deltaTime;
            if (_swingTimer <= 0)
                _isSwinging = false;
        }
    }

    /// <summary>
    /// Check if a given entity has already been hit during this swing.
    /// </summary>
    public bool HasHit(Entity e) => _hitEntities.Contains(e);

    /// <summary>
    /// Record that an entity was hit during this swing (prevents multi-hit).
    /// </summary>
    public void RecordHit(Entity e) => _hitEntities.Add(e);
}
