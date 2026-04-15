---
phase: 05-dungeon
plan: 04
subsystem: dungeon-rendering-gap-closure
tags: [dungeon, tmx, rendering, gap-closure, uat]
requires:
  - "Plan 05-01 DungeonScene + DungeonRoomData (needed a scene to render into)"
  - "Plan 05-02 TMX layer loader + object-group parsing (renders Ground layer GIDs)"
  - "assets/Maps/dungeon_r1.tmx manually patched reference pattern (farm_tileset @ firstgid=4481, Ground=4554)"
provides:
  - "6 dungeon room TMXs (r2, r3, r3a, r4, r4a, boss) with visible Ground floors"
  - "Defensive dark-grey Clear color fallback in Game1.Draw"
  - "Unblocked Phase 05 UAT Tests 2-6 (rendering + enemy visibility fixed)"
affects:
  - "assets/Maps/dungeon_r2.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "assets/Maps/dungeon_r3.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "assets/Maps/dungeon_r3a.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "assets/Maps/dungeon_r4.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "assets/Maps/dungeon_r4a.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "assets/Maps/dungeon_boss.tmx (Ground layer re-authored, farm_tileset ref added)"
  - "Game1.cs (Clear color changed from Black to Color(24,24,28))"
  - "src/Scenes/DungeonScene.cs (OUT-OF-BAND: enemy/boss draw block added)"
tech-stack:
  added: []
  patterns:
    - "Mirror-the-working-reference: copy dungeon_r1.tmx tileset/GID pattern across all sibling rooms"
    - "Defensive Clear color: a visible-but-unobtrusive fallback beats pitch-black when tile authoring regresses"
key-files:
  created:
    - ".planning/phases/05-dungeon/05-04-SUMMARY.md"
  modified:
    - "assets/Maps/dungeon_r2.tmx"
    - "assets/Maps/dungeon_r3.tmx"
    - "assets/Maps/dungeon_r3a.tmx"
    - "assets/Maps/dungeon_r4.tmx"
    - "assets/Maps/dungeon_r4a.tmx"
    - "assets/Maps/dungeon_boss.tmx"
    - "Game1.cs"
    - "src/Scenes/DungeonScene.cs"
decisions:
  - "Chose GID=4554 (farm_tileset localId 73) as floor across all 7 dungeon rooms; matches pre-existing r1 patch"
  - "Kept dungeon_tileset.tsx reference intact (firstgid=1) so future wall/prop tiles still resolve"
  - "Defensive Clear = Color(24,24,28) dark grey; visible enough to diagnose future all-transparent-tile regressions, invisible under fully-authored maps"
  - "Out-of-band enemy render fix in DungeonScene mirrors FarmScene verbatim (no new pattern introduced)"
metrics:
  duration: 25min
  completed: 2026-04-15
  tasks: 3
  commits: 4
requirements: [DNG-01, DNG-02, DNG-03, DNG-04]
---

# Phase 05 Plan 04: Dungeon Rendering Gap Closure Summary

Re-authored 6 dungeon room TMX Ground layers (r2, r3, r3a, r4, r4a, boss) to reference a visible floor tile (GID=4554 via farm_tileset @ firstgid=4481), mirroring the already-patched dungeon_r1.tmx pattern; added a defensive dark-grey Clear color in Game1 and (out-of-band, during human-verify) restored the missing enemy/boss draw block in DungeonScene so Phase 05 UAT Tests 2-6 can resume.

## What Changed

### Floor GID Choice

**GID chosen: 4554** (= farm_tileset.tsx firstgid 4481 + localId 73).

Rationale:
- dungeon_r1.tmx was already manually patched to use this GID; adopting it uniformly means all 7 dungeon rooms share one aesthetic without needing to re-author r1.
- The tile reads as a neutral stone/dirt floor that is legible under the scene's day/night overlay.
- No new tileset imported, no asset added — pure data realignment.

Alternative considered (and rejected): swap `dungeon_tileset.tsx` to point at a different image whose GID=1 is a real floor. Rejected because future plans may still host wall/prop tiles in that tileset; changing its image would cascade.

### Tasks Executed

