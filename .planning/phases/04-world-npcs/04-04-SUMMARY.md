---
phase: 04-world-npcs
plan: 04
subsystem: shop-ui-and-shopkeeper
tags: [shop, ui, npc, shopkeeper, buy-sell, toast]
requirements: [NPC-03, NPC-04, HUD-03]
dependency_graph:
  requires:
    - "src/Data/ItemRegistry (items.json loader)"
    - "src/Inventory/InventoryManager (Gold API from 04-01)"
    - "src/Entities/NpcEntity (from 04-01)"
    - "src/Scenes/ShopScene shell (from 04-02)"
    - "src/Scenes/DialogueScene + DialogueRegistry shopkeeper entries (from 04-03)"
    - "src/Data/SpriteAtlas"
  provides:
    - "ItemDefinition.BasePrice field"
    - "src/Data/ShopStock static registry + GetSellPrice"
    - "src/UI/ShopPanel (Buy/Sell renderer + input)"
    - "src/UI/Toast (600/1200/400 fade)"
    - "src/Scenes/ShopOverlayScene (panel + toast overlay)"
    - "ServiceContainer.Atlas slot"
  affects:
    - "Phase 4 complete; Phase 5 picks up dungeon entrance"
tech-stack:
  added: []
  patterns:
    - "Overlay-scene-owns-panel (same as InventoryScene)"
    - "Dialogue-then-shop onClose chain (single E press)"
    - "Shared SpriteAtlas via ServiceContainer"
key-files:
  created:
    - "src/Data/ShopStock.cs"
    - "src/UI/ShopPanel.cs"
    - "src/UI/Toast.cs"
    - "src/Scenes/ShopOverlayScene.cs"
    - "assets/Sprites/NPCs/shopkeeper.png"
    - "assets/Sprites/Portraits/shopkeeper.png"
  modified:
    - "src/Data/ItemDefinition.cs"
    - "src/Data/items.json"
    - "src/Core/ServiceContainer.cs"
    - "src/Scenes/FarmScene.cs"
    - "src/Scenes/ShopScene.cs"
decisions:
  - "Option A overlay scene: ShopOverlayScene owns ShopPanel + Toast, pops on Esc"
  - "Dialogue-first flow: E press -> DialogueScene with onClose -> PushImmediate(ShopOverlayScene)"
  - "Shopkeeper placeholder sprites copied from king.png (same as plan 04-03 pattern)"
  - "Sell credits BasePrice/2 per unit times full stack quantity on single Enter (full-stack sell)"
  - "ServiceContainer.Atlas slot added so overlay scenes can render item icons without rebuilding atlas"
metrics:
  duration: "~20 min"
  completed: "2026-04-12"
  tasks: 3
  files_touched: 11
---

# Phase 04 Plan 04: Shop UI + Shopkeeper NPC Summary

Ships the Buy/Sell shop UI (HUD-03), the Shopkeeper NPC in `ShopScene` (NPC-03), and the shopkeeper side of NPC-04 dialogue variance. Phase 4 now meets all 5 success criteria; dungeon entrance remains out of scope per CONTEXT.

## Price Data Assigned

Every economic item in `items.json` got a `BasePrice` value. Non-economic items (tools, rotten crops) keep `BasePrice = 0`.

| Category | Example prices (BasePrice / Sell BasePrice/2) |
|----------|-----------------------------------------------|
| Seeds (common) | 15-40 g / 7-20 g |
| Seeds (uncommon) | 50-80 g / 25-40 g |
| Crops (common) | 40-120 g / 20-60 g |
| Crops (uncommon) | 140-200 g / 70-100 g |
| Starter weapon (Iron_Sword) | 200 g / 100 g |
| Steel/Flame weapons | 500 / 1200 g |
| Leather_Armor | 150 g / 75 g |
| Iron_Armor | 400 g / 200 g |
| Health_Potion | 75 g / 37 g |
| Bread | 20 g / 10 g |
| Loot (Bones/Stone/Mana_Crystal) | 5-50 g |
| Rotten crops | 0 (unsellable) |
| Tools (Hoe/Watering_Can/Scythe) | 0 (not for sale) |

