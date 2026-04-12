# Phase 4: World & NPCs - Context

**Gathered:** 2026-04-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Players can navigate between Farm, Village, Castle interior, and Shop interior via fade-to-black map transitions. Village contains a King NPC (gives main quest) and a Shopkeeper NPC (buys/sells items). Dialogue appears in a styled box with portrait; NPC dialogue varies by main-quest state (NotStarted / Active / Complete). A simple text quest tracker shows on the HUD. Dungeon entrance is out of scope for Phase 4 (the quest completion hook is a placeholder to be wired by Phase 5).

</domain>

<decisions>
## Implementation Decisions

### Village Layout & Scope
- **D-01:** Village map is a single 960×540 screen (no scrolling). Castle on one side, Shop on the other, open path between them.
- **D-02:** Only two NPCs live in the village this phase: King (inside Castle) and Shopkeeper (inside Shop). No flavor villagers.
- **D-03:** Castle and Shop each have their own Scene (CastleScene, ShopScene) entered via door trigger zones in the village. Interiors are minimal (single screen each, NPC stands inside).

### Dialogue UX
- **D-04:** Dialogue box is anchored bottom of screen with a static portrait on the left side inside the box.
- **D-05:** Text reveals character-by-character (typewriter). E or Space: first press completes the current line instantly; second press advances to the next line or closes the box.
- **D-06:** Dialogue is linear-only in Phase 4 — no player choice menus. Quest acceptance is automatic on first King conversation.

### Shop UX
- **D-07:** Shop UI has two tabs: **Buy** (shopkeeper inventory with prices) and **Sell** (player inventory with sell values).
- **D-08:** Shopkeeper stock is curated: seeds + basic consumables (potions) + starter weapon + starter armor (~6–10 items). Matches NPC-03.
- **D-09:** Purchases complete on a single Buy press with a small confirmation toast ("Purchased X"). No confirmation popup.
- **D-10:** Buy button is disabled with a reason label when the player cannot afford the item or inventory is full ("Not enough gold" / "Inventory full"). Same pattern for Sell when nothing selected.

### Quest State & Tracker
- **D-11:** Main quest is represented as a single `MainQuest` with a `QuestState` enum: `NotStarted | Active | Complete`. Stored on GameState so save/load carries it. Designed so a future quest list can replace it without reworking consumers.
- **D-12:** Phase 4 exposes a `SetQuestComplete()` hook but does not wire it to real gameplay. Placeholder trigger = dev/debug key so we can test dialogue branches. Real trigger (boss defeated) lands in Phase 5.
- **D-13:** Quest tracker renders in the top-right HUD as plain text ("Quest: Clear the Dungeon") always visible during gameplay. Graphical polish deferred to Phase 6 (HUD-04).
- **D-14:** Both King and Shopkeeper have three dialogue variants keyed to MainQuest state (NotStarted / Active / Complete) — satisfies NPC-04 across two NPCs.

### Claude's Discretion
- Exact portrait art (placeholder pixel art is fine — art pass later).
- Trigger-zone implementation approach (tile-based polygon vs Rectangle on Entity) — researcher/planner picks based on existing TileMap collision code.
- Item prices and shop stock values — planner picks reasonable defaults; can tune later.
- Dialogue text content (wording) — planner drafts, user can rewrite.
- Scene-transition spawn points (where player appears after each transition) — planner derives from map layout.