**Task 1 — Re-author 6 Ground layers** (commit `1d2e198`)
- Added `<tileset firstgid="4481" source="farm_tileset.tsx"/>` declaration alongside the existing `dungeon_tileset.tsx` reference in each of r2/r3/r3a/r4/r4a/boss.
- Rewrote Ground layer CSV from all-`1` to all-`4554`, preserving row/col dimensions verbatim (30x17 for r2/r3/r4; 16x12 for r3a/r4a; boss kept its authored dims).
- Left every other layer, objectgroup, collision rect, trigger, door, spawn marker untouched.
- All 22 dungeon tests pass; grep confirms no Ground cell still uses raw GID=1.

**Task 2 — Defensive Clear color** (commit `506f988`)
- `Game1.cs:90` changed from `GraphicsDevice.Clear(Color.Black)` to `GraphicsDevice.Clear(new Color(24, 24, 28))`.
- Comment added explaining the regression-visibility rationale.
- `dotnet build` clean, no new warnings.

**Task 3 — Human-verify checkpoint**
- Resume signal: **"approved"**.
- All 7 dungeon rooms render a visible floor.
- Player no longer takes damage-before-seeing-attacker (see out-of-band fix below).
- UAT Tests 2-6 unblocked.

## Deviations from Plan

### Out-of-band Fix: DungeonScene enemy render block (commit `852077c`)

**Found during:** Task 3 human-verify.

**Issue:** After Task 1 landed and the floor became visible, the user reported skeletons were still invisible while dealing damage. Root cause: `DungeonScene.OnDrawWorld` was missing the enemy/boss draw block that `FarmScene.OnDrawWorld` already had. The skeletons were spawning, aggroing via `DetectionRange=120`, and hitting the player — but never rendered.

**Fix:** Added the enemy draw block (enemy.Draw + EnemyHealthBar) plus the boss draw block to `DungeonScene.OnDrawWorld`, mirroring FarmScene verbatim. No new rendering pattern introduced.

**Files modified:** `src/Scenes/DungeonScene.cs` (+11 lines).

**Commit:** `852077c`.

**Classification:** Rule 2 (auto-add missing critical functionality) — enemies must be visible for combat to be playable; this is a correctness requirement, not a feature.

**Scope note:** This fix is adjacent to, but not within, the original plan's stated scope (Ground-layer TMX + Clear color). It was discovered through the plan's human-verify checkpoint, which is exactly the point of that gate. Documenting here rather than spawning a follow-up plan because it is a one-line mechanical mirror of existing code with no design decisions.

## Confirmation: No Gameplay Logic Touched (Intentional Scope)

The plan's stated scope was data + one defensive line. Inside that scope:
- Spawns, triggers, doors, collision rects, enemy AI, combat math, quest logic: untouched.
- Only the Ground layer CSV and tileset declaration line changed in the 6 TMXs.
- Only `GraphicsDevice.Clear` changed in `Game1.cs`.

The out-of-band `DungeonScene.cs` change IS in the render pipeline, but it adds no new logic — it restores a block that `FarmScene` already uses. No new enemy behavior, no new combat rules.

## Verification Results

- [x] All 6 TMXs declare farm_tileset.tsx @ firstgid=4481.
- [x] Ground layer CSV uniformly uses GID=4554.
- [x] `grep` confirms no remaining raw GID=1 Ground cell.
- [x] `dotnet build` clean.
- [x] `dotnet test --filter "FullyQualifiedName~Dungeon"` — 22/22 pass.
- [x] `Game1.cs` Clear uses `new Color(24, 24, 28)` with explanatory comment.
- [x] Human verify "approved".
- [x] UAT Test 2 gap → resolved.
- [x] UAT Tests 3-6 → unblocked (pending).

## Handoff

**User action:** Re-run Phase 5 UAT Tests 2-6 end-to-end. With rendering and enemy visibility fixed, the full dungeon loop (clear r1 → r2 → r3 → r4 → optional r3a/r4a → boss → return to village → king dialogue → death reset) should now be observable and testable.

If any further Test-2 regression surfaces (e.g. door fails to open after clear), that belongs in a new debug session — the gap closed by this plan is rendering and enemy visibility only.

## Self-Check: PASSED

- Commit `1d2e198` — FOUND (Task 1, 6 TMX files).
- Commit `506f988` — FOUND (Task 2, Game1.cs).
- Commit `852077c` — FOUND (out-of-band DungeonScene enemy render).
- File `.planning/phases/05-dungeon/05-04-SUMMARY.md` — created (this file).
