# Phase 6: Progression & Polish — Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in `06-CONTEXT.md` — this log preserves the alternatives considered.

**Date:** 2026-04-16
**Phase:** 06-progression-polish
**Areas discussed:** XP/leveling + level-up feedback, Gold drops, Death penalty, HUD polish + save scope

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| XP, leveling & level-up feedback | XP per enemy, cap, curve, stats, how level-up feels | ✓ |
| Gold drops from enemies | LootTable has no gold today; UX of drops; amounts | ✓ |
| Death penalty details | Farm+dungeon scope, item loss pool, notification UX | ✓ |
| HUD polish + save scope | Which HUD elements get art pass + save triggers + migration | ✓ |

**User's choice:** All four areas selected.

---

## XP, leveling & level-up feedback

### XP amounts per enemy tier

| Option | Description | Selected |
|--------|-------------|----------|
| Tier-flat: 10/15/25/150 (Recommended) | Clean numbers, boss is a milestone | ✓ |
| Proportional to HP (~HP/4) | Rewards grinding tanks | |
| High-variance 5/10/30/200 | Pushes hunting harder enemies, slow early | |

**User's choice:** Tier-flat 10/15/25/150.
**Notes:** Captured as D-01.

### Level cap

| Option | Description | Selected |
|--------|-------------|----------|
| Cap 10, gentle curve (Recommended) | v1-tight, dungeon-clear = level 8–10 | |
| Cap 15, medium curve | Endgame headroom | |
| Cap 10, hard curve | Grindy but prestigious | |

**User's choice:** "Other" — "acho que 100 é o lv cap.. mas isso ocorreria depois de jogar muito e fazer todas as coisas do jogo.."
**Notes:** Cap 100 as theoretical ceiling. Reconciled in D-02/D-03: curve must make level 8–15 natural for dungeon-clear, level 100 unreachable in v1 content.

### Per-level stats

| Option | Description | Selected |
|--------|-------------|----------|
| +10 HP, +1 dmg, +5 stamina (Recommended) | Clean small increments | ✓ |
| +15 HP, +2 dmg, +10 stamina | Bigger jumps | |
| +HP only (+20) | Simpler | |

**User's choice:** +10 HP, +1 dmg, +5 stamina.
**Notes:** Captured as D-04. User reaffirmed the recommended values explicitly.

### Level-up FX

| Option | Description | Selected |
|--------|-------------|----------|
| Banner + particle burst (Recommended) | Brief banner + gold particles, no pause | ✓ |
| Silent HP refill + bar flash | Stardew-quiet | |
| Banner + brief pause | More impactful, interrupts combat | |

**User's choice:** Banner + particle burst.
**Notes:** Captured as D-06..D-08.

---

## Gold drops from enemies

### Drop mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Coin ItemDrop w/ magnetism (Recommended) | Reuses Phase 2 pipeline | ✓ |
| Direct AddGold on kill | Instant, no visual | |
| Floating '+N gold' text only | Middle ground | |

**User's choice:** Coin ItemDrop w/ magnetism.
**Notes:** Captured as D-09, D-12.

### Gold amounts per tier

| Option | Description | Selected |
|--------|-------------|----------|
| 5/8/15 + boss 100 (Recommended) | ~100–200g per dungeon | ✓ |
| 3/5/10 + boss 50 | Leaner | |
| 10/15/25 + boss 200 | Generous | |

**User's choice:** 5/8/15 + boss 100.
**Notes:** Captured as D-10. ±30% variance added.

### Drop rate

| Option | Description | Selected |
|--------|-------------|----------|
| Every kill guaranteed (Recommended) | Predictable economy | ✓ |
| 70% chance per kill | Loot variance | |
| Only bosses + chests | Economy leans on farming | |

**User's choice:** Every kill guaranteed.
**Notes:** Captured as D-11.

---

## Death penalty

### Death scope

