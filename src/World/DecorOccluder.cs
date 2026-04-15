using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.World;

/// <summary>
/// Renders a single Tiled tile-object from a "Decor" object layer with the
/// split-sprite fade-when-behind trick used by ResourceNode. Purely visual —
/// owns no collision. Per-tile <c>occlusion_y</c> (pixel row relative to tile
/// top) defines where the back half ends and the front half (which fades to
/// 50% alpha when the player stands behind) begins.
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
    /// Draw full sprite when player isn't behind, or just the back half
    /// (rows 0..occlusion_y) when player IS behind. The front half is drawn
    /// by <see cref="DrawAfterPlayer"/> at 50% alpha so the player remains
    /// visible through it.
    /// </summary>
    public void DrawBeforePlayer(SpriteBatch sb, PlayerEntity? player)
    {
        if (player == null || !ShouldUseFrontOccluder(player))
        {
            sb.Draw(_texture, _destRect, _sourceRect, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);
            return;
        }

        if (_occlusionY > 0)
        {
            var backSrc = new Rectangle(_sourceRect.X, _sourceRect.Y, _sourceRect.Width, _occlusionY);
            var backDest = new Rectangle(_destRect.X, _destRect.Y, _destRect.Width, _occlusionY);
            sb.Draw(_texture, backDest, backSrc, Color.White, 0f, Vector2.Zero, _flipEffects, 0f);
        }
    }

    /// <summary>
    /// When player is behind, draws the front half (rows occlusion_y..height)
    /// on top of the player at 50% alpha. No-op otherwise.
    /// </summary>
    public void DrawAfterPlayer(SpriteBatch sb, PlayerEntity player)
    {
        if (!ShouldUseFrontOccluder(player)) return;

        int frontH = _sourceRect.Height - _occlusionY;
        if (frontH <= 0) return;

        var frontSrc = new Rectangle(_sourceRect.X, _sourceRect.Y + _occlusionY, _sourceRect.Width, frontH);
        var frontDest = new Rectangle(_destRect.X, _destRect.Y + _occlusionY, _destRect.Width, frontH);
        sb.Draw(_texture, frontDest, frontSrc, Color.White * 0.5f, 0f, Vector2.Zero, _flipEffects, 0f);
    }

    /// <summary>
    /// Player is "behind" this decor when: they horizontally overlap it (with
    /// an 8px inset), vertically overlap the occlusion band (with a 6px
    /// bottom inset), and their feet are above the sort line (worldAnchor.Y).
    /// Matches ResourceNode.ShouldUseFrontOccluder exactly.
    /// </summary>
    private bool ShouldUseFrontOccluder(PlayerEntity player)
    {
        Rectangle pb = player.CollisionBox;
        bool overlapsH = pb.Right > _destRect.Left + 8 && pb.Left < _destRect.Right - 8;
        bool overlapsV = pb.Bottom > _destRect.Top + _occlusionY && pb.Top < _destRect.Bottom - 6;
        bool feetBehind = player.GetFootPosition().Y < _worldAnchor.Y - 1f;
        return overlapsH && overlapsV && feetBehind;
    }
}
