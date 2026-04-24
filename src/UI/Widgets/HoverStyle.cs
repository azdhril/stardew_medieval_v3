namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Visual style applied when the pointer is over an <see cref="IClickable"/> widget.
/// The default across concrete widgets is <see cref="NudgeHalo"/>, matching the
/// established pixel-art pattern in ChestScene / ShopPanel (1px upward nudge plus a
/// 4-direction 55%-white halo around the icon).
/// </summary>
public enum HoverStyle
{
    /// <summary>Classic: 1px upward nudge plus 4-direction 55%-white halo.</summary>
    NudgeHalo,

    /// <summary>Tint White on hover, no nudge. Safe for widgets where vertical jitter is undesirable.</summary>
    BrightenOnly,

    /// <summary>No visual feedback. For state-driven widgets that express hover differently (e.g. active tab).</summary>
    None,
}
