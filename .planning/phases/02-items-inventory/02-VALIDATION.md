---
phase: 2
slug: items-inventory
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-10
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit (via `dotnet new xunit`) or manual build verification |
| **Config file** | none — Wave 0 installs if unit tests desired |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` (manual smoke test) |
| **Estimated runtime** | ~5 seconds (build only) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` (manual playtest)
- **Before `/gsd-verify-work`:** Full build + manual playtest of all success criteria
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | INV-01 | — | N/A | unit | `dotnet build` | ❌ W0 | ⬜ pending |
| 02-01-02 | 01 | 1 | INV-03 | — | N/A | unit | `dotnet build` | ❌ W0 | ⬜ pending |
| 02-02-01 | 02 | 1 | INV-02 | — | N/A | manual | visual verification | N/A | ⬜ pending |
| 02-02-02 | 02 | 1 | HUD-02 | — | N/A | manual | press I, verify overlay | N/A | ⬜ pending |
| 02-02-03 | 02 | 1 | INV-04 | — | N/A | manual | visual verification | N/A | ⬜ pending |
| 02-03-01 | 03 | 2 | INV-05 | — | N/A | manual | harvest, verify magnetism | N/A | ⬜ pending |
| 02-03-02 | 03 | 2 | FARM-01 | — | N/A | manual | till/water, verify position | N/A | ⬜ pending |
| 02-03-03 | 03 | 2 | FARM-02 | — | N/A | manual | plant crop, verify sprites | N/A | ⬜ pending |
| 02-03-04 | 03 | 2 | FARM-03 | — | N/A | manual | harvest, verify in inventory | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `dotnet build` passes with no errors (baseline)
- [ ] InventoryManager is pure data class — unit testable without MonoGame rendering

*Note: Most Phase 2 requirements are visual/interactive and require manual testing. Unit tests only practical for InventoryManager data operations (TryAdd, MoveItem, equip/unequip).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Inventory grid with 20 slots visible | INV-01 | Requires visual rendering | Press I, count 20 slot boxes on screen |
| Hotbar slots 1-8 at screen bottom | INV-02 | Requires visual rendering | Verify 8 numbered slots visible during gameplay |
| Equipment stat changes reflected | INV-03 | Requires in-game stat display | Equip weapon, check attack stat in equipment tab |
| Rarity colors on item borders | INV-04 | Requires visual rendering | Place items of different rarity, verify border colors |
| Item magnetism pull toward player | INV-05 | Requires physics + visual | Drop item, walk near it, verify it accelerates toward player |
| Player position fix for farming | FARM-01 | Requires visual verification | Till/water tiles, verify action happens on facing tile |
| Crop sprites replace overlays | FARM-02 | Requires visual rendering | Plant and grow crop, verify real sprites instead of color overlays |
| Harvest flow to inventory | FARM-03 | Requires full loop test | Harvest ripe crop, verify item drop spawns, magnetism pulls, appears in inventory |
| Inventory opens with I key | HUD-02 | Requires input + visual | Press I during gameplay, verify overlay appears |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
