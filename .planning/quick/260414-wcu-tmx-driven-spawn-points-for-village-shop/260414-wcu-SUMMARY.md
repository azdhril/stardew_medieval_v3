---
phase: quick-260414-wcu
plan: 01
subsystem: scenes/spawn
tags: [spawn, tmx, authoring, scenes]
requires:
  - src/World/TileMap.cs (GetObjectGroup helper)
  - src/Core/GameplayScene.cs (Map field populated before GetSpawn)
provides:
  - GameplayScene.TryReadTmxSpawn protected helper
  - TMX-first spawn resolution in Village/Shop/Castle scenes
affects:
  - VillageScene, ShopScene, CastleScene spawn authoring flow
tech-stack:
  added: []
  patterns:
    - "TMX-first, dict-second, literal-third spawn resolution chain"
key-files:
  created: []
  modified:
    - src/Core/GameplayScene.cs
    - src/Scenes/VillageScene.cs
    - src/Scenes/ShopScene.cs
    - src/Scenes/CastleScene.cs
decisions:
  - Preserve hardcoded Spawns dicts as safety net (no TMX edits required by this plan)
  - Case-insensitive match on 'from_<Prev>' object names; dict lookup stays case-sensitive to match existing callers
metrics:
  duration: ~10min
  completed: 2026-04-14
---

# Quick 260414-wcu: TMX-Driven Spawn Points for Village/Shop/Castle Summary

Hub scenes (Village, Shop, Castle) now resolve the player's spawn from a TMX
`Spawn` object group first, falling back to the existing hardcoded `Spawns`
dictionary, then to the original literal default. Mirrors the DungeonScene
pattern and lets level designers reposition entry points per-previous-scene
without touching C#.

## Changes

### `src/Core/GameplayScene.cs`

Added `protected bool TryReadTmxSpawn(string fromScene, out Vector2 pos)`.
It looks in the TMX object group named `Spawn` for an object whose name
equals `from_<fromScene>` (case-insensitive) and returns the object's
`Point` on hit. Returns `false` if `Map` is null, `fromScene` is empty, or
no matching object exists — callers use the boolean to decide whether to
fall through to a dict/literal fallback.

### `src/Scenes/VillageScene.cs`, `ShopScene.cs`, `CastleScene.cs`

Each `GetSpawn` override replaced with a three-tier chain:

1. `TryReadTmxSpawn(fromScene, out var tmxPos)` — if true, use TMX-authored position.
2. `Spawns.TryGetValue(fromScene, out var p)` — fall back to the existing hardcoded dict.
3. Scene-specific literal fallback (`(48, 270)` for Village; `(208, 416)` for Shop & Castle).

Each branch logs a single line so designers can see which path resolved.
Format: `[<Scene>] Spawn from <fromScene> resolved via <TMX|dict> at (x,y)`.

Existing `Spawns` dictionary entries are untouched — they remain the
safety net so existing gameplay is unchanged until a TMX is authored.

## Designer Recipe (TMX Authoring)

To override a hub-scene spawn in Tiled:

1. Open the target `.tmx` (`village.tmx`, `shop.tmx`, or `castle.tmx`).
2. Create an **object group** named exactly `Spawn` (case-insensitive match
   in code, but stick with `Spawn` for consistency).
3. Drop a **point object** at the desired spawn position.
4. Name the object `from_<PrevSceneName>` — e.g. `from_Farm`, `from_Village`,
   `from_Shop`, `from_Castle`, `from_Dungeon`, `from_dungeon_entrance`,
   `from_castle_door`. The prev portion is case-insensitive.
5. Save. Next time the player enters that scene from that prev scene, the
   console will log `resolved via TMX at (x,y)` instead of `via dict`.

No C# rebuild required to move an authored spawn afterwards — just edit the
TMX and re-run the game.

## Fallback Guarantees

- **No TMX authored:** Every scene falls through to its existing `Spawns`
  dict — behavior identical to before this plan.
- **TMX authored but wrong name:** Same as above (falls through to dict).
- **From-scene unknown to dict:** Falls through to the literal scene default.
- **Map null (shouldn't happen):** Helper returns false, chain continues.

FarmScene and DungeonScene were intentionally left untouched:
- DungeonScene already has its own richer implementation (with position
  preservation) — deliberately outside scope.
- FarmScene uses the base `GameplayScene.GetSpawn` default.

## Verification

- `dotnet build` — succeeds with 0 errors, 1 pre-existing warning
  (`CS8602` at GameplayScene.cs:201, unrelated to this change).
- No `.tmx` files edited.
- No FarmScene/DungeonScene edits.
- Hardcoded `Spawns` dictionaries preserved in all three modified scenes.
- Manual smoke test deferred to user: transition Farm → Village → Shop →
  Village → Castle → Village and confirm spawns land at the same positions
  as before (all console logs should say `resolved via dict`).

## Deviations from Plan

None — plan executed exactly as written. Implementation snippets in the
plan were copied verbatim (only replaced the em-dash in one Console log
with an ASCII hyphen to avoid any font/encoding concerns in the log).

## Commits

- `b052ad3` feat(quick/260414-wcu): add TryReadTmxSpawn helper on GameplayScene
- `1dedba1` feat(quick/260414-wcu): TMX-driven spawn resolution for hub scenes

## Self-Check: PASSED

- `src/Core/GameplayScene.cs` — TryReadTmxSpawn helper present (verified).
- `src/Scenes/VillageScene.cs`, `ShopScene.cs`, `CastleScene.cs` —
  three-tier GetSpawn present (verified).
- Commits `b052ad3` and `1dedba1` exist in `git log`.
- `dotnet build` green.
