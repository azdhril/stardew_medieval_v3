---
quick_id: 260415-uq9
description: Substituir barras de HP e Stamina planas por sprites e adicionar painel decorativo atras da hotbar
date: 2026-04-16
status: code-complete (awaiting human verify)
---

# Quick Task 260415-uq9 — Summary

## Goal
Replace flat-color HP and Stamina bars in the HUD with sprite-based rendering, and add the decorative `UI_Panel_SlotPane` behind the hotbar slots.

## Result
Code-complete. Build passes (0 errors). Awaiting in-game human verification.

## Commits
- `b1799f8` — feat(quick-uq9-01): replace flat HP/Stamina bars with sprite rendering
- `4d9e188` — feat(quick-uq9-02): draw UI_Panel_SlotPane behind main hotbar slots

## Files Modified
- `src/UI/HUD.cs` (+77/-14): sprite-based HP + Stamina bars, source-rect crop fills, fail-secure flat-rect fallback when sprite load fails
- `src/UI/HotbarRenderer.cs` (+28): `UI_Panel_SlotPane` decorative backdrop spanning the 8 main slots only (Q consumable slot unchanged)

## Assets Used
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Bg.png`
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_HP.png`
- `assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_Green.png`
- `assets/Sprites/System/UI Elements/Panel/UI_Panel_SlotPane.png`

## Key Decisions
- Used `Texture2D.FromStream` + `File.OpenRead` to match existing `HotbarRenderer` / `FarmScene` / `ResourceRegistry` pattern. No `Content.mgcb` edits needed.
- Stamina fill: Green (matches "healthy" semantics; sprite IS the color, so dropped 3-tier color ternary).
- Fill technique: source-rect crop from left (pixel-perfect, no end-cap stretching).
- Fail-secure: missing PNG → log `[Module] Failed to load …` and fall back to flat-rect (HUD) or omit panel (Hotbar). Game never crashes.

## Out of Scope (intentionally not done)
- HP icon (`UI_Icon_HP.png`) — optional in plan, deferred
- Minimap, hearts row, title scroll, currency panels, vertical menu buttons, hourglass, XP bar
- No gameplay logic changes

## Awaiting Human Verify
Run `dotnet run` and confirm:
- Pixel-art HP bar renders top-left with crisp left-to-right shrinking fill
- Pixel-art Stamina bar renders bottom-left with crisp left-to-right shrinking fill
- Decorative panel sits behind the 8 hotbar slots (not behind Q)
- Drag/drop and 1-8 hotkeys still function

## Deviations
- An unrelated commit `08f2191 fix(05): wire boss victory effects via OnBossDefeated callback` came in via the executor's worktree base. The change itself is legitimate (Phase 5 UAT Test 4 gap fix), but it was NOT part of this quick task's scope. User should review whether to keep it bundled or split it out.
