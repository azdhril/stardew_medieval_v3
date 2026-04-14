---
phase: 04-world-npcs
plan: 03
subsystem: dialogue-and-quest-tracker
tags: [dialogue, npc, king, quest-tracker, hud, typewriter]
requirements: [NPC-01, NPC-02, NPC-04, HUD-05]
dependency_graph:
  requires:
    - "Quest.MainQuest + MainQuestState (Plan 04-01)"
    - "Entities.NpcEntity (Plan 04-01)"
    - "Scenes.CastleScene shell (Plan 04-02)"
    - "Services.Player, Services.Quest, Services.GameState slots (Plans 04-01/02)"
    - "Core.SceneManager.PushImmediate/PopImmediate"
  provides:
    - "Data.DialogueRegistry: (npcId, MainQuestState) -> string[]"
    - "UI.DialogueBox renderer (880x120 panel w/ portrait + typewriter)"
    - "UI.InteractionPrompt renderer (floating above entities)"
    - "Scenes.DialogueScene overlay (typewriter state machine)"
    - "UI.HUD.DrawQuestTracker (static, callable from any scene)"
    - "F9 dev hook (DEBUG-only) to force-advance quest"
    - "King NPC in CastleScene with portrait + sprite placeholders"
  affects:
    - "Plan 04-04 (Shopkeeper) reads DialogueRegistry and reuses DialogueBox/DialogueScene"
tech-stack:
  added: []
  patterns:
    - "Stateless renderer classes (DialogueBox, InteractionPrompt)"
    - "Overlay scene with its own state machine (DialogueScene)"
    - "Static DrawQuestTracker helper for cross-scene reuse"
    - "#if DEBUG-gated dev input hook (F9)"
key-files:
  created:
    - "src/Data/DialogueRegistry.cs"
    - "src/UI/DialogueBox.cs"
    - "src/UI/InteractionPrompt.cs"
    - "src/Scenes/DialogueScene.cs"
    - "assets/Sprites/Portraits/king.png"
    - "assets/Sprites/NPCs/king.png"
  modified:
    - "src/Scenes/CastleScene.cs"
    - "src/UI/HUD.cs"
    - "Game1.cs"
    - "src/Scenes/FarmScene.cs"
decisions:
  - "DrawQuestTracker made static helper so CastleScene (no HUD instance) can render it too"
  - "Advance indicator uses ASCII 'v' fallback instead of Unicode '▼' to avoid SpriteFont glyph gaps"
  - "King NPC walk-through (no collision box added to map) to avoid getting stuck around 32x32 sprite"
metrics:
  duration: "~25 min"
  completed: "2026-04-12"
  tasks: 3
  files_touched: 10
---

# Phase 04 Plan 03: Dialogue System + King NPC + Quest Tracker Summary

Ship the dialogue system (NPC-01, HUD-05), wire the King NPC with quest-activating dialogue (NPC-02), deliver three dialogue variants per NPC state (NPC-04 for King; Shopkeeper variants also authored so Plan 04 is pure UI composition), and render the plain-text quest tracker (D-13). F9 dev hook (D-12) in place for dialogue-branch testing.

## What Shipped

### UI Classes (new)
- **`src/UI/DialogueBox.cs`** — Stateless renderer for the 880x120 dialogue panel. Full-screen dim overlay → panel with 1px black outline + 1px bevel → 80x80 portrait slot (left, fallback rect when null) → text column → pulsing ▼ advance indicator (ASCII 'v' for SpriteFont safety).
- **`src/UI/InteractionPrompt.cs`** — Small floating panel (~120x24, sized dynamically). Caller converts world→screen; renders 20px above anchor point.
- **`src/Scenes/DialogueScene.cs`** — Overlay scene with `Typing` / `WaitingAdvance` state machine. 40 cps typewriter (CharInterval = 0.025s). E or Space: snap-to-full (mid-typing) or advance (after reveal). `PopImmediate` on last-line advance; invokes `onClose` first.

### Data (new)
- **`src/Data/DialogueRegistry.cs`** — 6 entries keyed by `(npcId, MainQuestState)`:
  - `(king, NotStarted)` 3 lines ending in **"clear the dungeon"** (activation phrase).
  - `(king, Active)` 2 lines (encouragement to finish).
  - `(king, Complete)` 2 lines (reward speech).
  - `(shopkeeper, NotStarted|Active|Complete)` 2 lines each (tone: warm-merchant).
  - Missing key returns `[ "..." ]` fallback.

