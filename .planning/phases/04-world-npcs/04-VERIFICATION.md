---
phase: 04-world-npcs
verified: 2026-04-12T22:08:53Z
status: passed
score: 5/5 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: human_needed
  signed_off: 2026-04-17
  signed_off_by: user (Renato Guedes)
  note: "All 8 human_verification tests confirmed in-game during play sessions after Phase 04. Fade transitions, dialogue rendering, HUD quest tracker, shop buy/sell flow, quest-state branching, and save round-trip all verified. Residual UX gaps (scroll, mouse-nav, save persistence wiring) were scoped into Phase 04.1 and closed there."
deferred:
  - truth: "Player can walk to edge of farm and transition to dungeon entrance"
    addressed_in: "Phase 5"
    evidence: "Phase 5 goal: 'Players can enter and progress through a complete dungeon experience from entrance to boss room'. CONTEXT.md §Phase Boundary: 'Dungeon entrance is out of scope for Phase 4'. 04-04-SUMMARY.md §Phase 5 Hand-off documents the exact trigger to add."
human_verification:
  - test: "Walk east to farm edge, observe screen fade to Village"
    expected: "Fade-to-black transition, player spawns at (48, 270) in VillageScene. Console shows '[VillageScene] Entered from Farm, spawn (48,270)'."
    why_human: "SceneManager fade animation requires a running game window. Cannot verify visually from code analysis."
  - test: "Walk west edge of Village back to Farm"
    expected: "Fade-to-black, player appears at (896, 272) on Farm east side. No teleport loop."
    why_human: "Spawn-outside-trigger safety requires interactive verification."
  - test: "Walk through castle door trigger in Village"
    expected: "Fade into CastleScene interior. Player at (208, 416). King NPC visible at ~(320,100). 'Press E to talk' prompt appears when player approaches within 28px."
    why_human: "NPC render position and interact prompt require visual confirmation."
  - test: "Press E near King; advance full dialogue; observe HUD change"
    expected: "Dialogue box slides up, 80x80 portrait on left, typewriter text. First E-press mid-type snaps line. Last E-press closes box. HUD switches from dim 'Quest: (none)' to gold 'Quest: Clear the Dungeon'."
    why_human: "Typewriter rendering, portrait slot, HUD color transition require live observation."
  - test: "Talk to King again (quest Active); press F9; talk again (quest Complete)"
    expected: "Active variant shows 2-line dialogue. F9 logs '[DEBUG] F9 pressed, quest state -> Complete'. Complete variant plays. HUD shows 'Quest: Clear the Dungeon v' (ASCII checkmark in LimeGreen)."
    why_human: "Dialogue branching by quest state requires interactive test."
  - test: "Walk shop door in Village; approach Shopkeeper; press E"
    expected: "Dialogue plays (shopkeeper NotStarted variant). On last-line advance, shop overlay opens: 720x400 panel, Buy tab active with 8 item rows, gold counter."
    why_human: "Shop UI layout and item row rendering require visual confirmation."
  - test: "Buy an affordable item; attempt to buy when gold insufficient; attempt when inventory full; switch to Sell tab and sell"
    expected: "Gold debited; 'Purchased X' LimeGreen toast fades in/out. 'Not enough gold' in red when broke. 'Inventory full' in red when slots full. Sell tab shows inventory; Enter sells full stack; 'Sold X for Ng' Gold toast appears."
    why_human: "Buy/Sell flow, disabled-reason labels, and toast animation require interactive play."
  - test: "Save game (sleep to advance day), exit, relaunch"
    expected: "Gold balance, inventory contents, and MainQuestState all persist across save/load cycle."
    why_human: "Save round-trip verification requires stopping and restarting the game."
---

# Phase 4: World & NPCs Verification Report

**Phase Goal:** Players can navigate between farm, village, and dungeon entrance, interact with NPCs, buy items, and receive the main quest.
**Verified:** 2026-04-12T22:08:53Z
**Status:** human_needed
**Re-verification:** No — initial verification.

## Goal Achievement

