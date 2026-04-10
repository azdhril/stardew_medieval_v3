using System;
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

    public void LoadContent(Texture2D spriteSheet)
    {
        SpriteSheet = spriteSheet;
        // Player sheet is 4 columns x 4 rows (down, left, right, up)
        FrameWidth = spriteSheet.Width / 4;
        FrameHeight = spriteSheet.Height / 4;
    }

    public void Update(float deltaTime, Vector2 input, TileMap map)
    {
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
            TryMove(delta, map);

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

    private void TryMove(Vector2 delta, TileMap map)
    {
        // Try X
        var newPos = Position + new Vector2(delta.X, 0);
        var oldPos = Position;
        Position = newPos;
        if (map.CheckCollision(CollisionBox))
            Position = new Vector2(oldPos.X, Position.Y);

        // Try Y
        newPos = Position + new Vector2(0, delta.Y);
        oldPos = Position;
        Position = newPos;
        if (map.CheckCollision(CollisionBox))
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

        spriteBatch.Draw(SpriteSheet, destRect, srcRect, Color.White);
    }

    /// <summary>
    /// Get the tile position the player is standing on.
    /// </summary>
    public Point GetTilePosition() => TileMap.WorldToTile(Position);

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
