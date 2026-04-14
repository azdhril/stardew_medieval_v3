---
phase: 05
slug: dungeon
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-04-14
updated: 2026-04-14
---

# Phase 05 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit (Wave 0 bootstraps — no test project exists yet) |
| **Config file** | `tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj` (Plan 01 Task 1 creates) |
| **Quick run command** | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "Category=quick" --nologo` |
| **Dungeon-only quick** | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Dungeon" --nologo` |
| **Full suite command** | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --nologo` |
| **Estimated runtime** | ~10–15 seconds (projected, pure-CS unit tests only) |

---

## Sampling Rate

- **After every task commit:** Quick run (dungeon-only filter, < 10s).
- **After every plan wave:** Full suite.
- **Before `/gsd-verify-work`:** Full suite green + manual UAT of full dungeon playthrough (entrance → 6 rooms → boss → village).
- **Max feedback latency:** 20 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| P01-T1 (Wave 0 bootstrap) | 05-01 | 1 | infra | — | N/A | infra | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --nologo` | ❌ W0 (this task creates it) | ⬜ pending |
| P01-T2 (DungeonState + Registry + Save v8) | 05-01 | 1 | DNG-01 | T-05-01 | Tampering | unit | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Dungeon\|FullyQualifiedName~Save" --nologo` | ✓ after P01-T1 | ⬜ pending |
| P01-T3 (CombatLoop + EnemySpawner refactor + DungeonScene shell) | 05-01 | 1 | DNG-02 | — | N/A | unit + build | `dotnet build stardew_medieval_v3.csproj --nologo && dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Dungeon" --nologo` | ✓ after P01-T1 | ⬜ pending |
| P02-T1 (DungeonDoor + tileset + village entry) | 05-02 | 2 | DNG-01, DNG-02 | T-05-07 | DoS (missing group fallback) | build | `dotnet build stardew_medieval_v3.csproj --nologo` | ✓ | ⬜ pending |
| P02-T2 (6 room TMX + chest seeding + Registry integrity) | 05-02 | 2 | DNG-01, DNG-03 | T-05-05, T-05-06 | Tampering, EoP | unit + build | `dotnet build stardew_medieval_v3.csproj --nologo && dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Dungeon" --nologo` | ✓ | ⬜ pending |
| P03-T1 (Boss TMX + spawn guard + victory handler) | 05-03 | 3 | DNG-04 | T-05-10 | DoS (missing BossSpawn fallback) | build | `dotnet build stardew_medieval_v3.csproj --nologo` | ✓ | ⬜ pending |
| P03-T2 (BossVictoryTests + DungeonState reset semantics) | 05-03 | 3 | DNG-04, DNG-01 | T-05-08, T-05-09 | Tampering, Repudiation | unit | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --filter "FullyQualifiedName~Boss\|FullyQualifiedName~Dungeon" --nologo` | ✓ | ⬜ pending |

---

## Wave 0 Requirements

- [ ] Create `tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj` (xunit + project reference).
- [ ] `tests/Dungeon/DungeonStateTests.cs` — stubs for DNG-01, boss persistence, death reset.
- [ ] `tests/Dungeon/DungeonRegistryTests.cs` — stubs for 7-room integrity, boss prerequisite, orphan trigger detection.
- [ ] `tests/Dungeon/RoomClearedTests.cs` — stubs for one-shot cleared event.
- [ ] `tests/Dungeon/LootRollTests.cs` — stubs for seeded determinism (DNG-03).
- [ ] `tests/Save/SaveV7ToV8MigrationTests.cs` — stubs for v7→v8 migration.
- [ ] Solution file lists both projects so `dotnet test` runs from repo root.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual door/gate transition on clear | DNG-02 | Animation/sprite swap needs eyes | Enter room, kill all enemies, confirm gate opens visibly |
| Boss fight feel & difficulty | DNG-04 | Balance/feel judgement | Play full dungeon, defeat boss, confirm quest completes |
| Chest overlay UX (drag/Take All/Sort) | DNG-03 | Reuses Phase 04 pattern but needs human feel-check | Open chest, drag items, close |
| Dungeon reset on death | DNG-01 | Transition clarity | Die in dungeon, confirm respawn at farm + state reset (doors closed, chests reclosed, loot re-rolled, BossDefeated persists) |
| King quest-complete dialogue | DNG-04 / NPC-04 | Exercises Phase 4 branch | Defeat boss, return to village, talk to King |
| Village → dungeon entry feel | DNG-01 | Trigger placement UX | Walk to cave entrance in village, confirm fade transition into r1 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify command
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (every task has one)
- [x] Wave 0 covers all MISSING references (test project bootstrap is P01-T1)
- [x] No watch-mode flags
- [x] Feedback latency < 20s target
- [x] `nyquist_compliant: true`

**Approval:** planned — awaiting execution
