---
phase: 05
slug: dungeon
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-14
---

# Phase 05 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit (Wave 0 installs — no test project exists yet) |
| **Config file** | `tests/stardew_medieval_v3.Tests.csproj` (Wave 0 creates) |
| **Quick run command** | `dotnet test tests/stardew_medieval_v3.Tests.csproj --filter Category=quick --nologo` |
| **Full suite command** | `dotnet test tests/stardew_medieval_v3.Tests.csproj --nologo` |
| **Estimated runtime** | ~15 seconds (projected) |

---

## Sampling Rate

- **After every task commit:** Run quick tests
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite must be green + manual UAT of dungeon playthrough
- **Max feedback latency:** 20 seconds

---

## Per-Task Verification Map

*Populated by planner. One row per task. Wave 0 bootstrap rows marked ❌ W0.*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD     | TBD  | 0    | (infra)     | —          | N/A             | infra     | `dotnet test`     | ❌ W0       | ⬜ pending |

---

## Wave 0 Requirements

- [ ] Create `tests/stardew_medieval_v3.Tests.csproj` (xunit + project reference)
- [ ] `tests/DungeonStateTests.cs` — stubs for DNG-01..DNG-04
- [ ] `tests/ChestPersistenceTests.cs` — stubs covering GameStateSnapshot Chests round-trip
- [ ] Wire solution file / build target so `dotnet test` runs from repo root

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual door/gate transition on clear | DNG-02 | Animation/sprite swap needs eyes | Enter room, kill all enemies, confirm gate opens visibly |
| Boss fight feel & difficulty | DNG-04 | Balance/feel judgement | Play full dungeon, defeat boss, confirm quest completes |
| Chest overlay UX (drag/Take All/Sort) | DNG-03 | Reuses Phase 04 pattern but needs human feel-check | Open chest, drag items, close |
| Dungeon reset on death | DNG-01 | Transition clarity | Die in dungeon, confirm respawn + state reset as designed |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (test project bootstrap)
- [ ] No watch-mode flags
- [ ] Feedback latency < 20s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
