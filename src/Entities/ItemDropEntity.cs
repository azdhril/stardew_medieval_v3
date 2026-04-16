using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.Entities;

/// <summary>
/// World-space item entity that spawns with a bounce animation, pulls toward the player
/// magnetically within range, and adds itself to inventory on pickup. If inventory is full,
/// the item stays on the ground.
/// </summary>
public class ItemDropEntity : Entity
{
    private static readonly Random _random = new();

    private readonly string _itemId;
    private readonly SpriteAtlas _atlas;
    private readonly Rectangle _spriteRect;
    private readonly Texture2D _spriteTexture;
    private readonly Vector2 _startPos;
    private readonly Vector2 _bounceOffset;

    private bool _isBouncing;
    private float _bounceTimer;
    private float _totalTime;

    private const float MagnetRange = 56f;
    private const float PickupRange = 8f;
    private const float MaxMagnetSpeed = 200f;
    private const float BounceDuration = 0.4f;
    private const float BounceHeight = 12f;
    private const float PickupDelay = 0.5f; // seconds after spawn before pickup/magnet activates

    /// <summary>The item identifier for this drop.</summary>
    public string ItemId => _itemId;

    /// <summary>Remaining quantity. Mutable because partial pickup may reduce it.</summary>
    public int Quantity { get; set; }

    /// <summary>True when fully collected and ready for removal.</summary>
    public bool IsCollected { get; private set; } = false;

    /// <summary>
    /// Create a new item drop at the given world position.
    /// </summary>
    /// <param name="itemId">Item registry Id (e.g. "Cabbage").</param>
    /// <param name="quantity">Number of items in this drop.</param>
    /// <param name="spawnPosition">World-space spawn position (center of tile).</param>
    /// <param name="atlas">Sprite atlas for rendering the item icon.</param>
    public ItemDropEntity(string itemId, int quantity, Vector2 spawnPosition, SpriteAtlas atlas)
    {
        _itemId = itemId;
        Quantity = quantity;
        _atlas = atlas;

        Position = spawnPosition;
        _startPos = spawnPosition;

        // Random lateral bounce offset for visual variety
        _bounceOffset = new Vector2(_random.Next(-8, 9), _random.Next(-4, 5));
        _isBouncing = true;
        _bounceTimer = 0f;
        _totalTime = 0f;

        // Look up the real item sprite (per D-08: use actual item sprite, not generic sack)
        var def = ItemRegistry.Get(itemId);
        string spriteId = def?.SpriteId ?? "";
        _spriteRect = atlas.GetRect(spriteId);
        _spriteTexture = atlas.GetTexture(spriteId);
    }

    /// <summary>
    /// Update the item drop with player position for magnetism and pickup.
    /// Call each frame from the scene.
    /// </summary>
    /// <param name="deltaTime">Elapsed time this frame in seconds.</param>
    /// <param name="playerPos">Current player world position.</param>
    /// <param name="inventory">Player inventory for pickup.</param>
    public void UpdateWithPlayer(float deltaTime, Vector2 playerPos, InventoryManager inventory)
    {
        if (IsCollected) return;

        _totalTime += deltaTime;

        // Bounce animation on spawn (per D-10)
        if (_isBouncing)
        {
            _bounceTimer += deltaTime;
            float t = MathHelper.Clamp(_bounceTimer / BounceDuration, 0f, 1f);

            // Parabolic arc: peaks at t=0.5
            float yOffset = -BounceHeight * 4f * t * (1f - t);
            float xOffset = _bounceOffset.X * t;

            Position = _startPos + new Vector2(xOffset, yOffset + _bounceOffset.Y * t);

            if (t >= 1f)
                _isBouncing = false;

            return; // No magnetism during bounce
        }

        // Pickup immunity: wait after spawn so player sees the item land
        if (_totalTime < PickupDelay)
            return;

        float dist = Vector2.Distance(Position, playerPos);

        // Pickup check
        if (dist <= PickupRange)
        {
            // Currency items (e.g. Gold_Coin) go directly to gold balance, no slot used
            var def = ItemRegistry.Get(_itemId);
            if (def?.Type == ItemType.Currency)
            {
                inventory.AddGold(Quantity);
                IsCollected = true;
                return;
            }

            int remaining = inventory.TryAdd(_itemId, Quantity);
            if (remaining == 0)
            {
                // Fully picked up
                IsCollected = true;
            }
            else if (remaining < Quantity)
            {
                // Partial pickup (per D-13: rest stays on ground)
                Quantity = remaining;
            }
            // else: inventory completely full, item stays on ground
            return;
        }

        // Magnetic pull toward player (per D-09: quadratic ease-in)
        if (dist <= MagnetRange)
        {
            float t = 1f - (dist / MagnetRange); // 0 at edge, 1 near center
            float speed = MathHelper.Lerp(40f, MaxMagnetSpeed, t * t);
            Vector2 direction = (playerPos - Position) / dist;
            Position += direction * speed * deltaTime;
        }
    }

    /// <summary>
    /// Draw the item drop at its current world position.
    /// </summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (IsCollected) return;

        // Subtle idle bobbing when not bouncing
        float bob = _isBouncing ? 0f : (float)Math.Sin(_totalTime * 3f) * 1.5f;

        // Draw 16x16 item icon at world position
        var destRect = new Rectangle(
            (int)Position.X - 8,
            (int)(Position.Y - 8 + bob),
            16, 16);

        spriteBatch.Draw(_spriteTexture, destRect, _spriteRect, Color.White);
    }
}
