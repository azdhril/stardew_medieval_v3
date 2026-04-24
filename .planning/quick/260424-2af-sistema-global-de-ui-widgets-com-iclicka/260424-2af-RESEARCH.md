# Quick Task 260424-2af: Sistema global de UI widgets — Research

**Researched:** 2026-04-24
**Domain:** MonoGame 3.8 / C# 12 retained-mode UI widget framework (in-house)
**Confidence:** HIGH on codebase patterns, MEDIUM on ecosystem conventions

## Summary

The codebase already has the *mechanics* of a widget system, just inlined three times: `ChestScene.DrawIconButton`, `ShopPanel.DrawHoverableIcon`, and `ShopPanel.DrawTab` all implement the same "nudge 1px up + 4-direction 55%-white halo" hover pattern. There are two `Mouse.SetCursor` call sites (`ChestScene:225`, `ShopPanel:551`) and ad-hoc `Rect.Contains(mousePos)` hit-tests scattered across `ChestScene`, `ShopPanel`, `PauseScene`, `InventoryScene`, and `HotbarRenderer`. The migration is less about *invention* and more about *extraction + canonicalization*.

External references (Myra, MonoGame.Extended.Gui, Nez) are all heavier than what this project needs — they ship scene graphs, layout engines, and XAML/JSON loaders. For Stardew Medieval the right pattern is **scene-owned, flat registration, per-frame update** — matching the existing scene lifecycle and the tiny widget count per scene (max ~15 in ChestScene).

**Primary recommendation:** `UIManager` is a **per-scene** object (owned in a protected `Scene` field), NOT a global singleton in `ServiceContainer`. This matches the existing `_deathBanner` / `_levelUpBanner` pattern in `GameplayScene` and sidesteps the scene-transition cleanup problem entirely. Widgets are flat (no tree), registered imperatively in `LoadContent`, hit-tested in update order, drawn in registration order. Cursor + SFX hooks live on `UIManager`. Disagreement with CONTEXT here — see *Deviation from CONTEXT* below.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Framework + migrate all scenes in this one task.** Cenas a migrar: ChestScene, ShopPanel, InventoryScene, PauseScene, DialogueScene (DialogueScene não tem botão clicável hoje — não migra nada, mas fica validada). Critério de pronto: zero `Mouse.SetCursor` manual em cenas.
- **API model:** interface `IClickable` mínima + classes concretas (`IconButton`, `TextButton`, `Tab`, `CloseButton`). NO abstract Widget base class. Extras via default interface methods ou composition.
- **IClickable contrato mínimo:** `Rectangle Bounds`, `bool Enabled`, `void OnClick()`. Opcionais via default interface methods: `OnHoverEnter()`, `OnHoverExit()`, `string? Tooltip`.
- **Hover visual:** embutido padrão (nudge 1px upward + 4-direction 55%-white halo). Override via parâmetro `HoverStyle` no construtor (`NudgeHalo` default, `BrightenOnly`, `None`).
- **Tooltip global:** string property em `IClickable`. `UIManager` triggers após ~500ms de hover estável. Reusa `DrawTooltipPanel` que já existe em ChestScene.
- **Keyboard navigation:** `UIManager._focusedWidget` + Tab/Shift-Tab. Enter/Space aciona `OnClick`. Arrow keys opcional (defer).
- **SFX hooks:** `UIManager.OnHoverSound`, `OnClickSound` como `Action?` delegates. No-op default. Plugam-se sem refactor quando áudio chegar.
- **Input gating:** quando `UIManager` trata um click, não propaga pra scene. Evita double-handling.
- **Focus visual:** outline dourado 1-2px (halo contínuo) ao redor do widget focado.

### Claude's Discretion

- **Diretório/namespace:** `src/UI/Widgets/` + `stardew_medieval_v3.UI.Widgets`. Recommended — matches existing convention (`src/UI/` já é o único namespace flat).
- **UIManager lifecycle:** CONTEXT sugere "live-in `ServiceContainer`, same lifetime do GraphicsDevice, cada scene registra em LoadContent, desregistra em UnloadContent." **Research recomenda scene-owned (protegido em `Scene` base class) por razões abaixo em §Deviation from CONTEXT. PM decide antes do PLAN.md.**

### Deferred Ideas (OUT OF SCOPE)

- Arrow-key keyboard navigation (Tab/Shift-Tab só)
- Actual SFX playback (hooks are stubs)
- Layout engine / anchors / auto-sizing
- Tree hierarchy (widgets são flat, scene posiciona cada um)
- Touch/gamepad input
- Animation tweens (hover é binário on/off)
- Drag-and-drop (já é customizado em InventoryGrid/HotbarRenderer; não vira widget)
- Context menu ("click outside close overlay" em ChestScene — mantém imperativo por enquanto, é estado complexo)

</user_constraints>

## Project Constraints (from CLAUDE.md)

