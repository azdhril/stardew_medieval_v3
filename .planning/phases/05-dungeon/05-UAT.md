---
status: diagnosed
phase: 05-dungeon
source: [05-01-SUMMARY.md, 05-02-SUMMARY.md, 05-03-SUMMARY.md, 05-VALIDATION.md]
started: 2026-04-14T20:16:53-03:00
updated: 2026-04-14T20:40:00-03:00
---

## Current Test

[paused — blocked on Test 2 bugs; routing to diagnosis + fix planning]

## Tests

### 1. Village → Dungeon Entry
expected: |
  From the village, walk to the cave entrance (castle door area). Stepping onto the
  trigger fades to black and loads dungeon room r1 — the player spawns inside r1
  facing the room's interior. No errors, no fall-through, no double-load.
result: pass
notes: |
  Previously crashed; fixed in 6b69ebd (resolve DungeonRoomData in DungeonScene
  ctor). Retested and confirmed passing.

### 2. Room-Clear Door Opening
expected: |
  Enter r1, defeat all enemies in the room. The exit door visibly changes state
  (sprite swap or color change from closed → open) and becomes walkable. Walk
  through r1 → r2 → r3 → r4 in sequence; each clears its own enemies and opens
  its exit independently.
result: issue
reported: "eu nao consigo tesatar entro na dungeon está tudo escuro e fico tomando dano"
severity: blocker
notes: |
  Two bugs blocking all downstream tests (2-6): (1) dungeon room renders all
  dark — lighting/visibility issue, nothing visible; (2) player takes continuous
  damage on entry — likely hazard damage tick, enemy overlap at spawn, or
  missing safe spawn clearance.

### 3. Optional Room Chest UX
expected: |
  From a main room with an optional exit (r3 or r4), enter the optional room
  (r3a or r4a). A chest is present. Open it: drag-and-drop loot to inventory
  feels smooth and matches the village chest UX. Leave and re-enter the optional
  room — the chest stays empty (state persisted across room re-entries within
  the run).
result: blocked
blocked_by: prior-phase
reason: "gated by Test 2 blockers (dark rendering + damage-on-spawn in dungeon)"

### 4. Boss Gate, Fight, Loot, Return
expected: |
  Reach r4 cleared, then enter the boss room. Boss spawns. Defeat boss: guaranteed
  loot drops as ItemDropEntity (pickupable on the floor, not a chest). MainQuest
  flips to Complete. Exit trigger returns the player to the village, spawning at
  the castle door at (208, 128).
result: blocked
blocked_by: prior-phase
reason: "gated by Test 2 blockers (dark rendering + damage-on-spawn in dungeon)"

### 5. King Quest-Complete Dialogue
expected: |
  After returning from the boss victory, walk to the King NPC in the village
  and talk to him. The NPC-04 quest-complete dialogue branch fires (different
  from the pre-quest dialogue) and acknowledges the dungeon being cleared.
result: blocked
blocked_by: prior-phase
reason: "gated by Test 2 blockers (dark rendering + damage-on-spawn in dungeon)"

### 6. Death Reset Semantics
expected: |
  Start a fresh run, partially clear rooms (open a door or two, open a chest),
  then die in the dungeon. Player respawns at the farm. Re-enter the dungeon:
  doors are closed again, chests are reclosed and re-seeded with fresh loot
  (RunSeed reroll), but if you had already defeated the boss in a prior run,
  BossDefeated remains true (the boss-cleared milestone persists across deaths).
result: blocked
blocked_by: prior-phase
reason: "gated by Test 2 blockers (dark rendering + damage-on-spawn in dungeon)"

## Summary

total: 6
passed: 1
issues: 1
pending: 0
blocked: 4
skipped: 0

## Gaps

[resolved: Test 1 previously reported crash is fixed (commit 6b69ebd) and now passes]

- truth: "Dungeon room is visible (lit) and does not continuously damage the player on entry"
  status: diagnosed
  reason: "User reported: 'entro na dungeon está tudo escuro e fico tomando dano'."
  severity: blocker
  test: 2
  root_cause: "Every dungeon room TMX Ground layer is filled with GID=1, which in the configured tileset (dungeon_tileset.tsx → 3_Props_and_Buildings_16x16.png) resolves to the blank top-left 16x16 edge of a props sheet. Result: tiles draw near-transparent over Game1's Color.Black clear → 'tudo escuro'. The continuous damage is a cascade: two Skeletons legitimately spawn at (152,144) and (312,144); player spawns at ~(240,208), ~109px away, inside DetectionRange=120, so they aggro and melee an invisible player. No separate hazard/invuln bug."
  artifacts:
    - path: "assets/Maps/dungeon_r1.tmx"
      issue: "Ground layer CSV entirely GID=1 (blank tile); same for r2/r3/r3a/r4/r4a/boss"
    - path: "assets/Maps/dungeon_tileset.tsx"
      issue: "Points at 3_Props_and_Buildings_16x16.png whose GID=1 is not a floor tile"
    - path: "assets/Sprites/Buildings/3_Props_and_Buildings_16x16.png"
      issue: "(0,0,16,16) region is blank/edge, not a floor"
  missing:
    - "Re-author every dungeon room Ground CSV to reference a real floor GID from the props sheet (primary fix), OR swap dungeon_tileset.tsx to a tileset whose GID 1 is a floor"
    - "Optional hardening: fallback floor color or non-black Clear so future 'all transparent tiles' regressions are visible rather than pitch-black"
  debug_session: .planning/debug/dungeon-dark-and-damage.md