### Wiring
- **`src/Scenes/CastleScene.cs`** — Spawns King NPC at `(320, 100)` with 32x32 sprite + 80x80 portrait loaded via `Texture2D.FromStream`. Per-frame proximity check (`IsInInteractRange`, 28px) toggles `_showPrompt`. E press pushes `DialogueScene` with `DialogueRegistry.Get("king", Services.Quest.State)`. `onClose` activates quest if NotStarted.
- **`src/UI/HUD.cs`** — `DrawQuestTracker(sb, font, pixel, state, screenWidth)` static helper. Top-right 200x20 panel, right-aligned text:
  - NotStarted: `"Quest: (none)"` in `Color.Gray * 0.7f`.
  - Active: `"Quest:"` in `Color.Gold` + `" Clear the Dungeon"` in `Color.White`.
  - Complete: same + `"v"` in `Color.LimeGreen` (ASCII check).
  - Non-static `HUD.SetQuest(MainQuest?)` binds quest reference; `HUD.Draw` calls `DrawQuestTracker` every frame.
  - FarmScene calls `_hud.SetQuest(_mainQuest)` after HUD creation.
  - CastleScene renders quest tracker directly (no HUD instance in castle — intentional: HUD is FarmScene-scoped, quest tracker is the only overlay needed in castle).
- **`Game1.cs`** — `#if DEBUG` F9 handler: NotStarted→Activate, Active→Complete. Console log: `[DEBUG] F9 pressed, quest state -> {state}`.

## Dialogue Texts Shipped

### King
| State | Lines |
|-------|-------|
| NotStarted | "Brave traveler, the realm is in peril." / "Dark creatures have overrun the old catacombs." / "I charge you: clear the dungeon and bring peace to my people." |
| Active | "The dungeon still festers with evil, hero." / "Return to me only when the beasts lie slain." |
| Complete | "You have done it! The catacombs fall silent once more." / "The kingdom owes you a debt beyond gold." |

### Shopkeeper (consumed by Plan 04-04)
| State | Lines |
|-------|-------|
| NotStarted | "Welcome, friend! Seeds, potions, or a sharp blade?" / "Coin talks louder than my chatter. Have a look." |
| Active | "Heading for the catacombs? You'll want a potion or three." / "I stock what the King's hero needs. Browse freely." |
| Complete | "The hero returns! The whole village drinks to your name." / "Discount? Bah. Buy what you like, on me today." |

All lines ≤ 80 chars. King NotStarted contains literal `clear the dungeon` (activation contract).

## Quest Tracker Render Location

- **Panel:** 200×20 px, top-right corner, 12px from top, 12px from right.
- **Scenes:** FarmScene (via `HUD.Draw` → `DrawQuestTracker`) and CastleScene (direct call to `HUD.DrawQuestTracker`). VillageScene/ShopScene currently do not render the tracker — out of scope for this plan (VillageScene is a corridor; ShopScene is Plan 04-04 territory). Acceptance steps 6-9 all test in Farm/Castle; requirement met.

## F9 Test Log (code excerpt)

```csharp
#if DEBUG
if (_input.IsKeyPressed(Keys.F9) && _services?.Quest != null)
{
    if (_services.Quest.State == MainQuestState.NotStarted)
        _services.Quest.Activate();
    else if (_services.Quest.State == MainQuestState.Active)
        _services.Quest.Complete();
    Console.WriteLine($"[DEBUG] F9 pressed, quest state -> {_services.Quest.State}");
}
#endif
```

Release build compiles without the block (verified: `dotnet build -c Release` exits 0 with 0 warnings).

## Manual Smoke Walkthrough

Execution environment is non-interactive (YOLO mode, no GUI session). Automated preconditions verified; live walkthrough **deferred to user for final sign-off** per standard CastleScene pattern from Plan 04-02.

