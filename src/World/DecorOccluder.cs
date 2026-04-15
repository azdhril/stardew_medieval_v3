using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.World;

/// <summary>
/// Renders a single Tiled tile-object from a "Decor" object layer with the
/// split-sprite fade-when-behind trick. Purely visual — owns no collision.
/// Per-tile <c>occlusion_y</c> is the pixel row (relative to sprite top)
/// where the trunk begins / canopy ends. Rows above <c>occlusion_y</c> are
/// the canopy: drawn on top of the player at 50% alpha when the player is
/// behind. Rows below are the trunk base: drawn behind the player so the
/// player walks in front of it.
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
    /// Draw full sprite when player isn't behind, or just the trunk half
    /// (rows <c>occlusion_y..height</c>) when player IS behind. The canopy
    /// (rows <c>0..occlusion_y</c>) is drawn by <see cref="DrawAfterPlayer"/>
    /// at 50% alpha so it covers the player's head semi-transparently.
    /// </summary>
    public void DrawBeforePlayer(SpriteBatch sb, PlayerEntity? player)
    {
        if (player == null || !ShouldUseFrontOccluder(player))
        {
            sb.Draw(_texture, _destRect, _sourceRect, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);
            return;
        }

        int trunkH = _sourceRect.Height - _occlusionY;
        if (trunkH > 0)
        {
            var trunkSrc = new Rectangle(_sourceRect.X, _sourceRect.Y + _occlusionY, _sourceRect.Width, trunkH);
            var trunkDest = new Rectangle(_destRect.X, _destRect.Y + _occlusionY, _destRect.Width, trunkH);
            sb.Draw(_texture, trunkDest, trunkSrc, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);
        }
    }

    /// <summary>
    /// When player is behind, draws the canopy (rows <c>0..occlusion_y</c>)
    /// on top of the player at 50% alpha. No-op otherwise.
    /// </summary>
    public void DrawAfterPlayer(SpriteBatch sb, PlayerEntity player)
    {
        if (!ShouldUseFrontOccluder(player)) return;
        if (_occlusionY <= 0) return;

        var canopySrc = new Rectangle(_sourceRect.X, _sourceRect.Y, _sourceRect.Width, _occlusionY);
        var canopyDest = new Rectangle(_destRect.X, _destRect.Y, _destRect.Width, _occlusionY);
        sb.Draw(_texture, canopyDest, canopySrc, Color.White * 0.5f, 0f, Vector2.Zero, _flipEffects, 0f);
    }

    /// <summary>
    /// Player is "behind" this decor when: they horizontally overlap it (with
    /// an 8px inset), vertically reach into the canopy band, and their feet
    /// are above the sort line (worldAnchor.Y).
    /// </summary>
    private bool ShouldUseFrontOccluder(PlayerEntity player)
    {
        Rectangle pb = player.CollisionBox;
        bool overlapsH = pb.Right > _destRect.Left + 8 && pb.Left < _destRect.Right - 8;
        bool reachesCanopy = pb.Top < _destRect.Top + _occlusionY && pb.Bottom > _destRect.Top;
        bool feetBehind = player.GetFootPosition().Y < _worldAnchor.Y - 1f;
        return overlapsH && reachesCanopy && feetBehind;
    }
}
