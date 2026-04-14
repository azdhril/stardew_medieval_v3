---
phase: 4
slug: world-npcs
status: draft
shadcn_initialized: false
preset: none
created: 2026-04-12
---

# Phase 4 — UI Design Contract

> Visual and interaction contract for Phase 4 "World & NPCs" (scene transitions, NPC prompts, dialogue box, shop UI, quest tracker). Consumed by gsd-ui-checker, gsd-planner, gsd-executor, gsd-ui-auditor.
>
> **Rendering context:** MonoGame 3.8 DesktopGL, 960×540 base resolution, pixel-art medieval aesthetic. Not a web/React project — shadcn and component-library tooling are not applicable. UI is rendered via `SpriteBatch.Draw` (1×1 pixel textures for panels/bars) and `SpriteBatch.DrawString` (SpriteFont bitmap text).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (MonoGame native rendering — no component library) |
| Preset | not applicable |
| Component library | none — custom renderers in `src/UI/*.cs` (pattern: `HUD.cs`, `HotbarRenderer.cs`, `InventoryGridRenderer.cs`) |
| Icon library | `SpriteAtlas` (existing, Phase 2). Portraits in `assets/Sprites/Portraits/*.png` (new). |
| Font | `assets/DefaultFont.spritefont` — Arial 12px Regular (existing, shared across HUD/Inventory/Hotbar). **Single font, single size for Phase 4.** |

**Design fingerprint pulled from existing code (do not diverge):**
- Solid-color rects drawn with a 1×1 white `Texture2D` (`_pixel`) tinted via `Color` parameter (see `HUD.cs:35–39`).
- Panels use dark brown `new Color(60, 40, 30)` as slot base, 1px black outline, `Color.Gold` for selection (`HotbarRenderer.cs:84, 96`).
- Text is `Color.White` on dark panels; `Color.Gray * 0.7f` for secondary labels (slot numbers, keybind hints).
- State colors: `Color.Red` (HP / danger), `Color.LimeGreen`/`Color.Yellow`/`Color.Red` gradient (stamina), `Color.Gold` (selection/currency).

---

## Spacing Scale

Declared values in **screen-space pixels** at 960×540 base resolution. All values are multiples of 4 to match the 16×16 tile grid and pixel-art aesthetic.

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Inner text padding inside a panel, gap between icon and label |
| sm | 8px | Gap between adjacent UI elements (e.g. dialogue portrait → text column) |
| md | 16px | Default panel inner padding, row height spacing, shop item-row vertical gap |
| lg | 24px | Section gap inside the Shop panel (header → tab strip → item list) |
| xl | 32px | Outer panel margin from screen edge (dialogue box bottom offset) |
| 2xl | 48px | Major vertical rhythm (Shop panel top offset from screen top) |
| 3xl | 64px | Reserved for full-screen layout breaks — not used this phase |

**Exceptions for Phase 4:**
- Dialogue portrait frame: **80×80 px** (research-recommended, `04-RESEARCH.md` line 79). Not on the scale — this is an art-asset size, not a spacing token.
- Interaction prompt offset above NPC head: **20 px** (tuned to clear a 32×32 sprite). Acceptable exception.
- NPC interact range (gameplay, not spacing): **28 px** (`04-RESEARCH.md` Pattern 3). Gameplay value, not layout.

---

## Typography

Single SpriteFont (Arial 12px Regular) is used for **all** text in Phase 4. MonoGame SpriteFont does not support runtime weight changes, so "weight" and "size" are encoded through **color, emphasis rects, and capitalization** rather than font variants.

| Role | Size | Weight / Emphasis | Line Height | Color |
|------|------|-------------------|-------------|-------|
| Body (dialogue text, shop item names, tooltips) | 12px (native font) | Regular | 16px (~1.33 — single font baseline + 4px leading) | `Color.White` on dark panel |
| Label (slot numbers, keybind hints, tab labels inactive) | 12px | Regular, dimmed | 16px | `Color.Gray * 0.7f` |
| Heading (panel title: "Buy" / "Sell" tab active, "Quest:" prefix) | 12px | Regular, **boxed** (panel highlight rect behind text) | 16px | `Color.White` on `Color.Gold` or `new Color(90, 60, 45)` accent rect |
| Display (purchase toast, quest state change banner) | 12px | Regular, centered, outlined | 16px | `Color.White` with 1px `Color.Black` drop-shadow offset |

