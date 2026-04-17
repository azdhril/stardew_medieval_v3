# Roadmap: Stardew Medieval

## Milestones

- ✅ **v1.0 MVP** — Phases 1-6 (shipped 2026-04-17) — [archive](milestones/v1.0-ROADMAP.md)
- 📋 **v1.1** — Phase 7+ (planned)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-6) — SHIPPED 2026-04-17</summary>

- [x] Phase 1: Architecture Foundation (4/4 plans)
- [x] Phase 2: Items & Inventory (3/3 plans)
- [x] Phase 3: Combat (3/3 plans)
- [x] Phase 3.1: Verification Backfill & Metadata Sync (3/3 plans) — INSERTED
- [x] Phase 4: World & NPCs (7/7 plans)
- [x] Phase 4.1: Shop UX & Save Gap Closure (3/3 plans) — INSERTED
- [x] Phase 5: Dungeon (6/6 plans)
- [x] Phase 6: Progression & Polish (3/3 plans)

Full details: [milestones/v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md).

</details>

### 📋 v1.1 (Planned)

- [ ] **Phase 7: Animation System & Mana Seed Integration** — refactor Entity/animation system to support named animations with variable per-frame timing, paper-doll layered sprites (body/hair/outfit/weapon), and integrate Mana Seed pack (8x8 64x64 sheets) replacing current 4x4 hardcoded layout. Enables idle/walk/run with correct timing, weapon-specific attacks, and base for character customization.

### Carried Tech Debt from v1.0

- **CMB-01 visual polish:** `SlashEffect` not wired in DungeonScene (FarmScene only). Combat mechanics fully functional.
- **Nyquist VALIDATION.md** gaps in phases 01/02/03.1/04/04.1/06.

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Architecture Foundation | v1.0 | 4/4 | Complete | 2026-04-10 |
| 2. Items & Inventory | v1.0 | 3/3 | Complete | 2026-04-11 |
| 3. Combat | v1.0 | 3/3 | Complete | 2026-04-12 |
| 3.1. Verification Backfill | v1.0 | 3/3 | Complete | 2026-04-12 |
| 4. World & NPCs | v1.0 | 7/7 | Complete | 2026-04-13 |
| 4.1. Shop UX & Save Gap | v1.0 | 3/3 | Complete | 2026-04-13 |
| 5. Dungeon | v1.0 | 6/6 | Complete | 2026-04-16 |
| 6. Progression & Polish | v1.0 | 3/3 | Complete | 2026-04-16 |
| 7. Animation System | v1.1 | 0/0 | Not started | - |