- C# 12 / .NET 8.0, MonoGame 3.8 DesktopGL — **no new NuGet packages**
- One public class per file (`src/UI/Widgets/IconButton.cs`, etc.)
- PascalCase public API, `_camelCase` private fields, 4-space indent, Allman braces
- Nullable reference types enabled — use `string?`, `Action?`, null-forgiving only when guaranteed non-null
- `Try`/`Is` prefix for boolean-returning methods (`TryClick`, `IsHovered`)
- Log with `[ModuleName]` prefix via `Console.WriteLine` (`[UIManager] ...`)
- XML doc comments `/// <summary>` obrigatórias em public API
- Events use `On` prefix (`OnClick`, `OnHoverEnter`)
- Services composed in `Game1.LoadContent` → `ServiceContainer`; scenes pull from there

## Existing Codebase Patterns (HIGH confidence — all verified by Read)

### 1. The hover helper — already extracted twice, needs a single home

`ShopPanel.DrawHoverableIcon` (static, lines 558–577) and `ChestScene.DrawIconButton` (lines 493–522) are **near-identical**: both do integer-scale icon + `int yNudge = hovered ? -1 : 0` + 4 cardinal 1px halo draws at `Color.White * 0.55f` + `Color.White` tint on hover vs `Color.LightGoldenrodYellow`/`new Color(238, 214, 151)` when idle. **This is the canonical `IconButton.DrawSelf(hovered)` body already written — just lift it.**

Same for tabs: `ShopPanel.DrawTab` (lines 600–613) adds `nudge = (!active && hovered) ? -1 : 0` on inactive tabs (active stays anchored — a deliberate distinction to preserve).

### 2. Tooltip panel — already extracted, copy verbatim

`ChestScene.DrawTooltipPanel` (lines 910–928) implements rounded-corner (`TooltipCorner = 4`) dark fill (`Color(40,23,20) * 0.96f`) with golden border (`Color(226,190,114)`) — lift this into `UIManager.DrawTooltip` alongside the tooltip-sizing logic from `ChestScene.DrawItemTooltip` (lines 762–842). The "title + description + stats" structure is item-tooltip specific; the widget tooltip is just string + font → the *rect-drawing* helper is what gets extracted.

### 3. Mouse input — edge-detect is already solved

`InputManager.IsLeftClickPressed` (line 24) already does the edge-detection. **`ChestScene` and `PauseScene` are ignoring this and rolling their own `_wasMouseDown` tracking via raw `Mouse.GetState()`** — bug surface. `UIManager` should consume `InputManager.IsLeftClickPressed` / `MousePosition` exclusively and delete these scene-local trackers.

### 4. Scene lifecycle (Scene.cs)

Exactly 4 virtual methods: `LoadContent`, `Update(dt)`, `Draw(sb)`, `UnloadContent`. There's NO `Initialize`. Widgets register in `LoadContent`. Scenes are popped via `SceneManager.PopImmediate()` which calls `UnloadContent()` — so widget cleanup happens there.

### 5. Lazy theme init pattern

`ChestScene:123–128`, `InventoryScene:49–54`, `FarmScene:182–185` all do the "if null, create and cache in Services" pattern. `UIManager` should follow the same: scenes that need widgets call `_ui = new UIManager(Services.Input, Services.Fonts!, Services.Theme)` in `LoadContent`. It's cheap; no need to share across scenes.

### 6. SpriteBatch state — PointClamp is the sampler

Every `Draw` that renders pixel-art UI opens `sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp)`. Widgets MUST assume they're inside a batch already opened by the scene (do NOT call `Begin`/`End`). CONTEXT says "widgets use `SpriteFontBase`" — that's already the case; `ChestScene._font` is `SpriteFontBase` from `Services.Fonts!.GetFont(FontRole.Body, 18)`.

## Deviation from CONTEXT — Scene-owned vs Services-owned UIManager

