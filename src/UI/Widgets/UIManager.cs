using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Per-scene UI orchestrator. Hit-tests the registered <see cref="IClickable"/>
/// widgets each frame, tracks hover/focus transitions, dispatches clicks,
/// manages the hardware cursor, surfaces tooltips after a short dwell, and
/// exposes hover/click SFX hooks. Intentionally NOT thread-safe (single-threaded
/// game loop). Owned per-scene via <c>Scene.Ui</c>; never a global singleton
/// (see RESEARCH §Deviation from CONTEXT — scene-owned avoids cross-scene hit-test
/// leaks when overlays are pushed on the stack).
/// </summary>
public sealed class UIManager
{
    /// <summary>Dwell time required before a widget tooltip surfaces (500ms per CONTEXT D-04).</summary>
    private const float TooltipDelay = 0.5f;

    /// <summary>Focus outline color (matches the existing golden hover halo).</summary>
    private static readonly Color FocusOutline = new(226, 190, 114);

    private readonly List<IClickable> _widgets = new();
    private IClickable? _hovered;
    private IClickable? _focused;
    private float _hoverTime;
    private bool _wasHoveringClickable;
    private static bool _didFirstRegisterLog;

    /// <summary>
    /// Hook invoked when hover transitions from no-widget to any widget. No-op by
    /// default. Replace with an audio callback once the project has an SFX system
    /// wired — CONTEXT extra-3.
    /// </summary>
    public Action? OnHoverSound { get; set; }

    /// <summary>
    /// Hook invoked on a successful click dispatch (mouse or keyboard activation).
    /// No-op by default — CONTEXT extra-3.
    /// </summary>
    public Action? OnClickSound { get; set; }

    /// <summary>
    /// Register a widget. The first registered widget wins hit-test when multiple
    /// widgets overlap — register in visual priority order.
    /// </summary>
    public void Register(IClickable w)
    {
        _widgets.Add(w);
        if (!_didFirstRegisterLog)
        {
            _didFirstRegisterLog = true;
            Console.WriteLine("[UIManager] Registered first widget; widget framework active.");
        }
    }

    /// <summary>Remove a widget (rare — used when a scene rebuilds layout mid-frame).</summary>
    public bool Unregister(IClickable w)
    {
        if (_hovered == w) _hovered = null;
        if (_focused == w) _focused = null;
        return _widgets.Remove(w);
    }

    /// <summary>
    /// Clear every registered widget and restore the cursor to
    /// <see cref="MouseCursor.Arrow"/>. Called by <see cref="Scene.UnloadContent"/>
    /// to avoid the stale Hand cursor leak when the scene is popped while a widget
    /// is hovered (Pitfall 2).
    /// </summary>
    public void Clear()
    {
        _widgets.Clear();
        _hovered = null;
        _focused = null;
        _hoverTime = 0f;
        if (_wasHoveringClickable)
        {
            Mouse.SetCursor(MouseCursor.Arrow);
            _wasHoveringClickable = false;
        }
    }

