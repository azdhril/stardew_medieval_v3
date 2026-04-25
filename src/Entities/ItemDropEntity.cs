using System;
using System.Collections.Generic;
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
    /// <summary>Rarity glow color for non-Common drops (null for Common — no glow).</summary>
    private readonly Color? _rarityGlow;

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
    /// When true, this drop was thrown to the ground by the player from the inventory/chest.
    /// Auto-pickup and magnet are SUPPRESSED until the player has stepped out of magnet range
    /// at least once (or presses E nearby via <see cref="TryManualPickup"/>). Without this,
    /// dragging an item out would just snap it right back into the bag.
    /// </summary>
    public bool DroppedByPlayer { get; init; } = false;

    /// <summary>True once the player has been outside <see cref="MagnetRange"/> at least once
    /// since this drop landed — flips on automatically inside <see cref="UpdateWithPlayer"/>.</summary>
    private bool _magnetArmed = false;

    /// <summary>
    /// Quality grade carried by this drop (0 = none, 1..3 = star tiers). Set by the
    /// fishing minigame on a successful catch and forwarded to inventory on pickup so
    /// the resulting <see cref="ItemStack"/> keeps the star tier.
    /// </summary>
    public int Quality { get; init; } = 0;

    // === Smoke particle system (PoE-style rarity wisp) ============================
    // Particles spawn at the item's base in the rarity color, rise upward with a small
    // horizontal drift, expand slightly, and fade out. Replaces the old static halo.

    private struct SmokeParticle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Age;
        public float Lifetime;
        public float SizeStart;
        public float SizeEnd;
    }

    private const float SmokeSpawnInterval = 0.10f; // ~10 particles/s per drop
    private readonly List<SmokeParticle> _smoke = new();
    private float _smokeAccumulator;
    private static readonly Random _smokeRng = new();

    /// <summary>Lazy-allocated radial-gradient texture (16×16) used by every drop's smoke.</summary>
    private static Texture2D? _smokeTex;
    private static Texture2D GetSmokeTexture(GraphicsDevice device)
    {
        if (_smokeTex != null) return _smokeTex;
        const int size = 16;
        var data = new Color[size * size];
        var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        float maxR = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float t = MathHelper.Clamp(1f - (d / maxR), 0f, 1f);
                // Soft Gaussian-ish falloff so the blob feels feathered.
                float alpha = t * t;
                data[y * size + x] = new Color(255, 255, 255, (int)(alpha * 255));
            }
        }
        _smokeTex = new Texture2D(device, size, size);
        _smokeTex.SetData(data);
        return _smokeTex;
    }

    private void TickSmoke(float dt)
    {
        // Advance + cull existing particles.
        for (int i = _smoke.Count - 1; i >= 0; i--)
        {
            var p = _smoke[i];
            p.Age += dt;
            if (p.Age >= p.Lifetime) { _smoke.RemoveAt(i); continue; }
            p.Pos += p.Vel * dt;
            // Subtle deceleration so particles "lose steam" near the top.
            p.Vel *= 1f - 0.5f * dt;
            _smoke[i] = p;
        }

        if (!_rarityGlow.HasValue) return;

        // Spawn new particles at a steady rate while the drop is on the ground.
        _smokeAccumulator += dt;
        while (_smokeAccumulator >= SmokeSpawnInterval)
        {
            _smokeAccumulator -= SmokeSpawnInterval;
            float ox = ((float)_smokeRng.NextDouble() - 0.5f) * 6f;   // ±3 horizontal
            float oy = ((float)_smokeRng.NextDouble() - 0.5f) * 2f;   // small vertical jitter at base
            float vy = -10f - (float)_smokeRng.NextDouble() * 14f;    // -10..-24 px/s upward
            float vx = ((float)_smokeRng.NextDouble() - 0.5f) * 8f;   // ±4 horizontal drift
            float life = 0.7f + (float)_smokeRng.NextDouble() * 0.5f; // 0.7..1.2s
            _smoke.Add(new SmokeParticle
            {
                Pos = new Vector2(Position.X + ox, Position.Y + 4f + oy),
                Vel = new Vector2(vx, vy),
                Age = 0f,
                Lifetime = life,
                SizeStart = 4f + (float)_smokeRng.NextDouble() * 2f,
                SizeEnd   = 8f + (float)_smokeRng.NextDouble() * 4f,
            });
        }
    }

    private void DrawSmoke(SpriteBatch spriteBatch)
    {
        if (!_rarityGlow.HasValue || _smoke.Count == 0) return;
        var tex = GetSmokeTexture(spriteBatch.GraphicsDevice);
        var baseColor = _rarityGlow.Value;
        foreach (var p in _smoke)
        {
            float t = p.Age / p.Lifetime;                  // 0..1
            float alpha = (1f - t) * 0.65f;                // fade out, max ~65% opacity
            float size = MathHelper.Lerp(p.SizeStart, p.SizeEnd, t);
            int s = Math.Max(2, (int)size);
            var rect = new Rectangle((int)(p.Pos.X - s / 2f), (int)(p.Pos.Y - s / 2f), s, s);
            spriteBatch.Draw(tex, rect, baseColor * alpha);
        }
    }

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

        // Cache rarity color so Draw doesn't re-lookup the registry every frame.
        _rarityGlow = def != null ? UI.Widgets.WidgetHelpers.GetRarityColor(def.Rarity) : null;
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
        TickSmoke(deltaTime);

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

        // Drops thrown by the player keep their pickup/magnet locked until the
        // player walks past MagnetRange at least once. This lets the player drop
        // items, walk around them, and only re-collect intentionally.
        if (DroppedByPlayer && !_magnetArmed)
        {
            if (dist > MagnetRange) _magnetArmed = true;
            return;
        }

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

            int remaining = inventory.TryAdd(_itemId, Quantity, Quality);
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
    /// Manual pickup triggered by E-key — bypasses the magnet-armed gate so the
    /// player can re-collect a just-dropped item without having to walk away first.
    /// Returns true on successful pickup. Currency is added straight to gold.
    /// </summary>
    public bool TryManualPickup(Vector2 playerPos, InventoryManager inventory)
    {
        if (IsCollected) return false;
        const float ManualPickupRange = 24f;
        if (Vector2.Distance(Position, playerPos) > ManualPickupRange) return false;

        var def = ItemRegistry.Get(_itemId);
        if (def?.Type == ItemType.Currency)
        {
            inventory.AddGold(Quantity);
            IsCollected = true;
            return true;
        }

        int remaining = inventory.TryAdd(_itemId, Quantity, Quality);
        if (remaining == 0)
        {
            IsCollected = true;
            return true;
        }
        if (remaining < Quantity)
        {
            Quantity = remaining;
            return true;
        }
        return false;
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

        // Rarity smoke — soft particles rising from the item base in the rarity color
        // (replaces the old static silhouette halo for a PoE/Diablo-style wisp feel).
        DrawSmoke(spriteBatch);

        spriteBatch.Draw(_spriteTexture, destRect, _spriteRect, Color.White);
    }
}
