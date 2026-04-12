---
phase: 4
slug: world-npcs
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-12
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual runtime smoke (no xUnit harness in repo yet) |
| **Config file** | `stardew_medieval_v3.csproj` |
| **Quick run command** | `dotnet build -c Debug --nologo -v q` |
| **Full suite command** | `dotnet build -c Debug --nologo && dotnet run --project stardew_medieval_v3.csproj` (manual smoke) |
| **Estimated runtime** | ~30s build, ~2min manual smoke |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build -c Debug --nologo -v q` (must be 0 errors, 0 warnings new)
- **After every plan wave:** Build + targeted manual smoke of the wave's scene/flow
- **Before `/gsd-verify-work`:** Full build green + full success-criteria walkthrough (5/5)
- **Max feedback latency:** ~30 seconds (build only)

---

## Per-Task Verification Map

> Populated by planner. Each task must map to a REQ-ID and a verification command (build/grep for code presence; manual smoke for runtime behaviors such as fade-to-black, dialogue rendering, shop transactions).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 04-XX-XX | — | — | WLD-01..04 / NPC-01..04 / HUD-03,05 | — | N/A (single-player, local save) | build + manual | `dotnet build` + runtime smoke | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] No new test framework install — phase uses `dotnet build` for compile-time checks and manual runtime smoke checklist
- [ ] Create `.planning/phases/04-world-npcs/SMOKE-CHECKLIST.md` (or inline in plan verification blocks) enumerating the 5 success-criteria smoke steps

*If existing infra is sufficient: "Existing build pipeline covers compile-time validation; runtime behaviors require manual smoke per success criteria."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Farm↔Village fade-to-black transition | WLD-01, WLD-02 | Visual/timing effect | Walk to farm edge trigger → observe fade → land on village spawn; reverse |
| Village interiors (castle, shop) accessible | WLD-03 | Door trigger + scene load | Walk onto castle door tile; walk onto shop door tile; each loads interior scene |
| King dialogue + quest grant | NPC-01, NPC-02 | Dialogue box rendering, quest state write | Interact with King; confirm portrait + text box; close; confirm `MainQuest.State == Active` |
| Shop buy/sell with gold | NPC-03, NPC-04 | UI interaction + gold/inventory delta | Open shop; buy item (gold decreases, item in inventory); sell item (reverse) |
| Quest-state-aware dialogue branching | NPC-02 | Runtime state switch | Test King dialogue in 3 states: none / active / complete (use dev key per research Open Q #5) |
| HUD quest tracker visibility | HUD-03, HUD-05 | Overlay rendering | Accept quest → HUD shows objective; complete → HUD updates/hides |

---

## Validation Sign-Off

- [ ] All tasks have build verification or manual smoke dependency declared
- [ ] Sampling continuity: every task commit triggers `dotnet build`
- [ ] Wave 0 covers SMOKE-CHECKLIST.md (or equivalent in plan verification)
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s for build
- [ ] `nyquist_compliant: true` set in frontmatter once planner populates the task map

**Approval:** pending
