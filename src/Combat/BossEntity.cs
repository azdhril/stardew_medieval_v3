using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Skeleton King boss entity. Extends EnemyEntity with telegraphed slash attacks,
/// minion summoning at HP thresholds (70% and 40%), and unique first-kill loot.
/// Rendered as a large (32x32) red rectangle with red flash telegraph before attacks.
/// </summary>
public class BossEntity : EnemyEntity
{
    /// <summary>Boss EnemyData definition: large, slow, high HP, high damage.</summary>
    public static readonly EnemyData BossData = new EnemyData(
        Id: "SkeletonKing",
        Name: "Skeleton King",
        PlaceholderColor: Color.Red,
        Width: 32,
        Height: 32,
        MaxHP: 300f,
        MoveSpeed: 40f,
        DetectionRange: 200f,
        AttackRange: 40f,
        AttackDamage: 25f,
        AttackCooldown: 3.0f,
        KnockbackResistance: 0.5f,
        IsRanged: false,
        ProjectileSpeed: 0f,
        Loot: new LootTable(new List<LootDrop>()) // Boss uses custom loot logic
    );

    // Summon phase tracking: phase 1 at 70% HP, phase 2 at 40% HP
    private int _summonPhase;
    private const float SummonThreshold1 = 0.70f; // 210 HP
    private const float SummonThreshold2 = 0.40f; // 120 HP

    // Telegraphed attack: 1s wind-up before slash
    private float _windUpTimer;
    private bool _isWindingUp;
    private bool _bossSlashReady;
    private const float WindUpDuration = 1.0f;

    // Visual telegraph: flash timer for red pulsing during wind-up
    private float _telegraphFlashTimer;
    private const float TelegraphFlashInterval = 0.1f;

    // Last known player position for directional slash hitbox
    private Vector2 _lastPlayerPos;

    /// <summary>True while the boss is winding up a telegraphed slash attack.</summary>
    public bool IsWindingUp => _isWindingUp;

