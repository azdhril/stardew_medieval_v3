---
phase: 06
slug: progression-polish
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-16
---

# Phase 06 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 |
| **Config file** | tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj |
| **Quick run command** | `dotnet test tests/stardew_medieval_v3.Tests --filter "Category=quick" --no-build -v q` |
| **Full suite command** | `dotnet test tests/stardew_medieval_v3.Tests -v q` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/stardew_medieval_v3.Tests --filter "Category=quick" --no-build -v q`
- **After every plan wave:** Run `dotnet test tests/stardew_medieval_v3.Tests -v q`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | PRG-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~XPTableTests" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-02 | 01 | 1 | PRG-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~ProgressionTests" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-03 | 01 | 1 | PRG-02 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~LevelUpStatTests" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-04 | 01 | 1 | PRG-03 | — | N/A | manual | `dotnet run` — kill skeleton, verify gold coin drop + pickup | — | ⬜ pending |
| 06-02-01 | 02 | 2 | PRG-04 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~DeathPenaltyTests" --no-build` | ❌ W0 | ⬜ pending |
| 06-02-02 | 02 | 2 | PRG-04 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~DeathPenaltyTests.EmptyInventory"` | ❌ W0 | ⬜ pending |
| 06-02-03 | 02 | 2 | SAV-01 | — | N/A | unit | `dotnet test --filter "FullyQualifiedName~SaveV8ToV9MigrationTests" --no-build` | ❌ W0 | ⬜ pending |
| 06-02-04 | 02 | 2 | SAV-02 | — | N/A | unit | same class | ❌ W0 | ⬜ pending |
| 06-03-01 | 03 | 3 | HUD-01 | — | N/A | manual | `dotnet run` — visual inspection of XP bar, NineSlice panels | — | ⬜ pending |
| 06-03-02 | 03 | 3 | HUD-04 | — | N/A | manual | `dotnet run` — quest tracker visual inspection | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/stardew_medieval_v3.Tests/Progression/XPTableTests.cs` — XP curve math (PRG-01)
- [ ] `tests/stardew_medieval_v3.Tests/Progression/LevelUpStatTests.cs` — stat grants per level (PRG-02)
- [ ] `tests/stardew_medieval_v3.Tests/Progression/DeathPenaltyTests.cs` — gold loss + item roll + empty inventory edge case (PRG-04)
- [ ] `tests/stardew_medieval_v3.Tests/Save/SaveV8ToV9MigrationTests.cs` — v8→v9 migration roundtrip (SAV-01, SAV-02)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Gold coin drops from enemies, magnetism pickup works | PRG-03 | Requires rendering + physics interaction | Kill Skeleton in dungeon; verify coin entity spawns, magnetizes toward player, gold counter increases by ~5 |
| XP bar renders above hotbar, NineSlice panels for clock/gold | HUD-01 | Visual rendering verification | Run game; verify XP bar visible above hotbar, clock in NineSlice panel, gold with icon |
| Quest tracker shows NineSlice panel + state text | HUD-04 | Visual rendering verification | Run game with active quest; verify quest tracker has NineSlice background |
| Level-up banner + particle burst displays | PRG-02 | Visual + timing verification | Kill enough enemies to level up; verify "LEVEL UP! Lv X" banner + gold particles |
| Death banner + Toast loss messages | PRG-04 | Visual UX verification | Die with items; verify red "You died" banner + Toast listing lost items/gold |
| Periodic auto-save every ~30s | SAV-01 | Timing + file write verification | Play for >30s; check console for "[Scene] Auto-save (periodic)" log |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