### Dungeon Entrance Note

The roadmap goal references "dungeon entrance" navigation. This is explicitly deferred to Phase 5 per `04-CONTEXT.md §Phase Boundary` ("Dungeon entrance is out of scope for Phase 4") and `04-04-SUMMARY.md §Phase 5 Hand-off` (documents the exact trigger and scene to add). The deferral is intentional and fully documented — not silently dropped. See the Deferred Items table below.

### Observable Truths (Roadmap Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Player walks to farm edge and transitions to village with fade-to-black | VERIFIED (code) / human needed (visual) | `test_farm.tmx` has `enter_village` trigger at (624,208,16x64). `FarmScene.Update` line 427: `if (t.Name == "enter_village") ... TransitionTo(new VillageScene(Services, "Farm"))`. SceneManager fade pipeline exists. Spawn dict in VillageScene sets (48,270) for "Farm". |
| 2 | Village has castle and shop accessible via door triggers | VERIFIED (code) / human needed (visual) | `village.tmx` objectgroup "Triggers" contains `door_castle` at (192,96,32x16) and `door_shop` at (720,96,32x16). `VillageScene.Update` dispatches both to `CastleScene` and `ShopScene` via `TransitionTo`. Build output confirms maps copy to `bin/Debug/.../assets/Maps/`. |
| 3 | King NPC gives main quest via styled dialogue box with portrait | VERIFIED (code) / human needed (visual) | `CastleScene.cs:74` spawns `new NpcEntity("king", _kingSprite, _kingPortrait, (320,100))`. Line 123 pushes `new DialogueScene` with `DialogueRegistry.Get("king", quest.State)`. onClose callback (line 118-122) calls `quest.Activate()` when NotStarted. `DialogueBox.cs` is a concrete renderer with portrait slot, typewriter at 40 cps (`CharInterval = 0.025f`), pulsing advance indicator. Portrait asset exists: `assets/Sprites/Portraits/king.png` (672 bytes). |
| 4 | Shop UI shows items with prices; player can buy/sell using gold | VERIFIED (code) / human needed (visual) | `ShopPanel.cs` renders Buy tab from `ShopStock.Items` (8 entries, all IDs verified in `items.json`). `TryBuy` debits via `inv.TrySpendGold(price)`, adds via `inv.TryAdd`. `TrySell` calls `inv.RemoveAt` + `inv.AddGold(BasePrice/2 * qty)`. `Toast` shows purchase/sale confirmation. `ShopOverlayScene` pushes on E-press after shopkeeper dialogue. |
| 5 | NPC dialogue changes based on quest state | VERIFIED (code) / human needed (interactive) | `DialogueRegistry.cs` has 6 entries: king×3 states, shopkeeper×3 states. `CastleScene.cs:115-117` and `ShopScene.cs:116-117` both read `Services.Quest?.State` to key into registry. All three variants authored with distinct text confirmed in code. |

**Score:** 5/5 truths supported by code evidence. All 5 require human sign-off for the visual/interactive layer.

### Deferred Items

