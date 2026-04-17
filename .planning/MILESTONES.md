# Milestones

## v1.0 MVP (Shipped: 2026-04-17)

**Phases completed:** 8 phases (01, 02, 03, 03.1, 04, 04.1, 05, 06), 29 plans
**Audit:** 42/42 v1 requirements satisfied (status: `tech_debt` — see Known Gaps)
**Archive:** [milestones/v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md), [milestones/v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md), [milestones/v1.0-MILESTONE-AUDIT.md](milestones/v1.0-MILESTONE-AUDIT.md)

### What Shipped

- **Architecture (Phase 1):** SceneManager with fade-to-black transitions, Entity base class, ServiceContainer, GameState v3 — Game1 reduced to thin shell delegating to scenes.
- **Items & Inventory (Phase 2):** 20-slot InventoryManager with stacking + equipment, 8-slot Hotbar, InventoryScene overlay (Tibia-style equipment tab), ItemDropEntity with bounce/magnetism, rarity tinting, harvest-to-inventory loop.
- **Combat (Phase 3):** Melee (LMB) + fireball (RMB) with i-frames/knockback/HP bars, three data-driven enemy types (Skeleton/DarkMage/Golem) with FSM AI, Skeleton King boss with telegraphed slash + minion summoning + save-persistent BossKilled.
- **World & NPCs (Phase 4 + 4.1):** Farm ↔ Village ↔ Castle ↔ Shop scene graph with trigger zones, state-preserving transitions, DialogueScene with quest-state branching, KingNpc + MainQuest, ShopkeeperNpc with mouse-navigable shop (scroll persistence + per-row qty), save round-trip for Gold/Inventory/Quest.
- **Dungeon (Phase 5):** 7-room dungeon (linear + optional chest rooms) with combat-gated doors, drag-drop chest loot (per-save permanent), boss finale, death-reset semantics, viewport/fullscreen polish.
- **Progression & Polish (Phase 6):** XPTable + ProgressionManager (exponential 50·1.22^(L−1) curve, OnLevelUp stat push), Gold_Coin currency with magnetism, DeathPenalty (10% gold + probabilistic item loss), LevelUpBanner + particles + DeathBanner + Toast queue, 30s auto-save, HUD with NineSlice panels + XP bar + quest tracker, Save v9 with cumulative v1→v9 migration.

### Core Loop Delivered

Farm → harvest → sell at shop → walk to dungeon → fight → XP/gold → level up → return → sleep/save → relaunch with progress intact. All three E2E flows (core loop, death, quest) verified by integration checker.

### Known Gaps (carried to v1.1)

- **CMB-01 visual polish:** `SlashEffect` wired in FarmScene but not DungeonScene. Melee mechanics fully functional; only the swing overlay is absent in dungeon combat.
- **Nyquist VALIDATION.md:** Phases 01/02/04/06 have `nyquist_compliant: false`; 03.1 and 04.1 have no VALIDATION.md. Can be closed via `/gsd-validate-phase N` per phase.

---
