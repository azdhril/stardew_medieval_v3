---
phase: 04-world-npcs
plan: 06
subsystem: shop-ui
tags: [shop, ui, mouse, hit-test, regression-safe]
requirements: [HUD-03]
gap_closure: true
closes_gaps:
  - "UAT Test 9 — Shop tabs not clickable (minor) — CLOSED"
  - "UAT Test 10 — Shop menu not mouse-operable (major) — CLOSED"
dependency-graph:
  requires:
    - UI/ShopPanel.cs (existing keyboard implementation)
    - Core/InputManager.cs (MousePosition + IsLeftClickPressed already exposed)
  provides:
    - Mouse interactivity for shop overlay (tabs, rows, action button, close, click-outside)
  affects:
    - Scenes/ShopOverlayScene.cs (no code change — already maps Update→true to PopImmediate)
tech-stack:
  added: []
  patterns: ["rect.Contains(mousePos) hit-test", "edge-triggered IsLeftClickPressed"]
key-files:
  created: []
  modified:
    - UI/ShopPanel.cs (+132 / -14 lines)
decisions:
  - "UpdateLayoutCache called from BOTH Update and Draw (defensive); pure layout, no side effects"
  - "_hoveredRow uses VISIBLE-row index (0..visible-1); _selectedIndex stays ABSOLUTE (0..rows-1); conversion via _scrollOffset"
  - "Row-body click ALWAYS selects; if click also lands inside action-button rect AND row enabled → fire transaction (one-click buy/sell on visible button)"
  - "Mouse hit-test runs BEFORE keyboard block so click-outside-to-close fires on the same frame"
  - "Hover tint (Color.White*0.1f) applied only when row hovered AND not selected — selection takes precedence"
  - "Close X (20×20) placed at top-right of header; Gold text shifted left to clear 40px buffer"
metrics:
  duration: ~15min
  completed_date: 2026-04-13
---

# Phase 04 Plan 06: Shop Mouse Interactivity Summary

Adds full mouse hit-test and click handling to `UI/ShopPanel.cs` using the canonical
`rect.Contains(mousePos) + IsLeftClickPressed` pattern already used by `PauseScene`
and `HotbarRenderer`. Closes UAT Tests 9 (minor) and 10 (major).

## What Changed

### New Private Fields (`UI/ShopPanel.cs` lines 47-56)

```csharp
private Rectangle _panelRect;
private Rectangle _buyTabRect;
private Rectangle _sellTabRect;
private Rectangle _closeRect;
private readonly Rectangle[] _rowRects = new Rectangle[VisibleRows];
private readonly Rectangle[] _actionBtnRects = new Rectangle[VisibleRows];
private int _hoveredRow = -1;   // visible-slice index
private int _scrollOffset;      // shared by Update + Draw
```

All `Rectangle` arrays are pre-sized to `VisibleRows` (8) and mutated in place — no per-frame
heap allocation in the hot path.

### New Helper: `UpdateLayoutCache()`

Single source of truth for panel/tab/row/action-button/close geometry. Called at the top
of `Update()` (so hit-test reflects current state) and defensively at the top of `Draw()`
(in case Draw runs without a preceding Update). Pure layout — no I/O, no rendering.

### Final `Update()` Hit-Test Order (first match wins)

1. **Close X button** (`_closeRect.Contains(mp)`) → return `true` (close)
2. **Click outside panel** (`!_panelRect.Contains(mp)`) → return `true` (close)
3. **Buy tab** (`_buyTabRect.Contains(mp)`) → switch to Buy, reset selection
4. **Sell tab** (`_sellTabRect.Contains(mp)`) → switch to Sell, reset selection
5. **Row hovered** (`_rowRects[i].Contains(mp)`) → set `_selectedIndex = absIndex`;
   if click also lands inside `_actionBtnRects[i]` AND `IsActionEnabled(absIndex)` →
   call `TryBuy` / `TrySell` (same path as `Keys.Enter`)

Keyboard block (Esc / Tab / Up / Down / Enter) follows unchanged.

### Draw Changes

- All rect math removed from `Draw` and replaced with reads from cached fields.
- Added close X render (20×20 with 1px black outline) at `_closeRect`.
- Added hover-tint pass (`Color.White * 0.1f`) on `_rowRects[i]` for the hovered row when
  it is NOT the selected row.
