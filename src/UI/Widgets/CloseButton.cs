using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Close-X button — thin preset over <see cref="IconButton"/> using the theme's
/// <c>BtnIconX</c> glyph with a cream idle tint (matches the pre-framework
/// <c>ChestScene.DrawCloseButton</c> behaviour). Kept as a dedicated class per
/// CONTEXT §decisions (explicit widget for call-site clarity), even though it is
/// a parameterization of <see cref="IconButton"/>.
/// </summary>
public sealed class CloseButton : IconButton
{
    /// <summary>
    /// Construct a close button from the shared <paramref name="btnIconX"/>
    /// texture (typically <c>UITheme.BtnIconX</c>).
    /// </summary>
    public CloseButton(Texture2D btnIconX) : base(btnIconX, background: null)
    {
        IdleTint = new Color(238, 214, 151);
        HoverTint = Color.White;
        Tooltip = null;
    }
}
