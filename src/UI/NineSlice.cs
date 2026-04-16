using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Stateless helper that draws a <see cref="Texture2D"/> as a 9-slice into an arbitrary
/// destination <see cref="Rectangle"/>. The source texture is carved into 9 regions using
/// <see cref="Insets"/>; corners are drawn at native pixel size, edges are stretched along
/// one axis, and the center fills the remaining area. All drawing is done with point-clamp
/// sampling (the caller's <see cref="SpriteBatch"/> is expected to already be in
/// <see cref="SamplerState.PointClamp"/> mode).
/// </summary>
public static class NineSlice
{
    /// <summary>Corner/edge thickness in source-texture pixels.</summary>
    public readonly record struct Insets(int Left, int Top, int Right, int Bottom)
    {
        /// <summary>Create an <see cref="Insets"/> with the same value on all 4 sides.</summary>
        public static Insets Uniform(int v) => new(v, v, v, v);
    }

    /// <summary>
    /// Draw <paramref name="tex"/> into <paramref name="dest"/> using 9-slice
    /// <paramref name="insets"/>. Optional <paramref name="tint"/> defaults to white.
    /// </summary>
    public static void Draw(SpriteBatch sb, Texture2D tex, Rectangle dest, Insets insets, Color? tint = null)
    {
        var color = tint ?? Color.White;
        int sw = tex.Width, sh = tex.Height;
        int l = insets.Left, t = insets.Top, r = insets.Right, b = insets.Bottom;

        // Clamp so a small dest doesn't produce negative middle widths.
        if (l + r > dest.Width) { l = dest.Width / 2; r = dest.Width - l; }
        if (t + b > dest.Height) { t = dest.Height / 2; b = dest.Height - t; }

        // Clamp insets so they don't exceed the source dimensions either.
        if (l + r > sw) { l = sw / 2; r = sw - l; }
        if (t + b > sh) { t = sh / 2; b = sh - t; }

        int cx = dest.X + l;
        int cy = dest.Y + t;
        int cw = dest.Width - l - r;
        int ch = dest.Height - t - b;

        int scx = l;
        int scy = t;
        int scw = sw - l - r;
        int sch = sh - t - b;

        // Corners
        sb.Draw(tex, new Rectangle(dest.X, dest.Y, l, t),           new Rectangle(0, 0, l, t), color);
        sb.Draw(tex, new Rectangle(cx + cw, dest.Y, r, t),          new Rectangle(scx + scw, 0, r, t), color);
        sb.Draw(tex, new Rectangle(dest.X, cy + ch, l, b),          new Rectangle(0, scy + sch, l, b), color);
        sb.Draw(tex, new Rectangle(cx + cw, cy + ch, r, b),         new Rectangle(scx + scw, scy + sch, r, b), color);

        // Edges
        sb.Draw(tex, new Rectangle(cx, dest.Y, cw, t),              new Rectangle(scx, 0, scw, t), color);
        sb.Draw(tex, new Rectangle(cx, cy + ch, cw, b),             new Rectangle(scx, scy + sch, scw, b), color);
        sb.Draw(tex, new Rectangle(dest.X, cy, l, ch),              new Rectangle(0, scy, l, sch), color);
        sb.Draw(tex, new Rectangle(cx + cw, cy, r, ch),             new Rectangle(scx + scw, scy, r, sch), color);

        // Center
        if (cw > 0 && ch > 0 && scw > 0 && sch > 0)
            sb.Draw(tex, new Rectangle(cx, cy, cw, ch),             new Rectangle(scx, scy, scw, sch), color);
    }

    /// <summary>Draw a non-sliced texture stretched into <paramref name="dest"/>.</summary>
    public static void DrawStretched(SpriteBatch sb, Texture2D tex, Rectangle dest, Color? tint = null)
        => sb.Draw(tex, dest, tint ?? Color.White);
}