| # | Step | Automated precondition |
|---|------|-----------------------|
| 1 | Launch → HUD top-right shows `Quest: (none)` dim grey | `HUD.Draw` calls `DrawQuestTracker` with state `Services.Quest?.State ?? NotStarted`. FarmScene binds `_hud.SetQuest(_mainQuest)`. |
| 2 | Walk Farm → Village → Castle; approach King | King spawned at `(320, 100)`; proximity check 28px (`NpcEntity.InteractRange`). |
| 3 | `Press E to talk` prompt above king sprite | `InteractionPrompt.Draw` called in screen space via camera transform when `IsInInteractRange`. |
| 4 | Press E → dialogue box slides up with portrait; typewriter reveals | `DialogueScene` pushed via `PushImmediate`; 40 cps reveal. |
| 5 | Press E mid-typing → line snaps full; ▼ ('v') pulses at 2 Hz | State machine snap-path; pulse `((int)(t*4)) % 2 == 0`. |
| 6 | Press E through lines; last press closes; HUD reads `Quest: Clear the Dungeon` | `onClose` callback calls `quest.Activate()` when state==NotStarted before PopImmediate. |
| 7 | Talk to King again → Active variant (2 lines) | `DialogueRegistry.Get("king", Active)` used on reopen. |
| 8 | Press F9 → console logs state → Complete; HUD shows ✓ | DEBUG-only F9 handler. |
| 9 | Talk to King → Complete variant | Active→Complete branch plays. |
| 10 | Save + reload → quest state persists | `MainQuest.SaveToState`/`LoadFromState` already tested in Plan 04-01; no change here. |

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `5a53890` | DialogueRegistry with 6 dialogue arrays |
| 2 | `035859a` | DialogueBox + InteractionPrompt + DialogueScene |
| 3 | `0972a7e` | King NPC in CastleScene + HUD quest tracker + F9 + assets |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] HUD is FarmScene-scoped; no quest tracker visible in CastleScene**
- **Found during:** Task 3
- **Issue:** Plan requires quest tracker visible on dialogue close (steps 6-9), but the dialogue closes inside CastleScene. `HUD` is only instantiated in `FarmScene` and cannot be constructed in CastleScene because its ctor requires `TimeManager`, `PlayerStats`, `ToolController`, `PlayerEntity`, `CombatManager` — none of which CastleScene owns.
- **Fix:** Promoted `DrawQuestTracker` to a `public static` method on `HUD`. FarmScene's `HUD.Draw` calls it internally (via the instance method that reads `_quest`). CastleScene calls it directly in its `Draw` method, passing `Services.Quest?.State ?? NotStarted`. Added `HUD.SetQuest(MainQuest?)` binder so the FarmScene path still works through a single call.
- **Files modified:** `src/UI/HUD.cs`, `src/Scenes/CastleScene.cs`, `src/Scenes/FarmScene.cs`.
- **Commit:** `0972a7e`.

**2. [Rule 2 - Missing critical functionality] ▼ Unicode glyph may not exist in DefaultFont SpriteFont**
- **Found during:** Task 2 authoring
- **Issue:** UI-SPEC copy calls for `▼` (U+25BC) advance indicator and `✓` (U+2713) complete checkmark, but `assets/DefaultFont.spritefont` uses Arial with no guaranteed CharacterRegion covering these. Missing glyphs crash `SpriteFont.DrawString`.
- **Fix:** Used ASCII `"v"` fallback for both indicators (dialogue-advance and complete-check). Documented in inline comment. Visual fidelity slightly reduced from spec; functional behavior (color-coded, pulsing, right-aligned) preserved. If a glyph-capable font ships later, changing the two string literals is trivial.
- **Files modified:** `src/UI/DialogueBox.cs`, `src/UI/HUD.cs`.

No architectural questions raised. No auth gates encountered.

## Threat Model Compliance

| Threat | Disposition | Verified |
|--------|-------------|----------|
| T-04-09 (E double-fire Activate) | mitigate | `IsKeyPressed` edge-triggered; `MainQuest.Activate()` is idempotent (guards on `State != NotStarted`). |
| T-04-10 (F9 in Release) | mitigate | `#if DEBUG` block; `dotnet build -c Release` compiles cleanly with block absent (verified: 0 warnings). |
| T-04-11 (quest state disclosure) | accept | Intentional UX. |
| T-04-12 (typing-state DoS) | mitigate | Auto-transition to WaitingAdvance on line-length reach; snap-to-full on key press as escape hatch. |

## Self-Check: PASSED

- src/Data/DialogueRegistry.cs: FOUND (contains `clear the dungeon`; 6 `MainQuestState.` keys)
- src/UI/DialogueBox.cs: FOUND (`class DialogueBox`)
- src/UI/InteractionPrompt.cs: FOUND (`class InteractionPrompt`)
- src/Scenes/DialogueScene.cs: FOUND (`class DialogueScene`, `CharInterval = 0.025f`, `IsKeyPressed(Keys.E)`, `IsKeyPressed(Keys.Space)`, `PopImmediate`)
- src/UI/HUD.cs: modified (contains `DrawQuestTracker`, `Quest: (none)`, `Clear the Dungeon`)
- src/Scenes/CastleScene.cs: modified (contains `new NpcEntity("king"`, `PushImmediate(new DialogueScene`)
- Game1.cs: modified (contains `Keys.F9` inside `#if DEBUG`)
- src/Scenes/FarmScene.cs: modified (contains `_hud.SetQuest(_mainQuest)`)
- assets/Sprites/NPCs/king.png: FOUND (355 bytes, 32x32 placeholder)
- assets/Sprites/Portraits/king.png: FOUND (672 bytes, 80x80 placeholder)
- Commits 5a53890, 035859a, 0972a7e: all FOUND in git log
- `dotnet build -c Debug --nologo -v q`: 0 warnings, 0 errors
- `dotnet build -c Release --nologo -v q`: 0 warnings, 0 errors (F9 DEBUG gating verified)
