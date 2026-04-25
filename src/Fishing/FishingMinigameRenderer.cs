using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Fishing;

/// <summary>
/// Screen-space renderer for <see cref="FishingMinigame"/>: the central ring
/// (rosca), the moving fish wedge, the player's bobber-as-joystick, the charge
/// bar above, and the patience bar below. Holds no state — call <see cref="Draw"/>
/// each frame the minigame is active.
/// </summary>
public static class FishingMinigameRenderer
{
    public const float OuterRadius = 120f;
    public const float InnerRadius = 96f;

    private const int RingSegments = 180; // 2° resolution
    private const float SegmentThickness = 2f;

    /// <summary>Draw the entire minigame overlay centered in the viewport.</summary>
    public static void Draw(SpriteBatch sb, Texture2D pixel, SpriteAtlas atlas,
        SpriteFontBase font, FishingMinigame mg, Rectangle viewport)
    {
        if (!mg.IsActive || mg.Fish == null) return;

        var center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

        // Dim the world behind the minigame so the ring reads as the focal point.
        sb.Draw(pixel, viewport, new Color(0, 0, 0, 110));

        DrawRing(sb, pixel, center, new Color(60, 60, 80, 220));
        DrawWasdDetents(sb, pixel, center);
        DrawToleranceArc(sb, pixel, center, mg);

        DrawFishOnRing(sb, atlas, center, mg);
        DrawBobberOnRing(sb, atlas, center, mg, pixel);

        DrawChargeBar(sb, pixel, center, mg);
        DrawPatienceBar(sb, pixel, center, mg);

        DrawHints(sb, font, center, mg);
    }

    private static void DrawRing(SpriteBatch sb, Texture2D pixel, Vector2 center, Color color)
    {
        float step = 360f / RingSegments;
        for (int i = 0; i < RingSegments; i++)
            DrawRadialSegment(sb, pixel, center, i * step, color);
    }

    private static void DrawToleranceArc(SpriteBatch sb, Texture2D pixel, Vector2 center, FishingMinigame mg)
    {
        // Wedge centered on the fish — colored by hittability:
        //   - Red/gray when the fish sits in a "dead zone" (no WASD detent within tol/2 °)
        //     so the player knows charging is impossible and to wait for the fish to move
        //   - Green when the joystick is currently on-target (charge is filling)
        //   - Blue when hittable but the joystick isn't pointed there yet
        float half = mg.ToleranceArc * 0.5f;
        float step = 360f / RingSegments;
        bool hittable = IsFishHittableByWasd(mg.FishAngleDeg, mg.ToleranceArc);

        Color color;
        if (!hittable)
            color = new Color(190, 80, 90, 230);             // dead zone — visible warning
        else if (mg.BobberOnTarget)
            color = new Color(120, 230, 140, 240);           // on-target green
        else
            color = new Color(110, 170, 230, 220);           // hittable blue (default)

        for (int i = 0; i < RingSegments; i++)
        {
            float ang = i * step;
            float delta = FishingMinigame.ShortestAngularDelta(ang, mg.FishAngleDeg);
            if (Math.Abs(delta) <= half)
                DrawRadialSegment(sb, pixel, center, ang, color);
        }
    }

