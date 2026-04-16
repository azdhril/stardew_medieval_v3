---
phase: quick/260415-q5n
plan: 01
subsystem: world/rendering
tags: [tiled, decor, occlusion, rendering, tilemap]
dependency-graph:
  requires: [src/World/TileMap.cs, src/World/ResourceNode.cs, src/Core/GameplayScene.cs]
  provides: [DecorOccluder, TileMap.Decor, "Decor" object-layer render loop]
  affects: [all GameplayScene subclasses — any map with a "Decor" object layer auto-renders]
tech-stack:
  added: []
  patterns: [split-sprite fade-when-behind mirrors ResourceNode; per-tileset property index]
key-files:
  created:
    - src/World/DecorOccluder.cs
  modified:
    - src/World/TileMap.cs
    - src/Core/GameplayScene.cs
decisions:
  - "DecorOccluder is a class (not struct/record) — keeps API symmetrical with ResourceNode and avoids boxing a Texture2D reference in a struct"
  - "occlusion_y lookup built once during Load (firstgid → localId → occY dict); O(1) per decor object"
  - "Decor renders BEFORE OnDrawWorld hook so FarmScene ResourceNode flashes/effects layer on top"
  - "No collision registered (purely visual per CONTEXT D)"
metrics:
  tasks_completed: 2
  tasks_planned: 3
  checkpoint_pending: "Task 3 (human-verify) — Tiled authoring + in-game visual test"
---

# Quick Task 260415-q5n: Generic Decor tile-object renderer Summary

**One-liner:** Tiled "Decor" object layer now auto-renders tile-objects with split-sprite alpha-0.5 fade when the player stands behind them, driven by per-tile `occlusion_y` in the .tsx — zero per-map C# required.

## Files Created

- **`src/World/DecorOccluder.cs`** (98 lines)
  - `DecorOccluder(Texture2D, Rectangle src, Rectangle dest, int occlusionY, SpriteEffects flip)` ctor
  - `DrawBeforePlayer(SpriteBatch, PlayerEntity?)` — full sprite OR back-half rows [0..occlusionY)
  - `DrawAfterPlayer(SpriteBatch, PlayerEntity)` — front-half rows [occlusionY..H) at Color.White * 0.5f
  - `ShouldUseFrontOccluder(PlayerEntity)` — 3-condition test matching ResourceNode exactly (horizontal overlap w/ 8px inset, vertical overlap w/ 6px bottom inset, feet above sort line)

## Files Modified

- **`src/World/TileMap.cs`**
  - `TmxObject` record extended with trailing `int Gid = 0` parameter
  - `GetObjectGroup` now populates `Gid` from `(int)obj.gid` (TiledCS)
  - New field `_decor` + public `IReadOnlyList<DecorOccluder> Decor` property
  - New field `_occlusionYByTileset` — per-firstgid dictionary of local-id → occlusion_y overrides
  - Tileset load loop now indexes each `TiledTile.properties` entry named `occlusion_y` into the per-tileset dict
  - New `LoadDecorObjects()` method called from `Load()` after `LoadTriggerObjects()` — resolves gid → tileset/src-rect, anchors at bottom-left per Tiled convention, applies flip flags, uses per-tile `occlusion_y` with `tileH/2` fallback
  - Summary log line extended with decor count

- **`src/Core/GameplayScene.cs`** (Draw method, ~lines 233–241)
  - Two new `foreach (var decor in Map.Decor)` loops around `Player.Draw(sb)`
  - Placed BEFORE `OnDrawWorld` / `OnDrawWorldAfterPlayer` so subclass hooks (ResourceNode etc.) still render on top

## How To Add Decor Tiles (Designer Workflow)

1. **Tileset setup (once per tile):** Open the relevant `.tsx` in Tiled's tileset editor. Select a tile, add a custom **int** property named `occlusion_y` with a pixel value (where the fade split should happen — typically ~2/3 of tile height, e.g. `22` for a 32-tall tile). Save.
   - Skip this step if `tileHeight/2` works — fallback is automatic.

2. **Map authoring:** Open a `.tmx`. Add an object layer named **`Decor`** (case-insensitive) if it doesn't exist.

3. **Painting:** Select the tileset → pick the tile → insert-tile tool (T) → click on the Decor layer to drop tile-objects. Tiled anchors at the tile's bottom-left; the game computes `destRect = (obj.x, obj.y - tileH, tileW, tileH)`.

4. **Flip flags:** Horizontal/vertical flip during placement flows through (`SpriteEffects.FlipHorizontally/Vertically`) automatically.

5. **Run:** No C# changes. `[TileMap] Loaded N decor occluders` confirms on stdout.

6. **Collision:** Decor is visual-only. If a decor object should block the player, paint a shape on the existing `Collision` object layer manually.

## Deviations from Plan

**None of consequence.** Minor defensive additions:

- `LoadDecorObjects` guards `tileW > 0 && tileH > 0` in addition to the planned `cols > 0` check (Rule 2 — Tiled-produced 0 dims would cause invalid Rectangles).
- `DrawAfterPlayer` returns early if the computed front-half height is 0 or negative (occurs when `occlusion_y == sourceHeight`; constructor clamps `occlusion_y` to `[0, H-1]` but the zero-width case survives if H == 1).

These are inline safety bumps; behavior identical to plan for all sane inputs.

## Deferred Issues

None. Pre-existing warning at `GameplayScene.cs:201` (possible null deref) is unrelated to this work and was already on master before these commits.

## Commits

- `2c9b62c` — feat(quick/260415-q5n): add DecorOccluder with tileset occlusion_y lookup
- `d557028` — feat(quick/260415-q5n): render Map.Decor in GameplayScene around player

## Checkpoint Pending

**Task 3 (human-verify):** Requires Tiled authoring of a test decor tile in `assets/maps/village.tmx` (add `occlusion_y` to one tileset tile, drop it on a new `Decor` object layer, run the game, walk behind it, confirm split-sprite fade). Executor stopped here per plan — Tiled GUI work is out of autonomous scope.

## Self-Check: PASSED

- [x] `src/World/DecorOccluder.cs` exists
- [x] `src/World/TileMap.cs` has `Decor` property, `Gid` on TmxObject, `LoadDecorObjects`
- [x] `src/Core/GameplayScene.cs` iterates `Map.Decor` around `Player.Draw`
- [x] Commit `2c9b62c` exists in `git log`
- [x] Commit `d557028` exists in `git log`
- [x] `dotnet build` — 0 errors, 1 pre-existing warning (not introduced)
