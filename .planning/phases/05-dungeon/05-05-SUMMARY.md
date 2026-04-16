---
phase: 05-dungeon
plan: 05
subsystem: dialogue-viewport-and-hud-gold
tags: [dialogue, hud, viewport, gap-closure, uat]
requires:
  - "Plan 05-04 dungeon rendering gap closure (needed floors/enemies visible before dialogue could be tested in context)"
  - "Existing HUD left-column stamina/HP/MP bar layout (the gold label re-uses barX + nextBarY)"
  - "Existing InventoryManager.Gold + OnGoldChanged event (no state-model change required)"
provides:
  - "Viewport-aware DialogueBox dim overlay + panel positioning (fullscreen + any window size)"
  - "HUD live gold readout in the left column under the Stamina bar"
  - "Closes 05-UAT Gap 1 (dialogue overlay + HUD gold)"
affects:
  - "src/UI/DialogueBox.cs (hardcoded 960x540 removed, Draw signature extended with viewportWidth/Height)"
  - "src/Scenes/DialogueScene.cs (reads Services.GraphicsDevice.Viewport and threads into Draw)"
  - "src/UI/HUD.cs (added InventoryManager? _inventory field + gold label render block)"
  - "src/Scenes/FarmScene.cs (HUD ctor call now passes Services.Inventory)"
  - "OUT-OF-BAND: src/Core/Camera.cs (public Reclamp() method)"
  - "OUT-OF-BAND: Game1.cs (OnClientSizeChanged + ToggleFullscreen call Camera.Reclamp)"
  - "OUT-OF-BAND: src/UI/ShopPanel.cs (viewport-aware port mirroring DialogueBox fix)"
  - "OUT-OF-BAND: src/Scenes/ShopOverlayScene.cs (threads viewport into ShopPanel.Update/Draw)"
tech-stack:
  added: []
  patterns:
    - "Viewport-aware overlay: additive int viewportWidth/viewportHeight params at the end of Draw signatures; no engine-handle leak into the renderer"
    - "Scene threads Services.GraphicsDevice.Viewport into the overlay component each frame (same pattern used by PauseScene/InventoryScene/ChestScene)"
    - "Additive nullable constructor param on HUD so other call sites stay compatible (defensive, not required by current code)"
key-files:
  created:
    - ".planning/phases/05-dungeon/05-05-SUMMARY.md"
  modified:
    - "src/UI/DialogueBox.cs"
    - "src/Scenes/DialogueScene.cs"
    - "src/UI/HUD.cs"
    - "src/Scenes/FarmScene.cs"
    - "src/Core/Camera.cs"
    - "Game1.cs"
    - "src/UI/ShopPanel.cs"
    - "src/Scenes/ShopOverlayScene.cs"
decisions:
  - "Panel size (880x120) kept constant — the fix is positioning, not scaling. Keeps the windowed-mode look approved by the user and avoids introducing DPI/scaling policy."
  - "Gold label placed in the left column under Stamina bar rather than top-right — keeps all stat readouts grouped and avoids clashing with the quest tracker."
  - "HUD ctor param is nullable + default null so the class stays defensively robust if any future scene builds a HUD without inventory."
  - "OUT-OF-BAND: Camera.Reclamp is a thin public wrapper over private ClampToBounds rather than making ClampToBounds public — keeps the contract narrow (callers signal 'viewport may have changed' without knowing internals)."
  - "OUT-OF-BAND: ShopPanel centered VERTICALLY in viewport (was top-anchored at PanelY=48) — matches the 'main dialog' treatment used by other overlays and gives correct visual layout at all window sizes."
metrics:
  duration: 40min
  completed: 2026-04-16
  tasks: 3
  commits: 4
requirements: [DNG-01, NPC-04, HUD-05]
---

# Phase 05 Plan 05: Dialogue Overlay + HUD Gold Summary

Closed 05-UAT Gap 1 by making the DialogueBox dim overlay + panel positioning viewport-aware (dropped hardcoded 960x540 constants, threaded viewport through Draw) and adding a live `Gold: N` readout to the HUD left column. Joint human-verify (together with 05-06) surfaced two adjacent viewport bugs — Issue A (Camera desync on F11 mid-dialogue) and Issue B (ShopPanel hardcoded 960x540) — which were fixed out-of-band to make the verify gate pass.

## What Changed

### Task 1 — Viewport-aware DialogueBox (commit `d7822fd`, prior session)
- **src/UI/DialogueBox.cs:** removed `ScreenWidth`/`ScreenHeight` consts and the `PanelX`/`PanelY` derived statics; extended `Draw` with `int viewportWidth, int viewportHeight` as the last two params (additive signature change); every Rectangle/Vector now uses runtime `panelX = (viewportWidth - PanelWidth) / 2` and `panelY = viewportHeight - 32 - PanelHeight`. Dim overlay draws `new Rectangle(0, 0, viewportWidth, viewportHeight)`.
- **src/Scenes/DialogueScene.cs:** `Draw` reads `var vp = Services.GraphicsDevice.Viewport;` and forwards `vp.Width, vp.Height` into `_box.Draw(...)`.

### Task 2 — HUD gold label (commit `4cf19a5`, prior session)
- **src/UI/HUD.cs:** added `private InventoryManager? _inventory` field, ctor now takes `InventoryManager? inventory = null` as the last param. Inside `Draw`, after the Stamina bar block, renders a drop-shadowed `Gold: N` label at `(barX, nextBarY + staminaBarHeight + spacing)`.
- **src/Scenes/FarmScene.cs:** HUD construction now passes `Services.Inventory`.