**Rules:**
- Never scale SpriteFont via `DrawString` scale parameter this phase (keeps pixels crisp).
- Typewriter pacing for dialogue: **40 characters/second** (~25ms/char). First `E`/`Space` press reveals full line instantly; second press advances (locked by CONTEXT D-05).
- All text anchored to integer pixel positions (round `Vector2` positions) to prevent sub-pixel blur.

---

## Color

Palette derived from existing HUD/Hotbar/Inventory renderers. Phase 4 does **not** introduce new hues — it reuses the established medieval-brown surface plus the existing state colors.

| Role | Value (RGB) | Hex | Usage |
|------|-------------|-----|-------|
| Dominant (60%) | `(20, 15, 12)` | `#140F0C` | Full-screen dim overlay behind dialogue/shop (`Color.Black * 0.55f` tint). Fade-to-black scene transitions are pure `Color.Black`. |
| Secondary (30%) | `(60, 40, 30)` | `#3C281E` | Panel fill (dialogue box body, shop background, quest-tracker background rect). Matches `HotbarRenderer.cs:84` slot base. |
| Secondary edge | `(90, 60, 45)` | `#5A3C2D` | Inner bevel / tab-strip divider / panel inner border (1px lighter than secondary). |
| Panel outline | `Color.Black` `(0,0,0)` | `#000000` | 1px outer border on every panel (matches existing `DrawRect` outline pattern in `HUD.cs:57`). |
| Accent (10%) | `Color.Gold` `(255,215,0)` | `#FFD700` | **Reserved for:** (1) active Buy/Sell tab highlight, (2) selected shop-item row background, (3) gold amount text ("Gold: 120"), (4) quest-tracker "Quest:" label prefix, (5) dialogue "▼" advance indicator. **Never** used for body text, panel fills, or generic borders. |
| Destructive / error | `Color.Red` `(255,0,0)` | `#FF0000` | Disabled-button reason label text ("Not enough gold", "Inventory full"). Inline error only — no destructive confirmation dialogs this phase. |
| Success / affordable | `Color.LimeGreen` `(50,205,50)` | `#32CD32` | Price text when player can afford the item; "Purchased X" toast text. |
| Dimmed text | `Color.Gray * 0.7f` | `#737373` | Disabled buy button label, inactive tab label, interaction hint prompt below threshold. |

**Accent reserved-for list (explicit):**
1. Active tab highlight (Buy or Sell)
2. Selected item row in shop list
3. Gold counter numeric value
4. "Quest:" label prefix in HUD tracker
5. Typewriter advance indicator "▼"

Accent is **not** used for: NPC prompts, dialogue text, panel titles, generic button fills, affordable price indicators (that is LimeGreen), or CTA borders.

**60/30/10 compliance:** dominant = darkened scene behind overlay + panel body bulk, secondary = panel fill + bevel, accent = the five items above (well under 10% of visible pixels).

---

## Copywriting Contract

All copy is **English** (game ships in English for v1 per CLAUDE.md); Portuguese is only for dev-facing documentation. All strings live in `src/Data/DialogueRegistry.cs` (NPC lines) and inline constants in `src/UI/ShopPanel.cs` / `src/UI/DialogueBox.cs` / `src/UI/HUD.cs`.

### Interaction prompts (NPC proximity, door triggers)

| Element | Copy |
|---------|------|
| Talk to NPC | `Press E to talk` |
| Enter door (castle) | `Press E to enter Castle` |
| Enter door (shop) | `Press E to enter Shop` |
| Enter door (exit interior) | `Press E to exit` |
| Edge transition (farm → village) | `Press E to travel to Village` |
| Edge transition (village → farm) | `Press E to return to Farm` |

Prompts render in a small secondary panel (60×40 → 60×40 border + text padding), 20px above the target entity or trigger centroid. `Color.White` text on secondary panel fill.

### Primary CTAs

| Element | Copy |
|---------|------|
| Shop Buy button (enabled) | `Buy` |
| Shop Sell button (enabled) | `Sell` |
| Shop close button / key hint | `Esc to close` |
| Dialogue advance hint | `▼` (icon, bottom-right of dialogue box, pulses at 2 Hz) |
| Dialogue close (final line) | `▼ Close` |

### Disabled-state reason labels

Rendered inline below or inside the disabled button in `Color.Red`. Locked by CONTEXT D-10.

