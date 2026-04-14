using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Screen-space boss HP bar displayed at the bottom center of the screen.
/// Per D-24: large bar with boss name text above it.
/// </summary>
public static class BossHealthBar
{
    private const int BarWidth = 300;
    private const int BarHeight = 12;
    private const int BottomMargin = 78; // sits above hotbar (hotbar top = screenH - 50)

    /// <summary>
    /// Draw a large boss health bar at the bottom center of the screen.
    /// Only draws when the boss has HP remaining.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in screen-space.</param>
    /// <param name="pixel">1x1 white texture for drawing.</param>
    /// <param name="font">Font for boss name text.</param>
    /// <param name="bossName">Display name of the boss.</param>
    /// <param name="hp">Current boss HP.</param>
    /// <param name="maxHp">Maximum boss HP.</param>
    /// <param name="screenWidth">Viewport width.</param>
    /// <param name="screenHeight">Viewport height.</param>
    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font,
        string bossName, float hp, float maxHp, int screenWidth, int screenHeight)
    {
        if (hp <= 0 || maxHp <= 0) return;

        int barX = (screenWidth - BarWidth) / 2;
        int barY = screenHeight - BottomMargin;

        // Border (1px lighter outline)
        spriteBatch.Draw(pixel,
            new Rectangle(barX - 1, barY - 1, BarWidth + 2, BarHeight + 2),
            new Color(100, 100, 100));

        // Background (dark gray)
        spriteBatch.Draw(pixel,
            new Rectangle(barX, barY, BarWidth, BarHeight),
            new Color(30, 30, 30));

        // Fill (dark red)
        float fill = MathHelper.Clamp(hp / maxHp, 0f, 1f);
        int fillWidth = (int)(BarWidth * fill);
        if (fillWidth > 0)
            spriteBatch.Draw(pixel,
                new Rectangle(barX, barY, fillWidth, BarHeight),
                new Color(180, 20, 20));

        // Boss name centered above bar
        var nameSize = font.MeasureString(bossName);
        float nameX = barX + (BarWidth - nameSize.X) / 2f;
        float nameY = barY - nameSize.Y - 4;
        spriteBatch.DrawString(font, bossName, new Vector2(nameX, nameY), Color.White);
    }
}
