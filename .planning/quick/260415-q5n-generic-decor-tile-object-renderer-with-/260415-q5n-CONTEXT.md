# Quick Task 260415-q5n: Generic Decor tile-object renderer - Context

**Gathered:** 2026-04-15
**Status:** Ready for planning

<domain>
## Task Boundary

Add a generic `Decor` object-group renderer to TileMap so designers can paint tile-objects in Tiled and they automatically fade when the player stands behind them — same split-sprite + alpha trick as `ResourceNode`, but with zero per-tree C# code. Tile-objects read per-tile `occlusion_y` from the .tsx tileset properties.

Out of scope: migrating existing `Tree1..Tree4` tile layers in `village.tmx` to Decor (those stay as-is). FarmScene fruit trees (ResourceNode) untouched.

</domain>

<decisions>
## Implementation Decisions

### Collision
- Decor tile-objects are **purely visual** — they never contribute to collision.
- If designer wants a specific decor object to block the player, they add a shape to the existing `Collision` object layer manually (standard project pattern).

### Occlusion line source
- Read `occlusion_y` (int pixels, relative to tile top) as a **per-tile property in the tileset `.tsx`**.
- Fallback when property is absent: `tileHeight / 2` (split in the middle).
- Precedence: tileset property > fallback. No per-object TMX override.

### Fade alpha
- Constant `0.5f` — matches existing `ResourceNode` POC for visual consistency across fruit trees and decor trees.
- Not configurable per tile/object.

### Claude's Discretion
- Struct vs class for DecorOccluder: struct/record if lightweight, class if simpler for the occluder render API.
- Where `DecorOccluder` instances live: inside `TileMap` (loaded once on `Load`, exposed for scenes to iterate), OR returned to scenes that place them in their entity list. Pick whichever keeps code small.
- Draw integration: reuse `OnDrawWorld` / `OnDrawWorldAfterPlayer` hooks in `GameplayScene` (already exist for the same purpose with ResourceNode — see [src/Core/GameplayScene.cs:233-236](src/Core/GameplayScene.cs#L233-L236)).
- Player detection: use the same 3-condition test as `ResourceNode.ShouldUseFrontOccluder` (horizontal overlap + vertical overlap + feet-behind-anchor).
- Anchor point of tile-object: Tiled tile-objects have origin at bottom-left by convention — verify and use bottom-center as world anchor.

</decisions>

<specifics>
## Specific Ideas

- Existing reference implementation to mirror: [src/World/ResourceNode.cs:76-157](src/World/ResourceNode.cs#L76-L157) (Draw / DrawBeforePlayer / DrawAfterPlayer / ShouldUseFrontOccluder).
- TMX helper available: `TileMap.GetObjectGroup("Decor")` returns `List<TmxObject>` ([src/World/TileMap.cs:127-164](src/World/TileMap.cs#L127-L164)) — each object has Bounds, Point, Properties, and for tile-objects also a GID accessible via the raw TiledObject (may need a small addition if GID is not currently surfaced).
- Tileset TiledCS access: `TiledTileset` has per-tile data including `properties` list on `TiledTile`. The loader must index these by local id to look up `occlusion_y` for a given gid.
- `.tsx` authoring in Tiled: select tile in tileset editor → right panel "Custom Properties" → add int property `occlusion_y`.

</specifics>

<canonical_refs>
## Canonical References

- CLAUDE.md "no premature abstraction" rule: keep DecorOccluder minimal; do not add features (animation, interaction, save state) until a second use case demands them.
- CLAUDE.md naming: PascalCase class, `_camelCase` private fields, `[TileMap]` console log prefix.
</canonical_refs>
