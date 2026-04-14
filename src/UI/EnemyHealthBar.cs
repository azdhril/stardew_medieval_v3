using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// World-space HP bar drawn above enemy sprites.
/// Per D-13: only rendered when HP is below max (hidden at full HP).
/// </summary>
public static class EnemyHealthBar
{
    private const int BarWidth = 24;
    private const int BarHeight = 3;

    /// <summary>
    /// Draw a health bar above an enemy sprite in world space.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in world-space transform.</param>
    /// <param name="pixel">1x1 white texture for drawing.</param>
    /// <param name="enemyPos">Enemy center position in world coordinates.</param>
    /// <param name="spriteHeight">Height of the enemy sprite in pixels.</param>
    /// <param name="hp">Current HP.</param>
    /// <param name="maxHp">Maximum HP.</param>
    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel,
        Vector2 enemyPos, int spriteHeight, float hp, float maxHp)
    {
        // Per D-13: only show when damaged
        if (hp >= maxHp) return;
        if (maxHp <= 0) return;

        // Center above the enemy sprite
        int x = (int)enemyPos.X - BarWidth / 2;
        int y = (int)enemyPos.Y - spriteHeight / 2 - 6;

        // Gray background
        spriteBatch.Draw(pixel, new Rectangle(x, y, BarWidth, BarHeight), new Color(60, 60, 60));

        // Red fill proportional to remaining HP
        float fill = MathHelper.Clamp(hp / maxHp, 0f, 1f);
        int fillWidth = (int)(BarWidth * fill);
        if (fillWidth > 0)
            spriteBatch.Draw(pixel, new Rectangle(x, y, fillWidth, BarHeight), Color.Red);
    }
}