| Option | Description | Selected |
|--------|-------------|----------|
| Dungeon deaths only (Recommended) | Farm stays cozy | |
| All deaths (farm + dungeon) | Consistent, harsher | ✓ |
| Dungeon + boss only | Only deep deaths | |

**User's choice:** All deaths.
**Notes:** Captured as D-13.

### Item loss rules

| Option | Description | Selected |
|--------|-------------|----------|
| 25% chance, random inventory slot (Recommended) | Equipment safe | |
| Always lose 1 inventory item | Harsher | |
| 25% chance, include hotbar/equipment | Brutal | |

**User's choice:** "Other" — "25% de chance de dropar 1 item random, 15% de chance de dropar 2 itens randoms qualquer coisa.. e perde GOld"
**Notes:** Captured as D-14/D-15. Bucket: 60% no item loss, 25% lose 1, 15% lose 2. Pool = any slot (inventory + hotbar + equipment). Gold -10% always.

### Loss UX

| Option | Description | Selected |
|--------|-------------|----------|
| Death banner + item list toast (Recommended) | Red banner + Toasts | ✓ |
| Silent | Check inventory yourself | |
| Full modal w/ continue | Solemn, interrupts | |

**User's choice:** Death banner + item list toast.
**Notes:** Captured as D-17.

---

## HUD polish + save scope

### Which HUD elements get the upgrade

| Option | Description | Selected |
|--------|-------------|----------|
| XP bar + level number (new) | Progress bar + Lv X label | ✓ |
| Clock/day panel | Replace text with styled panel | ✓ |
| Gold label polish | Coin icon + number panel | ✓ |
| Quest tracker (HUD-04) | NineSlice + quest icon | ✓ |

**User's choice:** All four + user-added annotation: "hp, mp, stamina text are ugly and not beautifull on the bar"
**Notes:** Captured as D-18 — additional in-bar text polish folded in (Claude's Discretion on final treatment: remove, shrink, tooltip, or mini-panel).

### XP bar placement

| Option | Description | Selected |
|--------|-------------|----------|
| Thin bar above hotbar (Recommended) | Full-width above hotbar panel | ✓ |
| Stacked under HP/Mana/Stamina (top-left) | Fourth bar in stack | |
| Only in pause menu | Minimal HUD | |

**User's choice:** Thin bar above hotbar.
**Notes:** Captured as D-18 (XP bar bullet).

### Save triggers

| Option | Description | Selected |
|--------|-------------|----------|
| Add on level-up + on death (Recommended) | Targeted additional triggers | |
| Add on scene transitions only | Heavier I/O | |
| Keep current set only | No new triggers | |

**User's choice:** "Other" — "de tempos em tempos (verificar se nao vai ficar lento o jogo) pra funcionar como um jogo normal q tudo q vc faz ja fica salvo mesmo q fechje o jogo"
**Notes:** Captured as D-20/D-21 — periodic auto-save (~30s) with perf guard. Level-up and post-death triggers also added (D-22) so the critical moments save immediately, not just on the next tick.

---

## Claude's Discretion

- Exact XP curve formula (respecting D-03 constraints).
- Particle burst implementation details.
- Color of XP bar (from kit variants).
- Treatment of "ugly" HP/MP/STA in-bar text (remove / shrink / tooltip / mini-panel).
- Periodic save exact interval (target 30s, tunable 20–60).
- Coin sprite choice from kit.
- Gold variance exact range (target ±30%).
- Item-loss dice model (single roll w/ buckets vs. two independent rolls, as long as probabilities match D-15).
- Exact Toast strings.

## Deferred Ideas

- XP from crops/quests → v2
- Skill trees / classes → v2
- Real mana system → v2
- Level-up SFX / audio → no audio system yet
- Save-anywhere-and-quit arbitrary position → v2
- Quest-item protection from penalty → when multiple quests exist
- Rest/sleep-to-save NPC → v2
- Leveling visual on player sprite → polish v2
- Achievements → out of scope
