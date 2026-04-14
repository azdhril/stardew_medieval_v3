---
status: diagnosed
trigger: "Shop menu has zero mouse interactivity (UAT Phase 04 Tests 9, 10, 12)"
created: 2026-04-13
updated: 2026-04-13
mode: diagnose-only
---

## Architectural Gap

`ShopPanel` was built as a keyboard-only controller: it uses only `IsKeyPressed(Tab/Up/Down/Enter/Escape)` and stores no layout rects outside local `Draw` variables. The codebase **does** have a well-established mouse-UI pattern (hit-test rects + `IsLeftClickPressed` edge detection + hover index) used by `HotbarRenderer`, `InventoryGridRenderer`, and `PauseScene` — the shop simply never adopted it. Additionally, `InputManager` exposes mouse position/click edges but **does not expose scroll-wheel delta** (`ScrollWheelValue`), and `ShopPanel` has no scroll state field at all (scroll is derived on-the-fly in `Draw` from `_selectedIndex`).

## Input Plumbing Status

- `src/Core/InputManager.cs:20` — `MousePosition` exposed ✓
- `src/Core/InputManager.cs:23-25` — `IsLeftClickPressed` edge detector exposed ✓
- `src/Core/InputManager.cs:28-30` — `IsRightClickPressed` edge detector exposed ✓
- **Missing:** no `ScrollWheelDelta` / `ScrollWheelValue` property. `MouseState.ScrollWheelValue` never sampled anywhere in the codebase (grep confirms).

## Existing Patterns to Follow

- `src/Scenes/PauseScene.cs:50-68` — canonical hover+click: build rect → `rect.Contains(mousePos)` → set `_hoveredIndex` → on `IsLeftClickPressed` dispatch by index. Simplest template for shop tabs + action button + close X.
- `src/UI/HotbarRenderer.cs:118-194`, `src/UI/InventoryGridRenderer.cs:115-184` — richer press/release model with `_wasMouseDown` for drag. Overkill for shop unless drag-to-scroll is wanted.
- `src/UI/InventoryGridRenderer.cs:400-418` `HitTestGrid/HitTestEquip` — template for repeated row hit-test helpers.

## Concrete Code Locations Needing Mouse Wiring

| Gap | File:Line | Notes |
|---|---|---|
| Tabs Buy/Sell not clickable | `src/UI/ShopPanel.cs:244-247` (DrawTab) / `60-73` (Update) | Store tab rects; click sets `_tab`, resets `_selectedIndex`. |
| Row hover/select | `src/UI/ShopPanel.cs:272-279` (DrawRow loop) / `75-84` | Compute each row rect `(listX, listY+i*RowHeight, PanelWidth-32, RowHeight-2)`; left-click sets `_selectedIndex = rowIndex + scroll`. |
| Action button (Buy/Sell) click | `src/UI/ShopPanel.cs:353-362` | Button rect `(actionX, y+8, 60, 24)`; click = same path as `Keys.Enter` (`TryBuy`/`TrySell`). |
| Scroll wheel | `src/UI/ShopPanel.cs` whole class | Needs new `_scrollOffset` field (replace on-the-fly `scroll` at :269-270); needs `InputManager.ScrollWheelDelta` first. |
| Scrollbar visual | `src/UI/ShopPanel.cs:272` (list region) | No track/thumb drawn today — purely additive render. Track rect at `PanelX+PanelWidth-20, listY, 8, VisibleRows*RowHeight`. |
| Close by clicking outside | `src/UI/ShopPanel.cs:60-93` Update return | Return `true` when `IsLeftClickPressed && !panelRect.Contains(mousePos)`. |
| X close button | `src/UI/ShopPanel.cs:229-242` (header) | Greenfield: add rect near `PanelX+PanelWidth-16` top-right; click → return `true`. |
| Sell quantity selector | `src/UI/ShopPanel.cs:162-202` `TrySell` + `185` `RemoveAt` | Hardcoded full-stack: `_inv.RemoveAt(slotIndex)` removes whole stack. Needs: (a) quantity state field, (b) +/- buttons or shift-click modifier, (c) swap `RemoveAt` for a per-quantity decrement path on `InventoryManager` (verify if one exists). |

## Data Path: Sell Quantity

`InventoryManager.RemoveAt(slotIndex)` is called unconditionally at `ShopPanel.cs:185` and returns the full stack; the quantity field on `InventorySlot` is then read at :192 for pricing. No quantity parameter is currently being ignored — **there is no quantity plumbing at all**. The fix requires either a new `InventoryManager.RemoveQuantity(slot, n)` API (check if one exists) or in-place `stack.Quantity -= n` + conditional `RemoveAt` when it hits zero.

## Summary for Each Missing Capability

- **Tab click** → add `_buyTabRect`/`_sellTabRect` fields; hit-test in `Update`.
- **Row click/hover** → compute row rects in a helper; add `_hoveredRow` for visual feedback parity with keyboard selection.
- **Action button click** → rect per row; only the selected row's button is active (matches `IsActionEnabled` gating at :355).
- **Scrollbar** → new `_scrollOffset` field + render track/thumb; clicking thumb/track = set offset.
- **Mouse wheel** → extend `InputManager` with `ScrollWheelDelta` (sample `_currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue`), consume in `ShopPanel.Update`.
- **Click outside to close** → panel rect hit-test + `IsLeftClickPressed` → return `true`.
- **X close button** → header rect + click → return `true`.
- **Sell quantity** → new UI widget (+/- or shift/ctrl modifier), new `_sellQty` field, per-quantity inventory decrement.
