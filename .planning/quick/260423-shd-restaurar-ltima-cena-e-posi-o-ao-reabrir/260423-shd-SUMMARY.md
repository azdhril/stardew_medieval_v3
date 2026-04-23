---
quick_id: 260423-shd
description: restaurar última cena e posição ao reabrir o jogo
date: 2026-04-23
status: code-complete / awaiting-human-verify
---

# Quick Task 260423-shd — Summary

## Goal

Fix: reopening the game always dropped the player back on the Farm at stale coordinates, regardless of where they saved.

Root causes (confirmed during discussion):
1. `Game1.cs` hardcoded `PushImmediate(new FarmScene(...))` — never consulted `save.CurrentScene`.
2. `FarmScene` force-stamped `CurrentScene = "Farm"` on both load and save-build paths, clobbering the saved scene tag.
3. `GameState` held a single `PlayerX/Y` pair — scene-local coordinates cross-contaminated when applied to a different map.

## What changed

Two atomic commits:

### 37641ea — `feat(quick-260423-shd-01): add PositionByScene dict and v9->v10 migration`

- `src/Core/GameState.cs` — new `PositionByScene: Dictionary<string, ScenePosition>`; `SaveVersion` default bumped to `10`. `PlayerX/Y` retained for legacy downgrade safety.
- `src/Core/SaveManager.cs` — `CURRENT_SAVE_VERSION = 10`; v9→v10 migration seeds `PositionByScene["Farm"] = (PlayerX, PlayerY)` and normalizes any stray `Dungeon:*` keys.
- `src/Core/GameStateSnapshot.cs` — `SaveNow` clones the prior dict, writes the live player's position into the current scene's entry. Skips `Dungeon:*` (dungeon rooms have their own snapshot in `DungeonState`).
- `src/Core/ServiceContainer.cs` — one-shot slots: `PendingRestoreScene`, `PendingRestorePosition`, `PendingRestoreSceneName`.
- `src/Core/GameplayScene.cs` — in `LoadContent`, if `PendingRestoreSceneName == SceneName`, player is positioned via `PendingRestorePosition` (consumed, then nulled). Otherwise falls through to the existing `GetSpawn(fromScene)` path — door transitions unchanged.

### 6b3ef8a — `feat(quick-260423-shd-01): save-aware boot routing + per-scene position restore`

- `Game1.cs` — `LoadContent` now pre-reads the save *before* pushing the initial scene. If `save.CurrentScene` ≠ `"Farm"` (and not null), seeds the `PendingRestore*` flags on `ServiceContainer`, then always pushes `FarmScene` first (required to initialize Player/Atlas/Inventory/Hotbar).
- `src/Scenes/FarmScene.cs`:
  - Removed the two redundant `CurrentScene = "Farm"` stamps (lines 245 and 734). The base `GameplayScene.LoadContent` already sets this correctly when a scene enters.
  - Added post-load hop: after `OnLoad` completes, if `PendingRestoreScene` is set and ≠ Farm, transitions to the saved scene (Village / Castle / Shop). Restore flags are consumed once so door-based travel keeps using spawn markers afterward.

## Build / test status

- `dotnet build`: **succeeds** with zero new warnings/errors. Only the pre-existing `CS8602` in `GameplayScene.cs:281` (unrelated).
- Task 3 of the plan was flagged `checkpoint:human-verify` — requires manual save/reopen cycles to prove the end-to-end behavior. See plan section "Verification / UAT".

## Human verification checklist

Run `dotnet run` and walk through each case:

- [ ] **Case 1 (Village)**: Walk to Village → `F5` → close game → reopen → should spawn in Village at the same position.
- [ ] **Case 2 (Castle)**: Walk to Castle → `F5` → close game → reopen → should spawn in Castle at the same position.
- [ ] **Case 3 (Shop)**: Enter Shop → `F5` → close game → reopen → should spawn in Shop.
- [ ] **Case 4 (Dungeon normalization)**: Save from a dungeon room → close/reopen → should spawn on Farm (dungeons have their own restore path via `DungeonState`; the boot router normalizes `Dungeon:*` back to Farm).
- [ ] **Case 5 (Legacy save)**: Start with a v9 save on disk (from before this change) → reopen → should spawn on Farm at the old `PlayerX/Y` (migration seeded `PositionByScene["Farm"]`).
- [ ] **Case 6 (Per-scene isolation)**: Save position A in Farm → move to Village → save position B → come back to Farm → confirm Farm position unchanged; go back to Village → confirm position B restored (not overwritten by Farm coords).
- [ ] **Case 7 (Door-travel regression guard)**: Walk Farm → Village via door → confirm you appear at the `from_Farm` spawn marker (not at whatever Village position was previously saved). The restore flag is one-shot.

## Commits

| Hash | Message |
|------|---------|
| `37641ea` | feat(quick-260423-shd-01): add PositionByScene dict and v9->v10 migration |
| `6b3ef8a` | feat(quick-260423-shd-01): save-aware boot routing + per-scene position restore |

## Files touched

- `Game1.cs`
- `src/Core/GameState.cs`
- `src/Core/GameStateSnapshot.cs`
- `src/Core/GameplayScene.cs`
- `src/Core/SaveManager.cs`
- `src/Core/ServiceContainer.cs`
- `src/Scenes/FarmScene.cs`
