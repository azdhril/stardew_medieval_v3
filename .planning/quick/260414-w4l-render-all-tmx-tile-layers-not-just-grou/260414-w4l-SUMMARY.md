---
phase: quick-260414-w4l
plan: 01
subsystem: world/tilemap
tags: [rendering, tmx, tiled, village]
requires: []
provides:
  - "Multi-layer TMX renderer with per-layer offsetx/offsety support"
affects:
  - src/World/TileMap.cs
tech-stack:
  added: []
  patterns:
    - "Iterate _map.Layers in TMX file order; skip ObjectLayer, !visible, and farmzone"
    - "Shift view rect into layer-local space before computing tile-cull bounds"
key-files:
  created: []
  modified:
    - src/World/TileMap.cs
decisions:
  - "farmzone layer matched case-insensitively as a gameplay tag (never rendered)"
  - "Offsets cast via (int)MathF.Round((float)layer.offsetX) — TiledCS exposes them as double"
  - "No caching/sorting — rely on TiledCS preserving TMX file order"
metrics:
  duration: "~5min"
  completed: 2026-04-14
---

# Quick 260414-w4l: Render All TMX Tile Layers Summary

Fixed `TileMap.Draw` to iterate every visible `TileLayer` in the TMX (not just hard-coded Ground/Water) and apply each layer's `offsetx`/`offsety` to tile destination rectangles. Village trees, castle, shop, and dungeon exit will now render; farm scene is unchanged.

## What Changed

- `Draw(SpriteBatch, Rectangle)` now loops `_map.Layers`, skipping:
  - non-`TileLayer` entries (ObjectLayer etc.)
  - layers with `visible == false`
  - the `farmzone` layer (case-insensitive) — it's a gameplay tag driving `IsFarmZone`
- `DrawLayer` reads `layer.offsetX`/`offsetY` (cast `double -> int` via `MathF.Round`) and:
  - shifts the input `viewArea` into layer-local space before computing `startX/startY/endX/endY` (so negative offsets like `offsety="-448"` still cull correctly)
  - adds `(offX, offY)` to each tile's destination `Rectangle`
- `_groundLayer` / `_waterLayer` / `_farmZoneLayer` assignments in `Load()` untouched — `IsWater` / `IsFarmZone` / collision still work.

## Files Modified

- `src/World/TileMap.cs` — `Draw` loop rewritten, `DrawLayer` offset-aware (+27 / -10 lines)

## Commits

- `5d16831` feat(quick/260414-w4l): render all visible TMX tile layers with offsets

## Verification

- `dotnet build stardew_medieval_v3.csproj -c Debug` — Build succeeded, 0 errors, 1 pre-existing unrelated warning (`GameplayScene.cs:177` CS8602, not introduced by this change).
- Visual verification (Task 2) is a `checkpoint:human-verify` — user to confirm trees/castle/shop/dungeon-exit render in village scene and farm scene is unregressed.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- FOUND: src/World/TileMap.cs (modified, contains `foreach (var layer in _map.Layers)` and `layer.offsetX`/`layer.offsetY`)
- FOUND: commit 5d16831
