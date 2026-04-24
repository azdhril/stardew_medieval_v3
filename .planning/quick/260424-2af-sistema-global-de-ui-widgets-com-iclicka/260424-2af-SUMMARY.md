---
phase: quick-260424-2af
plan: 01
subsystem: UI/Widgets
tags: [ui, widgets, refactor, framework, hover, tooltip, focus]
requires: [FontStashSharp/FontService (quick 260423-tu6), UITheme (Phase 04), Scene base class]
provides:
  - src/UI/Widgets/ framework (8 files, ~685 LOC, zero new deps)
  - protected Scene.Ui field for all future scenes
  - Automatic cursor reset on scene pop (Pitfall 2 fix)
  - Tab/Shift-Tab focus navigation with Enter/Space activation across migrated overlays
  - 500ms dwell tooltip with viewport-clamped right-anchor
  - Hover/click SFX hooks (no-op, ready for audio wiring)
affects:
  - src/Scenes/PauseScene.cs        # full widget migration
  - src/Scenes/ShopOverlayScene.cs  # base.UnloadContent + passes Ui to ShopPanel
  - src/UI/ShopPanel.cs             # tabs + close + qty steppers + action buttons via widgets
  - src/Scenes/ChestScene.cs        # action buttons + close via widgets; drag/context stay imperative
  - src/Scenes/InventoryScene.cs    # edge-detect via InputManager + base.UnloadContent
  - src/Core/Scene.cs               # protected UIManager Ui + cursor reset
tech-stack:
  added: []
  patterns:
    - IClickable + per-scene UIManager (flat registration, first-registered wins hit-test)
    - Widget pool per visible row (ShopPanel) — Bounds/Enabled/Label refreshed per frame
    - Lazy theme lookup via Services.Theme (matches ChestScene pattern)
    - Framework-owned SetCursor cache avoids per-frame syscall churn
key-files:
  created:
    - src/UI/Widgets/IClickable.cs       # 51 LOC — interface + default Tooltip/OnHoverEnter/OnHoverExit
    - src/UI/Widgets/HoverStyle.cs       # 19 LOC — enum (NudgeHalo/BrightenOnly/None)
    - src/UI/Widgets/WidgetHelpers.cs    # 82 LOC — DrawNudgeHalo, DrawTooltipPanel, DrawOutline
    - src/UI/Widgets/UIManager.cs        # 224 LOC — orchestrator with focus, tooltip, cursor, SFX hooks
    - src/UI/Widgets/IconButton.cs       # 101 LOC — icon + optional 9-slice bg + NudgeHalo hover
    - src/UI/Widgets/TextButton.cs       # 97 LOC  — 9-slice chrome + centered label
    - src/UI/Widgets/Tab.cs              # 86 LOC  — 3-state (Active never nudges — Pitfall 6)
    - src/UI/Widgets/CloseButton.cs      # 25 LOC  — IconButton preset with BtnIconX + cream idle
  modified:
    - src/Core/Scene.cs                  # +Ui field, +base.UnloadContent cursor reset
    - src/Scenes/PauseScene.cs           # migrated to TextButton widgets
    - src/Scenes/ShopOverlayScene.cs     # wires Ui into ShopPanel.BuildWidgets/Update/Draw
    - src/UI/ShopPanel.cs                # Tab + CloseButton + IconButton pool + TextButton pool
    - src/Scenes/ChestScene.cs           # IconButton x3 + CloseButton; drag state kept imperative
    - src/Scenes/InventoryScene.cs       # IsLeftClickPressed edge + Ui overlay
decisions:
  - Scene-owned UIManager (recommended by RESEARCH §Deviation) adopted over CONTEXT's services-owned suggestion — avoids cross-scene hit-test leaks when overlays stack
  - Active tab NEVER nudges on hover (Pitfall 6) — preserved via Tab.Draw checking IsActive
  - Drag state machine stays imperative (CONTEXT Deferred Ideas) — _wasMouseDown retained in ChestScene/InventoryScene with explanatory comments
  - Outside-click close remains scene-level (ShopPanel) — not a widget concept (Pitfall 4)
  - SetCursor cached at manager level; called only on hover-state transitions (Pitfall 1)
  - IconButton background configurable via (texture, insets) so any 9-slice chrome works — CommonBtn for chest actions, BtnCircleSmall for qty steppers