### Folded Todos
None — no pending todos matched Phase 4 scope.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-Level
- `CLAUDE.md` — project constraints (C# 12, MonoGame 3.8, 960×540, Tiled maps, pixel-art medieval style).
- `.planning/PROJECT.md` — vision and core loop.
- `.planning/REQUIREMENTS.md` — WLD-01..04, NPC-01..04, HUD-03, HUD-05 acceptance criteria.
- `.planning/ROADMAP.md` §"Phase 4: World & NPCs" — goal and success criteria.

### Prior Phase Context
- `.planning/phases/01-architecture-foundation/01-CONTEXT.md` — Scene/SceneManager/Entity decisions (door triggers and new scenes build on this).
- `.planning/phases/02-items-inventory/02-CONTEXT.md` — Inventory/hotbar/gold model used by Shop.
- `.planning/phases/03-combat/03-CONTEXT.md` — HP/damage model used by starter weapon/armor the shop sells.

### Verification Baselines
- `.planning/phases/02-items-inventory/02-VERIFICATION.md` — inventory contract the Shop must honor.
- `.planning/phases/03-combat/03-VERIFICATION.md` — combat contract relevant to shop-purchased gear.

No external specs/ADRs beyond the planning directory — requirements fully captured in decisions above plus the referenced files.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Core/SceneManager.cs` + `Core/Scene.cs` — scene lifecycle with fade transitions already proven by FarmScene ↔ TestScene. VillageScene / CastleScene / ShopScene plug in here.
- `Core/GameState.cs` — serializable DTO; extend with `CurrentScene`, `MainQuestState`, Gold is already tracked via Phase 2.
- `Core/SaveManager.cs` — JSON save with version migration. New fields go here.
- `Core/Entity.cs` — base for NPC entities. Player/enemies already use it.
- `World/TileMap.cs` — TMX loader with polygon collision. Door trigger zones can reuse polygon-object parsing.
- `UI/HUD.cs` — existing HUD surface; quest tracker renders here.
- `UI/InventoryGridRenderer.cs` — grid rendering pattern the Sell tab can reuse for player inventory display.
- `Scenes/InventoryScene.cs` — pause-over-gameplay UI scene pattern; ShopScene/dialogue overlay can mirror this.
- `Core/InputManager.cs` — edge-detection inputs already exist; E key and tab switching plug in.

### Established Patterns
- Scene-per-screen with fade transitions (FarmScene / TestScene / InventoryScene / PauseScene).
- Events pattern (TimeManager/PlayerStats publish events) — MainQuest state change should publish an event NPCs/HUD listen to.
- `[Module]` prefixed `Console.WriteLine` logging for state changes.
- `TryXxx` naming for fallible actions; `bool` returns for shop transactions (`TryBuy`, `TrySell`).

### Integration Points
- Farm edge transition tile triggers (WLD-03) — new layer or object group in `test_farm.tmx`, plus a new `village.tmx`, `castle.tmx`, `shop.tmx`.
- HUD top-right region — currently displays time/day (via HUD.cs); quest tracker slots in below or beside it.
- Inventory manager (from Phase 2) — Shop buy/sell operates on it; need Gold accessor already present.
- SaveManager — add MainQuestState + CurrentSceneId to serialized GameState with a migration bump.

</code_context>

<specifics>
## Specific Ideas

- Typewriter pacing should feel like Stardew Valley: ~30–60 chars/sec, first-press skip to full line is classic.
- Portrait frame visual should harmonize with existing pixel-art medieval palette; placeholder portraits acceptable for MVP.
- Shop UX should look and feel adjacent to the existing inventory grid (consistent with Phase 2 UI style).
- Quest tracker text can stay plain-text now; Phase 6 (HUD-04) owns the graphical upgrade.

</specifics>

<deferred>
## Deferred Ideas

- Flavor/idle villagers — revisit in a later phase once the core loop ships.
- Innkeeper / sleep-save NPC — belongs with save/respawn work in Phase 6.
- Branching dialogue choices (yes/no prompts, branching flows) — scope for Phase 6+ once quest system generalizes.
- Hold-to-buy for expensive items — nice-to-have polish, not needed for v1.
- Full quest list data structure (beyond single MainQuest) — follow up when multiple quests exist.
- Graphical quest tracker with icons/progress bar — owned by HUD-04 in Phase 6.
- Shop stock rotation / daily refresh / limited quantities — beyond Phase 4 scope.

### Reviewed Todos (not folded)
None — no todos were surfaced by the cross-reference step.

</deferred>

---

*Phase: 04-world-npcs*
*Context gathered: 2026-04-12*