    /// <summary>True when the boss slash has completed wind-up and is ready to deal damage.</summary>
    public bool IsBossSlashReady
    {
        get
        {
            if (_bossSlashReady)
            {
                _bossSlashReady = false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Create a new Skeleton King boss at the given spawn position.
    /// </summary>
    /// <param name="spawnPosition">World position for the boss spawn.</param>
    public BossEntity(Vector2 spawnPosition) : base(BossData, spawnPosition)
    {
    }

    /// <summary>
    /// Update boss AI, wind-up telegraph, movement, and base entity logic.
    /// Overrides normal melee attack flow: instead of instant attack, starts wind-up.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    /// <param name="playerPos">Current player world position.</param>
    /// <param name="projectiles">Projectile manager (unused by boss, melee only).</param>
    public new void Update(float deltaTime, Vector2 playerPos, ProjectileManager projectiles, TileMap? map = null)
    {
        if (!IsAlive) return;

        _lastPlayerPos = playerPos;

        // Update AI state machine
        AI.Update(deltaTime, Position, playerPos, Data);

        // Move based on AI direction (stop moving during wind-up)
        if (!_isWindingUp)
        {
            Vector2 moveDir = AI.GetMoveDirection(Position, playerPos, Data);
            if (moveDir != Vector2.Zero)
            {
                var delta = moveDir * Data.MoveSpeed * deltaTime;
                if (map != null)
                    ApplyCollisionMove(delta, map);
                else
                    Position += delta;
            }
        }

        // Update knockback and flash from Entity base
        UpdateKnockback(deltaTime);
        UpdateFlash(deltaTime);

        // Handle telegraphed attack: intercept the normal melee attack ready
        if (AI.IsAttackReady && !_isWindingUp)
        {
            // Start wind-up instead of immediate attack
            _isWindingUp = true;
            _windUpTimer = WindUpDuration;
            _telegraphFlashTimer = 0f;
            Console.WriteLine("[BossEntity] Skeleton King winding up slash attack!");
        }

        // Wind-up countdown
        if (_isWindingUp)
        {
            _windUpTimer -= deltaTime;
            _telegraphFlashTimer += deltaTime;

            if (_windUpTimer <= 0)
            {
                _isWindingUp = false;
                _bossSlashReady = true;
                Console.WriteLine("[BossEntity] Skeleton King slash attack ready!");
            }
        }
    }

    /// <summary>
    /// Check if the boss has crossed an HP threshold that triggers minion summoning.
    /// Phase 1: at 70% HP (210), spawns 2 Skeleton minions.
    /// Phase 2: at 40% HP (120), spawns 2 more Skeleton minions.
    /// </summary>
    /// <returns>List of spawned skeleton minions, or null if no threshold crossed.</returns>
    public List<EnemyEntity>? CheckSummonPhase()
    {
        if (!IsAlive) return null;

        float hpPercent = HP / MaxHP;

        if (_summonPhase < 1 && hpPercent <= SummonThreshold1)
        {
            _summonPhase = 1;
            Console.WriteLine("[BossEntity] Skeleton King enters phase 1! Summoning minions!");
            return SpawnMinions();
        }

        if (_summonPhase < 2 && hpPercent <= SummonThreshold2)
        {
            _summonPhase = 2;
            Console.WriteLine("[BossEntity] Skeleton King enters phase 2! Summoning more minions!");
            return SpawnMinions();
        }

        return null;
    }

    /// <summary>
    /// Get the boss slash hitbox: a wide rectangle (64x32) in front of the boss.
    /// Larger than normal melee to represent a wide sweeping attack.
    /// </summary>
    /// <returns>Rectangle representing the slash damage area.</returns>
    public Rectangle GetBossSlashHitbox()
    {
        int slashWidth = 64;
        int slashDepth = 32;

        // Determine 4-dir facing from player offset (dominant axis wins)
        Vector2 diff = _lastPlayerPos - Position;
        Direction facing;
        if (MathF.Abs(diff.X) > MathF.Abs(diff.Y))
            facing = diff.X >= 0 ? Direction.Right : Direction.Left;
        else
            facing = diff.Y >= 0 ? Direction.Down : Direction.Up;

        int halfW = Data.Width / 2;
        int halfH = Data.Height / 2;

        return facing switch
        {
            Direction.Up => new Rectangle(
                (int)Position.X - slashWidth / 2,
                (int)Position.Y - halfH - slashDepth,
                slashWidth, slashDepth),
            Direction.Down => new Rectangle(
                (int)Position.X - slashWidth / 2,
                (int)Position.Y + halfH,
                slashWidth, slashDepth),
            Direction.Left => new Rectangle(
                (int)Position.X - halfW - slashDepth,
                (int)Position.Y - slashWidth / 2,
                slashDepth, slashWidth),
            Direction.Right => new Rectangle(
                (int)Position.X + halfW,
                (int)Position.Y - slashWidth / 2,
                slashDepth, slashWidth),
            _ => Rectangle.Empty,
        };
    }

    /// <summary>
    /// Get boss-specific loot. First kill guarantees Flame_Blade; subsequent kills have 10% chance.
    /// </summary>
    /// <param name="bossAlreadyKilled">True if boss has been killed before (from GameState.BossKilled).</param>
    /// <returns>List of (itemId, quantity) loot drops.</returns>
    public List<(string itemId, int quantity)> GetBossLoot(bool bossAlreadyKilled)
    {
        var drops = new List<(string itemId, int quantity)>();

        // Gold proxy: 5x Stone_Chunk (gold system is Phase 6)
        drops.Add(("Stone_Chunk", 5));

        if (!bossAlreadyKilled)
        {
            // First kill: guaranteed Flame_Blade
            drops.Add(("Flame_Blade", 1));
            Console.WriteLine("[BossEntity] First kill! Dropping guaranteed Flame_Blade!");
        }
        else
        {
            // Subsequent kills: 10% chance for Flame_Blade
            var rng = new Random();
            if (rng.NextDouble() < 0.10)
            {
                drops.Add(("Flame_Blade", 1));
                Console.WriteLine("[BossEntity] Lucky drop! Flame_Blade!");
            }
        }

        return drops;
    }

    /// <summary>
    /// Draw the boss as a large red rectangle (32x32).
    /// During wind-up, flashes between Red and DarkRed every 0.1s for telegraph effect.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch in world-space.</param>
    /// <param name="pixel">1x1 white texture for drawing rectangles.</param>
    public new void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!IsAlive) return;

        Color drawColor;

        if (IsFlashing)
        {
            // Hit flash: white
            drawColor = Color.White;
        }
        else if (_isWindingUp)
        {
            // Telegraph: flash between Red and DarkRed
            int flashIndex = (int)(_telegraphFlashTimer / TelegraphFlashInterval);
            drawColor = (flashIndex % 2 == 0) ? Color.Red : Color.DarkRed;
        }
        else
        {
            drawColor = Data.PlaceholderColor; // Red
        }

        var rect = new Rectangle(
            (int)Position.X - Data.Width / 2,
            (int)Position.Y - Data.Height / 2,
            Data.Width,
            Data.Height);

        spriteBatch.Draw(pixel, rect, drawColor);
    }

    /// <summary>
    /// Spawn 2 Skeleton minions near the boss position.
    /// Offset by +/- 40px horizontally from boss center.
    /// </summary>
    /// <returns>List of 2 newly created Skeleton entities.</returns>
    private List<EnemyEntity> SpawnMinions()
    {
        var skeletonData = GetSkeletonData();
        var minions = new List<EnemyEntity>
        {
            new EnemyEntity(skeletonData, Position + new Vector2(-40, 0)),
            new EnemyEntity(skeletonData, Position + new Vector2(40, 0))
        };

        Console.WriteLine($"[BossEntity] Spawned 2 Skeleton minions at boss position");
        return minions;
    }

    /// <summary>
    /// Get the Skeleton EnemyData from the registry for minion spawning.
    /// Falls back to a minimal definition if not found.
    /// </summary>
    /// <returns>Skeleton enemy data definition.</returns>
    private static EnemyData GetSkeletonData()
    {
        var allEnemies = EnemyRegistry.GetAll();
        foreach (var data in allEnemies)
        {
            if (data.Id == "Skeleton")
                return data;
        }

        // Fallback (should not happen if EnemyRegistry is properly configured)
        return new EnemyData(
            Id: "Skeleton", Name: "Skeleton", PlaceholderColor: Color.White,
            Width: 16, Height: 16, MaxHP: 40f, MoveSpeed: 60f,
            DetectionRange: 120f, AttackRange: 24f, AttackDamage: 10f,
            AttackCooldown: 1.0f, KnockbackResistance: 0f, IsRanged: false,
            ProjectileSpeed: 0f, Loot: new LootTable(new List<LootDrop>
            {
                new LootDrop("Bones", 0.8f)
            })
        );
    }
}
