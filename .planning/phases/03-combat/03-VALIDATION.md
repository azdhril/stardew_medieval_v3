---
phase: 03
slug: combat
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-11
audited: 2026-04-12
---

# Phase 03 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual runtime verification |
| **Config file** | stardew_medieval_v3.csproj |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run --no-build` (visual check) |
| **Estimated runtime** | ~5 seconds (build), ~30 seconds (visual check) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run --no-build`
- **Before `/gsd-verify-work`:** Full build must succeed, visual check on all combat mechanics
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| 03-01-01 | 01 | 1 | CMB-01 | build+grep | `dotnet build && grep -r "TakeDamage" Core/Entity.cs` | COVERED |
| 03-01-02 | 01 | 1 | CMB-01 | build+grep | `dotnet build && grep -r "MeleeAttack\|AttackHitbox" Combat/` | COVERED |
| 03-01-03 | 01 | 1 | CMB-02 | build+grep | `dotnet build && grep -r "FireballEntity\|Projectile" Combat/` | COVERED |
| 03-01-04 | 01 | 1 | CMB-03 | build+grep | `dotnet build && grep -r "HealthBar\|HPBar" UI/` | COVERED |
| 03-02-01 | 02 | 2 | CMB-04, CMB-05 | build+grep | `dotnet build && grep -r "EnemyEntity\|EnemyData" Combat/` | COVERED |
| 03-02-02 | 02 | 2 | CMB-04 | build+grep | `dotnet build && grep -r "AIState\|Idle\|Chase\|Attack\|Return" Combat/` | COVERED |
| 03-03-01 | 03 | 3 | CMB-06 | build+grep | `dotnet build && grep -r "BossEntity\|SkeletonKing" Combat/` | COVERED |

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. MonoGame build pipeline already functional.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Sword swing visual + knockback | CMB-01 | Visual/animation feedback | Run game, equip Iron_Sword, LMB near enemy, verify knockback + slash arc visible |
| Fireball travels and hits | CMB-02 | Projectile visual + collision | RMB to cast, verify fireball travels in facing direction, damages enemy on hit |
| HP bars decrease on damage | CMB-03 | Visual overlay rendering | Attack enemy, verify HP bar shrinks. Take damage, verify player HP bar in HUD shrinks |
| 3 enemy types behave differently | CMB-04 | AI behavior visual check | Observe skeleton rushing, mage keeping distance and shooting, golem moving slowly |
| Boss telegraph + summon | CMB-06 | Complex animation timing | Engage Skeleton King, verify wind-up flash before slash, verify skeleton spawns at HP thresholds |
| i-frames on player damage | CMB-03 | Visual blink + damage immunity | Take damage, verify player blinks and doesn't take damage again for ~1s |

---

## Validation Sign-Off

- [x] All tasks have automated `dotnet build` verify
- [x] Sampling continuity: build check after every commit
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-04-12

---

## Validation Audit 2026-04-12

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

**Notes:**
- All 7 task-level build+grep commands executed and passed against current codebase.
- `dotnet build`: 0 warnings, 0 errors.
- Manual-only items verified by UAT (03-UAT.md): 15/15 passed, 0 issues.
- Phase 03 is Nyquist-compliant: every requirement has either an automated build+grep check or a completed UAT pass.
