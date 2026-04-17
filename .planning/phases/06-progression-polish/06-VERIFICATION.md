---
phase: 06-progression-polish
type: verification
date: 2026-04-17
verified: 2026-04-17T00:00:00Z
status: passed
score: 8/8 requirements verified
method: goal-backward (truths -> artifacts -> evidence)
requirements_verified: [PRG-01, PRG-02, PRG-03, PRG-04, HUD-01, HUD-04, SAV-01, SAV-02]
overrides_applied: 0
references:
  - .planning/phases/06-progression-polish/06-CONTEXT.md
  - .planning/phases/06-progression-polish/06-01-SUMMARY.md
  - .planning/phases/06-progression-polish/06-02-SUMMARY.md
  - .planning/phases/06-progression-polish/06-03-SUMMARY.md
  - .planning/v1.0-MILESTONE-AUDIT.md (integration check)
---

# Phase 6 Verification: Progression & Polish

## Summary

All 8 requirements assigned to Phase 6 are **Satisfied**. Implementation landed across plans 06-01 (XP/Level/Gold + save v9), 06-02 (Death penalty + feedback + auto-save), and 06-03 (HUD polish). This document provides goal-backward evidence mapping each requirement to concrete source artifacts, confirmed on disk at commit `70ad2cd` and cross-checked by the `gsd-integration-checker` integration report in the v1.0 milestone audit.

This VERIFICATION.md is a post-hoc artifact produced during v1.0 milestone re-audit (2026-04-17) to close the documentation gap — implementation and integration were already complete.

| Requirement | Status | Primary Evidence |
|-------------|--------|------------------|
| PRG-01 | Satisfied | src/Progression/XPTable.cs + src/Progression/ProgressionManager.cs (AwardXP) + src/Combat/CombatLoop.cs (OnEnemyKilled) |
| PRG-02 | Satisfied | src/Progression/ProgressionManager.cs OnLevelUp stat push (MaxHP, MaxStamina, BaseDamageBonus) + src/Combat/CombatManager.cs DamageBonus |
| PRG-03 | Satisfied | src/Data/items.json Gold_Coin + src/Entities/ItemDropEntity.cs Currency branch + src/Progression/ProgressionManager.cs RollGold |
| PRG-04 | Satisfied | src/Progression/DeathPenalty.cs + src/Scenes/DungeonScene.cs death path + src/Scenes/FarmScene.cs FromScene guard |
| HUD-01 | Satisfied | src/UI/HUD.cs (NineSlice panels, XP bar, icon labels) + src/UI/UITheme.cs |
| HUD-04 | Satisfied | src/UI/HUD.cs DrawQuestTracker + CastleScene/ShopScene overlay calls |
| SAV-01 | Satisfied | src/Core/SaveManager.cs v9 + src/Core/GameStateSnapshot.cs SaveNow |
| SAV-02 | Satisfied | src/Core/SaveManager.cs MigrateIfNeeded (cumulative v1→v9) |

---

## Per-Requirement Evidence

### PRG-01 — Sistema de XP

**Expected truth:** Matar inimigos da XP, threshold crescente por level.

**Evidence:**
- `src/Progression/XPTable.cs` defines the exponential curve `50 * 1.22^(level-1)` and the per-enemy XP lookup dictionary.
- `src/Progression/ProgressionManager.cs:55` `AwardXP(enemyId)` — resolves XP per enemy (fallback 5), adds to total, checks level-up threshold via `XPToNextLevel(Level)`.
- `src/Combat/CombatLoop.cs` — `OnEnemyKilled` callback invoked on both regular enemy death and boss death paths.
- `src/Scenes/FarmScene.cs` + `src/Scenes/DungeonScene.cs` — wire `OnEnemyKilled` to `Services.Progression.AwardXP` (confirmed by integration checker).

**Status:** Satisfied.

---

### PRG-02 — Level Up concede +HP, +damage, +stamina

**Expected truth:** Level up grants stat increases for 10–15 levels of v1 content.

