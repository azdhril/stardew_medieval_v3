using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Reusable center-bottom fade toast. UI-SPEC §Component Inventory:
/// ~240x32 panel, anchored center-bottom, 80px above screen bottom.
/// Three-phase timing: 0-600 ms fade-in, 600-1800 ms hold, 1800-2200 ms fade-out.
/// Supports sequential message queueing: if Show is called while a toast is active,
/// the new message is enqueued and displayed after the current one finishes.
/// </summary>
public class Toast
{
    private const float FadeIn = 0.6f;
    private const float Hold = 1.2f;
    private const float FadeOut = 0.4f;
    private const float TotalDuration = FadeIn + Hold + FadeOut; // 2.2f total

    private const int PanelWidth = 240;
    private const int PanelHeight = 32;

    private string _text = "";
    private Color _color = Color.White;
    private float _elapsed;
    private bool _active;

    /// <summary>Queued messages displayed sequentially after the current toast finishes.</summary>
    private readonly Queue<(string text, Color color)> _queue = new();

    /// <summary>True while the toast is still visible on screen.</summary>
    public bool IsActive => _active;

    /// <summary>
    /// Show a toast message. If a toast is currently active, the new message is
    /// enqueued and will display after the current one finishes.
    /// </summary>
    public void Show(string text, Color color)
    {
        if (_active)
        {
            _queue.Enqueue((text ?? "", color));
            return;
        }
        _text = text ?? "";
        _color = color;
        _elapsed = 0f;
        _active = true;
    }

    /// <summary>Advance the toast timer. Deactivates after 2200 ms total, then dequeues next.</summary>
    public void Update(float dt)
    {
        if (!_active) return;
        _elapsed += dt;
        if (_elapsed >= TotalDuration)
        {
            _active = false;
            // Dequeue next message if available
            if (_queue.Count > 0)
            {
                var (nextText, nextColor) = _queue.Dequeue();
                _text = nextText;
                _color = nextColor;
                _elapsed = 0f;
                _active = true;
            }
        }
    }

    /// <summary>
    /// Draw the toast. Caller owns <c>SpriteBatch.Begin/End</c>. No-op when inactive.
    /// Screen assumed 960x540; panel centered horizontally, anchored ~80px from bottom.
    /// </summary>
    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!_active) return;

        // Alpha ramp (600 / 1200 / 400 ms)
        float alpha;
        if (_elapsed < FadeIn)
            alpha = _elapsed / FadeIn;
        else if (_elapsed < FadeIn + Hold)
            alpha = 1f;
        else
            alpha = 1f - ((_elapsed - FadeIn - Hold) / FadeOut);
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        int screenW = 960;
        int screenH = 540;
        int panelX = (screenW - PanelWidth) / 2;          // 360
        int panelY = screenH - 80 - PanelHeight;          // 428

        // Outline
        sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, PanelWidth + 2, PanelHeight + 2),
            Color.Black * alpha);
        // Fill
        sb.Draw(pixel, new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            new Color(60, 40, 30) * alpha);

        var textSize = font.MeasureString(_text);
        int tx = panelX + (PanelWidth - (int)textSize.X) / 2;
        int ty = panelY + (PanelHeight - (int)textSize.Y) / 2;
        sb.DrawString(font, _text, new Vector2(tx, ty), _color * alpha);
    }
}
