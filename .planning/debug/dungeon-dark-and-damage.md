---
status: diagnosed
trigger: "UAT Test 2 — entering dungeon r1 = all dark + continuous damage"
created: 2026-04-14T00:00:00Z
updated: 2026-04-14T00:00:00Z
---

## Current Focus

hypothesis: CONFIRMED — dungeon Ground layer uses GID=1 which maps to the transparent/empty top-left 16x16 corner of `3_Props_and_Buildings_16x16.png`. Nothing visible is drawn for the floor, so `GraphicsDevice.Clear(Color.Black)` shows through → "tudo escuro". With the screen black, the player cannot see two Skeleton enemies spawned ~109px away which aggro (DetectionRange=120) and melee-attack on contact → continuous damage.
test: static analysis of TMX data + tileset image inspection + TileMap rendering path
expecting: floor tiles (GID=1) resolve to src rect (0,0,16,16) of the dungeon tileset, which is empty/black
next_action: return ROOT CAUSE FOUND for plan-phase --gaps

## Symptoms

expected: dungeon room visible (lit), no continuous damage on entry
actual: all-dark rendering + continuous damage per tick on spawn
errors: none beyond symptoms
reproduction: Phase 5 UAT Test 2, enter village cave → dungeon r1
started: 2026-04-14 (post commit 6b69ebd which fixed the prior crash)

## Eliminated

- hypothesis: day/night overlay causing the darkness
  evidence: overlay max darkness ≈ 0.54 * Color.Black (~54% alpha) — dim, not pitch. Village/Farm scenes also use this overlay and render fine. Not sufficient to produce "tudo escuro".
  timestamp: 2026-04-14

- hypothesis: camera not positioned / bounds wrong
  evidence: GameplayScene.LoadContent sets `Camera.Bounds = Map.GetWorldBounds()` (480x272) and `Camera.SnapTo(Player.Position)`. Spawn resolves to (240,208) which is the center-ish of the room. Camera is fine.
  timestamp: 2026-04-14

- hypothesis: trigger overlap at spawn causes damage
  evidence: spawn `from_village` center = (240,208). Player CollisionBox ≈ (235,215,10,6). Nearest trigger `exit_r1_to_village` is (208,224,64,32) — y=224..256. No overlap.
  timestamp: 2026-04-14

- hypothesis: spawn overlaps an enemy/hazard in TMX
  evidence: dungeon_r1.tmx EnemySpawns are at (152,144) and (312,144). No hazard/spike object group exists. No overlap with spawn (240,208).
  timestamp: 2026-04-14

- hypothesis: scene-transition invulnerability missing → compound with something else
  evidence: no i-frames on scene entry in PlayerEntity or CombatManager, but damage comes from `IsMeleeAttackReady` which requires an enemy to be adjacent. Not "continuous" per se — it's "as soon as a skeleton reaches the invisible player, it melees every 1s".
  timestamp: 2026-04-14

## Evidence

- timestamp: 2026-04-14
  checked: src/Scenes/DungeonScene.cs (HEAD version — worktree has "D" status due to worktree checkout anomaly; read via `git show HEAD:...`)
  found: DungeonScene extends GameplayScene, loads map via TileMap, calls CombatLoop.Update with enemy list. No custom lighting/ambient manager. No invulnerability frames on entry.
  implication: rendering pipeline and damage pipeline are the standard GameplayScene/CombatLoop — any bug is in data (TMX, tileset) or in the base pipeline.

- timestamp: 2026-04-14
  checked: src/Core/GameplayScene.cs lines 192-232 (Draw method)
  found: pipeline draws `Map.Draw` → `OnDrawWorld` → `Player.Draw` → `OnDrawWorldAfterPlayer` → trigger markers, then screen-space darkness overlay `Color.Black * (darkness * 0.6f)` where darkness = 1 - LightIntensity (capped at 1). Max 60% alpha even at worst night.
  implication: day/night overlay alone is insufficient to blacken the scene; it only dims it.

- timestamp: 2026-04-14
  checked: Game1.cs Draw method
  found: `GraphicsDevice.Clear(Color.Black)` sets the backdrop. Any region where tiles fail to draw shows pure black.
  implication: if the tile layer draws transparent pixels for every cell, the entire world appears pitch black.