**Evidence:**
- `src/Progression/ProgressionManager.cs:72` increments `BaseDamageBonus` on level-up; derives new `MaxHP`/`MaxStamina` and pushes to `_player.MaxHP` + `_stats.MaxStamina`.
- `src/Combat/CombatManager.cs` — `DamageBonus` property added to melee damage calculation; synced via `OnLevelUp` subscription in FarmScene/DungeonScene.
- `src/Core/GameState.cs` — `MaxHP`, `MaxStamina`, `BaseDamageBonus` persist as v9 save fields.
- `OnLevelUp?.Invoke(Level)` (line 77) fires event consumed by `LevelUpBanner`, `LevelUpParticles`, and immediate `SaveNow`.

**Status:** Satisfied.

---

### PRG-03 — Sistema de Gold

**Expected truth:** Gold drops from enemies + sale of crops/items; currency item with pickup.

**Evidence:**
- `src/Data/items.json` — `Gold_Coin` defined with `ItemType.Currency`.
- `src/Data/ItemType.cs` — `Currency` enum value.
- `src/Entities/ItemDropEntity.cs` — currency-type check bypasses inventory slot, calls `AddGold` directly on magnetism pickup.
- `src/Progression/ProgressionManager.cs:119` `RollGold(enemyId, rng)` — returns gold amount for enemy kill.
- `src/Combat/CombatLoop.cs` — on enemy/boss kill, spawns `Gold_Coin` drop via `SpawnItemDrop`.
- `src/Scenes/ShopScene.cs` / `ShopOverlayScene` — `TrySpendGold` / `AddGold` wired for buy/sell flow.

**Status:** Satisfied.

---

### PRG-04 — Consequência de Morte

**Expected truth:** Perder 10% gold + chance aleatória de perder 1 item; respawn na fazenda.

**Evidence:**
- `src/Progression/DeathPenalty.cs` `Apply()` — deducts `floor(gold * 0.10)` (0 for amounts <10); rolls RNG for item loss (15% × 2 items, 25% × 1 item, 60% none); unified pool includes inventory slots + equipped items; `PruneBrokenReferences()` clears stale hotbar/consumable refs.
- `src/Scenes/DungeonScene.cs` — applies penalty BEFORE HP restore, save, and run reset; transitions to `FarmScene` with `FromScene="DungeonDeath"`.
- `src/Scenes/FarmScene.cs` — guard `FromScene != "DungeonDeath"` prevents double-apply; shows `DeathBanner`.
- `src/Inventory/InventoryManager.cs` — `ForceRemoveEquipment` added for death penalty.
- `tests/stardew_medieval_v3.Tests/Progression/DeathPenaltyTests.cs` — 8 unit tests pass.

**Status:** Satisfied.

---

### HUD-01 — HUD Gráfica com Sprites

**Expected truth:** Barra de HP, barra de stamina, hotbar com ícones, relógio/dia renderizados com sprites.

**Evidence:**
- `src/UI/HUD.cs` — NineSlice panels for clock/day (line 171), gold (line 288), quest tracker (line 421).
- `src/UI/HUD.cs:316` `DrawXPBar` — Style1 progress sprites with level label, 6px above hotbar at full width.
- `src/UI/HUD.cs:303` orchestrates draw order; icon-decorated labels (clock icon, gold coin icon); numeric text removed from HP/MP/STA bars per 06-03 plan (bars self-expressive at 960x540).
- `src/UI/UITheme.cs` — eagerly loaded in `FarmScene.LoadContent` so HUD NineSlice panels render from frame 1.
- `src/UI/HotbarRenderer.cs` — hotbar with slot icons (already from Phase 2, integrated into HUD).

**Status:** Satisfied.

---

### HUD-04 — Quest Tracker

**Expected truth:** Tracker simples mostrando missão ativa e objetivo atual.

**Evidence:**
- `src/UI/HUD.cs:408` `DrawQuestTracker(sb, font, pixel, questState, screenWidth, theme)` — static helper (made static in Phase 4-03 per STATE.md decisions) so overlay scenes can call it without HUD instance.
- `src/UI/HUD.cs:310` — called from `HUD.Draw` passing `MainQuestState`.
- `src/Scenes/CastleScene.cs` + `src/Scenes/ShopScene.cs` — explicit `DrawQuestTracker` calls so tracker stays visible over overlays.
- State transitions drive tracker text: `NotStarted` hidden → `Active` shows objective → `Complete` shows completion line, driven by `src/Quests/MainQuest.cs` state.

**Status:** Satisfied.

---

### SAV-01 — Save/Load Estendido