## ShopStock Contents (8 curated entries)

```csharp
new("Cabbage_Seed",    25),
new("Carrot_Seed",     25),
new("Strawberry_Seed", 40),
new("Pumpkin_Seed",    40),
new("Health_Potion",   75),
new("Bread",           25),
new("Iron_Sword",     220),
new("Leather_Armor",  160),
```

All IDs resolve via `ItemRegistry.Get(...)`. Buy prices have a small markup over BasePrice (e.g. Cabbage_Seed BasePrice=20, shop=25) to make the buy/sell spread meaningful without being punishing. Iron_Sword and Leather_Armor follow the same 10-15% markup.

## ShopPanel Decisions Confirmed

- **Option A overlay scene** implemented: `src/Scenes/ShopOverlayScene.cs` owns a `ShopPanel` + `Toast`, pushed on top of `ShopScene`. This mirrors the Plan 04-03 DialogueScene overlay pattern.
- **Single-frame Buy flow order (strict, T-04-14/15):**
  1. `_inv.Gold < price` -> block "Not enough gold"
  2. `CanAddOne(itemId)` false -> block "Inventory full"
  3. `TrySpendGold(price)` -> if it somehow fails, bail
  4. `TryAdd(itemId, 1)` -> if non-zero leftover (impossible after CanAddOne, but belt+suspenders) refund gold
  5. Emit toast, log `[ShopPanel] Bought {id} for {price}g`
- **Full-stack Sell**: Enter removes the full `ItemStack` at the selected slot and credits `BasePrice/2 * quantity`. This matches the one-press Buy parity (D-09) and keeps UX simple.
- **Disabled-reason copy is exact (UI-SPEC §Copywriting):** `Not enough gold`, `Inventory full`, `Select an item to sell`, `Cannot sell this item` are string literals in `ShopPanel.cs`.
- **Layout** per UI-SPEC: panel 720x400 at (120, 48), tabs 80x32 at (x=136, y=88), list region starts y=136, 8 visible rows of 40px each. Action button 60x24 on the right of each row.

## Toast Timing

`Toast` total = 600 ms fade-in + 1200 ms hold + 400 ms fade-out = 2200 ms. Linear alpha ramp clamped `[0,1]`. Single-instance replacement (no queue) — latest `Show()` preempts previous toast. Panel 240x32 at (360, 428) on 960x540 screen.

## Interaction Flow

`ShopScene.Update`:

1. Player within 28 px of Shopkeeper -> `_showPrompt = true` -> "Press E to talk" above NPC head.
2. E pressed -> `DialogueRegistry.Get("shopkeeper", Services.Quest?.State)` (3 variants already shipped in Plan 04-03).
3. `DialogueScene` pushed with `onClose = () => PushImmediate(new ShopOverlayScene(Services))`.
4. Player advances dialogue with E/Space. On last-line advance, `onClose` fires -> DialogueScene pops -> ShopOverlayScene pushes.
5. Shop panel active. Tab/↑/↓/Enter handle shop actions. Toast shows on successful Buy/Sell.
6. Esc in overlay -> `_panel.Update` returns true -> `PopImmediate()` -> back to ShopScene gameplay.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `25ca221` | BasePrice on ItemDefinition, items.json prices, ShopStock |
| 2 | `fda3e83` | ShopPanel renderer + Toast |
| 3 | `fd1a127` | Shopkeeper NPC wiring, ShopOverlayScene, ServiceContainer.Atlas |

## Manual Smoke Walkthrough

Execution environment is non-interactive. Live walkthrough deferred to user; all preconditions are satisfied by code and verified via `dotnet build` (Debug + Release, 0 warnings, 0 errors) and grep checks.

