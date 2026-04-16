# Quick 260415-rli — Flip DecorOccluder split

**Commit:** `494c0d7`
**File:** `src/World/DecorOccluder.cs`

## Change

Swapped the two sprite halves in `DecorOccluder`:

| Half                      | Before          | After           |
| ------------------------- | --------------- | --------------- |
| Top (rows 0..occlusion_y) | back (opaque)   | canopy (fades)  |
| Bottom (occlusion_y..H)   | front (fades)   | trunk (opaque)  |

`occlusion_y` is now documented as "pixel row where the trunk begins / canopy ends".

When player is behind:
- `DrawBeforePlayer` renders the trunk portion only (behind player)
- `DrawAfterPlayer` renders the canopy portion at 50% alpha (over player's head)

When player is NOT behind: full sprite drawn via `DrawBeforePlayer` (unchanged).

`ShouldUseFrontOccluder` vertical check now gates on "player reaches canopy band" instead of the old trunk-band check.

## Scope

- `ResourceNode` left untouched — its POC (`OcclusionStartY = 0`) depends on the original semantics (whole-sprite fade).
- No TMX or tileset changes required; existing `occlusion_y` values on .tsx tiles now just need to point to the trunk top (e.g. 32 for a 48-tall tree with 16px trunk).

## Verify

1. Reabra o jogo (feche a instância atual que está bloqueando o .exe).
2. Entre na village, passe atrás da árvore Decor existente.
3. Esperado: copa fica semi-transparente cobrindo a cabeça do player; tronco fica opaco atrás do player.
4. Saia da zona atrás da árvore: copa volta opaca.