CONTEXT (Claude's Discretion) suggests `UIManager` lives in `ServiceContainer`. Research recommends **scene-owned** instead. Present both for PM to decide:

| Aspect | Scene-owned (recommended) | Services-owned (CONTEXT suggested) |
|---|---|---|
| Transitions | Dies with scene → no cleanup bugs | Must `Clear()` on every `UnloadContent` — easy to forget |
| Discovery | Obvious: `this._ui.Register(...)` | Cross-scene: `Services.UI.Register(...)` — whose widgets? |
| Testing | Instantiate inline | Need to stub `ServiceContainer` |
| Pattern match | Matches `_deathBanner`, `_levelUpBanner`, `_hoveredIndex` | Matches `Services.Toast`, `Services.Hud` |
| HUD / Hotbar overlap | HUD widgets (if ever) get their own `UIManager` in HUD | One manager for all → HUD and overlay compete for focus |
| Input gating across stack | Top scene's `UIManager` only — clean | Bottom scene's still-registered widgets intercept? |

**The killer argument:** `SceneManager` draws **all scenes bottom-to-top** (`SceneManager.cs:170`). If `Services.UI` is shared, the FarmScene widgets still exist while ChestScene is pushed on top — they'd get hit-tests. A scene-owned `UIManager` exists only while that scene is on the stack's top and active in `Update`.

**Recommended contract:** `Scene` base class gains a `protected readonly UIManager Ui = new UIManager();`. Each scene calls `Ui.Register(widget)` in `LoadContent`, `Ui.Update(...)` in `Update`, `Ui.Draw(...)` in `Draw`. `Scene.UnloadContent` base disposes `Ui` (but since widgets are managed — no unmanaged resources — dispose is a no-op in practice).

**If PM insists on services-owned:** keep a stack inside `UIManager` (`Push(List<IClickable>)` on scene enter, `Pop()` on exit), and the top frame's widgets are the active set. Adds complexity for no win.

## Standard Stack — no new dependencies

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.* (existing) | `Mouse`, `MouseCursor`, `SpriteBatch` | Already in csproj |
| FontStashSharp | existing (post-260423-tu6) | `SpriteFontBase` for widget labels/tooltips | Already wired via `FontService` |

### Rejected alternatives (investigated)
| Library | Why not |
|---------|---------|
| [Myra](https://github.com/rds1983/Myra) | Full scene graph + layout engine + XAML — massive overkill, brings CPU cost for 5–15 widgets per scene, different draw/update model |
| [MonoGame.Extended.Gui](https://github.com/MonoGame-Extended/Monogame-Extended) | Being deprecated in favor of GUM integration per [MonoGame.Extended blog](https://www.monogameextended.net/blog/monogame-extended-gum/) — moving target |
| [GUM](https://github.com/vchelaru/gum) | Editor-first workflow; we have no UI layout editor and compose via code |
| [Steropes.UI](https://github.com/RabbitStewDio/Steropes.UI) | Low activity, stylesheet-based — wrong model for our inline pixel-art aesthetic |

**Verdict:** Ecosystem libraries solve a problem we don't have (rich cross-platform UI w/ layout). Our widget count is small, our aesthetic is inlined pixel-art, our composition is per-scene code — a 300-LOC in-house framework will outperform any library both in fit and in onboarding cost.

## Architecture Patterns — API shape

### Recommended directory/namespace

```
src/UI/Widgets/
├── IClickable.cs          # interface + HoverStyle enum
├── UIManager.cs           # registration, update, draw, focus, tooltip, SFX hooks
├── Widget.cs              # OPTIONAL — tiny shared state struct (Bounds, Enabled, Hovered flag)
├── IconButton.cs          # texture icon (optional bg), NudgeHalo default
├── TextButton.cs          # 9-slice YellowBtnSmall + text, matches Buy/Sell
├── Tab.cs                 # TabOn/TabOff + text, 3-state (Active/Inactive/Hovered)
└── CloseButton.cs         # thin wrapper over IconButton with BtnIconX preset
```

Namespace: `stardew_medieval_v3.UI.Widgets`. All widgets reference `UITheme` via constructor param (not `Services.Theme` directly — testability + explicit deps).

### `IClickable` — the interface (LOCKED in CONTEXT)

```csharp
namespace stardew_medieval_v3.UI.Widgets;

/// <summary>Hover visual style — default is NudgeHalo (1px up + 55%-white halo).</summary>
public enum HoverStyle
{
    NudgeHalo,    // classic: 1px up + 4-direction halo (matches existing ChestScene/ShopPanel)
    BrightenOnly, // tint White on hover, no nudge (for tabs where you don't want vertical jitter)
    None,         // no visual feedback (for disabled or state-driven widgets)
}

/// <summary>
/// Minimum contract for anything UIManager can hit-test, hover, focus, or click.
/// Widgets also implement Draw — but since signatures vary (tint, scale, batched state),
/// UIManager doesn't call Draw itself. Scenes draw their widgets explicitly in their
/// Draw method, inside their own SpriteBatch.Begin/End pair. See §Rendering Integration.
/// </summary>
public interface IClickable
{
    /// <summary>Screen-space AABB used for hit-test. Recomputed each frame if layout moves.</summary>
    Rectangle Bounds { get; }

    /// <summary>When false, widget ignores hover/click and renders desaturated.</summary>
    bool Enabled { get; }

    /// <summary>Invoked by UIManager on edge-triggered left-click inside Bounds when Enabled.</summary>
    void OnClick();

    /// <summary>Optional tooltip string. Null = no tooltip. UIManager shows after ~500ms hover.</summary>
    string? Tooltip => null;

    /// <summary>Hover-enter hook. Default = no-op. Override to trigger animations/sounds/state.</summary>
    void OnHoverEnter() { }

    /// <summary>Hover-exit hook. Default = no-op.</summary>
    void OnHoverExit() { }
}
```

### `UIManager` — the orchestrator (sketch)

```csharp
namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Per-scene UI orchestrator. Hit-tests registered widgets, tracks hover/focus,
/// dispatches clicks, manages cursor, surfaces tooltips, fires SFX hooks.
/// NOT thread-safe (single-threaded game loop).
/// </summary>
public sealed class UIManager
{
    private readonly List<IClickable> _widgets = new();
    private IClickable? _hovered;
    private IClickable? _focused;
    private float _hoverTime;   // seconds since hover started, for tooltip delay
    private const float TooltipDelay = 0.5f;

    // Focus outline color — matches the existing golden-yellow halo.
    private static readonly Color FocusOutline = new(226, 190, 114);

    /// <summary>Hook invoked when hover transitions from no-widget to any widget. No-op until audio exists.</summary>
    public Action? OnHoverSound { get; set; }
    /// <summary>Hook invoked on successful click dispatch. No-op until audio exists.</summary>
    public Action? OnClickSound { get; set; }

    /// <summary>Register a widget. Order matters for Tab navigation and click priority (first hit wins).</summary>
    public void Register(IClickable w) => _widgets.Add(w);

    /// <summary>Remove a widget (e.g., if a scene rebuilds layout mid-frame). Usually not needed.</summary>
    public bool Unregister(IClickable w) => _widgets.Remove(w);

    /// <summary>Clear all widgets. Called on scene UnloadContent if UIManager is reused.</summary>
    public void Clear() { _widgets.Clear(); _hovered = _focused = null; }

    /// <summary>
    /// Run hit-test + focus + click dispatch. Call once per frame in Scene.Update, BEFORE
    /// scene-specific input handling, so consumed clicks don't propagate.
    /// </summary>
    /// <returns>True if UIManager consumed the click (scene should skip its own click handling).</returns>
    public bool Update(float dt, InputManager input)
    {
        // 1. Hit-test (first registered widget wins to match draw order intuition).
        Point mp = input.MousePosition;
        IClickable? prevHover = _hovered;
        _hovered = null;
        for (int i = 0; i < _widgets.Count; i++)
        {
            var w = _widgets[i];
            if (w.Enabled && w.Bounds.Contains(mp)) { _hovered = w; break; }
        }

        // 2. Hover transitions + SFX + tooltip timer.
        if (_hovered != prevHover)
        {
            prevHover?.OnHoverExit();
            _hovered?.OnHoverEnter();
            _hoverTime = 0f;
            if (_hovered != null) OnHoverSound?.Invoke();
        }
        else if (_hovered != null)
        {
            _hoverTime += dt;
        }

        // 3. Cursor — only SetCursor when state CHANGES (see Pitfall 1).
        bool hoveringClickable = _hovered != null;
        if (hoveringClickable != _wasHoveringClickable)
        {
            Mouse.SetCursor(hoveringClickable ? MouseCursor.Hand : MouseCursor.Arrow);
            _wasHoveringClickable = hoveringClickable;
        }

        // 4. Keyboard focus navigation: Tab / Shift-Tab.
        if (input.IsKeyPressed(Keys.Tab))
        {
            bool back = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
            AdvanceFocus(back);
        }

        // 5. Click dispatch.
        bool consumed = false;
        if (input.IsLeftClickPressed && _hovered != null)
        {
            _focused = _hovered;
            _hovered.OnClick();
            OnClickSound?.Invoke();
            consumed = true;
        }
        else if (_focused != null && (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space)))
        {
            if (_focused.Enabled)
            {
                _focused.OnClick();
                OnClickSound?.Invoke();
                consumed = true;
            }
        }

        return consumed;
    }

    private bool _wasHoveringClickable;

    private void AdvanceFocus(bool backward) { /* wrap-around by index */ }

    /// <summary>
    /// Draw focus outline + tooltip. Call at the END of Scene.Draw, inside the existing
    /// screen-space SpriteBatch.Begin/End pair (so the overlay lives on top of widgets).
    /// </summary>
    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFontBase font, UITheme theme, int viewportW, int viewportH)
    {
        // Focus outline (1px golden ring around _focused, if any + not already _hovered).
        if (_focused != null && _focused.Enabled)
        {
            var b = _focused.Bounds;
            // thicker outline — 2px for readability.
            DrawOutline(sb, pixel, new Rectangle(b.X - 1, b.Y - 1, b.Width + 2, b.Height + 2), FocusOutline, 2);
        }

        // Tooltip — only if hovered widget has one and dwell time exceeded threshold.
        if (_hovered is { Tooltip: { } text } && _hoverTime >= TooltipDelay && !string.IsNullOrEmpty(text))
            DrawTooltip(sb, pixel, font, text, _hovered.Bounds, viewportW, viewportH);
    }

    private static void DrawOutline(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c, int thick) { /* 4 edges */ }
    private static void DrawTooltip(SpriteBatch sb, Texture2D pixel, SpriteFontBase font, string text, Rectangle anchor, int vpW, int vpH)
    {
        // Reuse ChestScene.DrawTooltipPanel rounded-corner logic — extracted verbatim.
        // Position: anchor + 8px to the right; clamp to viewport.
    }
}
```

### `IconButton` — the embedded-hover pattern

```csharp
namespace stardew_medieval_v3.UI.Widgets;

/// <summary>
/// Pixel-art icon button with optional 9-slice background. Default hover = NudgeHalo
/// (1px up + 4-direction 55%-white halo + White tint). Matches the pre-framework
/// DrawHoverableIcon helper in ShopPanel and DrawIconButton helper in ChestScene.
/// </summary>
public sealed class IconButton : IClickable
{
    public Rectangle Bounds { get; set; }    // settable so scenes can re-layout per frame
    public bool Enabled { get; set; } = true;
    public string? Tooltip { get; set; }
    public Action? OnClickAction { get; set; }
    public HoverStyle Hover { get; set; } = HoverStyle.NudgeHalo;
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    public Color IdleTint { get; set; } = Color.LightGoldenrodYellow;
    public Color HoverTint { get; set; } = Color.White;
    public Color DisabledTint { get; set; } = Color.White * 0.35f;

    private readonly Texture2D _icon;
    private readonly Texture2D? _background;    // null = no 9-slice bg, just icon
    private readonly UITheme _theme;            // for BtnSlot insets if background provided
    private bool _isHovered;

    public IconButton(Texture2D icon, UITheme theme, Texture2D? background = null)
    {
        _icon = icon;
        _theme = theme;
        _background = background;
    }

    public void OnClick() => OnClickAction?.Invoke();
    public void OnHoverEnter() => _isHovered = true;
    public void OnHoverExit()  => _isHovered = false;

    /// <summary>
    /// Draw the button at its current Bounds. Assumes SpriteBatch is already open with
    /// BlendState.AlphaBlend + SamplerState.PointClamp. Mirrors ChestScene.DrawIconButton
    /// verbatim for visual parity during migration.
    /// </summary>
    public void Draw(SpriteBatch sb)
    {
        bool hovered = _isHovered && Enabled && Hover != HoverStyle.None;

        if (_background != null)
            NineSlice.Draw(sb, _background, Bounds, _theme.BtnSlotInsets);

        int scale = Math.Max(1, Math.Min(Bounds.Width / _icon.Width, Bounds.Height / _icon.Height));
        int iw = _icon.Width * scale, ih = _icon.Height * scale;
        int yNudge = (hovered && Hover == HoverStyle.NudgeHalo) ? -1 : 0;
        var iconRect = new Rectangle(
            Bounds.X + (Bounds.Width  - iw) / 2,
            Bounds.Y + (Bounds.Height - ih) / 2 + yNudge,
            iw, ih);

        if (hovered && Hover == HoverStyle.NudgeHalo)
        {
            var glow = Color.White * 0.55f;
            sb.Draw(_icon, new Rectangle(iconRect.X - 1, iconRect.Y, iw, ih), null, glow, 0f, Vector2.Zero, Effects, 0f);
            sb.Draw(_icon, new Rectangle(iconRect.X + 1, iconRect.Y, iw, ih), null, glow, 0f, Vector2.Zero, Effects, 0f);
            sb.Draw(_icon, new Rectangle(iconRect.X, iconRect.Y - 1, iw, ih), null, glow, 0f, Vector2.Zero, Effects, 0f);
            sb.Draw(_icon, new Rectangle(iconRect.X, iconRect.Y + 1, iw, ih), null, glow, 0f, Vector2.Zero, Effects, 0f);
        }

        Color tint = !Enabled ? DisabledTint : hovered ? HoverTint : IdleTint;
        sb.Draw(_icon, iconRect, null, tint, 0f, Vector2.Zero, Effects, 0f);
    }
}
```

### Scene integration — sample (ChestScene migrated)

```csharp
public override void LoadContent()
{
    base.LoadContent(); // if Scene base gains one
    _theme = /* lazy init, unchanged */;

    // Build widgets once; Bounds updated per frame by our SetBounds calls in Update/Draw
    // because GetLayout() is viewport-responsive.
    _sendBtn   = new IconButton(_theme.IconArrowRight, _theme, _theme.BtnSlot)
                   { OnClickAction = () => MoveAllFromPlayerToChest(), Tooltip = "Send all" };
    _takeBtn   = new IconButton(_theme.IconArrowRight, _theme, _theme.BtnSlot)
                   { Effects = SpriteEffects.FlipHorizontally,
                     OnClickAction = () => MoveAllFromChestToPlayer(), Tooltip = "Take all" };
    _sortBag   = new IconButton(_theme.IconSort, _theme)
                   { OnClickAction = () => _playerInventory.SortByDefault(), Tooltip = "Sort bag" };
    _sortChest = new IconButton(_theme.IconSort, _theme)
                   { OnClickAction = () => _chest.Container.SortByDefault(), Tooltip = "Sort chest" };
    _closeBtn  = new CloseButton(_theme) { OnClickAction = CloseOverlay };

    Ui.Register(_sendBtn); Ui.Register(_takeBtn);
    Ui.Register(_sortBag); Ui.Register(_sortChest);
    Ui.Register(_closeBtn);
}

public override void Update(float dt)
{
    // Update bounds from the layout helper (viewport-responsive).
    GetPanelPosition(out int px, out int py);
    GetLayout(px, py, out var playerPane, out var chestPane, out _, out _, out _, out _);
    _sendBtn.Bounds   = GetActionButtonRect(playerPane, chestPane, ButtonAction.SendAll);
    _takeBtn.Bounds   = GetActionButtonRect(playerPane, chestPane, ButtonAction.TakeAll);
    _sortBag.Bounds   = GetActionButtonRect(playerPane, chestPane, ButtonAction.SortBolsa);
    _sortChest.Bounds = GetActionButtonRect(playerPane, chestPane, ButtonAction.SortChest);
    _closeBtn.Bounds  = GetCloseButtonRect(px, py);

    // UIManager runs FIRST — consumes click if a widget was hit.
    bool consumed = Ui.Update(dt, Services.Input);
    if (consumed) { _wasMouseDown = true; return; }

    // Rest of existing click/drag logic (grid hit-tests, context menu) — unchanged.
    // Delete DrawIconButton + the 5 Rect.Contains buttons + Mouse.SetCursor — all gone.
}
```

## Don't Hand-Roll — already in place

| Problem | Don't Build | Use Existing |
|---|---|---|
| Mouse edge-detect | Don't track `_wasMouseDown` per scene | `InputManager.IsLeftClickPressed` (already exists, just not adopted) |
| Hover visual | Don't reinvent nudge+halo | Lift from `ChestScene.DrawIconButton` / `ShopPanel.DrawHoverableIcon` |
| Tooltip panel | Don't re-implement rounded corners | Lift from `ChestScene.DrawTooltipPanel` |
| 9-slice drawing | Don't write new stretcher | `NineSlice.Draw(sb, tex, rect, insets)` already used everywhere |
| Font management | Don't load TTFs per widget | `Services.Fonts!.GetFont(FontRole.Body, 18)` |
| Cursor state | Don't call `Mouse.SetCursor` per frame | `UIManager` tracks `_wasHoveringClickable` and only flips on transitions |

## Common Pitfalls

### Pitfall 1: `Mouse.SetCursor` every frame can crash on some MonoGame paths

**What goes wrong:** Current code (`ChestScene:225`, `ShopPanel:551`) calls `Mouse.SetCursor(...)` unconditionally every Draw frame. Per [MonoGame community reports](https://community.monogame.net/t/solved-crash-when-setting-a-hardware-mouse-cursor-from-a-sprite-any-ideas/11209), `Mouse.SetCursor` from `Texture2D` every frame has been observed to crash on Windows. Hardware `MouseCursor.Hand`/`Arrow` is safer, but wasteful.
**Why it happens:** `SetCursor` is a syscall (SDL `SDL_SetCursor` under DesktopGL) — not just a property set. Even if stable, it's unnecessary churn.
**How to avoid:** `UIManager.Update` caches `_wasHoveringClickable`; only calls `SetCursor` when state transitions. **Verified safe with stock `MouseCursor.Hand`/`Arrow`** ([MEDIUM confidence — based on community reports, not profiled]).
**Warning signs:** CPU spike on mouse hover; occasional DesktopGL null-ref on shutdown.

### Pitfall 2: Scene transition leaks cursor state

**What goes wrong:** Pop ChestScene while hovering a button → cursor stays `Hand` in FarmScene until next hover change.
**Why it happens:** `UnloadContent` doesn't reset cursor.
**How to avoid:** `UIManager.Clear()` should restore `Mouse.SetCursor(MouseCursor.Arrow)` unconditionally. Call from `Scene.UnloadContent` override in `Scene` base (or in each migrated scene's `UnloadContent`).

### Pitfall 3: Bounds drift after viewport resize

**What goes wrong:** `ChestScene.GetLayout` recomputes every frame (`GetPanelPosition` reads `Viewport.Width/Height`). If widgets capture `Bounds` in `LoadContent`, they go stale on fullscreen toggle.
**Why it happens:** Viewport changes asynchronously (`Window.ClientSizeChanged`, `ToggleFullscreen`).
**How to avoid:** Scenes MUST update `Widget.Bounds` in `Update` (or the start of `Draw`) — make `Bounds` a `get; set;` auto-property, not `get;` only. The CONTEXT-described `IClickable.Bounds { get; }` contract allows a property where the setter is internal to the widget.

### Pitfall 4: Click-outside-to-close semantics

**What goes wrong:** `ShopPanel.Update` (lines 116–120) uses `!_panelRect.Contains(mp)` to close on outside-click. If `UIManager` hit-tests widgets and returns `consumed=false` for outside, scene still needs the close logic.
**Why it happens:** "Click outside closes me" isn't a widget — it's a scene-level rule.
**How to avoid:** Keep this in the scene's `Update`, after `Ui.Update(...)` returns false. Don't try to force it into the framework. Document as "scene-level responsibility."

### Pitfall 5: Focus visual on disabled widgets

**What goes wrong:** Tab advances onto a disabled button; the gold outline appears but Enter does nothing — confusing.
**How to avoid:** `AdvanceFocus` skips widgets where `!Enabled`; focus outline check also `&& Enabled`.

### Pitfall 6: Active tab vs hovered inactive tab distinction

**What goes wrong:** `ShopPanel.DrawTab` deliberately doesn't nudge the active tab on hover — hover-nudge only on inactive. Naive `HoverStyle.NudgeHalo` would nudge every hovered tab.
**How to avoid:** `Tab` widget has an `IsActive` flag; when true, internally sets `Hover = HoverStyle.None` (or equivalent). Preserves the active/inactive distinction.

### Pitfall 7: Tooltip positioning with cursor near edge

**What goes wrong:** Tooltip renders off-screen to the right for widgets near the right edge.
**How to avoid:** `UIManager.DrawTooltip` clamps X to `viewportW - tooltipWidth - 4`, mirrors `ChestScene.DrawItemTooltip:817`.

## Code Examples — Migration Snippets

### Before (ShopPanel, lines 538–551)
```csharp
bool anyClickable = _closeRect.Contains(mouse)
    || _buyTabRect.Contains(mouse) || _sellTabRect.Contains(mouse);
int visibleForCursor = Math.Min(GetRowCount(), VisibleRows);
for (int i = 0; !anyClickable && i < visibleForCursor; i++) { /* ... */ }
Mouse.SetCursor(anyClickable ? MouseCursor.Hand : MouseCursor.Arrow);
```

### After (framework)
```csharp
// This entire block is DELETED. UIManager sets the cursor based on registered widgets.
```

### Before (ChestScene, lines 210–225)
```csharp
Point mouse = Mouse.GetState().Position;
// ...
DrawIconButton(spriteBatch, sendRect, _theme.IconArrowRight, SpriteEffects.None, sendRect.Contains(mouse), withBackground: true);
// ... 3 more
bool anyHover = sendRect.Contains(mouse) || /*...*/ || closeRect.Contains(mouse);
Mouse.SetCursor(anyHover ? MouseCursor.Hand : MouseCursor.Arrow);
```

### After (framework)
```csharp
_sendBtn.Draw(spriteBatch);
_takeBtn.Draw(spriteBatch);
_sortBag.Draw(spriteBatch);
_sortChest.Draw(spriteBatch);
_closeBtn.Draw(spriteBatch);
// UIManager.Draw was already called at end of Draw to render focus outline + tooltip.
```

## Migration Strategy — recommended order

1. **Land framework** (empty scenes, no migrations). Files: `IClickable.cs`, `HoverStyle.cs`, `UIManager.cs`, `IconButton.cs`, `CloseButton.cs`, `Tab.cs`, `TextButton.cs`. Compile + unit smoke-test (create a UIManager, register a mock `IClickable`, simulate click, assert `OnClick` fired).
2. **Migrate PauseScene first** — lowest risk: 4 buttons, no drag, no grid, no context menu. High signal: proves focus + keyboard nav. Pick this over ChestScene for task ordering.
3. **Migrate ShopPanel** — introduces `Tab` widget variant + `TextButton` (Buy/Sell action buttons). The `+`/`-` qty steppers are `IconButton` variants. Scroll wheel stays scene-level.
4. **Migrate ChestScene** — most complex: validates all widget types work together. Grid / context menu / drag stay imperative (not widgets, per CONTEXT Deferred Ideas).
5. **Migrate InventoryScene** — only has the implicit slot grid (already `InventoryGridRenderer`, not widget-migrated). Scene has no standalone clickable buttons — verify by reading it carefully. DialogueScene has zero clickable buttons — formally exempt.
6. **Audit pass:** `grep "Mouse.SetCursor\|_wasMouseDown\|Mouse.GetState" src/UI src/Scenes` should return 0 hits outside `UIManager`, `InputManager`, `InventoryGridRenderer` (drag state — legitimate), and `HotbarRenderer` (drag state — legitimate).
7. **Sanity `dotnet run`** — per user memory rule `feedback_visual_retest.md`, request visual retest before commit.

**Can old scenes coexist during rollout?** YES — `UIManager` is scene-owned, so un-migrated scenes simply don't use it. Each scene migrates independently. **Do migrations in separate commits** for easier bisect.

## Open Questions

1. **Focus visual on hover?** Should a mouse hover also grab focus (so Enter works after hovering), or is focus strictly keyboard-driven (Tab only)?
   - **Recommendation:** Strictly keyboard-driven. Mouse hover shows `_hovered` distinct from `_focused`. This matches most desktop UIs (Windows Explorer doesn't focus items on hover).
   - **Open to PM:** if the game is mostly mouse-driven, pre-focusing on hover might feel right. Low cost either way; decide in review.

2. **Qty stepper (`ShopPanel._qtyMinusRects` / `_qtyPlusRects`) — widgets or leave inline?**
   - **Recommendation:** Make them `IconButton` with `BtnCircleSmall` background + `IconMinus`/`IconPlus` overlay. 14 `_qtyMinusRects` + 14 `_qtyPlusRects` across visible rows → 28 widgets per ShopPanel. That's fine; hit-test is O(n) and n is tiny.
   - **Alternative:** leave inline — the widget count doubles scene complexity for an internal stepper. Valid call too.

3. **Do `CloseButton`, `TextButton`, `Tab` need separate classes, or are they parameterized `IconButton`?**
   - **Recommendation:**
     - `CloseButton` = thin wrapper: `new IconButton(theme.BtnIconX, theme) { IdleTint = new Color(238,214,151) }`. Could be a static factory on `IconButton` instead of a new class.
     - `TextButton` ≠ `IconButton` — different draw (9-slice bg + centered text, not icon). Separate class.
     - `Tab` ≠ either — 3-state (active/inactive/hovered) + text. Separate class.
   - Final call: ONE widget class per CONTEXT's listed concrete types (`IconButton`, `TextButton`, `Tab`, `CloseButton`). Zero overhead to have them, clearer call sites.

4. **ShopPanel is NOT a Scene — it's a UI component owned by `ShopOverlayScene`.** Where does its `UIManager` live?
   - **Recommendation:** `ShopPanel` gets its own `UIManager` field, owned by `ShopOverlayScene` OR inside `ShopPanel` itself. Lifecycle: `ShopPanel` is constructed in `ShopOverlayScene.LoadContent`; its widgets register there, cleared when scene unloads. **Pass `UIManager` into `ShopPanel.Update(dt, input, ...)` as a parameter** so `ShopPanel` stays agnostic of scene-vs-manager ownership. This keeps `ShopPanel` testable standalone.

5. **Tooltip font — `FontRole.Body 15` or `18`?** Current `ChestScene` item tooltip uses mixed fonts (`_titleFont 24`, `_font 18`, `_smallFont 15`). Widget tooltips are plain one-liners — start with `FontRole.Body 15` (matches `_smallFont`), readable but not noisy. Parameterize in `UIManager` constructor for override.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|---|---|---|---|---|
| MonoGame.Framework.DesktopGL | Mouse, SpriteBatch, Texture2D | ✓ | 3.8.* | — |
| FontStashSharp | SpriteFontBase for labels/tooltips | ✓ | (in-project post-260423-tu6) | — |
| `Services.Theme` (UITheme) | 9-slice backgrounds on IconButton/TextButton/Tab | ✓ | Lazy-init, all migrated scenes already instantiate | — |
| `Services.Fonts` (FontService) | Tooltip + TextButton labels | ✓ | Lazy-init, all migrated scenes already instantiate | — |
| `Services.Input` (InputManager) | `IsLeftClickPressed`, `MousePosition`, `IsKeyPressed` | ✓ | Required by every scene already | — |

No external dependencies, no install steps. Build with existing `dotnet build`.

## Sources

### Primary (HIGH confidence — verified by Read)
- `src/Scenes/ChestScene.cs` — `DrawIconButton` (493–522), `DrawTooltipPanel` (910–928), `Mouse.SetCursor` (225)
- `src/UI/ShopPanel.cs` — `DrawHoverableIcon` (558–577), `DrawTab` (600–613), `Mouse.SetCursor` (551)
- `src/Scenes/PauseScene.cs` — minimal button list pattern (52–102)
- `src/Scenes/InventoryScene.cs` — scene template w/o widgets
- `src/Core/InputManager.cs` — `IsLeftClickPressed` edge-detect (24–26)
- `src/Core/Scene.cs`, `src/Core/GameplayScene.cs`, `src/Core/SceneManager.cs` — lifecycle + transition
- `src/Core/ServiceContainer.cs` — composition root shape
- `src/UI/UITheme.cs` — available textures & insets
- `src/Core/FontService.cs` — FontStashSharp wrapper
- `.planning/quick/260424-2af-CONTEXT.md` — user-locked decisions

### Secondary (MEDIUM confidence — community + docs)
- [MonoGame `Mouse` API docs](https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Mouse.html) — confirms `SetCursor` signature + hardware cursor semantics
- [Community report: SetCursor crash every frame](https://community.monogame.net/t/solved-crash-when-setting-a-hardware-mouse-cursor-from-a-sprite-any-ideas/11209) — motivates Pitfall 1 cache
- [MonoGame.Extended GUM blog](https://www.monogameextended.net/blog/monogame-extended-gum/) — confirms ecosystem libs are in flux; don't adopt mid-transition

### Tertiary (LOW confidence — noted, not cited)
- [Myra](https://github.com/rds1983/Myra), [Steropes.UI](https://github.com/RabbitStewDio/Steropes.UI) — inspected names + surface; confirmed too heavy for our scope. Not used as code reference.

## Metadata

**Confidence breakdown:**
- Existing patterns (hover visual, tooltip, hit-test, cursor): HIGH — Read-verified in 4+ files
- API shape (IClickable, UIManager, widgets): HIGH — straightforward extraction of existing inline code
- Scene-owned vs Services-owned recommendation: MEDIUM — strong argument but contradicts CONTEXT; PM adjudicates
- Pitfall 1 (SetCursor per frame): MEDIUM — based on community reports, not locally profiled
- Ecosystem rejection (Myra/Extended/Nez): HIGH — projects each ship 10–100× our scope; mismatch is obvious

**Research date:** 2026-04-24
**Valid until:** ~2026-05-24 (stable domain; MonoGame 3.8 API surface doesn't drift)

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|---|---|---|
| A1 | `Mouse.SetCursor(MouseCursor.Hand)` every frame is safe-but-wasteful rather than crash-prone on DesktopGL | Pitfall 1 | LOW — caching is cheap either way; worst case framework is extra-safe |
| A2 | 500ms tooltip delay matches Stardew Valley / general RPG convention | UIManager sketch | LOW — trivially tunable via const |
| A3 | Scene.Ui.Update before scene input eliminates double-handling across all migrated scenes | Integration sample | MEDIUM — ChestScene has complex context-menu interactions that may need ordering tweaks |
| A4 | Integer-scale icon math (Math.Max(1, Math.Min(w/icon.w, h/icon.h))) works for all existing widgets at all viewports | IconButton code | LOW — already proven in ChestScene + ShopPanel |
| A5 | Keyboard focus via Tab in a game overlay matches player expectation | UIManager keyboard nav | MEDIUM — players may expect Tab to be gameplay (next target). Defer to PM; ESC/Enter still work without Tab |