    /// <summary>
    /// Returns true when at least one of the 8 WASD aim directions sits within the
    /// fish's tolerance arc — i.e. the player can actually hit it from somewhere.
    /// When false, the fish is in the gap between detents (a "dead zone") and the
    /// renderer paints the wedge red so the player understands to wait it out.
    /// </summary>
    private static bool IsFishHittableByWasd(float fishAngle, float tolerance)
    {
        float half = tolerance * 0.5f;
        for (int i = 0; i < 8; i++)
        {
            float wasdAng = i * 45f;
            if (MathF.Abs(FishingMinigame.ShortestAngularDelta(wasdAng, fishAngle)) <= half)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Draw 8 small tick marks just outside the ring at the WASD aim angles
    /// (0°/45°/90°/…/315°). Lets the player read where their joystick can park,
    /// turning the "8 discrete detents on a 360° ring" tradeoff into a teachable
    /// pattern instead of guesswork.
    /// </summary>
    private static void DrawWasdDetents(SpriteBatch sb, Texture2D pixel, Vector2 center)
    {
        const float tickInner = OuterRadius + 3f;
        const float tickOuter = OuterRadius + 9f;
        var color = new Color(230, 230, 245, 200);

        for (int i = 0; i < 8; i++)
        {
            float rad = MathHelper.ToRadians(i * 45f);
            var a = center + new Vector2(MathF.Cos(rad) * tickInner, MathF.Sin(rad) * tickInner);
            var b = center + new Vector2(MathF.Cos(rad) * tickOuter, MathF.Sin(rad) * tickOuter);
            DrawLine(sb, pixel, a, b, color, 2f);
        }
    }

    private static void DrawRadialSegment(SpriteBatch sb, Texture2D pixel, Vector2 center, float angDeg, Color color)
    {
        float rad = MathHelper.ToRadians(angDeg);
        var inner = center + new Vector2(MathF.Cos(rad) * InnerRadius, MathF.Sin(rad) * InnerRadius);
        var outer = center + new Vector2(MathF.Cos(rad) * OuterRadius, MathF.Sin(rad) * OuterRadius);
        DrawLine(sb, pixel, inner, outer, color, SegmentThickness);
    }

    private static void DrawFishOnRing(SpriteBatch sb, SpriteAtlas atlas, Vector2 center, FishingMinigame mg)
    {
        if (mg.Fish == null) return;
        float midR = (OuterRadius + InnerRadius) * 0.5f;
        float rad = MathHelper.ToRadians(mg.FishAngleDeg);
        var pos = center + new Vector2(MathF.Cos(rad) * midR, MathF.Sin(rad) * midR);

        string spriteId = mg.Fish.Id.ToLowerInvariant();
        var src = atlas.GetRect(spriteId);
        var tex = atlas.GetTexture(spriteId);
        // 24×24 so the sprite reads at 96×54 monitors but still fits inside the ring band.
        const int size = 24;
        var dest = new Rectangle((int)(pos.X - size / 2f), (int)(pos.Y - size / 2f), size, size);
        sb.Draw(tex, dest, src, Color.White);
    }

    /// <summary>
    /// Render the bobber as a fliperama-joystick handle anchored at the ring's
    /// center. A small fixed base disc reads as the joystick "boot"; the big
    /// bobber sprite sits on top of it. The sprite frame (0..2) and flip flags
    /// are picked from <see cref="FishingMinigame.JoystickTilt"/> so harder
    /// pushes show more lean and each of the 8 directions reuses the same
    /// 3-row sheet via mirroring.
    /// </summary>
    private static void DrawBobberOnRing(SpriteBatch sb, SpriteAtlas atlas, Vector2 center, FishingMinigame mg, Texture2D pixel)
    {
        // Visual offset is small — the sprite already encodes the lean visually,
        // we just nudge it slightly toward the tilt for extra readability.
        const float MaxLean = 14f;
        const int RenderSize = 48;          // ~player-size; big enough to read at the center
        const int BaseSize = 14;

        Vector2 offset = mg.JoystickTilt * MaxLean;
        Vector2 sprPos = center + offset;

        // Static base disc (the joystick "boot"). Subtle so the sprite stays the focus.
        var baseRect = new Rectangle(
            (int)(center.X - BaseSize / 2f),
            (int)(center.Y - BaseSize / 2f + 6),
            BaseSize, BaseSize / 2);
        sb.Draw(pixel, baseRect, new Color(25, 25, 40, 200));

        // Pick row/frame/flip for the sprite based on aim direction + lean magnitude.
        var (row, col, flip) = ResolveBobberFrame(mg);
        string spriteId = $"big_bobber_{row}_{col}";
        var src = atlas.GetRect(spriteId);
        var tex = atlas.GetTexture(spriteId);

        int size = mg.BobberOnTarget ? RenderSize + 4 : RenderSize;
        Color tint = mg.BobberOnTarget ? new Color(255, 240, 140) : Color.White;
        // Sprite is anchored just above the base so it reads as standing on it.
        var dest = new Rectangle(
            (int)(sprPos.X - size / 2f),
            (int)(sprPos.Y - size / 2f - 6),
            size, size);
        sb.Draw(tex, dest, src, tint, 0f, Vector2.Zero, flip, 0f);
    }

    /// <summary>
    /// Pick the (row, col, flip) tuple that renders the bobber leaning in the
    /// joystick's current direction. Frame index (col) maps to lean magnitude;
    /// row + flip map to one of the 8 cardinal/diagonal directions.
    /// </summary>
    private static (int row, int col, SpriteEffects flip) ResolveBobberFrame(FishingMinigame mg)
    {
        // Frame index 0..2 by lean magnitude (smooth ramp on top of magnitude).
        float mag = mg.JoystickTilt.Length();
        int col = mag < 0.33f ? 0 : (mag < 0.7f ? 1 : 2);

        // Neutral / barely leaning → upright "rest" pose: row 0 frame 0 with no flip.
        if (!mg.IsAiming) return (0, 0, SpriteEffects.None);

        // Bucket the aim angle into 8 directions. Screen-space convention:
        // 0° = right, 90° = down, 180° = left, 270° = up.
        int bucket = ((int)MathF.Round(mg.AimAngleDeg / 45f) % 8 + 8) % 8;

        return bucket switch
        {
            0 => (2, col, SpriteEffects.FlipHorizontally),                                         // right
            1 => (1, col, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically),          // down-right
            2 => (0, col, SpriteEffects.FlipVertically),                                           // down
            3 => (1, col, SpriteEffects.FlipVertically),                                           // down-left
            4 => (2, col, SpriteEffects.None),                                                     // left
            5 => (1, col, SpriteEffects.None),                                                     // up-left (base diagonal)
            6 => (0, col, SpriteEffects.None),                                                     // up
            7 => (1, col, SpriteEffects.FlipHorizontally),                                         // up-right
            _ => (0, col, SpriteEffects.None),
        };
    }

    private static void DrawChargeBar(SpriteBatch sb, Texture2D pixel, Vector2 center, FishingMinigame mg)
    {
        var f = mg.Fish!;
        const int width = 280;
        const int height = 14;
        int x = (int)(center.X - width / 2f);
        int y = (int)(center.Y - OuterRadius - height - 14);

        // Frame
        sb.Draw(pixel, new Rectangle(x - 2, y - 2, width + 4, height + 4), new Color(20, 20, 30, 230));
        sb.Draw(pixel, new Rectangle(x, y, width, height), new Color(50, 50, 65, 230));

        // Threshold tick marks (Min / Good / Perfect)
        DrawTick(sb, pixel, x, y, width, height, f.MinChargePct, new Color(200, 80, 80));
        DrawTick(sb, pixel, x, y, width, height, f.GoodChargePct, new Color(220, 200, 90));
        DrawTick(sb, pixel, x, y, width, height, f.PerfectChargePct, new Color(120, 230, 140));

        // Fill — colored by which tier the current charge sits in
        int fillW = (int)(width * (mg.ChargePct / 100f));
        Color fillColor;
        if (mg.ChargePct < f.MinChargePct) fillColor = new Color(180, 70, 70);
        else if (mg.ChargePct < f.GoodChargePct) fillColor = new Color(220, 180, 80);
        else if (mg.ChargePct < f.PerfectChargePct) fillColor = new Color(120, 220, 220);
        else fillColor = new Color(160, 240, 170);
        sb.Draw(pixel, new Rectangle(x, y, fillW, height), fillColor);
    }

    private static void DrawTick(SpriteBatch sb, Texture2D pixel, int x, int y, int width, int height, float pct, Color color)
    {
        int tx = x + (int)(width * (pct / 100f));
        sb.Draw(pixel, new Rectangle(tx, y - 2, 2, height + 4), color);
    }

    private static void DrawPatienceBar(SpriteBatch sb, Texture2D pixel, Vector2 center, FishingMinigame mg)
    {
        const int width = 220;
        const int height = 8;
        int x = (int)(center.X - width / 2f);
        int y = (int)(center.Y + OuterRadius + 14);

        sb.Draw(pixel, new Rectangle(x - 2, y - 2, width + 4, height + 4), new Color(20, 20, 30, 230));
        sb.Draw(pixel, new Rectangle(x, y, width, height), new Color(50, 50, 65, 230));

        int fillW = (int)(width * mg.PatiencePct);
        // Shift to red as the timer drains — "the fish is getting away".
        Color color = mg.PatiencePct > 0.5f
            ? Color.Lerp(new Color(220, 180, 80), new Color(120, 220, 140), (mg.PatiencePct - 0.5f) * 2f)
            : Color.Lerp(new Color(200, 60, 60), new Color(220, 180, 80), mg.PatiencePct * 2f);
        sb.Draw(pixel, new Rectangle(x, y, fillW, height), color);
    }

    private static void DrawHints(SpriteBatch sb, SpriteFontBase font, Vector2 center, FishingMinigame mg)
    {
        string fishName = mg.Fish?.Id.Replace('_', ' ') ?? "";
        var nameSize = font.MeasureString(fishName);
        sb.DrawString(font, fishName,
            new Vector2(center.X - nameSize.X / 2f, center.Y - OuterRadius - 60),
            Color.White);

        const string hint = "WASD para mirar • Clique para fisgar";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint,
            new Vector2(center.X - hintSize.X / 2f, center.Y + OuterRadius + 32),
            new Color(200, 200, 210));
    }

    private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.01f) return;
        float angle = MathF.Atan2(d.Y, d.X);
        sb.Draw(pixel, a, null, color, angle, new Vector2(0, 0.5f),
            new Vector2(len, thickness), SpriteEffects.None, 0f);
    }
}
