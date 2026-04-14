---
phase: 04-world-npcs
plan: 05
subsystem: scenes/save-persistence
tags: [save, persistence, bug, blocker, fix]
gap_closure: true
closes_gaps: ["UAT Test 13 — Save persistence (blocker)"]
requirements: [WLD-04]
dependency_graph:
  requires: [04-04]
  provides: ["Live in-memory src/Inventory/Quest survives Farm re-entry"]
  affects: ["src/Scenes/FarmScene.cs OnLoad lifecycle"]
tech_stack:
  added: []
  patterns: ["First-entry guard mirroring Services.Player == null pattern"]
key_files:
  created: []
  modified:
    - "src/Scenes/FarmScene.cs"
decisions:
  - "Narrow fix: only gate src/Inventory/Quest/Time/Stamina/Player-position behind firstEntry. Farm cells still re-hydrate from disk every entry (existing behavior, not in scope of UAT Test 13 blocker)."
  - "Use Services.Inventory == null as the canonical firstEntry signal; Quest gets a defensive parallel guard."
metrics:
  duration: "~10min"
  completed: "2026-04-13"
  tasks: 1
  files: 1
---

# Phase 04 Plan 05: Save persistence first-entry guard Summary

Single-file fix that makes `FarmScene.OnLoad` reuse live `Services.Inventory` / `Services.Quest` on re-entry instead of creating fresh instances and overwriting them with stale on-disk state — closing the UAT Test 13 blocker (Gold/Quest/Inventory loss after F1+relaunch).

## What Changed

### src/Scenes/FarmScene.cs (1 file, 1 task)

Three logical edits inside `OnLoad`, all gated on a new `bool firstEntry = Services.Inventory == null` flag that mirrors the existing `Services.Player == null` guard one block up.

**1. Inventory + Quest construction (lines 82-105)**

```csharp
// Before
_inventory = new InventoryManager();
Services.Inventory = _inventory;
_mainQuest = new MainQuest();
Services.Quest = _mainQuest;

// After
bool firstEntry = Services.Inventory == null;
if (firstEntry)
{
    _inventory = new InventoryManager();
    Services.Inventory = _inventory;
}
else
{
    _inventory = Services.Inventory!;
}

if (Services.Quest == null)
{
    _mainQuest = new MainQuest();
    Services.Quest = _mainQuest;
}
else
{
    _mainQuest = Services.Quest!;
}
```

**2. Disk hydration scoped to firstEntry (lines 138-166)**

```csharp
var save = SaveManager.Load();
if (save != null)
{
    _gridManager.LoadFromSaveData(save.FarmCells, CropRegistry.All); // unchanged: every entry
    if (firstEntry)
    {
        Services.Time.SetDay(save.DayNumber);
        Services.Time.SetGameTime(save.GameTime);
        pl.Position = new Vector2(save.PlayerX, save.PlayerY);
        pl.Stats.SetStamina(save.StaminaCurrent);
        _inventory.LoadFromState(save);
        _mainQuest.LoadFromState(save);
        Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
    }
    else
    {
        Console.WriteLine($"[FarmScene] Re-entry; live Gold={_inventory.Gold}, Quest={_mainQuest.State}");
    }
    _loadedState = save;
}
else
{
    _loadedState = new GameState();
    if (firstEntry)
        Console.WriteLine($"[FarmScene] MainQuest state loaded: {_mainQuest.State}");
}
```

**3. Starter-tool + test-item seeding scoped to firstEntry (lines 171-183)**

```csharp
if (firstEntry && save == null) { /* starter tools */ }
if (firstEntry && (save == null || save.Inventory.Count == 0)) { /* test items */ }
```

This prevents duplicate Hoe / Watering_Can / Scythe (and the test items) from being added every time the player walks back onto the farm.

### OnDayAdvanced — UNCHANGED

