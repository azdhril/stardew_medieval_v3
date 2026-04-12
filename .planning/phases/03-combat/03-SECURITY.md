---
phase: 3
slug: combat
status: verified
threats_open: 0
asvs_level: 1
created: 2026-04-12
---

# Phase 3 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

Not applicable — single-player offline game with no network, auth, or external data. All input is local keyboard/mouse. Persistence is a local JSON save file under `%LOCALAPPDATA%\StardewMedieval\savegame.json`. No ASVS categories apply to this phase's scope.

---

## Threat Register

No threats identified across plans 03-01, 03-02, and 03-03. Each PLAN.md `<threat_model>` block explicitly declares "No threats identified. Single-player offline game with local save files only."

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| (none)    | —        | —         | —           | —          | —      |

---

## Accepted Risks Log

No accepted risks.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-04-12 | 0             | 0      | 0    | /gsd-secure-phase (no threats in PLAN artifacts) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer) — N/A, zero threats
- [x] Accepted risks documented in Accepted Risks Log — none
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-04-12