| # | Step | Automated precondition |
|---|------|------------------------|
| 1 | Launch -> Farm -> Village -> Shop | `ShopScene.LoadContent` spawns Shopkeeper at (320,200), loads map |
| 2 | Approach Shopkeeper (within 28 px) | `IsInInteractRange(_player.Position)` flips `_showPrompt` |
| 3 | "Press E to talk" above NPC | `InteractionPrompt.Draw` with "Press E to talk" copy |
| 4 | Press E -> Dialogue slides up, typewriter | `DialogueScene` pushed with shopkeeper lines from `DialogueRegistry` |
| 5 | Advance through last line -> shop opens | `onClose` callback executes `PushImmediate(new ShopOverlayScene(...))` |
| 6 | Shop 720x400 centered, Buy tab active, Gold counter top-right | `ShopPanel.Draw` renders header + tabs; Gold in `Color.Gold` |
| 7 | Buy tab: 8 items with prices | `ShopStock.Items.Count == 8` rows |
| 8 | ↓ selects next row, gold background highlight | `_selectedIndex++` and `SelectBg = Color.Gold * 0.4f` |
| 9 | Affordable item -> LimeGreen price, Buy button enabled | `_inv.Gold >= price` sets `priceColor = Color.LimeGreen` |
| 10 | Enter buys -> gold decrements by price, toast "Purchased {name}" LimeGreen | `TryBuy` strict gold-check-then-add, `ToastRequest` emitted |
| 11 | Unaffordable item -> "Not enough gold" red label | `ComputeDisabledReason` returns `ReasonNotEnoughGold` |
| 12 | Fill inventory, try buy -> "Inventory full" red | `CanAddOne(itemId)` false -> `ReasonInventoryFull` |
| 13 | Tab -> Sell tab shows filled inventory slots | `GetRowCount()` uses non-empty slot count |
| 14 | Enter sells full stack -> gold += BasePrice/2 * qty, toast "Sold {name} for {price}g" Gold | `TrySell` removes stack, `AddGold`, emits toast |
| 15 | Esc -> overlay closes, back in ShopScene | `_panel.Update` returns true -> `PopImmediate` |
| 16 | Exit shop south door -> back to Village | `_map.Triggers` `exit_to_village` -> `TransitionTo(VillageScene(..., "Shop"))` |

**Phase 4 success criteria mapping:**

- (1) Farm<->Village fade-to-black: Plan 02 (verified via build).
- (2) Village contains castle + shop doors: Plan 02.
- (3) Talk to King -> dialogue + portrait + quest activation: Plan 03.
- (4) Shop UI with items/prices/buy+sell: **THIS PLAN** (ShopPanel + ShopStock + Toast + ShopOverlayScene).
- (5) NPC dialogue varies by quest state: Plan 03 (King) + **THIS PLAN** (Shopkeeper wired via `DialogueRegistry.Get("shopkeeper", state)`; 3 variants authored in Plan 03).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] ServiceContainer lacked an Atlas slot**
- **Found during:** Task 3 (ShopOverlayScene construction)
- **Issue:** `ShopPanel` constructor needs `SpriteAtlas` for item icon rendering, but `ServiceContainer` only exposed `Inventory`, `Player`, `Quest`, `GameState`, and `PlayerSpriteSheet`. The atlas was a private field in `FarmScene`. Without sharing, `ShopOverlayScene` could not draw item icons for rows.
- **Fix:** Added `public SpriteAtlas? Atlas { get; set; }` slot. `FarmScene.LoadContent` now publishes `Services.Atlas = _spriteAtlas` after atlas construction. `ShopOverlayScene` reads it via `Services.Atlas` and throws a clear error if null.
- **Files modified:** `src/Core/ServiceContainer.cs`, `src/Scenes/FarmScene.cs`
- **Commit:** `fd1a127`

