using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Player;

/// <summary>
/// Player entity with movement, animation, and collision.
/// Inherits position, HP, animation fields, and collision from Entity base class.
/// </summary>
public class PlayerEntity : Entity
{
    public PlayerStats Stats { get; } = new();

    private const float Speed = 80f; // pixels per second (5 tiles/s at 16px)

    private bool _isMoving;

    /// <summary>
    /// I-frame timer set by CombatManager after taking damage.
    /// Per D-15: player blinks and is immune for 1s after being hit.
    /// </summary>
    public float IFrameTimer { get; set; }

    public void LoadContent(Texture2D spriteSheet)
    {
        SpriteSheet = spriteSheet;
        // Player sheet is 4 columns x 4 rows (down, left, right, up)
        FrameWidth = spriteSheet.Width / 4;
        FrameHeight = spriteSheet.Height / 4;
    }

    public void Update(float deltaTime, Vector2 input, TileMap map, IEnumerable<Entity>? solids = null)
    {
        // Update combat timers (knockback, flash)
        UpdateKnockback(deltaTime);
        UpdateFlash(deltaTime);

        if (IFrameTimer > 0)
            IFrameTimer -= deltaTime;

        _isMoving = input != Vector2.Zero;

        if (_isMoving)
        {
            // Update facing direction (snap to 4-direction cardinal)
            if (MathF.Abs(input.X) > MathF.Abs(input.Y))
                FacingDirection = input.X > 0 ? Direction.Right : Direction.Left;
            else
                FacingDirection = input.Y > 0 ? Direction.Down : Direction.Up;

            // Try to move with collision
            var delta = input * Speed * deltaTime;
            TryMove(delta, map, solids);

            // Animate
            AnimationTimer += deltaTime;
            if (AnimationTimer >= FrameTime)
            {
                AnimationTimer -= FrameTime;
                FrameIndex = (FrameIndex + 1) % 4;
            }
        }
        else
        {
            FrameIndex = 0;
            AnimationTimer = 0;
        }
    }

    private void TryMove(Vector2 delta, TileMap map, IEnumerable<Entity>? solids)
    {
        // Try X
        var beforeBox = CollisionBox;
        var oldPos = Position;
        Position = oldPos + new Vector2(delta.X, 0);
        if (map.CheckCollision(CollisionBox) || CreatesNewSolidOverlap(solids, beforeBox, CollisionBox))
            Position = new Vector2(oldPos.X, Position.Y);

        // Try Y (compare to post-X box so Y doesn't re-enter what X escaped)
        beforeBox = CollisionBox;
        oldPos = Position;
        Position = oldPos + new Vector2(0, delta.Y);
        if (map.CheckCollision(CollisionBox) || CreatesNewSolidOverlap(solids, beforeBox, CollisionBox))
            Position = new Vector2(Position.X, oldPos.Y);

        // Clamp to map bounds
        var bounds = map.GetWorldBounds();
        Position = new Vector2(
            Microsoft.Xna.Framework.MathHelper.Clamp(Position.X, FrameWidth / 2f, bounds.Width - FrameWidth / 2f),
            Microsoft.Xna.Framework.MathHelper.Clamp(Position.Y, FrameHeight / 2f, bounds.Height - FrameHeight / 2f)
        );
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (SpriteSheet == null) return;

        // I-frame blink effect: toggle visibility every 0.1s (per D-15)
        if (IFrameTimer > 0)
        {
            bool visible = ((int)(IFrameTimer * 10) % 2 == 0);
            if (!visible) return;
        }

        int row = FacingDirection switch
        {
            Direction.Down => 0,
            Direction.Right => 1,
            Direction.Up => 2,
            Direction.Left => 3,
            _ => 0
        };

        var srcRect = new Rectangle(
            FrameIndex * FrameWidth,
            row * FrameHeight,
            FrameWidth,
            FrameHeight
        );

        var destRect = new Rectangle(
            (int)(Position.X - FrameWidth / 2f),
            (int)(Position.Y - FrameHeight / 2f),
            FrameWidth,
            FrameHeight
        );

        // Flash white on hit
        Color tint = IsFlashing ? Color.Red : Color.White;
        spriteBatch.Draw(SpriteSheet, destRect, srcRect, tint);
    }

    /// <summary>
    /// Returns true if moving from <paramref name="oldBox"/> to <paramref name="newBox"/>
    /// would increase overlap area with any solid. Prevents entering further while still
    /// allowing escape when an enemy pushes into the player (no "sticking").
    /// </summary>
    private static bool CreatesNewSolidOverlap(IEnumerable<Entity>? solids, Rectangle oldBox, Rectangle newBox)
    {
        if (solids == null) return false;
        foreach (var e in solids)
        {
            if (e == null || !e.IsAlive) continue;
            var eb = e.CollisionBox;
            int oldOverlap = OverlapArea(oldBox, eb);
            int newOverlap = OverlapArea(newBox, eb);
            if (newOverlap > oldOverlap) return true;
        }
        return false;
    }

    private static int OverlapArea(Rectangle a, Rectangle b)
    {
        var r = Rectangle.Intersect(a, b);
        return r.IsEmpty ? 0 : r.Width * r.Height;
    }

    /// <summary>
    /// Get the foot position (center-bottom of collision box) used for tile calculations.
    /// Position is sprite center (head level), but gameplay tiles should be based on feet.
    /// </summary>
    public Vector2 GetFootPosition()
    {
        var box = CollisionBox;
        return new Vector2(box.X + box.Width / 2f, box.Y + box.Height / 2f);
    }

    /// <summary>
    /// Get the tile position the player is standing on (based on feet, not sprite center).
    /// </summary>
    public Point GetTilePosition() => TileMap.WorldToTile(GetFootPosition());

    /// <summary>
    /// Get the tile position the player is facing (for interactions).
    /// </summary>
    public Point GetFacingTile()
    {
        var tile = GetTilePosition();
        return FacingDirection switch
        {
            Direction.Up => new Point(tile.X, tile.Y - 1),
            Direction.Down => new Point(tile.X, tile.Y + 1),
            Direction.Left => new Point(tile.X - 1, tile.Y),
            Direction.Right => new Point(tile.X + 1, tile.Y),
            _ => tile
        };
    }
}
