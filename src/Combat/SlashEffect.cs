using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Visual overlay for melee sword swing. Draws a simple rectangle in the attack
/// direction that fades over the swing duration.
/// Per D-03: 2-3 frames over 0.3s, "pode ser sprite basico gerado por codigo".
/// </summary>
public class SlashEffect
{
    private float _timer;
    private const float Duration = 0.3f;
    private bool _active;
    private Direction _direction;
    private Vector2 _playerPos;

    /// <summary>True while the slash visual is being rendered.</summary>
    public bool IsActive => _active;

    /// <summary>
    /// Start the slash effect at the player's position in the given direction.
    /// </summary>
    /// <param name="playerPos">Player center position.</param>
    /// <param name="facing">Direction of the attack.</param>
    public void Trigger(Vector2 playerPos, Direction facing)
    {
        _playerPos = playerPos;
        _direction = facing;
        _timer = Duration;
        _active = true;
    }

    /// <summary>
    /// Update the slash timer.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        if (!_active) return;

        _timer -= deltaTime;
        if (_timer <= 0)
            _active = false;
    }

    /// <summary>
    /// Draw the slash visual overlay. First half of duration draws larger,
    /// second half draws smaller (fade effect).
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in world-space.</param>
    /// <param name="pixel">1x1 white texture for drawing.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!_active) return;

        float progress = 1f - (_timer / Duration);
        // First half: larger (expanding), second half: smaller (fading)
        float scale = progress < 0.5f ? 1f : 1f - (progress - 0.5f) * 2f;
        float alpha = 0.6f * scale;

        if (alpha <= 0.01f) return;

        // Base hitbox dimensions matching MeleeAttack.GetHitbox
        int baseWidth = (int)(34 * scale);
        int baseDepth = (int)(24 * scale);
        if (baseWidth < 2 || baseDepth < 2) return;

        Rectangle rect = _direction switch
        {
            Direction.Up => new Rectangle(
                (int)_playerPos.X - baseWidth / 2,
                (int)_playerPos.Y - baseDepth + 2,
                baseWidth, baseDepth),
            Direction.Down => new Rectangle(
                (int)_playerPos.X - baseWidth / 2,
                (int)_playerPos.Y + 8,
                baseWidth, baseDepth),
            Direction.Left => new Rectangle(
                (int)_playerPos.X - baseDepth - 4,
                (int)_playerPos.Y - baseWidth / 2,
                baseDepth, baseWidth),
            Direction.Right => new Rectangle(
                (int)_playerPos.X + 4,
                (int)_playerPos.Y - baseWidth / 2,
                baseDepth, baseWidth),
            _ => Rectangle.Empty
        };

        if (rect == Rectangle.Empty) return;

        spriteBatch.Draw(pixel, rect, Color.White * alpha);
    }
}
