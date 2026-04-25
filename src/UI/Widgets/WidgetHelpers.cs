using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Stateless draw helpers shared by concrete widgets and <see cref="UIManager"/>.
/// All helpers assume the caller has already opened a <see cref="SpriteBatch"/>
/// batch with <see cref="SamplerState.PointClamp"/> (matching the rest of the UI
/// rendering pipeline — see <c>src/UI/NineSlice.cs</c>). Zero allocations per call.
/// </summary>
internal static class WidgetHelpers
{
    /// <summary>
    /// Classic hover halo: draws the same <paramref name="icon"/> four additional
    /// times, each nudged 1px in one cardinal direction, at 55% white. Lifted
    /// verbatim from <c>ChestScene.DrawIconButton</c> (pre-framework) so visual
    /// parity is preserved for any widget that opts into
    /// <see cref="HoverStyle.NudgeHalo"/>. The caller draws the centered icon on
    /// top afterwards.
    /// </summary>
    /// <param name="sb">Open sprite batch (PointClamp sampler).</param>
    /// <param name="icon">Icon texture.</param>
    /// <param name="iconRect">Destination rect of the already-nudged (up 1px) icon.</param>
    /// <param name="glow">Halo tint. Callers pass <see cref="Color.White"/> * 0.55f.</param>
    /// <param name="effects">Sprite effects (e.g. horizontal flip) — applied to each halo draw.</param>
    public static void DrawNudgeHalo(SpriteBatch sb, Texture2D icon, Rectangle iconRect, Color glow, SpriteEffects effects)
    {
        int w = iconRect.Width;
        int h = iconRect.Height;
        sb.Draw(icon, new Rectangle(iconRect.X - 1, iconRect.Y, w, h), null, glow, 0f, Vector2.Zero, effects, 0f);
        sb.Draw(icon, new Rectangle(iconRect.X + 1, iconRect.Y, w, h), null, glow, 0f, Vector2.Zero, effects, 0f);
        sb.Draw(icon, new Rectangle(iconRect.X, iconRect.Y - 1, w, h), null, glow, 0f, Vector2.Zero, effects, 0f);
        sb.Draw(icon, new Rectangle(iconRect.X, iconRect.Y + 1, w, h), null, glow, 0f, Vector2.Zero, effects, 0f);
    }

    /// <summary>
    /// Rounded-corner dark-fill panel with a golden border — the tooltip-style
    /// chrome lifted from <c>ChestScene.DrawTooltipPanel</c> (pre-framework).
    /// Exact colors: fill <c>(40,23,20) * 0.96f</c>, border <c>(226,190,114)</c>.
    /// </summary>
    /// <param name="sb">Open sprite batch.</param>
    /// <param name="pixel">1x1 white texture.</param>
    /// <param name="rect">Panel rect.</param>
    /// <param name="corner">Corner radius in pixels (default 4, matches Chest tooltip).</param>
    public static void DrawTooltipPanel(SpriteBatch sb, Texture2D pixel, Rectangle rect, int corner = 4)
    {
        var fill = new Color(40, 23, 20) * 0.96f;
        var border = new Color(226, 190, 114);
        int r = corner;

        // Cross-body fill: two rectangles that together cover the panel minus corners.
        sb.Draw(pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, rect.Height), fill);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y + r, rect.Width, rect.Height - r * 2), fill);

        // 4 border edges.
        sb.Draw(pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, 1), border);
        sb.Draw(pixel, new Rectangle(rect.X + r, rect.Bottom - 1, rect.Width - r * 2, 1), border);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y + r, 1, rect.Height - r * 2), border);
        sb.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y + r, 1, rect.Height - r * 2), border);

        // 4 corner nibs that round the outline a single pixel inward.
        sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + r - 1, r - 1, 1), border);
        sb.Draw(pixel, new Rectangle(rect.Right - r, rect.Y + r - 1, r - 1, 1), border);
        sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Bottom - r, r - 1, 1), border);
        sb.Draw(pixel, new Rectangle(rect.Right - r, rect.Bottom - r, r - 1, 1), border);
    }

    /// <summary>
    /// Draws a 1-to-N-pixel-thick outline as four edge rectangles around
    /// <paramref name="rect"/>. Used by <see cref="UIManager"/> for the focus
    /// outline (2px golden).
    /// </summary>
    public static void DrawOutline(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        int t = thickness < 1 ? 1 : thickness;
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
        sb.Draw(pixel, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), color);
    }

    /// <summary>
    /// Draw a panel/modal title centered inside <paramref name="rect"/>. The default look
    /// mirrors the ChestScene + ShopPanel title style: letter-spacing 2px, faux-bold
    /// (extra 1px-right pass in the same color), and a 1px-down shadow at 82% opacity.
    /// Turn off <paramref name="shadow"/> for subtler sub-pane labels (e.g. "Bolsa" / "Baú").
    /// </summary>
    /// <param name="sb">Open sprite batch (PointClamp sampler).</param>
    /// <param name="font">Pre-sized bold font (FontStashSharp — native size, no scaling).</param>
    /// <param name="text">Title text.</param>
    /// <param name="rect">Destination rect; text is centered horizontally and vertically inside.</param>
    /// <param name="color">Title color (typically <see cref="Color.LightGoldenrodYellow"/>).</param>
    /// <param name="letterSpacing">Extra pixels added between glyphs. 2f produces the medieval "spaced" look.</param>
    /// <param name="shadow">When true, adds faux-bold + drop shadow for modal title headings.</param>
    /// <summary>
    /// Color associated with each <see cref="Rarity"/> tier — used by the inner-border
    /// slot frame (inventory / chest / hotbar) and by the in-world item glow.
    /// Returns null for Common (no visual marker).
    /// </summary>
    public static Color? GetRarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Uncommon => new Color(80, 220, 90),    // green
        Rarity.Rare     => new Color(255, 215, 60),   // gold
        _ => null,
    };

    /// <summary>
    /// Draw an inner colored border around <paramref name="slotRect"/> to mark item
    /// rarity (replaces the old translucent fill overlay so icons stay color-accurate).
    /// </summary>
    public static void DrawRarityBorder(SpriteBatch sb, Texture2D pixel, Rectangle slotRect, Rarity rarity, int thickness = 2)
    {
        var color = GetRarityColor(rarity);
        if (!color.HasValue) return;
        DrawOutline(sb, pixel, slotRect, color.Value, thickness);
    }

    public static void DrawPanelTitle(
        SpriteBatch sb,
        SpriteFontBase font,
        string text,
        Rectangle rect,
        Color color,
        float letterSpacing = 2f,
        bool shadow = true)
    {
        var raw = font.MeasureString(text);
        float width = raw.X + (text.Length > 1 ? letterSpacing * (text.Length - 1) : 0f);
        float height = raw.Y;
        var pos = new Vector2(
            rect.X + (rect.Width - width) / 2f,
            rect.Y + (rect.Height - height) / 2f);

        float x = pos.X;
        for (int i = 0; i < text.Length; i++)
        {
            string c = text[i].ToString();
            if (shadow)
            {
                // Drop shadow (+1px down, 82% opacity) — drawn first so main glyph sits on top.
                sb.DrawString(font, c, new Vector2(x, pos.Y + 1), color * 0.82f);
                // Faux-bold sibling (+1px right, same color) — thickens the glyph without needing a bolder weight.
                sb.DrawString(font, c, new Vector2(x + 1, pos.Y), color);
            }
            sb.DrawString(font, c, new Vector2(x, pos.Y), color);
            x += font.MeasureString(c).X + letterSpacing;
        }
    }
}
