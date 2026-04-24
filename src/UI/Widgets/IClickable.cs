using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Minimal contract that every widget registered with <see cref="UIManager"/> must
/// honor. <see cref="UIManager"/> hit-tests, hovers, focuses, and dispatches clicks
/// based on this interface. Drawing is NOT part of the contract because per-widget
/// Draw signatures vary (tint, scale, batched state); scenes Draw their widgets
/// explicitly after <see cref="UIManager.Update"/> has computed the hover/focus
/// state. Log prefix convention: <c>[Widget]</c>.
/// </summary>
public interface IClickable
{
    /// <summary>
    /// Screen-space axis-aligned bounding box used for hit-test. MUST be recomputed
    /// each frame by the scene if the layout is viewport-responsive (see Pitfall 3:
    /// Bounds drift after viewport resize).
    /// </summary>
    Rectangle Bounds { get; }

    /// <summary>
    /// When <c>false</c>, the widget is skipped by hit-test, focus navigation, and
    /// click dispatch, and concrete widgets render a desaturated tint.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Invoked by <see cref="UIManager"/> on an edge-triggered left-click inside
    /// <see cref="Bounds"/> (when <see cref="Enabled"/>) or on Enter/Space when the
    /// widget currently holds keyboard focus.
    /// </summary>
    void OnClick();

    /// <summary>
    /// Optional tooltip text. Returning <c>null</c> disables the tooltip. When set,
    /// <see cref="UIManager"/> surfaces the tooltip after ~500ms of continuous hover.
    /// </summary>
    string? Tooltip => null;

    /// <summary>
    /// Hover-enter hook. Default implementation is a no-op. Override to flip hover
    /// state, trigger animations, or run widget-local sound effects.
    /// </summary>
    void OnHoverEnter() { }

    /// <summary>
    /// Hover-exit hook. Default implementation is a no-op.
    /// </summary>
    void OnHoverExit() { }
}
