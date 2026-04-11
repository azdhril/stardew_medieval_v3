using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Entities;

/// <summary>
/// A simple test NPC entity that proves Entity base class extensibility.
/// Patrols horizontally back and forth, drawn as a colored rectangle.
/// Used to satisfy SC-3: non-player entity spawned via Entity inheritance.
/// </summary>
public class DummyNpc : Entity
{
    private int _paceDirection = 1;
    private float _paceTimer;
    private const float PaceInterval = 2.0f;
    private const float MoveSpeed = 30f;

    /// <summary>
    /// Creates a new DummyNpc at the given position.
    /// </summary>
    /// <param name="pixel">1x1 white texture used for rectangle rendering.</param>
    /// <param name="startPosition">Initial world position of the NPC.</param>
    public DummyNpc(Texture2D pixel, Vector2 startPosition)
    {
        SpriteSheet = pixel;
        Position = startPosition;
        FrameWidth = 16;
        FrameHeight = 24;
        FacingDirection = Direction.Right;
        Console.WriteLine($"[DummyNpc] Spawned at ({startPosition.X}, {startPosition.Y})");
    }

    /// <summary>
    /// Collision box at the NPC's feet (12x8 pixels centered horizontally).
    /// </summary>
    public override Rectangle CollisionBox
    {
        get
        {
            int w = 12, h = 8;
            return new Rectangle(
                (int)Position.X - w / 2,
                (int)Position.Y + FrameHeight / 2 - h,
                w, h);
        }
    }

    /// <summary>
    /// Updates patrol movement. Reverses horizontal direction every 2 seconds.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public override void Update(float deltaTime)
    {
        _paceTimer += deltaTime;
        if (_paceTimer >= PaceInterval)
        {
            _paceTimer = 0f;
            _paceDirection *= -1;
        }

        var pos = Position;
        pos.X += _paceDirection * MoveSpeed * deltaTime;
        Position = pos;

        FacingDirection = _paceDirection > 0 ? Direction.Right : Direction.Left;
    }

    /// <summary>
    /// Draws the NPC as a green rectangle with a red debug collision box.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch for rendering.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (SpriteSheet == null)
            return;

        // Draw NPC body as green rectangle
        spriteBatch.Draw(SpriteSheet,
            new Rectangle(
                (int)Position.X - FrameWidth / 2,
                (int)Position.Y - FrameHeight / 2,
                FrameWidth, FrameHeight),
            Color.ForestGreen);

        // Draw collision box as semi-transparent red for debug
        var box = CollisionBox;
        spriteBatch.Draw(SpriteSheet,
            new Rectangle(box.X, box.Y, box.Width, box.Height),
            Color.Red * 0.4f);
    }
}