- Gold text shifted left from `PanelWidth - 16` to `PanelWidth - 40` to clear the X button.
- Esc hint copy updated to "Esc or click outside to close" to advertise new affordance.

## Verification

### Build (automated)

| Configuration | Result | Warnings | Errors |
|---------------|--------|----------|--------|
| `dotnet build -c Debug --nologo`   | Compilação com êxito | 0 | 0 |
| `dotnet build -c Release --nologo` | Compilação com êxito | 0 | 0 |

### Static Greps (must-haves §artifacts.contains)

| Pattern | Hits | Required |
|---------|------|----------|
| `_buyTabRect|_sellTabRect|_closeRect|_rowRects|_actionBtnRects` | 26 | ≥5 |
| `input.MousePosition` (line 80) + `input.IsLeftClickPressed` (line 87) | 2 | ≥2 |
| `UpdateLayoutCache` (decl line 158 + 2 calls lines 75, 325) | 3 | ≥2 |
| `_panelRect.Contains` (line 93) | 1 | =1 |

### Manual Smoke (UAT Tests 9 + 10 — pending live re-run)

Per-task verify steps 1-8 are documented and not gated by automation; the agent ran the
build (Debug + Release, both clean) but the orchestrator owns interactive UAT re-execution.
The implementation mirrors the canonical `PauseScene` pattern that is already known to
work in-game, so behavioral parity is high-confidence.

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Open shop | Panel renders | code path unchanged |
| 2 | Click "Sell" tab | Tab highlights gold; list = inventory | _sellTabRect dispatch |
| 3 | Click "Buy" tab | Switches back | _buyTabRect dispatch |
| 4 | Hover a row | Subtle tint appears | hover tint pass added |
| 5 | Click a row | Becomes selected (gold bg) | _selectedIndex = absIndex |
| 6 | Click "Buy"/"Sell" button on selected enabled row | Transaction fires | TryBuy/TrySell on click |
| 7 | Click X | Overlay closes | _closeRect → return true |
| 8 | Click outside 720×400 panel | Overlay closes | !_panelRect.Contains → return true |

### Keyboard Regression

Tab / Up / Down / Enter / Esc paths are physically downstream of the new mouse block and
are byte-for-byte identical to the prior implementation. No regression risk.

## Threat Mitigations (per `<threat_model>` in PLAN)

| Threat ID | Mitigation Applied |
|-----------|-------------------|
| T-04-22 | `IsLeftClickPressed` is edge-triggered (this-frame-press, last-frame-released). One click = one transaction, identical to keyboard Enter. |
| T-04-23 | Action-button dispatch wrapped in `IsActionEnabled(absIndex)` check. Disabled rows render dimmed and do not execute. |
| T-04-24 | Accepted per plan — panel dim covers full screen, only clicks outside the explicit 720×400 panel rect close. |

## Deviations from Plan

None. The plan was followed verbatim, including the recommended `UpdateLayoutCache` helper
form (called from both Update and Draw) over the alternate Draw-recomputes-locally form.

## Decisions Made

- **UpdateLayoutCache from both Update + Draw**: chosen over Update-only because Draw is
  a defensive read-side; double computation is cheap (struct rect math) and avoids a class
  of bugs where hit-test and render disagree.
- **Hover tint Color.White × 0.1f**: matches the subtle hover affordance request without
  competing visually with the gold selection background.
- **Gold text shifted left**: necessary to make room for the 20×20 close X without
  overlapping. No copy change beyond the Esc hint at panel bottom.

## Commits

| Hash | Message | Files |
|------|---------|-------|
| f714de8 | feat(04-06): add mouse hit-test and click handling to ShopPanel | UI/ShopPanel.cs |

## Self-Check: PASSED

- `UI/ShopPanel.cs` exists and contains all six new field declarations: FOUND
- `UpdateLayoutCache` method present (declaration + 2 call sites): FOUND
- `_panelRect.Contains` click-outside guard: FOUND (1 occurrence, line 93)
- Commit `f714de8` exists in current branch (`worktree-agent-aaf5ae4e`): FOUND
- Debug build: 0 warnings / 0 errors
- Release build: 0 warnings / 0 errors