- timestamp: 2026-04-14
  checked: assets/Maps/dungeon_r1.tmx (plus r2, r3, r3a, r4, r4a, boss)
  found: every room's "Ground" tile layer is filled entirely with GID=1. No other tile layers present.
  implication: only one pixel source is ever used for the floor.

- timestamp: 2026-04-14
  checked: assets/Maps/dungeon_tileset.tsx → image ../Sprites/Buildings/3_Props_and_Buildings_16x16.png (512x2240, 32 cols, 4480 tiles)
  found: visual inspection of the tileset PNG shows the top-left 16x16 region (src rect for localId 0 / GID 1) is a blank/empty dark corner of a roof tile — NOT a floor tile. By contrast, `0_Complete_Tileset_16x16.png` (farm tileset used by village/farm) has a visible green grass tile at (0,0).
  implication: `DrawTileByGid(gid=1)` on dungeon tileset draws 16x16 pixels of near-empty texture at every tile position → floor is effectively invisible → `Color.Black` backdrop shows through → "tudo escuro".

- timestamp: 2026-04-14
  checked: src/World/TileMap.cs DrawTileByGid (lines 408-445) and Draw/DrawLayer (after commit 5d16831)
  found: gid→localId math is correct; tileset resolution is correct; draw call uses `Color.White` tint (no accidental darken). Nothing in the render pipeline tints or skips the draw for dungeon-specific cases.
  implication: the rendering code is not at fault — the bug is purely TMX data (wrong GID) pointing to a visually empty region of a tileset that was chosen for "props and buildings" rather than dungeon floors.

- timestamp: 2026-04-14
  checked: src/Combat/EnemyData.cs (Skeleton definition) and src/Combat/EnemyEntity.cs attack logic
  found: Skeleton has DetectionRange=120, AttackRange=24, AttackDamage=10, AttackCooldown=1.0s, MoveSpeed=60. Two skeletons spawn at (152,144) and (312,144); player spawns at ~(240,208). Distance ≈ 109 px → within detection. Both aggro on entry, chase, and reach player in ≈1.5s. Once adjacent, melee hits player for 10 damage every 1.0s.
  implication: the "continuous damage" is ordinary aggro → chase → melee against an invisible (to the player) target. Not a separate hazard/spawn bug. The player is blind because of bug #1 and gets beaten up in the dark.

## Resolution

root_cause: Dungeon room TMXs (dungeon_r1 through dungeon_r4a + dungeon_boss) fill their "Ground" tile layer entirely with GID=1. In the configured dungeon tileset (`3_Props_and_Buildings_16x16.png`), GID=1 resolves to the empty top-left 16x16 pixels of the sheet — effectively transparent. The floor never draws anything, so `GraphicsDevice.Clear(Color.Black)` shows through and the whole scene looks pitch black. With the player unable to see, the two Skeleton enemies correctly spawned at (152,144) and (312,144) aggro via their 120-px detection range, chase to melee range, and continuously strike for 10 damage per 1-second cooldown — producing the "fico tomando dano" symptom.

Bug #1 and Bug #2 share a root cause: the player cannot see. Fix bug #1 (the dungeon floor renders) and bug #2 reverts to normal combat ("I can see a skeleton running at me and can fight back"). No separate spawn-overlap or hazard bug exists.

fix: (not applied — goal is find_root_cause_only)

Suggested fix directions for plan-phase --gaps:
  1. Primary — TMX data fix: re-author the dungeon room Ground layers in Tiled to use a real floor GID from `3_Props_and_Buildings_16x16.png`. The sheet contains visible floor/stone tiles further down; pick an appropriate dungeon-floor tile and rebuild the CSV for each room. This is the targeted data fix.
  2. Alternative — tileset swap: point `dungeon_tileset.tsx` at a tileset whose tile 1 IS a floor (e.g., reuse the farm tileset or a dungeon-specific floor sheet), then keep the existing CSVs. Cheaper than re-authoring but semantically wrong ("props and buildings" is the wrong source for floor tiles).
  3. Defensive — render-pipeline safeguard (nice-to-have, not required for the bug): have GameplayScene clear to a non-black default (e.g., dark grey) or paint a solid fallback floor color under the tile layer so a missing/empty tileset never results in a pitch-black scene. This would make the class of bug easier to notice next time.

No code changes are required to fix the reported symptoms; the bug is entirely in `assets/Maps/dungeon_r*.tmx` authoring.

verification: (not yet — will be performed by the implementation phase)
files_changed: []
