using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Enemy behavior states for the finite state machine.
/// Per D-19: Idle -> Chase -> Attack -> Return cycle.
/// </summary>
public enum EnemyState
{
    /// <summary>Enemy is idle at spawn, waiting for player detection.</summary>
    Idle,
    /// <summary>Enemy is chasing the player after detection.</summary>
    Chase,
    /// <summary>Enemy is in attack range and actively attacking.</summary>
    Attack,
    /// <summary>Enemy is returning to spawn position after losing the player.</summary>
    Return
}

/// <summary>
/// Finite State Machine AI for enemies. Handles state transitions based on
/// distance to player, attack cooldowns, and spawn position tracking.
/// Uses A* pathfinding (when available) to navigate around obstacles instead
/// of walking straight into walls. Ranged enemies use smart-flee scoring
/// to avoid backing into corners and strafe-kite at optimal distance.
/// </summary>
public class EnemyAI
{
    /// <summary>Current AI state.</summary>
    public EnemyState State { get; private set; } = EnemyState.Idle;

    /// <summary>Original spawn position for return behavior.</summary>
    public Vector2 SpawnPosition { get; }

    private float _attackTimer;
    private bool _attackReady;

    // --- A* path following ---
    private List<Point>? _path;
    private int _pathIndex;
    private float _pathTimer;
    private Point _lastGoalTile;
    private const float PathRecalcInterval = 0.35f;

    // Stagger so all enemies don't recalc on the same frame
    private static int _staggerSeed;

    // --- Ranged strafe-kite ---
    private float _strafeSign = 1f;
    private float _strafeFlipTimer;
    private const float StrafeFlipInterval = 2.0f;