**Expected truth:** Persistir inventory, equipment, XP/level, gold, quest state, scene atual.

**Evidence:**
- `src/Core/SaveManager.cs:14` `CURRENT_SAVE_VERSION = 9`.
- `src/Core/GameStateSnapshot.cs` `SaveNow` serializes: Inventory (slots + equipment + hotbar), Gold, XP/Level/MaxHP/MaxStamina/BaseDamageBonus, MainQuest state, CurrentScene, Chests, Resources, Dungeon run state.
- `SaveNow` triggered by: 30s auto-save timer (GameplayScene base), F5 manual, `OnLevelUp`, shop close, dungeon death, farm respawn, boss defeat.
- Integration checker traced full save round-trip: quit → reopen → FarmScene.OnLoad calls `SaveManager.Load` then `_inventory.LoadFromState`, `_mainQuest.LoadFromState`, `Progression.LoadFromState`. All state restores.

**Status:** Satisfied.

---

### SAV-02 — Migração de Versão

**Expected truth:** Save files antigos continuam carregando após bump de versão.

**Evidence:**
- `src/Core/SaveManager.cs:67` `MigrateIfNeeded(state)` — cumulative migration chain v1→v9.
- v8→v9 block (added in 06-01): derives `MaxHP=100`, `MaxStamina=100`, `BaseDamageBonus=0` as defaults.
- `tests/stardew_medieval_v3.Tests/Save/SaveV7ToV8MigrationTests.cs` — updated in 06-01 to assert `SaveVersion==9` after full chain (auto-fix during execution; all 44 tests pass).
- `tests/stardew_medieval_v3.Tests/Save/SaveV8ToV9MigrationTests.cs` — new tests for v9 defaults.
- v2→v3 migration previously human-verified in Phase 01 re-verification (see 01-VERIFICATION.md `re_verification` block).

**Status:** Satisfied.

---

## Cross-Phase Integration

Verified by `gsd-integration-checker` during v1.0 milestone re-audit. Full wiring map in `.planning/v1.0-MILESTONE-AUDIT.md`. Summary:

- `CombatLoop.OnEnemyKilled` → `AwardXP` + `RollGold` + `SpawnItemDrop("Gold_Coin")` wired in both FarmScene and DungeonScene.
- `CombatManager.DamageBonus` ← `Services.Progression.BaseDamageBonus` (pushed on load + `OnLevelUp`) in both scenes.
- `DeathPenalty.Apply` — DungeonScene pre-save + FarmScene with de-dup guard.
- `Services.Toast` — shared via ServiceContainer so death-penalty messages survive DungeonScene→FarmScene transition.
- `GameplayScene` base — hosts banners, particles, Toast, auto-save timer. FarmScene/DungeonScene inherit.
- `HUD.DrawQuestTracker` — static helper, callable from any scene (CastleScene, ShopScene).

**No cross-phase integration gaps affecting any Phase 6 requirement.**

---

## Tests

- `tests/stardew_medieval_v3.Tests/Progression/XPTableTests.cs` — pass.
- `tests/stardew_medieval_v3.Tests/Progression/ProgressionManagerTests.cs` — pass.
- `tests/stardew_medieval_v3.Tests/Progression/DeathPenaltyTests.cs` — 8 tests pass.
- `tests/stardew_medieval_v3.Tests/Save/SaveV7ToV8MigrationTests.cs` — pass (updated for v9 chain).
- `tests/stardew_medieval_v3.Tests/Save/SaveV8ToV9MigrationTests.cs` — new, pass.
- Full suite: 52 tests pass at end of 06-02; build clean at 06-03.

---

## Known Follow-ups (Non-Blocking)

1. **SlashEffect not wired in DungeonScene** — tracked in v1.0 milestone audit as CMB-01 visual-polish gap (tech debt). Combat mechanics fully functional; only the swing overlay is absent in the dungeon. Carries into v1.1 polish.
2. **Nyquist VALIDATION.md** — `nyquist_compliant: false`, `wave_0_complete: false`. Can be closed post-milestone with `/gsd-validate-phase 06`.

---

## Verdict

**PASS** — all 8 Phase 6 requirements satisfied with traceable evidence in source code and integration verified end-to-end. Phase 6 closes the v1.0 core loop.

---

*Verification produced during v1.0 milestone re-audit on 2026-04-17.*