Lines 500-525 (`OnDayAdvanced`) already serialize the live `_inventory.SaveToState(state)` and `_mainQuest.SaveToState(state)`. Because `_inventory` and `_mainQuest` are now always references to the live `Services.Inventory` / `Services.Quest`, the F1/sleep save now writes the **live** Gold/Quest/Inventory — not the disk copy that was previously overwriting them. No code change needed here.

### Farm cells — UNCHANGED on purpose

`_gridManager.LoadFromSaveData(save.FarmCells, CropRegistry.All)` continues to run on every Farm entry (including re-entry). Rationale documented inline in the plan: UAT Test 13 only reported Gold/Quest/Inventory loss; farm cells already re-hydrate from disk because `_gridManager` is per-scene. Leaving this branch unconditional keeps the diff minimal. If farm-cell loss on re-entry surfaces in a future UAT, address in a follow-up.

## UAT Test 13 Walkthrough (expected after this fix)

| # | Step | Expected | Status |
|---|------|----------|--------|
| 1 | Launch → Farm. Note Gold/quest. | Defaults (0 / NotStarted) on fresh save. | passes (no live verification yet — code change only) |
| 2 | Farm → Village → Castle. Talk King. | Quest becomes Active. | passes (logic unchanged) |
| 3 | Return to Village → Shop. Talk Shopkeeper. | Shop opens. | passes (04-04 verified) |
| 4 | Buy item. | Gold ↓, item ↑ inventory. | passes (04-04 verified) |
| 5 | Esc shop → walk Village → Farm. | Returns to Farm scene. | **FIXED** — src/Inventory/Quest no longer wiped on re-entry |
| 6 | Open inventory (I). | Bought item present, HUD shows Active quest. | **FIXED** — live Services.Inventory / Services.Quest reused |
| 7 | Press F1/P (sleep). | OnDayAdvanced fires; live state serialized. | **FIXED** — live `_inventory` / `_mainQuest` are now Services-held refs |
| 8 | Quit. | Process exits. | n/a |
| 9 | Relaunch → Farm. Open inventory. | Bought item present, Active quest, Gold matches. | **FIXED** — disk now contains the live state from step 7 |

The previous failure mode was: step 5/6 created a fresh `InventoryManager` + `MainQuest`, hydrated them from the disk save (which was last written before the purchase), then step 7 saved that stale state — destroying the player's purchase. With the firstEntry guard in place, the live instances created on step 1 survive through step 9.

## Verification

- `dotnet build -c Debug --nologo -v q` → 0 errors, 1 pre-existing warning (`GameplayScene.cs(156,9)` — unrelated).
- `dotnet build -c Release --nologo -v q` → 0 errors, 1 pre-existing warning (same).
- `grep -n "Services.Inventory == null" src/Scenes/FarmScene.cs` → 1 hit (line 86).
- `grep -n "bool firstEntry" src/Scenes/FarmScene.cs` → 1 hit (line 86).
- `grep -n "firstEntry && save == null" src/Scenes/FarmScene.cs` → 1 hit (line 172).
- `grep -n "firstEntry &&" src/Scenes/FarmScene.cs` → 2 hits (lines 172, 183).

## Deviations from Plan

None. Plan was executed exactly as written (using the "Revised final shape" provided in the plan's `<action>` step 3).

## Threat Mitigations Applied

- **T-04-19** (stale disk overwrites live state): mitigated by `firstEntry` guard wrapping `LoadFromState` calls.
- **T-04-20** (repudiation of lost Gold/items): mitigated by the re-entry log `[FarmScene] Re-entry; live Gold=... Quest=...`.
- **T-04-21** (duplicated test items from re-seeding): mitigated by `firstEntry && save == null` and `firstEntry && save.Inventory.Count == 0` guards on the seeding blocks.

## Self-Check: PASSED

- `src/Scenes/FarmScene.cs` — modified, present, builds clean (Debug + Release).
- Commit `e8ee8a6` exists in git log: `fix(04-05): gate FarmScene OnLoad hydration behind first-entry guard`.
- All four grep verification anchors hit at expected line numbers.