| Condition | Copy |
|-----------|------|
| Player cannot afford item | `Not enough gold` |
| Inventory is full on buy | `Inventory full` |
| No item selected for sell | `Select an item to sell` |
| Item is not sellable (quest item, etc.) | `Cannot sell this item` |

### Empty states

| Element | Copy |
|---------|------|
| Quest tracker (MainQuest.NotStarted) | `Quest: (none)` — dimmed `Color.Gray * 0.7f` |
| Quest tracker (MainQuest.Active) | `Quest: Clear the Dungeon` — "Quest:" in gold, objective in white |
| Quest tracker (MainQuest.Complete) | `Quest: Clear the Dungeon ✓` — "✓" in LimeGreen |
| Shop Sell tab with empty inventory | `Your inventory is empty` — centered, dimmed |
| Shop Buy tab (future-proof, always populated in Phase 4) | `Shop is closed` — fallback only |

### Error states

Phase 4 has no user-facing error states beyond the disabled-button labels above. Save/load or scene-load failures remain dev-only (`Console.WriteLine` logs), consistent with the existing convention (CLAUDE.md "Error Handling").

### Destructive actions

**None in Phase 4.** Buy and Sell are reversible through their inverse operation. No confirmation popups (locked by CONTEXT D-09 — single-press Buy with toast). If Phase 6 adds item destruction, that contract is out of scope here.

### Dialogue text (placeholder, planner finalizes wording)

Planner drafts full copy in `src/Data/DialogueRegistry.cs`. Contract requirements:

- Each NPC × MainQuestState = **1 dialogue** (6 total: King × 3 states, Shopkeeper × 3 states).
- Maximum **3 lines** per dialogue, maximum **80 characters per line** (fits the dialogue text column: panel width 880px − portrait 80px − padding 48px ≈ 750px available, ~80 chars at 12px Arial).
- King NPC.NotStarted dialogue must contain the phrase `clear the dungeon` (triggers quest activation via NPC-02).
- Tone: medieval-formal for King, warm-merchant for Shopkeeper. Planner drafts; user reviews.

### Purchase / sale toast

| Event | Copy | Color |
|-------|------|-------|
| Buy success | `Purchased {item name}` | `Color.LimeGreen` |
| Sell success | `Sold {item name} for {price}g` | `Color.Gold` |

Toast renders center-bottom, 600ms fade-in + 1200ms hold + 400ms fade-out. No click-to-dismiss.

---

## Component Inventory (Phase 4 surfaces)

Declared so the planner knows exactly what UI classes to create and the executor has sizes ready.

| Surface | Renderer class (new) | Size / Anchor | Notes |
|---------|---------------------|---------------|-------|
| Fade-to-black transition | `SceneManager` (existing — no new class) | Full screen 960×540 | Already implemented; reuse `TransitionTo`. 300ms fade out, action, 300ms fade in. |
| NPC interaction prompt | `src/UI/InteractionPrompt.cs` (new, lightweight) | ~120×24 px, anchored 20px above entity | Secondary panel + black 1px outline + white text. Shows only when player within 28px. |
| Door / trigger prompt | Same `InteractionPrompt.cs` | ~140×24 px, anchored at trigger centroid | Reuses the same component; only copy differs. |
| Dialogue box | `src/UI/DialogueBox.cs` (new) | **880×120 px**, anchored bottom-center, 32px from screen bottom (20px bottom margin + HUD clearance) | Portrait slot 80×80 at inner-left (md padding); text column fills remainder. Advance indicator `▼` at bottom-right inner corner. |
| Shop panel | `src/UI/ShopPanel.cs` (new) | **720×400 px**, centered horizontally, 48px (2xl) from top | Tab strip (2 tabs, 80×32 each) at top. Item list below with scroll (up to 8 visible rows, 40px tall each). Gold counter at top-right of panel header. |
| HUD quest tracker | Extension of `src/UI/HUD.cs` (existing) | ~200×20 px, anchored top-right, 12px from top, 12px from right | Single-line text `Quest: {objective}` with state-based color per Copywriting contract. |
| Purchase toast | `src/UI/Toast.cs` (new, reusable) | ~240×32 px, anchored center-bottom, 80px above screen bottom | Auto-dismisses at 2200ms total. |

