using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Three-state tab (Active / Inactive / Hovered) that swaps between
/// <c>TabOn</c> and <c>TabOff</c> textures. CRITICAL: when
/// <see cref="IsActive"/> is <c>true</c> the tab does NOT nudge on hover — this
/// preserves the visual distinction established by <c>ShopPanel</c>
/// (Pitfall 6). Inactive tabs nudge 1px up when hovered.
/// </summary>
public sealed class Tab : IClickable
{
    /// <summary>Screen-space bounds. Scenes re-assign per frame when layout moves.</summary>
    public Rectangle Bounds { get; set; }

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public string? Tooltip { get; set; }

    /// <summary>Callback invoked from <see cref="OnClick"/>. Null = no-op.</summary>
    public Action? OnClickAction { get; set; }

    /// <summary>Label rendered centered on the tab.</summary>
    public string Label { get; set; }

    /// <summary>Whether this tab is currently active. Toggled externally by the scene.</summary>
    public bool IsActive { get; set; }

    /// <summary>Active-tab text color (default black on gold).</summary>
    public Color ActiveTextColor { get; set; } = Color.Black;

    /// <summary>Inactive-tab text color (default white on brown).</summary>
    public Color InactiveTextColor { get; set; } = Color.White;

    private readonly Texture2D _activeChrome;
    private readonly Texture2D _inactiveChrome;
    private readonly NineSlice.Insets _insets;
    private readonly SpriteFontBase _font;
    private bool _isHovered;

    /// <summary>
    /// Construct a tab. <paramref name="activeChrome"/> is <c>UITheme.TabOn</c>
    /// and <paramref name="inactiveChrome"/> is <c>UITheme.TabOff</c>;
    /// <paramref name="insets"/> are <c>UITheme.TabInsets</c>.
    /// </summary>
    public Tab(string label, Texture2D activeChrome, Texture2D inactiveChrome, NineSlice.Insets insets, SpriteFontBase font)
    {
        Label = label;
        _activeChrome = activeChrome;
        _inactiveChrome = inactiveChrome;
        _insets = insets;
        _font = font;
    }

    /// <inheritdoc />
    public void OnClick() => OnClickAction?.Invoke();

    /// <inheritdoc />
    public void OnHoverEnter() => _isHovered = true;

    /// <inheritdoc />
    public void OnHoverExit() => _isHovered = false;

    /// <summary>Draw tab chrome + label. SpriteBatch must already be open.</summary>
    public void Draw(SpriteBatch sb)
    {
        var tex = IsActive ? _activeChrome : _inactiveChrome;

        // Active tab NEVER nudges on hover (Pitfall 6). Only inactive hovered tabs nudge.
        int nudge = (!IsActive && _isHovered && Enabled) ? -1 : 0;
        var drawRect = new Rectangle(Bounds.X, Bounds.Y + nudge, Bounds.Width, Bounds.Height);
        NineSlice.Draw(sb, tex, drawRect, _insets);

        var size = _font.MeasureString(Label);
        var pos = new Vector2(
            drawRect.X + (drawRect.Width - size.X) / 2f,
            drawRect.Y + (drawRect.Height - size.Y) / 2f);
        sb.DrawString(_font, Label, pos, IsActive ? ActiveTextColor : InactiveTextColor);
    }
}