    /// <summary>True when the attack cooldown has elapsed and an attack should fire.</summary>
    public bool IsAttackReady
    {
        get
        {
            if (_attackReady)
            {
                _attackReady = false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Create a new EnemyAI with the given spawn position.
    /// </summary>
    /// <param name="spawnPosition">World position where the enemy was spawned.</param>
    public EnemyAI(Vector2 spawnPosition)
    {
        SpawnPosition = spawnPosition;
        // Spread initial path timers across enemies so they don't all recalc
        // on the same frame (10 slots * 0.035s = 0.35s spread)
        _pathTimer = (_staggerSeed++ % 10) * 0.035f;
        // Randomize initial strafe direction
        _strafeSign = (_staggerSeed % 2 == 0) ? 1f : -1f;
        _strafeFlipTimer = StrafeFlipInterval * (0.5f + (_staggerSeed % 5) * 0.1f);
    }

    /// <summary>
    /// Update FSM state transitions, attack timing, and path recalc timer.
    /// </summary>
    public void Update(float deltaTime, Vector2 enemyPos, Vector2 playerPos, EnemyData data)
    {
        float distToPlayer = Vector2.Distance(enemyPos, playerPos);
        float distToSpawn = Vector2.Distance(enemyPos, SpawnPosition);

        // Tick path recalculation timer
        _pathTimer -= deltaTime;

        // Tick strafe flip timer for ranged kiting
        _strafeFlipTimer -= deltaTime;
        if (_strafeFlipTimer <= 0)
        {
            _strafeSign = -_strafeSign;
            _strafeFlipTimer = StrafeFlipInterval;
        }

        var prevState = State;

        switch (State)
        {
            case EnemyState.Idle:
                if (distToPlayer < data.DetectionRange)
                {
                    State = EnemyState.Chase;
                    Console.WriteLine($"[EnemyAI] {data.Id} detected player at {distToPlayer:F0}px");
                }
                break;

            case EnemyState.Chase:
                if (distToPlayer < data.AttackRange)
                {
                    State = EnemyState.Attack;
                    _attackTimer = 0f;
                }
                else if (distToPlayer > data.DetectionRange * 1.5f)
                {
                    State = EnemyState.Return;
                    Console.WriteLine($"[EnemyAI] {data.Id} lost player, returning to spawn");
                }
                break;

            case EnemyState.Attack:
                if (distToPlayer > data.AttackRange * 1.2f)
                {
                    State = EnemyState.Chase;
                }
                else
                {
                    _attackTimer -= deltaTime;
                    if (_attackTimer <= 0)
                    {
                        _attackReady = true;
                        _attackTimer = data.AttackCooldown;
                    }
                }
                break;

            case EnemyState.Return:
                if (distToSpawn < 4f)
                {
                    State = EnemyState.Idle;
                    Console.WriteLine($"[EnemyAI] {data.Id} returned to spawn");
                }
                else if (distToPlayer < data.DetectionRange)
                {
                    State = EnemyState.Chase;
                }
                break;
        }

        // Clear cached path on state change so the new state gets a fresh path
        if (State != prevState)
            InvalidatePath();
    }

    /// <summary>
    /// Get the movement direction vector based on current AI state.
    /// Uses A* pathfinding when a Pathfinder is available; falls back to
    /// direct line-of-sight movement otherwise.
    /// </summary>
    public Vector2 GetMoveDirection(Vector2 enemyPos, Vector2 playerPos, EnemyData data,
        Pathfinder? pathfinder = null)
    {
        switch (State)
        {
            case EnemyState.Chase:
                return GetPathDirection(enemyPos, playerPos, pathfinder);

            case EnemyState.Return:
                return GetPathDirection(enemyPos, SpawnPosition, pathfinder);

            case EnemyState.Attack:
                if (data.IsRanged)
                    return GetSmartKiteDirection(enemyPos, playerPos, data, pathfinder);
                return Vector2.Zero;

            default: // Idle
                return Vector2.Zero;
        }
    }

    /// <summary>
    /// Navigate toward target using A* path when available, with staggered
    /// recalculation and waypoint-following. Falls back to direct line.
    /// </summary>
    private Vector2 GetPathDirection(Vector2 enemyPos, Vector2 target, Pathfinder? pathfinder)
    {
        if (pathfinder == null)
            return DirectionTo(enemyPos, target);

        var startTile = WorldToTile(enemyPos);
        var goalTile = WorldToTile(target);

        // Recalculate when: timer expired, path exhausted, or goal tile changed
        bool needRecalc = _path == null
            || _pathTimer <= 0
            || _pathIndex >= _path.Count
            || goalTile != _lastGoalTile;

        if (needRecalc)
        {
            _path = pathfinder.FindPath(startTile, goalTile);
            _pathIndex = 0;
            _pathTimer = PathRecalcInterval;
            _lastGoalTile = goalTile;

            if (_path == null || _path.Count < 2)
                return DirectionTo(enemyPos, target);

            // Skip the tile we're standing on
            if (_path.Count > 1 && _path[0] == startTile)
                _pathIndex = 1;
        }

        // Follow the path waypoints
        if (_path != null && _pathIndex < _path.Count)
        {
            var wp = TileCenter(_path[_pathIndex]);
            float distToWp = Vector2.Distance(enemyPos, wp);

            // Close enough to waypoint — advance
            if (distToWp < 6f)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                    return DirectionTo(enemyPos, target);
                wp = TileCenter(_path[_pathIndex]);
            }

            return DirectionTo(enemyPos, wp);
        }

        return DirectionTo(enemyPos, target);
    }

    /// <summary>
    /// Smart kite behavior for ranged enemies in Attack state.
    /// - Too close: flee using direction scoring (avoids backing into walls)
    /// - Optimal range: strafe perpendicular to player (circle the player)
    /// - Too far: approach using A* pathfinding
    /// </summary>
    private Vector2 GetSmartKiteDirection(Vector2 enemyPos, Vector2 playerPos,
        EnemyData data, Pathfinder? pathfinder)
    {
        float dist = Vector2.Distance(enemyPos, playerPos);

        if (dist < data.AttackRange * 0.6f)
        {
            // Too close — smart flee: score 8 directions by open space + away-from-player
            return GetSmartFleeDirection(enemyPos, playerPos, pathfinder);
        }
        else if (dist > data.AttackRange)
        {
            // Too far — approach using pathfinding
            return GetPathDirection(enemyPos, playerPos, pathfinder);
        }
        else
        {
            // Optimal range — strafe perpendicular to player vector
            var toPlayer = playerPos - enemyPos;
            if (toPlayer == Vector2.Zero) return Vector2.Zero;
            toPlayer.Normalize();

            // Perpendicular (strafe) direction
            var strafe = new Vector2(-toPlayer.Y, toPlayer.X) * _strafeSign;

            // Gentle distance correction blended in
            float rangeMid = data.AttackRange * 0.8f;
            float approachWeight = (dist - rangeMid) / (data.AttackRange * 0.4f);
            approachWeight = MathHelper.Clamp(approachWeight, -0.3f, 0.3f);

            var combined = strafe + toPlayer * approachWeight;

            // Check if strafe direction is blocked — flip if so
            if (pathfinder != null)
            {
                int openness = pathfinder.ScoreDirectionOpenness(enemyPos, strafe, 2);
                if (openness == 0)
                {
                    _strafeSign = -_strafeSign;
                    strafe = new Vector2(-toPlayer.Y, toPlayer.X) * _strafeSign;
                    combined = strafe + toPlayer * approachWeight;
                }
            }

            if (combined != Vector2.Zero) combined.Normalize();
            return combined;
        }
    }

    /// <summary>
    /// Sample 8 directions and pick the one that best combines fleeing from
    /// the player with having open space ahead. Prevents ranged enemies from
    /// backing into walls when trying to escape.
    /// </summary>
    private Vector2 GetSmartFleeDirection(Vector2 enemyPos, Vector2 playerPos,
        Pathfinder? pathfinder)
    {
        var awayDir = enemyPos - playerPos;
        if (awayDir != Vector2.Zero) awayDir.Normalize();

        if (pathfinder == null)
            return awayDir;

        Vector2 bestDir = awayDir;
        float bestScore = -999f;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathF.PI / 4f;
            var candidate = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            // How aligned is this direction with "away from player"?
            float awaynessScore = Vector2.Dot(candidate, awayDir) * 10f;

            // How much open space is ahead?
            int openTiles = pathfinder.ScoreDirectionOpenness(enemyPos, candidate, 4);
            float opennessScore = openTiles * 20f;

            // Slight bonus for perpendicular movement (helps escape L-shaped corners)
            float perpScore = MathF.Abs(Vector2.Dot(candidate,
                new Vector2(-awayDir.Y, awayDir.X))) * 3f;

            float score = awaynessScore + opennessScore + perpScore;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = candidate;
            }
        }

        return bestDir;
    }

    /// <summary>Clear cached path (e.g., on state transition).</summary>
    private void InvalidatePath()
    {
        _path = null;
        _pathIndex = 0;
        _pathTimer = 0f;
    }

    private static Vector2 DirectionTo(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        if (dir != Vector2.Zero) dir.Normalize();
        return dir;
    }

    private static Point WorldToTile(Vector2 worldPos)
    {
        int ts = TileMap.TileSize;
        return new Point(
            (int)MathF.Floor(worldPos.X / ts),
            (int)MathF.Floor(worldPos.Y / ts));
    }

    private static Vector2 TileCenter(Point tile)
    {
        int ts = TileMap.TileSize;
        return new Vector2(tile.X * ts + ts / 2f, tile.Y * ts + ts / 2f);
    }
}