### Task 3 — Human-verify
- **Resume signal:** "todos os testes dos 3 rounds passaram" (2026-04-16, joint with 05-06 + out-of-band fixes).
- All 3 rounds passed: dialogue fullscreen, 05-06 chest one-time loot, AND shop fullscreen.

## Deviations from Plan

### Out-of-band Fix A — Camera.Reclamp on viewport change (commit `ed49f4b`)

**Found during:** joint human-verify (Round A).

**Issue:** Toggling F11 fullscreen *while* DialogueScene was on top left the camera showing off-map / misaligned tiles until the dialogue closed. Root cause: `SceneManager` only updates the top scene, so the paused gameplay scene under the overlay never runs `Camera.Follow()` → `ClampToBounds()` was never re-evaluated against the new viewport/zoom.

**Fix:**
- Added `public void Reclamp()` on `Camera` that simply calls the private `ClampToBounds()`.
- `Game1.OnClientSizeChanged` and `Game1.ToggleFullscreen` now call `_services?.Camera.FitZoomToViewport(3.0f); _services?.Camera.Reclamp();` immediately after the viewport change.

**Classification:** correctness fix — the bug existed before 05-05 but was exposed by the new viewport-aware dialogue overlay making the misalignment impossible to miss.

**Scope note:** adjacent to (not within) the plan's stated scope. Documented here rather than spawning a follow-up plan because it's a 3-line wire-up to make the new viewport-aware code behave correctly under F11.

### Out-of-band Fix B — Viewport-aware ShopPanel (commit `5c1d5b6`)

**Found during:** joint human-verify (Round A, user observed during dialogue verify).

**Issue:** `ShopPanel` had the identical hardcoded-960x540 bug that DialogueBox carried — `private const int ScreenWidth = 960;`, `PanelX = (ScreenWidth - PanelWidth) / 2`, `PanelY = 48`, and `sb.Draw(pixel, new Rectangle(0, 0, 960, 540), Dim);` at line 452. In fullscreen the dim didn't cover the screen and the panel sat top-anchored.

**Fix:** mirror-ported the DialogueBox pattern:
- Dropped the hardcoded constants.
- Added instance `_panelX` / `_panelY` fields, computed each frame inside `UpdateLayoutCache(int viewportWidth, int viewportHeight)`.
- Extended `ShopPanel.Update` and `ShopPanel.Draw` signatures with additive `int viewportWidth, int viewportHeight` params.
- `ShopOverlayScene` reads `Services.GraphicsDevice.Viewport` and threads it through both.
- Panel now centers VERTICALLY in the viewport (was top-anchored at Y=48) — intentional change so fullscreen looks right; matches other "main dialog" overlays.

**Classification:** Rule 2 (auto-add missing critical functionality) — the bug was literally the same gap as Test 5 Gap 1, just in another overlay that the original plan didn't scope. Fixing it was required to make the human-verify round pass.

## Anti-Pattern Recorded

Before declaring any viewport-awareness work complete, **grep `src/UI/` and `src/Scenes/` for `960` / `540` literals and `ScreenWidth` / `ScreenHeight` consts**. Every overlay scene must thread viewport dimensions into Draw. Both DialogueBox and ShopPanel carried identical variants of the same bug.

Similarly, after any viewport change (fullscreen toggle, window resize), **call `Camera.Reclamp()`** so paused scenes underneath any overlay stay correctly framed — the paused scene's `Update` never runs and `Camera.Follow` never re-clamps on its own.

## Verification Results

- [x] `src/UI/DialogueBox.cs` no longer contains `ScreenWidth` / `ScreenHeight` tokens.
- [x] `DialogueBox.Draw` signature ends with `..., int viewportWidth, int viewportHeight)`.
- [x] Dim overlay Rectangle uses viewport dimensions, not `960, 540`.
- [x] `DialogueScene.Draw` reads `Services.GraphicsDevice.Viewport` and forwards width/height.
- [x] HUD left column shows live `Gold: N` under Stamina bar.
- [x] `dotnet build` clean (only pre-existing CS8602 warning in GameplayScene.cs:221, unrelated).
- [x] `dotnet test --filter "FullyQualifiedName~Dungeon"` — 24/24 pass (no regressions).
- [x] Human verify round A + round C: approved.
- [x] `ShopPanel` no longer contains hardcoded `960` / `540` / `ScreenWidth` / `PanelX` / `PanelY`.
- [x] `Camera.Reclamp` wired into both `Game1.OnClientSizeChanged` and `Game1.ToggleFullscreen`.

## Handoff

The dialogue overlay + shop overlay + HUD gold readout are all correct at 960x540 windowed, 1280x720 windowed, and fullscreen. Downstream plans that push new overlays on the scene stack should:
1. Add viewport params (additive, last) to any full-screen component's Draw.
2. Thread `Services.GraphicsDevice.Viewport` from the scene into the component each frame.
3. Do NOT scale the panel itself with viewport — keep fixed sizes and re-anchor at runtime.

## Self-Check: PASSED

- Commit `d7822fd` — FOUND (Task 1, DialogueBox + DialogueScene).
- Commit `4cf19a5` — FOUND (Task 2, HUD + FarmScene).
- Commit `ed49f4b` — FOUND (out-of-band Camera.Reclamp).
- Commit `5c1d5b6` — FOUND (out-of-band ShopPanel port).
- File `.planning/phases/05-dungeon/05-05-SUMMARY.md` — created (this file).
