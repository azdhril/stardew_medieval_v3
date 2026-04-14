---
phase: 04-world-npcs
plan: 07
status: complete
gap_closure: true
closes_gaps:
  - "UAT Test 12 — Shop scroll + visible scrollbar + partial-stack sell (major)"
---

# Plan 04-07 — Shop scroll wheel + scrollbar + partial-stack sell

## What changed

### `src/Core/InputManager.cs`
- Added `ScrollWheelDelta` property returning `_currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue`.
- Positive = wheel up, negative = wheel down. Native MonoGame delta is ±120 per tick; consumers normalize with `Math.Sign`.
- No changes to `Update()` (already samples `_currentMouse`/`_previousMouse`).

### `src/Inventory/InventoryManager.cs`
- Added `ItemStack? RemoveQuantity(int index, int quantity)` after `RemoveAt`.
- Semantics:
  - Returns `null` for out-of-range index, empty slot, or `quantity <= 0`.
  - If `quantity >= stack.Quantity`, delegates to `RemoveAt` (full clear).
  - Otherwise mutates `stack.Quantity -= quantity` in place (ItemStack.Quantity is mutable — confirmed via `TryAdd`/`TryConsume` both mutating it directly), returns a new `ItemStack` with the removed portion, fires `OnInventoryChanged`.
- Mutation strategy: **in-place** (not replace). Matches existing codebase patterns (`TryAdd` line ~106, `TryConsume` line ~140).

### `src/UI/ShopPanel.cs`
- **New fields:**
  - `_sellQty` (default 1) — selling quantity on the Sell tab.
  - `_lastTabForQty`, `_lastSelectedIndexForQty` — trigger reset of `_sellQty` to 1 when tab/row changes.
  - `_scrollTrackRect`, `_scrollThumbRect` — scrollbar render/hit-test geometry.
  - `_qtyMinusRect`, `_qtyLabelRect`, `_qtyPlusRect` — sell-quantity widget geometry.
- **`_scrollOffset` behavior change:** now user-controlled via wheel. `UpdateLayoutCache` clamps it to `[0, max(0, rows - VisibleRows)]`, then follows selection to guarantee the selected row remains visible.
- **Wheel consumption:** `Update` reads `input.ScrollWheelDelta` and shifts `_scrollOffset` by `Math.Sign(wheel)` (one tick = one row) before layout.
- **Sell-qty widget mouse handling:** Added before the row hit-test in the `IsLeftClickPressed` block so the small `-`/`+` buttons win over the underlying row. Mouse `+` clamps to `stack.Quantity` via `CurrentSellStack()`.
- **Sell-qty widget keyboard handling:** `Keys.Left` decrements, `Keys.Right` increments, only on Sell tab with a valid row.
- **`TrySell` rewired to `RemoveQuantity`:** `qty = Math.Clamp(_sellQty, 1, stack.Quantity)`. Credits `price * qty`. Resets `_sellQty = 1` after sell. Re-clamps `_scrollOffset` to new `remainingRows - VisibleRows` range. Toast now reads `"Sold {def.Name} x{qty} for {total}g"`.
- **Scrollbar render (Draw):** when `rows > VisibleRows`, draws track (`Bevel * 0.5f`) + thumb (`Gold * 0.85f`) proportional to `VisibleRows/rows`, position proportional to `_scrollOffset/(rows - VisibleRows)`, minimum thumb height 16px.
- **Sell-qty widget render (Draw):** only on Sell tab when `_qtyLabelRect != Rectangle.Empty`; renders `[ - ][ xN ][ + ]` pill to the LEFT of the selected row's Sell action button.
- Buy tab: no qty widget (1-unit-per-click semantics unchanged).

### `CurrentSellStack()` helper
Resolves the live `ItemStack` for `_selectedIndex` on the Sell tab (or `null` on Buy / empty selection). Shared between keyboard `+` and mouse `+` paths to ensure consistent clamping.

## Verification

- `dotnet build -c Debug` → 0 CS errors, 0 new warnings (only pre-existing `GameplayScene.cs:156 CS8602`).
- Grep validation:
  - `_scrollOffset` → 8+ hits in ShopPanel.cs.
  - `_sellQty` → 10+ hits.
  - `_scrollTrackRect | _scrollThumbRect` → 6+ hits.
  - `RemoveQuantity` → 1 hit in ShopPanel.cs (TrySell), 1 definition in InventoryManager.cs.
  - `ScrollWheelDelta` → 1 definition (InputManager), 1 consumer (ShopPanel).
- UAT Test 12 runtime walkthrough: **not executed** (review-only verification — code path is deterministic).

## Threat mitigations applied (from PLAN threat model)

- **T-04-25** (over-sell): `Math.Clamp(_sellQty, 1, stack.Quantity)` in TrySell + clamp on mouse/keyboard `+` paths; `RemoveQuantity` defensively delegates to `RemoveAt` when `quantity >= stack.Quantity`.
- **T-04-26** (scroll out-of-range): `UpdateLayoutCache` clamps `_scrollOffset` to `[0, maxScroll]` every frame.
- **T-04-27** (wheel spam): `Math.Sign(wheel)` normalizes magnitude — one tick = one row.
- **T-04-28** (zero/negative qty): `RemoveQuantity` early-return on `quantity <= 0`.

## Key files modified
- `src/Core/InputManager.cs`
- `src/Inventory/InventoryManager.cs`
- `src/UI/ShopPanel.cs`

## Commits
- `3b74a22`: feat(04-07): add ScrollWheelDelta and RemoveQuantity APIs
- `5741efd`: feat(04-07): add scroll wheel, scrollbar, and partial-stack sell to shop
