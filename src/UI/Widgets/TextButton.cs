using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// 9-sliced button with centered text. Default skin is <c>YellowBtnSmall</c>
/// (matches <c>ShopPanel</c>'s Buy/Sell action buttons). Hovering nudges the
/// entire button up 1px when <see cref="HoverStyle.NudgeHalo"/> is active.
/// </summary>
public sealed class TextButton : IClickable
{
    /// <summary>Screen-space bounds. Scenes re-assign per frame when layout moves.</summary>
    public Rectangle Bounds { get; set; }

    /// <summary>When <c>false</c>, the button renders with <see cref="DisabledTextColor"/> and dimmed chrome.</summary>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public string? Tooltip { get; set; }

    /// <summary>Callback invoked from <see cref="OnClick"/>. Null = no-op.</summary>
    public Action? OnClickAction { get; set; }

    /// <summary>Text shown centered inside the button.</summary>
    public string Label { get; set; }

    /// <summary>Hover visual. Defaults to <see cref="HoverStyle.NudgeHalo"/> (1px up).</summary>
    public HoverStyle Hover { get; set; } = HoverStyle.NudgeHalo;

    /// <summary>Idle text color. Default black matches Buy/Sell buttons on yellow.</summary>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>Text color when hovered. Default keeps black — the 9-slice chrome carries the visual shift.</summary>
    public Color HoverTextColor { get; set; } = Color.Black;

    /// <summary>Text color when <see cref="Enabled"/> is <c>false</c>.</summary>
    public Color DisabledTextColor { get; set; } = Color.Gray;

    /// <summary>Chrome tint when enabled (passed as tint into <see cref="NineSlice.Draw"/>).</summary>
    public Color ChromeTint { get; set; } = Color.White;

    /// <summary>Chrome tint when disabled.</summary>
    public Color DisabledChromeTint { get; set; } = Color.White * 0.4f;

    private readonly Texture2D _chrome;
    private readonly NineSlice.Insets _insets;
    private readonly SpriteFontBase _font;
    private bool _isHovered;

    /// <summary>
    /// Construct a text button. <paramref name="chrome"/> is the 9-sliced
    /// background texture (typically <c>UITheme.YellowBtnSmall</c>);
    /// <paramref name="insets"/> are its 9-slice insets.
    /// </summary>
    public TextButton(string label, Texture2D chrome, NineSlice.Insets insets, SpriteFontBase font)
    {
        Label = label;
        _chrome = chrome;
        _insets = insets;
        _font = font;
    }

    /// <inheritdoc />
    public void OnClick() => OnClickAction?.Invoke();

    /// <inheritdoc />
    public void OnHoverEnter() => _isHovered = true;

    /// <inheritdoc />
    public void OnHoverExit() => _isHovered = false;

    /// <summary>
    /// Draw chrome + centered label. Assumes the caller has opened a
    /// <see cref="SpriteBatch"/> with <see cref="SamplerState.PointClamp"/>.
    /// </summary>
    public void Draw(SpriteBatch sb)
    {
        bool hovered = _isHovered && Enabled && Hover != HoverStyle.None;
        int yNudge = (hovered && Hover == HoverStyle.NudgeHalo) ? -1 : 0;

        var drawRect = new Rectangle(Bounds.X, Bounds.Y + yNudge, Bounds.Width, Bounds.Height);
        Color chromeTint = Enabled ? ChromeTint : DisabledChromeTint;
        NineSlice.Draw(sb, _chrome, drawRect, _insets, chromeTint);

        var size = _font.MeasureString(Label);
        var pos = new Vector2(
            drawRect.X + (drawRect.Width - size.X) / 2f,
            drawRect.Y + (drawRect.Height - size.Y) / 2f);
        Color textColor = !Enabled
            ? DisabledTextColor
            : hovered ? HoverTextColor : TextColor;
        sb.DrawString(_font, Label, pos, textColor);
    }
}
