using System;
using Microsoft.Xna.Framework;

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
/// Per D-19: distance-based detection, chase, attack, and return behaviors.
/// </summary>
public class EnemyAI
{
    /// <summary>Current AI state.</summary>
    public EnemyState State { get; private set; } = EnemyState.Idle;

    /// <summary>Original spawn position for return behavior.</summary>
    public Vector2 SpawnPosition { get; }

    private float _attackTimer;
    private bool _attackReady;

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
    }

    /// <summary>
    /// Update FSM state transitions and attack timing.
    /// Per D-19: Idle->Chase on detection, Chase->Attack in range, Attack->Chase if out of range,
    /// Chase->Return if player leaves 1.5x detection range, Return->Idle at spawn.
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    /// <param name="enemyPos">Current enemy world position.</param>
    /// <param name="playerPos">Current player world position.</param>
    /// <param name="data">Enemy type data for range/cooldown values.</param>
    public void Update(float deltaTime, Vector2 enemyPos, Vector2 playerPos, EnemyData data)
    {
        float distToPlayer = Vector2.Distance(enemyPos, playerPos);
        float distToSpawn = Vector2.Distance(enemyPos, SpawnPosition);

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
                    _attackTimer = 0f; // Attack immediately on first entering Attack state
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
                    // Re-aggro if player comes close while returning
                    State = EnemyState.Chase;
                }
                break;
        }
    }

    /// <summary>
    /// Get the movement direction vector based on current AI state.
    /// For ranged enemies in Attack state, maintains optimal distance (kiting behavior).
    /// </summary>
    /// <param name="enemyPos">Current enemy world position.</param>
    /// <param name="playerPos">Current player world position.</param>
    /// <param name="data">Enemy type data for range checks.</param>
    /// <returns>Normalized direction vector, or Vector2.Zero if no movement needed.</returns>
    public Vector2 GetMoveDirection(Vector2 enemyPos, Vector2 playerPos, EnemyData data)
    {
        Vector2 direction;

        switch (State)
        {
            case EnemyState.Chase:
                direction = playerPos - enemyPos;
                if (direction != Vector2.Zero)
                    direction.Normalize();
                return direction;

            case EnemyState.Return:
                direction = SpawnPosition - enemyPos;
                if (direction != Vector2.Zero)
                    direction.Normalize();
                return direction;

            case EnemyState.Attack:
                if (data.IsRanged)
                {
                    float dist = Vector2.Distance(enemyPos, playerPos);
                    // Maintain optimal distance: move away if too close, approach if too far
                    if (dist < data.AttackRange * 0.6f)
                    {
                        // Too close, back away
                        direction = enemyPos - playerPos;
                        if (direction != Vector2.Zero)
                            direction.Normalize();
                        return direction;
                    }
                    else if (dist > data.AttackRange)
                    {
                        // Too far, approach
                        direction = playerPos - enemyPos;
                        if (direction != Vector2.Zero)
                            direction.Normalize();
                        return direction;
                    }
                }
                // Melee attackers or ranged at optimal distance: stand still
                return Vector2.Zero;

            default: // Idle
                return Vector2.Zero;
        }
    }
}