    /// <summary>
    /// Run hit-test, hover transitions, cursor, focus navigation, and click
    /// dispatch. Call once per frame at the TOP of <see cref="Scene.Update"/>,
    /// BEFORE any scene-specific input handling, so consumed clicks do not
    /// propagate to imperative handlers underneath (CONTEXT "input gating").
    /// </summary>
    /// <returns><c>true</c> if the manager consumed the left-click this frame.</returns>
    public bool Update(float dt, InputManager input)
    {
        // 1. Hit-test (first registered widget wins on overlap).
        Point mp = input.MousePosition;
        IClickable? prevHover = _hovered;
        _hovered = null;
        for (int i = 0; i < _widgets.Count; i++)
        {
            var w = _widgets[i];
            if (w.Enabled && w.Bounds.Contains(mp))
            {
                _hovered = w;
                break;
            }
        }

        // 2. Hover transitions + SFX + tooltip timer.
        if (_hovered != prevHover)
        {
            prevHover?.OnHoverExit();
            _hovered?.OnHoverEnter();
            _hoverTime = 0f;
            if (_hovered != null)
                OnHoverSound?.Invoke();
        }
        else if (_hovered != null)
        {
            _hoverTime += dt;
        }

        // 3. Cursor cache (Pitfall 1) — only call SetCursor when state CHANGES.
        bool hoveringClickable = _hovered != null;
        if (hoveringClickable != _wasHoveringClickable)
        {
            Mouse.SetCursor(hoveringClickable ? MouseCursor.Hand : MouseCursor.Arrow);
            _wasHoveringClickable = hoveringClickable;
        }

        // 4. Focus navigation: Tab / Shift-Tab.
        if (input.IsKeyPressed(Keys.Tab))
        {
            bool backward = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
            AdvanceFocus(backward);
        }

        // 5. Click dispatch (mouse edge-click OR keyboard Enter/Space on focused widget).
        bool consumed = false;
        if (input.IsLeftClickPressed && _hovered != null && _hovered.Enabled)
        {
            _focused = _hovered;
            _hovered.OnClick();
            OnClickSound?.Invoke();
            consumed = true;
        }
        else if (_focused != null && _focused.Enabled
                 && (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space)))
        {
            _focused.OnClick();
            OnClickSound?.Invoke();
            consumed = true;
        }

        return consumed;
    }

    /// <summary>
    /// Draw focus outline around the focused widget and tooltip for the hovered
    /// widget (if its tooltip is non-null and the dwell threshold has been met).
    /// Call at the END of <see cref="Scene.Draw"/>, inside the scene's open
    /// screen-space SpriteBatch pair, so overlays render on top of widget art.
    /// </summary>
    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFontBase font, int viewportW, int viewportH)
    {
        // Focus outline — 2px golden ring around the focused widget (Pitfall 5: skip if disabled).
        if (_focused != null && _focused.Enabled)
        {
            var b = _focused.Bounds;
            var ring = new Rectangle(b.X - 1, b.Y - 1, b.Width + 2, b.Height + 2);
            WidgetHelpers.DrawOutline(sb, pixel, ring, FocusOutline, 2);
        }

        // Tooltip — only if hovered widget opts in and dwell time exceeded.
        if (_hovered == null) return;
        string? tip = _hovered.Tooltip;
        if (string.IsNullOrEmpty(tip)) return;
        if (_hoverTime < TooltipDelay) return;

        var textSize = font.MeasureString(tip);
        int padX = 8;
        int padY = 6;
        int tipW = (int)Math.Ceiling(textSize.X) + padX * 2;
        int tipH = (int)Math.Ceiling(textSize.Y) + padY * 2;

        // Anchor to the right of the widget; clamp to viewport (Pitfall 7).
        int anchorX = _hovered.Bounds.Right + 8;
        int anchorY = _hovered.Bounds.Y;
        int x = Math.Clamp(anchorX, 4, viewportW - tipW - 4);
        int y = Math.Clamp(anchorY, 4, viewportH - tipH - 4);

        var tipRect = new Rectangle(x, y, tipW, tipH);
        WidgetHelpers.DrawTooltipPanel(sb, pixel, tipRect);
        sb.DrawString(font, tip, new Vector2(x + padX, y + padY), Color.LightGoldenrodYellow);
    }

    /// <summary>
    /// Advance keyboard focus to the next (or previous) enabled widget with
    /// wrap-around. Skips disabled widgets (Pitfall 5). No-op if no widget is
    /// enabled.
    /// </summary>
    private void AdvanceFocus(bool backward)
    {
        if (_widgets.Count == 0) return;

        int start = _focused != null ? _widgets.IndexOf(_focused) : -1;
        int step = backward ? -1 : 1;
        int n = _widgets.Count;

        for (int i = 1; i <= n; i++)
        {
            int idx = ((start + step * i) % n + n) % n;
            if (_widgets[idx].Enabled)
            {
                _focused = _widgets[idx];
                return;
            }
        }
        // No enabled widget — leave _focused as-is (likely stale; caller decides).
    }
}
