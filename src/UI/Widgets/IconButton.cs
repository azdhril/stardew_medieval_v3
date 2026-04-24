using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Pixel-art icon button with an optional 9-slice background. Default hover is
/// <see cref="HoverStyle.NudgeHalo"/> (1px upward nudge + 4-direction 55%-white
/// halo + White tint) — the visual contract established by
/// <c>ChestScene.DrawIconButton</c> and <c>ShopPanel</c>'s icon steppers in the
/// pre-framework code. Scenes update <see cref="Bounds"/> each frame when the
/// layout is viewport-responsive (Pitfall 3).
/// </summary>
public class IconButton : IClickable
{
    /// <summary>Screen-space bounds. Scenes re-assign per frame as layout moves.</summary>
    public Rectangle Bounds { get; set; }

    /// <summary>When <c>false</c>, renders with <see cref="DisabledTint"/> and skips hover/halo.</summary>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public string? Tooltip { get; set; }

    /// <summary>Callback invoked from <see cref="OnClick"/>. Null = no-op.</summary>
    public Action? OnClickAction { get; set; }

    /// <summary>Hover visual. Defaults to <see cref="HoverStyle.NudgeHalo"/>.</summary>
    public HoverStyle Hover { get; set; } = HoverStyle.NudgeHalo;

    /// <summary>Sprite effects applied to the icon (e.g. horizontal flip for mirrored arrows).</summary>
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    /// <summary>Tint used when idle (not hovered). Matches existing chest-cream default.</summary>
    public Color IdleTint { get; set; } = Color.LightGoldenrodYellow;

    /// <summary>Tint used when hovered. White pops the halo against the idle cream.</summary>
    public Color HoverTint { get; set; } = Color.White;

    /// <summary>Tint used when <see cref="Enabled"/> is <c>false</c>.</summary>
    public Color DisabledTint { get; set; } = Color.White * 0.35f;

    /// <summary>Optional 9-slice insets for the background texture. Ignored if no background was supplied.</summary>
    public NineSlice.Insets BackgroundInsets { get; set; }

    private readonly Texture2D _icon;
    private readonly Texture2D? _background;
    private bool _isHovered;

    /// <summary>
    /// Construct an icon button. When <paramref name="background"/> is supplied
    /// the button renders a 9-sliced chrome behind the icon using
    /// <paramref name="backgroundInsets"/>. When <paramref name="background"/>
    /// is <c>null</c>, only the icon is drawn (matches the close-X pattern).
    /// </summary>
    public IconButton(Texture2D icon, Texture2D? background = null, NineSlice.Insets backgroundInsets = default)
    {
        _icon = icon;
        _background = background;
        BackgroundInsets = backgroundInsets;
    }

    /// <inheritdoc />
    public void OnClick() => OnClickAction?.Invoke();

    /// <inheritdoc />
    public void OnHoverEnter() => _isHovered = true;

    /// <inheritdoc />
    public void OnHoverExit() => _isHovered = false;

    /// <summary>
    /// Draw the button at its current <see cref="Bounds"/>. Assumes the caller has
    /// opened the <see cref="SpriteBatch"/> with <see cref="SamplerState.PointClamp"/>.
    /// </summary>
    public void Draw(SpriteBatch sb)
    {
        bool hovered = _isHovered && Enabled && Hover != HoverStyle.None;

        if (_background != null)
            NineSlice.Draw(sb, _background, Bounds, BackgroundInsets);

        // If icon fits at 1x or larger, use integer scaling (crisp pixel art).
        // If icon is larger than bounds, fall back to uniform fractional fit with 20% inner padding
        // so the icon reads as centered inside its button chrome instead of clipping.
        int iw, ih;
        if (_icon.Width <= Bounds.Width && _icon.Height <= Bounds.Height)
        {
            int scale = Math.Max(1, Math.Min(Bounds.Width / _icon.Width, Bounds.Height / _icon.Height));
            iw = _icon.Width * scale;
            ih = _icon.Height * scale;
        }
        else
        {
            float fit = Math.Min((float)Bounds.Width / _icon.Width, (float)Bounds.Height / _icon.Height) * 0.8f;
            iw = (int)(_icon.Width * fit);
            ih = (int)(_icon.Height * fit);
        }
        int yNudge = (hovered && Hover == HoverStyle.NudgeHalo) ? -1 : 0;
        var iconRect = new Rectangle(
            Bounds.X + (Bounds.Width - iw) / 2,
            Bounds.Y + (Bounds.Height - ih) / 2 + yNudge,
            iw, ih);

        if (hovered && Hover == HoverStyle.NudgeHalo)
        {
            WidgetHelpers.DrawNudgeHalo(sb, _icon, iconRect, Color.White * 0.55f, Effects);
        }

        Color tint = !Enabled ? DisabledTint : hovered ? HoverTint : IdleTint;
        sb.Draw(_icon, iconRect, null, tint, 0f, Vector2.Zero, Effects, 0f);
    }
}