Items not yet met but explicitly addressed in later milestone phases.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Player navigates to dungeon entrance (literal roadmap wording) | Phase 5 | Phase 5 goal: "Players can enter and progress through a complete dungeon experience." `04-04-SUMMARY.md §Phase 5 Hand-off` specifies exact implementation: add `dungeon_door` trigger to `village.tmx`, dispatch to `DungeonScene` in `VillageScene.Update`. `04-CONTEXT.md §Phase Boundary` documents the deferral explicitly. |

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/Quest/MainQuestState.cs` | enum with NotStarted=0, Active=1, Complete=2 | VERIFIED | Contains literal `NotStarted = 0`, `Active = 1`, `Complete = 2`. |
| `src/Quest/MainQuest.cs` | Quest container with event, Activate(), Complete() | VERIFIED | `public event Action<MainQuestState>? OnQuestStateChanged` at line 17. `Activate()` at line 22, `Complete()` at line 34. `LoadFromState`/`SaveToState` present. |
| `src/Entities/NpcEntity.cs` | NPC base with portrait + proximity | VERIFIED | `InteractRange = 28f`, `IsInInteractRange` returning Distance check, `NpcId`, `Portrait` all present. |
| `src/World/TriggerZone.cs` | record struct with Name and Bounds | VERIFIED | `public record TriggerZone(string Name, Rectangle Bounds)` with `ContainsPoint` helper. |
| `src/World/TileMap.cs` | LoadTriggerObjects + Triggers list | VERIFIED | `LoadTriggerObjects()` at line 83, called from constructor at line 74. `public IReadOnlyList<TriggerZone> Triggers` at line 36. |
| `src/Core/SaveManager.cs` | CURRENT_SAVE_VERSION=5, v4→v5 migration | VERIFIED | `CURRENT_SAVE_VERSION = 5` at line 14. Migration branch `if (state.SaveVersion < 5)` at line 100. |
| `src/Inventory/InventoryManager.cs` | Gold accessor + TrySpendGold | VERIFIED | `public int Gold` at line 33. `public bool TrySpendGold(int amount)` at line 62. `OnGoldChanged` event, `AddGold`, `SetGold` all present. Gold round-trips via `state.Gold` in `LoadFromState`/`SaveToState`. |
| `assets/Maps/village.tmx` | Village map with all 3 trigger zones | VERIFIED | `exit_to_farm` at (0,240,16,64), `door_castle` at (192,96,32,16), `door_shop` at (720,96,32,16). Both `Collision` and `Triggers` objectgroups present. |
| `assets/Maps/castle.tmx` | Castle map with exit trigger | VERIFIED | `exit_to_village` at (192,464,32,16). |
| `assets/Maps/shop.tmx` | Shop map with exit trigger | VERIFIED | `exit_to_village` at (192,464,32,16). |
| `assets/Maps/test_farm.tmx` | Farm map extended with enter_village trigger | VERIFIED | `enter_village` at (624,208,16,64) added to Triggers objectgroup. |
| `src/Scenes/VillageScene.cs` | Village scene with all trigger dispatch | VERIFIED | Dispatches `exit_to_farm`, `door_castle`, `door_shop`. Writes `Services.GameState.CurrentScene = "Village"`. Spawn dict covers "Farm", "Castle", "Shop" entries. |
| `src/Scenes/CastleScene.cs` | Castle scene with King NPC and dialogue | VERIFIED | King NPC spawned, proximity check, PushImmediate(DialogueScene) on E, onClose activates quest, DrawQuestTracker called. |
| `src/Scenes/ShopScene.cs` | Shop scene with Shopkeeper and overlay | VERIFIED | Shopkeeper NPC spawned, DialogueRegistry.Get("shopkeeper"), PushImmediate(ShopOverlayScene) in onClose. |
| `src/Data/DialogueRegistry.cs` | 6 dialogue arrays keyed by (npcId, state) | VERIFIED | 6 dictionary entries. King NotStarted contains `"clear the dungeon"`. Fallback `["..."]` for missing keys. |
| `src/UI/DialogueBox.cs` | 880x120 dialogue panel renderer | VERIFIED | Class exists, panel dimensions hard-coded, portrait slot with null fallback, typewriter pattern. |
| `src/UI/InteractionPrompt.cs` | Floating "Press E to talk" prompt | VERIFIED | Class exists. Called from CastleScene and ShopScene Draw when `_showPrompt`. |
| `src/Scenes/DialogueScene.cs` | Overlay scene with typewriter state machine | VERIFIED | `CharInterval = 0.025f` (40 cps). E/Space via `IsKeyPressed` (edge-triggered). `PopImmediate` on last advance. `_onClose?.Invoke()` before pop. |
| `src/UI/HUD.cs` (DrawQuestTracker) | Quest tracker in HUD | VERIFIED | `public static void DrawQuestTracker(...)` at line 111. Three state variants with correct copy: "Quest: (none)", "Quest: Clear the Dungeon", complete variant. |
| `Game1.cs` (#if DEBUG F9) | F9 advances quest state in DEBUG only | VERIFIED | `if (_input.IsKeyPressed(Keys.F9)` inside `#if DEBUG` block at line 73. Release build compiles clean (0 warnings). |
| `src/Data/ShopStock.cs` | Curated 8-item shop stock | VERIFIED | 8 entries. All item IDs (`Cabbage_Seed`, `Carrot_Seed`, `Strawberry_Seed`, `Pumpkin_Seed`, `Health_Potion`, `Bread`, `Iron_Sword`, `Leather_Armor`) resolve in `items.json`. `GetSellPrice` returns `BasePrice / 2`. |
| `src/Data/ItemDefinition.cs` | BasePrice field | VERIFIED | `public int BasePrice { get; set; } = 0;` at line 31. |
| `src/Data/items.json` | BasePrice on every item | VERIFIED | 78 `"BasePrice"` keys counted. All ShopStock items have non-zero BasePrice. |
| `src/UI/ShopPanel.cs` | Buy/Sell renderer with all UX rules | VERIFIED | Disabled-reason constants match UI-SPEC verbatim. Buy order: gold check → `TrySpendGold` → `TryAdd` → refund on leftover. Sell: `RemoveAt` + `AddGold(BasePrice/2 * qty)`. References `ShopStock.Items`. |
| `src/UI/Toast.cs` | 2200ms toast with 3-phase alpha | VERIFIED | `FadeIn = 0.6f`, `Hold = 1.2f`, `FadeOut = 0.4f` (total 2.2s). Single-instance replacement. |
| `src/Scenes/ShopOverlayScene.cs` | Overlay scene owning ShopPanel + Toast | VERIFIED | `public class ShopOverlayScene : Scene`. Calls `_panel.Update`; returns true → `PopImmediate()`. PointClamp sampler (mirrors InventoryScene pattern). |
| `assets/Sprites/Portraits/king.png` | King portrait asset | VERIFIED | File exists (672 bytes). |
| `assets/Sprites/Portraits/shopkeeper.png` | Shopkeeper portrait asset | VERIFIED | File exists (672 bytes). |
| `assets/Sprites/NPCs/king.png` | King overworld sprite | VERIFIED | File exists (355 bytes). |
| `assets/Sprites/NPCs/shopkeeper.png` | Shopkeeper overworld sprite | VERIFIED | File exists (355 bytes). |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `src/Scenes/FarmScene.cs` | `src/Scenes/VillageScene.cs` | `TransitionTo(new VillageScene(Services, "Farm"))` on `enter_village` trigger | WIRED | FarmScene.cs line 429. |
| `src/Scenes/VillageScene.cs` | `src/Scenes/CastleScene.cs` | `door_castle` trigger → `TransitionTo(new CastleScene(Services, "Village"))` | WIRED | VillageScene.cs line 89. |
| `src/Scenes/VillageScene.cs` | `src/Scenes/ShopScene.cs` | `door_shop` trigger → `TransitionTo(new ShopScene(Services, "Village"))` | WIRED | VillageScene.cs line 92. |
| `src/Scenes/CastleScene.cs` | `src/Scenes/FarmScene.cs` | `exit_to_farm` trigger in VillageScene | WIRED | Chain: CastleScene → exit_to_village → VillageScene → exit_to_farm → FarmScene. |
| `src/Scenes/CastleScene.cs` | `src/Scenes/DialogueScene.cs` | E press near King → `PushImmediate(new DialogueScene(...))` | WIRED | CastleScene.cs line 123. |
| `src/Scenes/DialogueScene.cs` | `src/Quest/MainQuest.cs` | `onClose` callback → `quest.Activate()` when NotStarted | WIRED | CastleScene.cs onClose lambda line 118-122. |
| `src/UI/HUD.cs` | `src/Quest/MainQuest.cs` | `Services.Quest?.State` read every frame in DrawQuestTracker | WIRED | HUD.cs line 131+ reads state for color/copy selection. |
| `src/Scenes/ShopScene.cs` | `src/Scenes/ShopOverlayScene.cs` | `onClose` callback → `PushImmediate(new ShopOverlayScene(Services))` | WIRED | ShopScene.cs line 120. |
| `src/UI/ShopPanel.cs` | `src/Inventory/InventoryManager.cs` | `TrySpendGold`, `TryAdd`, `RemoveAt`, `AddGold` | WIRED | ShopPanel.cs lines 144, 149, 153, 185, 193. |
| `src/UI/ShopPanel.cs` | `src/Data/ShopStock.cs` | `ShopStock.Items` for Buy tab rows | WIRED | ShopPanel.cs line 98. |
| `src/Scenes/FarmScene.cs` | `src/Core/GameState.cs` | `MainQuest.LoadFromState`/`SaveToState` round-trip via `state.QuestState` | WIRED | FarmScene.cs lines 152, 699. |
| `src/Inventory/InventoryManager.cs` | `src/Core/GameState.cs` | `state.Gold` round-trip in `LoadFromState`/`SaveToState` | WIRED | InventoryManager.cs lines 439, 484. |
| `src/Core/SaveManager.cs` | `src/Core/GameState.cs` | v4→v5 migration branch normalizes `state.QuestState` | WIRED | SaveManager.cs line 100. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|--------------------|--------|
| `src/Scenes/CastleScene.cs` | `_king` NpcEntity | `NpcEntity("king", ...)` loaded from `Texture2D.FromStream` + hardcoded position | Static position; NPC sprite loaded from disk | FLOWING (NPC is not data-driven from DB; positional hardcode is by design for MVP) |
| `src/UI/HUD.cs` DrawQuestTracker | `state` (MainQuestState) | `Services.Quest?.State` from `MainQuest` | `MainQuest.State` written by `Activate()`/`Complete()` (edge-triggered E press) | FLOWING |
| `src/UI/ShopPanel.cs` Buy tab | `ShopStock.Items` | Static `IReadOnlyList<Entry>` in `src/Data/ShopStock.cs` | 8 real items with prices from `items.json` BasePrice data | FLOWING |
| `src/UI/ShopPanel.cs` Sell tab | `_inv.GetSlot(i)` | `InventoryManager` slot array loaded from `GameState` | Real inventory contents persist via `SaveManager` | FLOWING |
| `src/UI/ShopPanel.cs` gold counter | `_inv.Gold` | `InventoryManager.Gold` loaded from `GameState.Gold` | Real saved gold value | FLOWING |

### Behavioral Spot-Checks

Step 7b SKIPPED for fade-transition, dialogue, and shop UX behaviors — all require a running game window and cannot be verified without a GUI session. See Human Verification Required section.

Build-level spot-checks performed:

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Debug build succeeds with 0 warnings | `dotnet build -c Debug --nologo -v q` | "Compilação com êxito. 0 Aviso(s) 0 Erro(s)" | PASS |
| Release build succeeds with 0 warnings (F9 excluded) | `dotnet build -c Release --nologo -v q` | "Compilação com êxito. 0 Aviso(s) 0 Erro(s)" | PASS |
| All ShopStock IDs resolve in items.json | grep each ID against src/Data/items.json | All 8 IDs found with non-zero BasePrice | PASS |
| All TMX trigger names correct | grep trigger names in each map file | All 5 trigger names verified | PASS |
| Maps copied to build output | `ls bin/Debug/net8.0/assets/Maps/` | 5 files: village.tmx, castle.tmx, shop.tmx, test_farm.tmx, farm_tileset.tsx | PASS |
| Save version bumped to 5 | grep `CURRENT_SAVE_VERSION` | `private const int CURRENT_SAVE_VERSION = 5` | PASS |
| Commits documented in SUMMARYs exist | `git log --oneline` | All 9 commits (19f7733 through fd1a127) found | PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| WLD-01 | 04-02-PLAN.md | Map transitions Farm ↔ Village ↔ Dungeon with fade to black | VERIFIED (code) | SceneManager.TransitionTo wired in FarmScene, VillageScene, CastleScene, ShopScene. Farm↔Village, Village↔Castle, Village↔Shop all wired. |
| WLD-02 | 04-02-PLAN.md | Village map with castle and shop | VERIFIED | village.tmx with door_castle and door_shop triggers. CastleScene and ShopScene exist and load from castle.tmx / shop.tmx. |
| WLD-03 | 04-01-PLAN.md + 04-02-PLAN.md | Trigger zones on edges/doors for transitions | VERIFIED | TriggerZone record + TileMap.LoadTriggerObjects parse "Triggers" objectgroup. 5 trigger zones across 4 maps. |
| WLD-04 | 04-02-PLAN.md | Player state preserved across map transitions | VERIFIED | Services.Player shared instance created lazily in FarmScene, reused by all scenes. Gold+Inventory in InventoryManager (Services.Inventory). HP/stamina on PlayerEntity. |
| NPC-01 | 04-03-PLAN.md | Dialogue box with NPC portrait, advance button | VERIFIED (code) | DialogueBox renderer (880x120, portrait slot, typewriter), DialogueScene overlay, InteractionPrompt. |
| NPC-02 | 04-03-PLAN.md | King NPC in castle gives main quest | VERIFIED (code) | King NpcEntity in CastleScene; onClose activates MainQuest; dialogue contains "clear the dungeon". |
| NPC-03 | 04-04-PLAN.md | Shopkeeper NPC with buy/sell UI | VERIFIED (code) | Shopkeeper NpcEntity in ShopScene; ShopOverlayScene with ShopPanel. |
| NPC-04 | 04-03-PLAN.md + 04-04-PLAN.md | NPCs with quest-state-varying dialogue | VERIFIED (code) | DialogueRegistry.Get(npcId, state) for both King and Shopkeeper. CastleScene and ShopScene read `Services.Quest?.State` to key dialogue. |
| HUD-03 | 04-04-PLAN.md | Shop UI with item list, prices, buy/sell buttons | VERIFIED (code) | ShopPanel renders 720x400 overlay with Buy/Sell tabs, 8 rows, affordability coloring, disabled-reason labels, Toast. |
| HUD-05 | 04-03-PLAN.md | Styled dialogue box with NPC portrait and text advance | VERIFIED (code) | DialogueBox: panel fill Color(60,40,30), portrait slot, typewriter 40 cps, advance indicator, full-screen dim overlay. |

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `src/UI/DialogueBox.cs`, `src/UI/HUD.cs` | `"v"` used instead of `▼`/`✓` Unicode glyphs | Info | Visual fidelity slightly reduced vs. UI-SPEC. Functional behavior (pulsing, color) preserved. Documented in 04-03-SUMMARY.md — caused by SpriteFont glyph gap risk. Trivial to fix if a Unicode-capable font ships. |
| `assets/Sprites/Portraits/king.png`, `assets/Sprites/Portraits/shopkeeper.png` | Placeholder 80x80 copied from king sprite (same bytes) | Info | Art placeholder per CONTEXT §Claude's Discretion. No functional impact. Art pass deferred to later phase. |
| Manual smoke walkthroughs in all 4 SUMMARYs | "deferred to user" (YOLO/non-interactive execution) | Warning | All 4 plans document visual smoke tests but defer them to the user because the execution environment had no GUI session. This verification report formalizes those as Human Verification items. |

No blockers found. No TODO/FIXME/placeholder comments in implementation files. No empty `return {}` or `return []` stubs in feature code paths.

### Human Verification Required

#### 1. Farm → Village Fade Transition

**Test:** Launch game, walk player east to the edge of the farm map.
**Expected:** Screen fades to black and fades back in showing the village map. Player appears at approximately (48, 270) near the west edge of the village. Console shows `[VillageScene] Entered from Farm, spawn (48,270)`. No crash, no infinite loop.
**Why human:** SceneManager 300ms fade-out / 300ms fade-in requires a running game window.

#### 2. Full Navigation Loop (Village ↔ Castle ↔ Shop ↔ Farm)

**Test:** From Village: walk through castle door → enter castle → walk south exit → return to village. Walk through shop door → enter shop → walk south exit → return to village. Walk west edge of village → return to farm.
**Expected:** Each hop has a clean fade. Player spawn points are outside trigger zones (no teleport loop). Console logs scene names at each entry.
**Why human:** Spawn-outside-trigger safety and absence of re-entry loops require interactive verification.

#### 3. King NPC Dialogue + Quest Activation

**Test:** Enter castle, walk toward King (32x32 sprite at ~(320,100)). Come within 28px.
**Expected:** "Press E to talk" prompt appears above King. Press E: dialogue box opens with 80x80 portrait on left, "Brave traveler, the realm is in peril." reveals character-by-character at 40 cps. Mid-reveal E-press snaps line. Second E advances. After final line E-press: box closes. HUD top-right switches from dim grey "Quest: (none)" to gold+white "Quest: Clear the Dungeon". Talking again shows the Active variant.
**Why human:** Portrait render, typewriter speed, HUD color transition, and dialogue branching require live observation.

#### 4. F9 Debug Key + Quest Complete Variant

**Test:** While in castle (quest Active), press F9.
**Expected:** Console logs `[DEBUG] F9 pressed, quest state -> Complete`. HUD shows "Quest: Clear the Dungeon v" (LimeGreen "v" checkmark). Talking to King again shows Complete variant (2 lines).
**Why human:** Visual confirmation of HUD state and dialogue branch.

#### 5. Shop Dialogue → Shop UI → Buy → Sell

**Test:** Navigate to shop, approach Shopkeeper, press E.
**Expected:** Shopkeeper dialogue plays (NotStarted variant). Last-line advance opens 720x400 shop overlay. Buy tab shows 8 rows with item names and prices. LimeGreen price text for affordable items. Navigate with arrow keys; highlighted row has gold background. Press Enter to buy an affordable item: gold decreases, "Purchased X" LimeGreen toast fades in/out in ~2.2s. Press Tab to switch to Sell; select a held item, press Enter: gold increases by BasePrice/2×qty, "Sold X for Ng" Gold toast. Press Esc: overlay closes.
**Why human:** UI layout, affordability coloring, toast animation, and buy/sell flow require interactive play.

#### 6. Disabled-State Labels

**Test:** With 0 gold, try to buy; with full inventory (20/20 slots), try to buy.
**Expected:** "Not enough gold" in red below the action button. "Inventory full" in red below the action button.
**Why human:** Edge-case UI states require setup and visual confirmation.

#### 7. Save/Load Round-Trip

**Test:** Buy items and adjust gold, then save (trigger a sleep/day-advance), exit game, relaunch.
**Expected:** Gold balance, inventory contents, and quest state (Active or Complete) all persist. HUD reflects the saved quest state immediately on launch.
**Why human:** Save persistence confirmation requires stopping and restarting the game.

### Gaps Summary

No structural gaps found. All 5 roadmap success criteria have concrete code evidence:

- Success criterion 1 (farm↔village transition): trigger wired in FarmScene and VillageScene, TMX trigger zone verified.
- Success criterion 2 (village has castle + shop via doors): VillageScene dispatches door_castle and door_shop; both interior scenes exist and load maps.
- Success criterion 3 (King dialogue + portrait + quest): King NpcEntity in CastleScene, DialogueScene with DialogueBox and portrait slot, quest activation in onClose.
- Success criterion 4 (shop UI with buy/sell): ShopPanel with 8-item stock from ShopStock, TrySpendGold + TryAdd buy flow, TrySell sell flow, Toast confirmation.
- Success criterion 5 (NPC dialogue by quest state): DialogueRegistry with 6 entries, both CastleScene and ShopScene key into registry with current quest state.

The only outstanding items are 8 live-interaction smoke tests that require a GUI game session. All automated preconditions for each test are satisfied.

---

_Verified: 2026-04-12T22:08:53Z_
_Verifier: Claude (gsd-verifier)_