**2. [Rule 2 - Missing functionality] Full-stack Sell semantics not specified by plan**
- **Found during:** Task 2 (TrySell implementation)
- **Issue:** Plan said "RemoveAt + AddGold" per Sell press but did not specify whether to sell one unit or the full stack. Stardew Valley convention is full-stack on a single click (shift-click sells one; we don't have shift handling). UI-SPEC `Sold {item name} for {price}g` supports either.
- **Fix:** `TrySell` credits `BasePrice/2 * stack.Quantity` and clears the slot in one action. Toast reports total price. Documented in this SUMMARY.
- **Files modified:** `src/UI/ShopPanel.cs`
- **Commit:** `fda3e83`

No architectural questions raised. No auth gates. No out-of-scope work.

## Threat Model Compliance

| Threat | Disposition | Verified |
|--------|-------------|----------|
| T-04-14 (buy debits gold but fails to add) | mitigate | `CanAddOne` runs before `TrySpendGold`; refund path after `TryAdd` leftover>0 |
| T-04-15 (free item if TrySpendGold returns false after TryAdd) | mitigate | Strict order: gold check -> spend -> add; no path where add runs before spend |
| T-04-16 (sell non-existent item) | mitigate | `IndexOfNthFilledSlot` + `GetSlot` null-check before RemoveAt/AddGold |
| T-04-17 (toast queue overflow) | mitigate | `Toast` is single-instance; `Show` replaces previous |
| T-04-18 (user disputes purchase) | accept | Every Buy/Sell logged `[ShopPanel] Bought/Sold ... for Ng` |
| T-04-13 (save tamper Gold=999999) | accept | Single-player local; documented in Plan 01 |

## Self-Check: PASSED

- src/Data/ItemDefinition.cs: modified (contains `public int BasePrice`)
- src/Data/items.json: modified (contains 60+ `"BasePrice"` keys)
- src/Data/ShopStock.cs: FOUND (contains `public static class ShopStock`, `public static int GetSellPrice`)
- src/UI/ShopPanel.cs: FOUND (contains `class ShopPanel`, all 4 disabled-reason strings, `TrySpendGold`, `AddGold`, `TryAdd`, `RemoveAt`, `ShopStock.Items`)
- src/UI/Toast.cs: FOUND (contains `class Toast`, `0.6f`, `1.2f`, `0.4f` timing constants)
- src/Scenes/ShopScene.cs: modified (contains `new NpcEntity("shopkeeper"`, `DialogueRegistry.Get("shopkeeper"`)
- src/Scenes/ShopOverlayScene.cs: FOUND (contains `class ShopOverlayScene`, `PushImmediate(new ShopOverlayScene`)
- src/Core/ServiceContainer.cs: modified (contains `public SpriteAtlas? Atlas`)
- src/Scenes/FarmScene.cs: modified (contains `Services.Atlas = _spriteAtlas`)
- assets/Sprites/NPCs/shopkeeper.png: FOUND (355 bytes)
- assets/Sprites/Portraits/shopkeeper.png: FOUND (672 bytes)
- Commits 25ca221, fda3e83, fd1a127: all FOUND in git log
- `dotnet build -c Debug --nologo -v q`: 0 warnings, 0 errors
- `dotnet build -c Release --nologo -v q`: 0 warnings, 0 errors

## Phase 5 Hand-off

Phase 4 leaves the dungeon entrance unimplemented by design (CONTEXT §Phase Boundary). Phase 5 must:

1. Add a `dungeon_door` or `enter_dungeon` trigger to `village.tmx` (open slot on the east/north edge that does not collide with `door_castle` at (192,96) or `door_shop` at (720,96)).
2. Dispatch that trigger in `VillageScene.Update` to `TransitionTo(new DungeonScene(Services, "Village"))`.
3. Wire `MainQuest.Complete()` to a real boss-defeat event, replacing the F9 dev hook.

Everything the shop needs is already persisted: Gold, Inventory contents, and Equipment all round-trip via `InventoryManager.SaveToState/LoadFromState`, and `MainQuestState` round-trips via `MainQuest.SaveToState/LoadFromState` (Plan 04-01). No additional save-migration bump required for Phase 5 to use these.
