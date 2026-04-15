---
status: pending
phase: 05-dungeon
source: [05-01-SUMMARY.md, 05-02-SUMMARY.md, 05-03-SUMMARY.md, 05-04-SUMMARY.md, 05-VALIDATION.md]
started: 2026-04-14T20:16:53-03:00
updated: 2026-04-15T00:00:00-03:00
---

## Current Test

[ready for re-run — Test 2 gap closed by plan 05-04; Tests 2-6 pending user re-attempt]

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
result: pending
resolved_by: 05-04-SUMMARY.md
notes: |
  Gap resolved. Plan 05-04 re-authored the Ground layer of all 6 remaining
  dungeon rooms (r2, r3, r3a, r4, r4a, boss) to use a visible floor GID=4554
  mirroring r1 (commit 1d2e198), added a defensive dark-grey Clear fallback
  in Game1 (506f988), and — discovered during human-verify — restored the
  missing enemy/boss draw block in DungeonScene so skeletons are visible
  while they aggro (852077c). Human-verify returned "approved". Test 2
  full end-to-end (clear all 4 rooms + door opens) now awaits user re-run.

### 3. Optional Room Chest UX
expected: |
  From a main room with an optional exit (r3 or r4), enter the optional room
  (r3a or r4a). A chest is present. Open it: drag-and-drop loot to inventory
  feels smooth and matches the village chest UX. Leave and re-enter the optional
  room — the chest stays empty (state persisted across room re-entries within
  the run).
result: pending
was_blocked_by: "Test 2 rendering gap (resolved by 05-04)"
notes: "Unblocked. Awaiting user re-run."

### 4. Boss Gate, Fight, Loot, Return
expected: |
  Reach r4 cleared, then enter the boss room. Boss spawns. Defeat boss: guaranteed
  loot drops as ItemDropEntity (pickupable on the floor, not a chest). MainQuest
  flips to Complete. Exit trigger returns the player to the village, spawning at
  the castle door at (208, 128).
result: pending
was_blocked_by: "Test 2 rendering gap (resolved by 05-04)"
notes: "Unblocked. Awaiting user re-run."

### 5. King Quest-Complete Dialogue
expected: |
  After returning from the boss victory, walk to the King NPC in the village
  and talk to him. The NPC-04 quest-complete dialogue branch fires (different
  from the pre-quest dialogue) and acknowledges the dungeon being cleared.
result: pending
was_blocked_by: "Test 2 rendering gap (resolved by 05-04)"
notes: "Unblocked. Awaiting user re-run."

### 6. Death Reset Semantics
expected: |
  Start a fresh run, partially clear rooms (open a door or two, open a chest),
  then die in the dungeon. Player respawns at the farm. Re-enter the dungeon:
  doors are closed again, chests are reclosed and re-seeded with fresh loot
  (RunSeed reroll), but if you had already defeated the boss in a prior run,
  BossDefeated remains true (the boss-cleared milestone persists across deaths).
result: pending
was_blocked_by: "Test 2 rendering gap (resolved by 05-04)"
notes: "Unblocked. Awaiting user re-run."

## Summary

total: 6
passed: 1
issues: 0
pending: 5
blocked: 0
skipped: 0

## Gaps

[resolved: Test 1 previously reported crash is fixed (commit 6b69ebd) and now passes]
[resolved: Test 2 dark-rendering + invisible-enemies gap closed by plan 05-04 (commits 1d2e198, 506f988, 852077c); human-verify approved]

- truth: "Dungeon room is visible (lit) and does not continuously damage the player on entry"
  status: resolved
  resolved_by: ".planning/phases/05-dungeon/05-04-SUMMARY.md"
  resolution: "6 dungeon room TMXs re-authored with visible floor GID=4554 (farm_tileset localId 73); defensive dark-grey Clear color in Game1; enemy/boss draw block restored in DungeonScene (out-of-band during human-verify). Human-verify returned approved."
  prior_status: diagnosed
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
