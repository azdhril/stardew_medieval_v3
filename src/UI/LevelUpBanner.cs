using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Golden "LEVEL UP! Lv X" banner that fades in, holds, and fades out.
/// Renders in screen-space at the top-center of the viewport.
/// Does NOT pause gameplay (D-08).
/// </summary>
public class LevelUpBanner
{
    private const float Duration = 1.5f;
    private const float FadeInEnd = 0.3f;
    private const float FadeOutStart = 1.2f;

    private float _elapsed;
    private int _level;
    private bool _active;

    /// <summary>True while the banner is visible on screen.</summary>
    public bool IsActive => _active;

    /// <summary>Trigger the level-up banner for the given new level.</summary>
    /// <param name="newLevel">The level the player just reached.</param>
    public void Show(int newLevel)
    {
        _level = newLevel;
        _elapsed = 0f;
        _active = true;
    }

    /// <summary>Advance the banner timer. Deactivates after Duration seconds.</summary>
    public void Update(float dt)
    {
        if (!_active) return;
        _elapsed += dt;
        if (_elapsed >= Duration)
            _active = false;
    }

    /// <summary>
    /// Draw the banner in screen-space. Caller owns SpriteBatch.Begin/End.
    /// No-op when inactive.
    /// </summary>
    /// <param name="sb">Active SpriteBatch.</param>
    /// <param name="font">SpriteFont for text rendering.</param>
    /// <param name="screenWidth">Current viewport width for horizontal centering.</param>
    public void Draw(SpriteBatch sb, SpriteFont font, int screenWidth)
    {
        if (!_active) return;

        // Alpha: fade in 0-0.3s, hold 0.3-1.2s, fade out 1.2-1.5s
        float alpha;
        if (_elapsed < FadeInEnd)
            alpha = _elapsed / FadeInEnd;
        else if (_elapsed < FadeOutStart)
            alpha = 1f;
        else
            alpha = 1f - ((_elapsed - FadeOutStart) / (Duration - FadeOutStart));
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        string text = $"LEVEL UP! Lv {_level}";
        var textSize = font.MeasureString(text);
        float x = (screenWidth - textSize.X) / 2f;
        float y = 60f; // Below clock area

        // Shadow
        sb.DrawString(font, text, new Vector2(x + 1, y + 1), Color.Black * alpha);
        // Gold text
        sb.DrawString(font, text, new Vector2(x, y), Color.Gold * alpha);
    }
}
