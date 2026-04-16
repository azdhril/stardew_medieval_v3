---
status: complete
phase: 05-dungeon
source: [05-01-SUMMARY.md, 05-02-SUMMARY.md, 05-03-SUMMARY.md, 05-04-SUMMARY.md, 05-VALIDATION.md]
started: 2026-04-14T20:16:53-03:00
updated: 2026-04-16T00:00:00-03:00
---

## Current Test

[testing complete]

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
result: pass
notes: |
  Re-run after plan 05-04 (commits 1d2e198, 506f988, 852077c). User confirmed
  full r1→r2→r3→r4 clear flow with doors opening on room clear.

### 3. Optional Room Chest UX
expected: |
  From a main room with an optional exit (r3 or r4), enter the optional room
  (r3a or r4a). A chest is present. Open it: drag-and-drop loot to inventory
  feels smooth and matches the village chest UX. Leave and re-enter the optional
  room — the chest stays empty (state persisted across room re-entries within
  the run).
result: pass
notes: |
  First re-run FAILED: chest respawned with fresh loot on re-entry. Root
  cause — DungeonScene hydration seeded from ChestContents on every room
  load without consulting DungeonState.IsChestOpened. Fixed in commit
  aa17ed5: guard seed branch with IsChestOpened + render chest in
  FrameOpen state on re-entry (new ChestInstance.SetOpenedInstant).
  Re-verified by user: chest stays open + empty across re-entries.

### 4. Boss Gate, Fight, Loot, Return
expected: |
  Reach r4 cleared, then enter the boss room. Boss spawns. Defeat boss: guaranteed
  loot drops as ItemDropEntity (pickupable on the floor, not a chest). MainQuest
  flips to Complete. Exit trigger returns the player to the village, spawning at
  the castle door at (208, 128).
result: pass

### 5. King Quest-Complete Dialogue
expected: |
  After returning from the boss victory, walk to the King NPC in the village
  and talk to him. The NPC-04 quest-complete dialogue branch fires (different
  from the pre-quest dialogue) and acknowledges the dungeon being cleared.
result: issue
reported: "nao sei direito como saber se funcionou ou não... pontos pra ajeitar.. a conversa com o npc agora que a tela está maior e tem full screen faz um overlay preto transparente mas nao está cobrindo a tela toda e o retangulo da conversa está no lugar errado da tela por conta disso. não tem na hud um lugar onde mostra quanto dinheiro eu tenho"
severity: major

### 6. Death Reset Semantics
expected: |
  Start a fresh run, partially clear rooms (open a door or two, open a chest),
  then die in the dungeon. Player respawns at the farm. Re-enter the dungeon:
  doors are closed again, chests are reclosed and re-seeded with fresh loot
  (RunSeed reroll), but if you had already defeated the boss in a prior run,
  BossDefeated remains true (the boss-cleared milestone persists across deaths).
result: issue
reported: "os baus não deveriam estar re-seeded with fresh loot e reclosed... pois o player vai pegar tudo se matar volta lá e pega tudo dnv infinitamente? n faz sentido.. a coleta do bau tem que ser uma unica vez"
severity: major

## Summary

total: 6
passed: 4
issues: 2
pending: 0
blocked: 0
skipped: 0

## Gaps

[resolved: Test 1 previously reported crash is fixed (commit 6b69ebd) and now passes]
[resolved: Test 2 dark-rendering + invisible-enemies gap closed by plan 05-04 (commits 1d2e198, 506f988, 852077c); human-verify approved]
[resolved: Test 3 chest respawn-on-re-entry fixed in commit aa17ed5 (DungeonScene now checks IsChestOpened before seeding contents; chest renders in opened frame on re-entry). Awaiting user re-verification.]

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

- truth: "NPC dialogue overlay covers full screen and dialogue box is positioned correctly in fullscreen/larger window"
  status: failed
  reason: "User reported: a conversa com o npc agora que a tela está maior e tem full screen faz um overlay preto transparente mas nao está cobrindo a tela toda e o retangulo da conversa está no lugar errado da tela por conta disso. não tem na hud um lugar onde mostra quanto dinheiro eu tenho"
  severity: major
  test: 5
  root_cause: "DialogueBox.cs uses hardcoded ScreenWidth=960, ScreenHeight=540 constants. Overlay draws a fixed 960x540 rectangle regardless of actual viewport. Other UI scenes (PauseScene, InventoryScene, ChestScene) correctly query Services.GraphicsDevice.Viewport but DialogueBox does not have access to GraphicsDevice."
  artifacts:
    - path: "src/UI/DialogueBox.cs"
      issue: "Lines 14-15: hardcoded ScreenWidth=960, ScreenHeight=540; Line 43: overlay draws fixed 960x540 rect"
  missing:
    - "Pass viewport dimensions to DialogueBox.Draw() or give it GraphicsDevice access"
    - "Add gold/money display to HUD"
  debug_session: ""

- truth: "Dungeon chest loot is collected once per save — chests stay empty permanently after being opened"
  status: failed
  reason: "User reported: os baus não deveriam estar re-seeded with fresh loot e reclosed... pois o player vai pegar tudo se matar volta lá e pega tudo dnv infinitamente? n faz sentido.. a coleta do bau tem que ser uma unica vez"
  severity: major
  test: 6
  root_cause: "On death, DungeonScene.OnPreUpdate() calls BeginRun() which clears OpenedChestIds and ChestContents, then DungeonChestSeeder.Seed() re-rolls all chests. SaveNow() is never called between death and the wipe, so opened-chest state is lost. Chest opened state is per-run, not per-save — needs to be permanent."
  artifacts:
    - path: "src/Scenes/DungeonScene.cs"
      issue: "Lines 298-302: death handler calls BeginRun() which wipes chest state before any save"
    - path: "src/Combat/DungeonState.cs"
      issue: "Lines 43-52: BeginRun() clears OpenedChestIds and ChestContents unconditionally"
  missing:
    - "Move opened chests to permanent GameState persistence (not per-run)"
    - "Either save before BeginRun() on death, or preserve opened chest IDs across runs"
  debug_session: ""
