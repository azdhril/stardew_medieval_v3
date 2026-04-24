using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Stateless renderer for the 880x120 dialogue panel anchored bottom-center of the current viewport.
/// Draws a full-viewport dim overlay, the panel with portrait slot + text column,
/// and (optionally) a pulsing ▼ advance indicator. All values per UI-SPEC §Component
/// Inventory and §Color. Panel size is fixed; only its X/Y position scales with the
/// supplied viewport dimensions so the overlay behaves correctly in fullscreen or any
/// non-native window size.
/// </summary>
public class DialogueBox
{
    private const int PanelWidth = 880;
    private const int PanelHeight = 120;
    private const int PortraitSize = 80;
    private const int Padding = 8;
    private const int BottomMargin = 32;

    private static readonly Color DimOverlay = Color.Black * 0.55f;
    private static readonly Color PanelFill = new Color(60, 40, 30);
    private static readonly Color PanelBevel = new Color(90, 60, 45);
    private static readonly Color PanelOutline = Color.Black;

    /// <summary>
    /// Draws the dialogue overlay. Caller is responsible for opening/closing
    /// the SpriteBatch (PointClamp recommended) and for passing the current
    /// viewport dimensions so the dim overlay fills the screen and the panel
    /// is anchored to the bottom-center of the actual viewport (not a fixed 960x540).
    /// </summary>
    /// <param name="sb">Open SpriteBatch.</param>
    /// <param name="font">Font for body text.</param>
    /// <param name="pixel">1x1 white texture for solid rects.</param>
    /// <param name="portrait">Optional 80x80 NPC portrait; null draws placeholder.</param>
    /// <param name="revealedText">Current line, truncated by the typewriter.</param>
    /// <param name="showAdvance">True when the full line has been revealed.</param>
    /// <param name="pulseOn">When showAdvance, controls the 2Hz ▼ pulse.</param>
    /// <param name="viewportWidth">Current GraphicsDevice viewport width (px).</param>
    /// <param name="viewportHeight">Current GraphicsDevice viewport height (px).</param>
    public void Draw(SpriteBatch sb, SpriteFontBase font, Texture2D pixel,
        Texture2D? portrait, string revealedText, bool showAdvance, bool pulseOn,
        int viewportWidth, int viewportHeight)
    {
        // Runtime panel anchor: horizontally centered, BottomMargin above the viewport bottom.
        int panelX = (viewportWidth - PanelWidth) / 2;
        int panelY = viewportHeight - BottomMargin - PanelHeight;

        // 1. Dim overlay — covers the entire real viewport, not a fixed 960x540.
        sb.Draw(pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), DimOverlay);

        // 2. Panel outline (1px black)
        sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, PanelWidth + 2, PanelHeight + 2), PanelOutline);
        // Panel fill
        sb.Draw(pixel, new Rectangle(panelX, panelY, PanelWidth, PanelHeight), PanelFill);
        // 1px inner bevel
        DrawRectOutline(sb, pixel, new Rectangle(panelX + 1, panelY + 1, PanelWidth - 2, PanelHeight - 2), PanelBevel);

        // 3. Portrait slot (80x80) at inner-left
        int portraitX = panelX + Padding;
        int portraitY = panelY + Padding;
        var portraitRect = new Rectangle(portraitX, portraitY, PortraitSize, PortraitSize);
        if (portrait != null)
        {
            sb.Draw(portrait, portraitRect, Color.White);
        }
        else
        {
            sb.Draw(pixel, portraitRect, PanelBevel);
        }
        // Thin outline around portrait slot
        DrawRectOutline(sb, pixel, portraitRect, PanelOutline);

        // 4. Text column
        int textX = portraitX + PortraitSize + Padding;
        int textY = portraitY;
        sb.DrawString(font, revealedText, new Vector2(textX, textY), Color.White);

        // 5. Advance indicator (▼) bottom-right of panel
        if (showAdvance)
        {
            string indicator = "v"; // fallback; font may lack ▼ glyph. Keep ASCII for safety.
            var indicatorSize = font.MeasureString(indicator);
            int indX = panelX + PanelWidth - (int)indicatorSize.X - 12;
            int indY = panelY + PanelHeight - (int)indicatorSize.Y - 6;
            Color c = pulseOn ? Color.Gold : Color.Gold * 0.3f;
            sb.DrawString(font, indicator, new Vector2(indX, indY), c);
        }
    }

    private static void DrawRectOutline(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
    {
        sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 1), c);
        sb.Draw(pixel, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), c);
        sb.Draw(pixel, new Rectangle(r.X, r.Y, 1, r.Height), c);
        sb.Draw(pixel, new Rectangle(r.Right - 1, r.Y, 1, r.Height), c);
    }
}