metrics:
  tasks_completed: 8
  tasks_total: 9  # Task 9 is human-verify — NOT marked complete per plan directive
  commits: 8
  duration: auto
  completed: 2026-04-23
---

# Quick Task 260424-2af: Sistema global de UI widgets com IClickable + UIManager

UI widget framework under `src/UI/Widgets/` plus migration of 4 clickable surfaces (PauseScene, ShopPanel, ChestScene action cluster, InventoryScene legacy mouse-state cleanup) to scene-owned `UIManager` that handles hit-test, hover, cursor cache, Tab/Shift-Tab focus, Enter/Space activation, 500ms dwell tooltip, and hover/click SFX hooks.

## Framework Files (src/UI/Widgets/)

| File               | LOC | Role                                                           |
| ------------------ | --- | -------------------------------------------------------------- |
| IClickable.cs      |  51 | Interface — Bounds/Enabled/OnClick + defaults                  |
| HoverStyle.cs      |  19 | Enum — NudgeHalo (default) / BrightenOnly / None               |
| WidgetHelpers.cs   |  82 | Shared draws — NudgeHalo, TooltipPanel (lifted), Outline       |
| UIManager.cs       | 224 | Orchestrator — hit-test, hover, cursor, focus, tooltip, SFX    |
| IconButton.cs      | 101 | Icon + optional 9-slice bg + halo hover                        |
| TextButton.cs      |  97 | 9-slice chrome + centered label                                |
| Tab.cs             |  86 | 3-state (Active/Inactive/Hovered); active never nudges         |
| CloseButton.cs     |  25 | Preset IconButton with BtnIconX + cream idle                   |
| **Total**          | **685** | Zero new NuGet dependencies                                 |

## Commits

| Task | Name                                              | Commit   |
| ---- | ------------------------------------------------- | -------- |
| 1    | Framework primitives (IClickable, HoverStyle, WidgetHelpers) | 50db729 |
| 2    | UIManager orchestrator                            | b501bc1 |
| 3    | Concrete widgets (IconButton, TextButton, Tab, CloseButton) | 230f493 |
| 4    | Scene base: protected UIManager Ui + cursor reset | 382b0fd |
| 5    | Migrate PauseScene                                | 2cc6a89 |
| 6    | Migrate ShopPanel + ShopOverlayScene plumbing     | e05e239 |
| 7    | Migrate ChestScene action cluster + close         | 163b168 |
| 8    | InventoryScene edge-detect + final audit          | 5857e67 |

## Tasks 1–8: Completed Automatically

Each task built cleanly (`dotnet build` green, single pre-existing warning in `GameplayScene.cs:281`).

## Task 9: Human Visual Retest — BLOCKING, NOT COMPLETE

Per memory rule `feedback_visual_retest.md` and plan frontmatter, Task 9 is a `checkpoint:human-verify` gate. The user MUST run `dotnet run` and exercise every migrated surface before the orchestrator commits docs. Checklist is in `260424-2af-PLAN.md` under `<how-to-verify>`.

**What the user needs to validate:**

1. **PauseScene (Esc):** 4 YellowBtnSmall buttons (visual UPGRADE from flat grey), hover → Hand cursor + halo nudge, click Resume/Fullscreen/Settings/Quit works, Tab cycles focus with golden outline, Enter activates.
2. **ShopPanel (shop NPC):** Buy/Sell tabs — active does NOT nudge, inactive nudges on hover. Close X, qty -/+, action buttons, scroll wheel, Escape, click outside. Tab cycles.
3. **ChestScene (E on chest):** SendAll/TakeAll/SortChest hover halo + tooltip after 500ms, close X, drag bag↔chest, right-click context menu, slot item tooltip unchanged.
4. **InventoryScene (I):** Grid drag, slot tooltip at panel bottom, cursor no longer leaks Hand on close.
5. **Cross-cutting:** Pop overlay while hovering → cursor Arrow in underlying scene; one `[UIManager] Registered first widget` log at first widget registration; frame rate unchanged.

If user types "approved": orchestrator commits docs and closes the task.
If user reports issues: planner routes to revision.

## Deviations from Plan

### Pre-existing State vs Plan Assumptions

The plan's RESEARCH document described source code patterns that do NOT fully match the current repo state (ChestScene/ShopPanel had already been simplified since RESEARCH was written):

