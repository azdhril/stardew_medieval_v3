---
status: testing
phase: 05-dungeon
source: [05-01-SUMMARY.md, 05-02-SUMMARY.md, 05-03-SUMMARY.md, 05-VALIDATION.md]
started: 2026-04-14T20:16:53-03:00
updated: 2026-04-14T20:16:53-03:00
---

## Current Test

number: 1
name: Village → Dungeon Entry
expected: |
  From the village, walk to the cave entrance (castle door area). Stepping onto the
  trigger fades to black and loads dungeon room r1 — the player spawns inside r1
  facing the room's interior. No errors, no fall-through, no double-load.
awaiting: user response

## Tests

### 1. Village → Dungeon Entry
expected: |
  From the village, walk to the cave entrance (castle door area). Stepping onto the
  trigger fades to black and loads dungeon room r1 — the player spawns inside r1
  facing the room's interior. No errors, no fall-through, no double-load.
result: issue
reported: "achei a dungeon entrei nela e o jogo crashou"
severity: blocker
notes: |
  Entrance was findable (initial discoverability complaint superseded). Stepping
  onto the trigger and transitioning into dungeon r1 crashes the game.

### 2. Room-Clear Door Opening
expected: |
  Enter r1, defeat all enemies in the room. The exit door visibly changes state
  (sprite swap or color change from closed → open) and becomes walkable. Walk
  through r1 → r2 → r3 → r4 in sequence; each clears its own enemies and opens
  its exit independently.
result: [pending]

### 3. Optional Room Chest UX
expected: |
  From a main room with an optional exit (r3 or r4), enter the optional room
  (r3a or r4a). A chest is present. Open it: drag-and-drop loot to inventory
  feels smooth and matches the village chest UX. Leave and re-enter the optional
  room — the chest stays empty (state persisted across room re-entries within
  the run).
result: [pending]

### 4. Boss Gate, Fight, Loot, Return
expected: |
  Reach r4 cleared, then enter the boss room. Boss spawns. Defeat boss: guaranteed
  loot drops as ItemDropEntity (pickupable on the floor, not a chest). MainQuest
  flips to Complete. Exit trigger returns the player to the village, spawning at
  the castle door at (208, 128).
result: [pending]

### 5. King Quest-Complete Dialogue
expected: |
  After returning from the boss victory, walk to the King NPC in the village
  and talk to him. The NPC-04 quest-complete dialogue branch fires (different
  from the pre-quest dialogue) and acknowledges the dungeon being cleared.
result: [pending]

### 6. Death Reset Semantics
expected: |
  Start a fresh run, partially clear rooms (open a door or two, open a chest),
  then die in the dungeon. Player respawns at the farm. Re-enter the dungeon:
  doors are closed again, chests are reclosed and re-seeded with fresh loot
  (RunSeed reroll), but if you had already defeated the boss in a prior run,
  BossDefeated remains true (the boss-cleared milestone persists across deaths).
result: [pending]

## Summary

total: 6
passed: 0
issues: 1
pending: 5
skipped: 0

## Gaps

- truth: "Village has a visible, discoverable cave entrance that transitions to dungeon r1"
  status: failed
  reason: "User reported: didnt find cave entrance. Investigation: enter_dungeon trigger exists in village.tmx at world (880, 240) with 32x32 AABB and is wired in VillageScene.HandleTrigger, but no visual tile/sprite marks the location — player has no way to discover it."
  severity: blocker
  test: 1
  artifacts: ["assets/Maps/village.tmx:enter_dungeon@880,240", "src/Scenes/VillageScene.cs:53"]
  missing: ["cave entrance tile/sprite on village tileset placed at trigger location", "optional: label or minimap hint"]
