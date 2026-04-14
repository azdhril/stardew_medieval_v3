# DEBUG: Save persistence — Gold / Quest / Inventory lost after F1+relaunch

## Root Cause

`FarmScene.OnLoad` is NOT idempotent across scene re-entries. On every entry (including returns from Village/Shop/Castle), it unconditionally:
1. Creates a brand-new `InventoryManager` (`FarmScene.cs:84`) and overwrites `Services.Inventory` (line 85) — wiping the Gold balance and inventory grid held in memory.
2. Creates a brand-new `MainQuest` (line 87) and overwrites `Services.Quest` (line 88) — resetting quest state to `NotStarted`.
3. Calls `SaveManager.Load()` (line 122) and hydrates the new instances from disk via `_inventory.LoadFromState(save)` / `_mainQuest.LoadFromState(save)` (lines 130-131).

Because hydration pulls from the **last on-disk save** (not the in-memory state mutated while the player was in Village/Shop), any Gold earned/spent, quest advancement, or items purchased during the shop visit are silently discarded the moment the player walks back to the farm. When the user then presses F1/P (`FarmScene.cs:186-189` -> `TimeManager.ForceSleep`), `OnDayAdvanced` (lines 500-524) serializes the *already-reverted* state back to disk — overwriting the save with stale data. After quit/relaunch, the user sees the pre-shop values.

Contrast with the Player: lines 64-71 correctly gate player creation behind `if (Services.Player == null)`. Inventory and Quest are missing that same guard.

## Field-by-field classification

- **Gold** — (c) serialized + loaded, then **overwritten by fresh `new InventoryManager()` + reload-from-disk** every time FarmScene is re-entered. (`InventoryManager.cs:33` default `Gold = 0`; `LoadFromState` at :439 reads from the stale disk copy.)
- **QuestState (MainQuest)** — (c) same pattern: `new MainQuest()` defaults `State = NotStarted` (`MainQuest.cs:14`), then `LoadFromState` reads the stale disk value.
- **Inventory items** — (c) same pattern: `_slots` is re-allocated in the new `InventoryManager`, then `LoadFromState` (:442-447) reads stale disk items. Items picked up or purchased between the last save and the FarmScene re-entry are lost.

## v4 → v5 migration

Not the bug. `SaveManager.cs:100-108` only clamps `QuestState` to `[0,2]` and bumps version; no field is dropped. `Gold`, `Inventory`, `Equipment`, `HotbarSlots`, `ConsumableRefs`, `QuestState` are all present in `GameState` (v3+) and round-tripped correctly by `InventoryManager.SaveToState`/`LoadFromState` and `MainQuest.SaveToState`/`LoadFromState`.

## Fix direction (not applied)

Gate src/Inventory/Quest creation behind null-checks on `Services.Inventory` / `Services.Quest` (mirror the Player pattern at `FarmScene.cs:64`). Only call `LoadFromState` on first creation (when `save != null` AND the service was just instantiated). On re-entry, reuse the live in-memory instance.
