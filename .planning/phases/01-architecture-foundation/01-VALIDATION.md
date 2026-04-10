---
phase: 1
slug: architecture-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-10
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (MSTest / xUnit — to be decided by planner) |
| **Config file** | none — Wave 0 installs |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run --project stardew_medieval_v3.csproj` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual launch verify
- **Before `/gsd-verify-work`:** Full build must be green, game must boot into FarmScene
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| 01-01-01 | 01 | 1 | ARCH-02 | build | `dotnet build` | ⬜ pending |
| 01-01-02 | 01 | 1 | ARCH-05 | build | `dotnet build` | ⬜ pending |
| 01-02-01 | 02 | 1 | ARCH-03 | build | `dotnet build` | ⬜ pending |
| 01-02-02 | 02 | 1 | ARCH-04 | build | `dotnet build` | ⬜ pending |
| 01-03-01 | 03 | 2 | ARCH-01 | build+run | `dotnet build && dotnet run` | ⬜ pending |
| 01-03-02 | 03 | 2 | ARCH-05 | build+run | `dotnet build && dotnet run` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `dotnet build` passes with zero errors before any changes
- [ ] Current game launches and runs (baseline verification)

---

## Validation Architecture

### Verification Strategy
MonoGame GraphicsDevice cannot be instantiated in headless test environments. Validation relies on:
1. **Build verification** — `dotnet build` must pass after every task
2. **Runtime verification** — Game must boot and reach FarmScene without exceptions
3. **Regression check** — Visual behavior must match pre-refactor (manual spot-check)
4. **Save round-trip** — Save, quit, reload must preserve all state including new fields

### Key Invariants
- Game1.cs delegates to SceneManager (no direct entity/farming logic)
- FarmScene reproduces exact same visual output as pre-refactor Game1
- Old saves (v2) load successfully with defaults for new fields
- Entity base class compiles and PlayerEntity inherits from it
- ItemRegistry loads items.json without errors

---

*Phase: 01-architecture-foundation*
*Validation strategy created: 2026-04-10*