**Hit targets & padding:** All clickable UI elements (tabs, Buy/Sell buttons, item rows) must be ≥ 32×32 px to match existing inventory interaction (`InventoryGridRenderer` slot size). The shop item row is 40px tall — OK.

---

## Interaction Contracts

### Input bindings (all via existing `InputManager`, no new keys beyond these)

| Action | Key | Context |
|--------|-----|---------|
| Interact / talk / enter door / advance dialogue | `E` or `Space` | Proximity-based when gameplay; scene-scoped when dialogue is open |
| Close shop / close dialogue early | `Esc` | Pops overlay scene (`SceneManager.PopImmediate`) |
| Switch shop tab | `Tab` (toggle Buy↔Sell) or mouse click on tab | — |
| Navigate shop item list | `↑` / `↓` arrow keys or mouse click on row | — |
| Confirm Buy / Sell | `Enter` or mouse click Buy/Sell button | Disabled state must block both paths |
| Dev-only: force quest complete | `F9` | Placeholder per CONTEXT D-12, removed or gated behind `#if DEBUG` |

### State machine: dialogue box

1. **Entering** — background dim fades in 150ms; panel slides up from bottom over 200ms.
2. **Typing** — current line reveals at 40 chars/sec. `▼` indicator hidden.
3. **Line complete** — typewriter finishes. `▼` appears and pulses at 2 Hz.
4. **Advance pressed (mid-typing)** — line snaps to fully revealed; go to state 3.
5. **Advance pressed (line complete)** — if more lines, go to state 2 with next line. If last line, go to state 6.
6. **Exiting** — panel slides down 200ms; overlay pops.

### State machine: shop buy/sell

- **Idle** — no row selected. Buy/Sell button disabled with `Select an item to {buy/sell}`.
- **Row selected** — price displayed; affordability check runs continuously.
- **Action disabled** — button greyed, reason label visible in red.
- **Action enabled** — button gold-highlighted, price in LimeGreen.
- **Action confirmed** — single-frame: inventory/gold mutate, toast scheduled, row reselected if stack remains.

### Quest state transitions (visual)

- `NotStarted → Active`: tracker text fades from dim grey to white+gold over 400ms; subtle screen-corner flash (optional polish — not required for checker sign-off).
- `Active → Complete`: `✓` icon fades in next to objective over 400ms; objective text color shifts to `Color.LimeGreen * 0.9f`.

No modal "Quest Accepted!" popup in Phase 4 (deferred — belongs with HUD-04 polish in Phase 6).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| (none — MonoGame project, no component registries) | n/a | not applicable |

No shadcn, no third-party UI registries, no npm packages. All UI code is hand-authored C# in the repo's `src/UI/` directory using MonoGame primitives. Registry safety gate is therefore **N/A** — but the planner MUST verify no new NuGet UI packages are added (per `04-RESEARCH.md` "Não adicionar nada novo no NuGet").

---

## Asset Checklist (for planner / executor)

| Asset | Path | Size | Notes |
|-------|------|------|-------|
| King portrait | `assets/Sprites/Portraits/king.png` | 80×80 | Placeholder pixel art acceptable per CONTEXT "Claude's Discretion". Register in `Content.mgcb`. |
| Shopkeeper portrait | `assets/Sprites/Portraits/shopkeeper.png` | 80×80 | Same. |
| King NPC overworld sprite | `assets/Sprites/NPCs/king.png` | 32×32 (single frame, idle) | Can reuse `DummyNpc` sprite as placeholder. |
| Shopkeeper NPC overworld sprite | `assets/Sprites/NPCs/shopkeeper.png` | 32×32 | Same. |
| Gold coin icon (for shop price display) | `assets/Sprites/UI/coin.png` | 16×16 | Optional — price can be rendered text-only (`{n}g`) if icon unavailable. |

---

## Success Criteria Mapping (from ROADMAP)

| Criterion | UI element providing it |
|-----------|------------------------|
| Fade-to-black farm ↔ village | `SceneManager.TransitionTo` (existing) |
| Door triggers to castle/shop | `InteractionPrompt` + trigger-zone proximity |
| King dialogue with portrait | `DialogueBox` with 80×80 portrait slot |
| Shop UI with buy/sell | `ShopPanel` with Buy/Sell tabs |
| NPC dialogue varies by quest state | `DialogueRegistry` keyed by `MainQuestState`; tracker reflects state |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS (N/A — no external registries)

**Approval:** pending
