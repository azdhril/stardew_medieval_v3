using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.World;

/// <summary>
/// Gated door entity for dungeon rooms. When closed, exposes its AABB via
/// <see cref="CollisionBox"/> so <see cref="DungeonScene.GetSolids"/> blocks the
/// player; when open, collides with nothing so the player can walk through and
/// trigger the exit zone behind it. Sprite swaps between closed (column 0) and
/// open (column 1) frames of a 2-frame sheet when one is provided; otherwise a
/// colored placeholder rectangle is drawn (red = closed, green = open).
/// </summary>
public class DungeonDoor : Entity
{
    private readonly string _doorId;
    private readonly string _targetRoomId;
    private readonly Rectangle _closedBounds;
    private readonly Texture2D? _sprite;

    /// <summary>Stable identifier for save/debug (unique within a room).</summary>
    public string DoorId => _doorId;

    /// <summary>Room id that this door leads to when opened.</summary>
    public string TargetRoomId => _targetRoomId;

    /// <summary>True once the associated main room is cleared (door is passable).</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// AABB used for collision while closed. Empty once the door is open so the
    /// player can walk through without bumping into invisible geometry.
    /// </summary>
    public override Rectangle CollisionBox => IsOpen ? Rectangle.Empty : _closedBounds;

    /// <summary>
    /// Construct a new door.
    /// </summary>
    /// <param name="doorId">Stable door id (e.g. "door_r1_to_r2").</param>
    /// <param name="targetRoomId">Room id reached after opening.</param>
    /// <param name="closedBounds">World-space AABB of the closed door.</param>
    /// <param name="sprite">Optional 2-frame sheet (closed, open). Null = fallback rectangle.</param>
    public DungeonDoor(string doorId, string targetRoomId, Rectangle closedBounds, Texture2D? sprite)
    {
        _doorId = doorId;
        _targetRoomId = targetRoomId;
        _closedBounds = closedBounds;
        _sprite = sprite;
        Position = new Vector2(
            closedBounds.X + closedBounds.Width / 2f,
            closedBounds.Y + closedBounds.Height / 2f);
    }

    /// <summary>Open the door (idempotent). Logs only on first open.</summary>
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        Console.WriteLine($"[DungeonDoor] {_doorId} opened (→ {_targetRoomId})");
    }

    /// <summary>Close the door (idempotent). Used by death reset / debug.</summary>
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Console.WriteLine($"[DungeonDoor] {_doorId} closed");
    }

    /// <summary>
    /// Draw the door. If a sprite is provided, swap source rect between columns
    /// 0 (closed) and 1 (open); otherwise draw a flat colored rectangle (red =
    /// closed, green = open) per RESEARCH §Environment fallback.
    /// </summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_sprite != null)
        {
            int w = _sprite.Width / 2;
            int h = _sprite.Height;
            var src = new Rectangle(IsOpen ? w : 0, 0, w, h);
            spriteBatch.Draw(_sprite, _closedBounds, src, Color.White);
            return;
        }

        // Fallback: 1x1 pixel tint. Scene owns a pixel texture; we can't reach
        // it from here without a ref, so we use the fallback only when a pixel
        // texture was injected via the sprite param (null = skip draw).
        // Draw is still safe when sprite is null.
    }

    /// <summary>
    /// Draw a fallback colored rectangle using an externally owned 1x1 pixel
    /// texture. DungeonScene calls this when no door sprite sheet is available
    /// so the door is at least visually present during bring-up.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="pixel">1x1 white pixel texture for tinting.</param>
    public void DrawFallback(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_sprite != null) { Draw(spriteBatch); return; }
        var tint = IsOpen ? new Color(60, 200, 80) : new Color(200, 40, 40);
        spriteBatch.Draw(pixel, _closedBounds, tint * 0.85f);
        // 1px border for readability
        var border = IsOpen ? new Color(40, 140, 40) : new Color(120, 20, 20);
        var b = _closedBounds;
        spriteBatch.Draw(pixel, new Rectangle(b.X, b.Y, b.Width, 1), border);
        spriteBatch.Draw(pixel, new Rectangle(b.X, b.Y + b.Height - 1, b.Width, 1), border);
        spriteBatch.Draw(pixel, new Rectangle(b.X, b.Y, 1, b.Height), border);
        spriteBatch.Draw(pixel, new Rectangle(b.X + b.Width - 1, b.Y, 1, b.Height), border);
    }
}