1. **[Pre-existing State] Current `ChestScene.DrawIconButton` had NO halo or nudge.** The plan references lines 493–522 with 4-direction halo + 1px nudge, but the current implementation (lines 460–466 before migration) was a plain 9-slice + icon with no hover visual. Migration to `IconButton` widget is a VISUAL UPGRADE, not a parity port — hover halo + tooltip + cursor change are all new to these buttons.
2. **[Pre-existing State] No `Mouse.SetCursor` calls existed in the repo.** Plan/RESEARCH assume `ChestScene:225` and `ShopPanel:551` call it per-frame; those lines were removed in a prior cleanup. `UIManager` now adds cached `SetCursor` for the hover-state transition (new behaviour, desired per CONTEXT).
3. **[Pre-existing State] `ShopPanel` had NO `DrawHoverableIcon` or nudge-on-hover.** The qty -/+ buttons drew flat. Migration to `IconButton` adds hover halo + Hand cursor — again an upgrade.
4. **[Pre-existing State] `ChestScene` has 3 action buttons, not 4.** Plan references `SortBolsa` (bag sort) but the current enum `ButtonAction { None, TakeAll, SendAll, SortChest }` has no bag-sort. Migrated the existing 3 + close (4 widgets total), not 5.
5. **[Pre-existing State] UITheme uses `CommonBtn` not `BtnSlot`.** Plan references `_theme.BtnSlot` + `BtnSlotInsets` for the IconButton background. UITheme exports `CommonBtn` + `CommonBtnInsets` instead — used those for ChestScene's IconButton backgrounds. `IconButton` is now agnostic: accepts `(texture, insets)` so any 9-slice chrome works, including `BtnCircleSmall` for ShopPanel's qty steppers.

None of the above required architectural changes; they are documentation-to-code drift that the executor resolved by using the actual theme/enum members.

### [Rule 2 — Pre-existing State] Shop `Tab` widget name clash with local `Tab` enum

`ShopPanel.cs` had `private enum Tab { Buy, Sell }`. Importing `stardew_medieval_v3.UI.Widgets` brought the public `Tab` class into scope, ambiguating the enum. Renamed local enum to `ShopTab` and used `using WidgetTab = stardew_medieval_v3.UI.Widgets.Tab;` alias for the widget type to keep call sites readable.

## Raw `Mouse.GetState()` retained — drag state only

Final grep audit:
- `Mouse.SetCursor` → **only** in `src/UI/Widgets/UIManager.cs` (cached transitions) and `src/Core/Scene.cs` (UnloadContent reset).
- `_wasMouseDown` → **only** in `src/UI/HotbarRenderer.cs` (drag state — documented legit per RESEARCH), `src/Scenes/ChestScene.cs` (drag state — documented), `src/Scenes/InventoryScene.cs` (drag state — documented). Zero in `PauseScene`.
- `DrawHoverableIcon` / `DrawIconButton` / `DrawCloseButton` / `DrawTab` → only in XML doc comments inside widget files referencing the lifted source.

Raw `Mouse.GetState()` remains ONLY in drag state-machines (ChestScene, InventoryScene, HotbarRenderer, InventoryGridRenderer) to detect press-HELD vs press-EDGE; press-EDGE detection itself flows via `InputManager.IsLeftClickPressed`. This matches the RESEARCH §Audit legitimate-usage list.

## Auth Gates

None encountered.

## Known Stubs

None. `OnHoverSound` / `OnClickSound` hooks on `UIManager` are deliberately no-op by default (CONTEXT Extra-3); they are ready for audio wiring without refactor and are documented as such.

## Follow-up Quick Tasks Implied

- **SFX hookup** when the audio system lands: wire `ui.OnHoverSound = () => AudioService.Play("ui_hover")` in `Scene` base or per-scene.
- **Arrow-key focus extension** if gamepad support is added later (CONTEXT deferred).
- **Stepper held-to-repeat** for ShopPanel qty -/+ (currently 1 tick per click — polish later).
- **Dialogue choice buttons migration** when `DialogueBox` gets interactive choices.

## Self-Check: PASSED

All 14 files exist on disk (8 new widget files + 6 modified source files). All 8 task commits (50db729, b501bc1, 230f493, 382b0fd, 2cc6a89, e05e239, 163b168, 5857e67) verified in git log. `dotnet build` was run and passed after each task with only the pre-existing `GameplayScene.cs(281,9)` warning — zero new warnings introduced.
