using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Red "You died" banner that fades in, holds, and fades out at screen center.
/// Renders in screen-space. Same timing structure as <see cref="LevelUpBanner"/>
/// but centered vertically and colored red.
/// </summary>
public class DeathBanner
{
    private const float Duration = 1.5f;
    private const float FadeInEnd = 0.3f;
    private const float FadeOutStart = 1.2f;

    private float _elapsed;
    private bool _active;

    /// <summary>True while the banner is visible on screen.</summary>
    public bool IsActive => _active;

    /// <summary>Trigger the death banner.</summary>
    public void Show()
    {
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
    /// Draw the banner centered on screen. Caller owns SpriteBatch.Begin/End.
    /// No-op when inactive.
    /// </summary>
    public void Draw(SpriteBatch sb, SpriteFontBase font, int screenWidth, int screenHeight)
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

        string text = "You died";
        var textSize = font.MeasureString(text);
        float x = (screenWidth - textSize.X) / 2f;
        float y = (screenHeight - textSize.Y) / 2f;

        // Shadow
        sb.DrawString(font, text, new Vector2(x + 1, y + 1), Color.Black * alpha);
        // Red text
        sb.DrawString(font, text, new Vector2(x, y), Color.Red * alpha);
    }
}
