---
phase: 03
slug: combat
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-11
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
| 03-01-01 | 01 | 1 | CMB-01 | build+grep | `dotnet build && grep -r "TakeDamage" Core/Entity.cs` | pending |
| 03-01-02 | 01 | 1 | CMB-01 | build+grep | `dotnet build && grep -r "MeleeAttack\|AttackHitbox" Combat/` | pending |
| 03-01-03 | 01 | 1 | CMB-02 | build+grep | `dotnet build && grep -r "FireballEntity\|Projectile" Combat/` | pending |
| 03-01-04 | 01 | 1 | CMB-03 | build+grep | `dotnet build && grep -r "DrawHPBar\|HPBar" UI/` | pending |
| 03-02-01 | 02 | 2 | CMB-04, CMB-05 | build+grep | `dotnet build && grep -r "EnemyEntity\|EnemyData" Combat/` | pending |
| 03-02-02 | 02 | 2 | CMB-04 | build+grep | `dotnet build && grep -r "AIState\|Idle\|Chase\|Attack\|Return" Combat/` | pending |
| 03-03-01 | 03 | 3 | CMB-06 | build+grep | `dotnet build && grep -r "BossEntity\|SkeletonKing" Combat/` | pending |

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

- [ ] All tasks have automated `dotnet build` verify
- [ ] Sampling continuity: build check after every commit
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
