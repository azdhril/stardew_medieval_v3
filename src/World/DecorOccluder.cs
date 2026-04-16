using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.World;

/// <summary>
/// Renders a single Tiled tile-object from a "Decor" object layer with the
/// split-sprite fade-when-behind trick. Purely visual — owns no collision.
/// Per-tile <c>occlusion_y</c> is pixels from top of sprite to the base of
/// the canopy / top of the trunk. Rows 0..occlusion_y are the canopy: drawn
/// AFTER the player at 50% alpha when the player is behind (covers head,
/// semi-transparent). Rows occlusion_y..height are the trunk: drawn BEFORE
/// the player so the player walks in front of the trunk base.
/// Note: ResourceNode uses the inverse convention (top=back, bottom=front)
/// and is intentionally left untouched — existing POC depends on it.
/// </summary>
public class DecorOccluder
{
    private readonly Texture2D _texture;
    private readonly Rectangle _sourceRect;
    private readonly Rectangle _destRect;
    private readonly int _occlusionY;
    private readonly SpriteEffects _flipEffects;
    private readonly Vector2 _worldAnchor;

    /// <summary>
    /// Build a decor occluder for a Tiled tile-object. <paramref name="destRect"/>
    /// is the world-space rectangle (Tiled tile-object anchor is bottom-left —
    /// callers must pass <c>(obj.x, obj.y - tileH, tileW, tileH)</c>).
    /// </summary>
    public DecorOccluder(
        Texture2D texture,
        Rectangle sourceRect,
        Rectangle destRect,
        int occlusionY,
        SpriteEffects flipEffects)
    {
        _texture = texture;
        _sourceRect = sourceRect;
        _destRect = destRect;
        _occlusionY = Math.Clamp(occlusionY, 0, Math.Max(0, sourceRect.Height - 1));
        _flipEffects = flipEffects;
        _worldAnchor = new Vector2(destRect.Center.X, destRect.Bottom);
    }

    /// <summary>
    /// Draw pass before the player. Three cases, selected by per-decor Y-sort
    /// against player feet:
    ///  1. Player is in front (feet.Y >= anchor.Y) → draw full sprite here.
    ///  2. Player is behind AND standing within footprint (split case) →
    ///     draw trunk only; canopy at 50% alpha in <see cref="DrawAfterPlayer"/>.
    ///  3. Player is behind but outside footprint (pure Y-sort, e.g. fence one
    ///     row below) → draw nothing here; full sprite drawn after player.
    /// </summary>
    public void DrawBeforePlayer(SpriteBatch sb, PlayerEntity? player)
    {
        if (player == null || IsPlayerInFront(player))
        {
            DrawFull(sb);
            return;
        }

        if (ShouldSplitOcclude(player))
        {
            int trunkH = _sourceRect.Height - _occlusionY;
            if (trunkH > 0)
            {
                var trunkSrc = new Rectangle(_sourceRect.X, _sourceRect.Y + _occlusionY, _sourceRect.Width, trunkH);
                var trunkDest = new Rectangle(_destRect.X, _destRect.Y + _occlusionY, _destRect.Width, trunkH);
                sb.Draw(_texture, trunkDest, trunkSrc, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);
            }
        }
        // else: case 3 — drawn in DrawAfterPlayer.
    }

    /// <summary>
    /// Draw pass after the player. Mirror of <see cref="DrawBeforePlayer"/>:
    /// case 2 paints canopy at 50% over the player; case 3 paints the full
    /// sprite so it occludes the player (pure Y-sort). No-op when player
    /// is in front.
    /// </summary>
    public void DrawAfterPlayer(SpriteBatch sb, PlayerEntity player)
    {
        if (IsPlayerInFront(player)) return;

        if (ShouldSplitOcclude(player))
        {
            if (_occlusionY <= 0) return;
            var canopySrc = new Rectangle(_sourceRect.X, _sourceRect.Y, _sourceRect.Width, _occlusionY);
            var canopyDest = new Rectangle(_destRect.X, _destRect.Y, _destRect.Width, _occlusionY);
            sb.Draw(_texture, canopyDest, canopySrc, Color.White * 0.5f, 0f, Vector2.Zero, _flipEffects, 0f);
        }
        else
        {
            DrawFull(sb);
        }
    }

    private void DrawFull(SpriteBatch sb)
        => sb.Draw(_texture, _destRect, _sourceRect, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);

    /// <summary>Player is in front when their feet are at or below this decor's anchor.</summary>
    private bool IsPlayerInFront(PlayerEntity player)
        => player.GetFootPosition().Y >= _worldAnchor.Y;

    /// <summary>
    /// True when the player is standing within this decor's footprint so the
    /// trunk/canopy split is visually needed (feet hidden behind trunk, head
    /// under faded canopy). Horizontal overlap (8px inset) + canopy band.
    /// </summary>
    private bool ShouldSplitOcclude(PlayerEntity player)
    {
        Rectangle pb = player.CollisionBox;
        bool overlapsH = pb.Right > _destRect.Left + 8 && pb.Left < _destRect.Right - 8;
        bool reachesCanopy = pb.Top < _destRect.Top + _occlusionY && pb.Bottom > _destRect.Top;
        return overlapsH && reachesCanopy;
    }
}
