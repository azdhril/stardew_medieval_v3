using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Data-driven enemy spawner. Accepts an injected spawn list (enemyId, position)
/// instead of hardcoding world coordinates, so DungeonScene can drive its own
/// per-room spawn config from <see cref="stardew_medieval_v3.Data.DungeonRegistry"/>.
/// </summary>
public class EnemySpawner
{
    private readonly Dictionary<string, EnemyData> _enemyTypes;

    /// <summary>
    /// Create a new EnemySpawner. Loads all enemy type definitions from EnemyRegistry.
    /// </summary>
    public EnemySpawner()
    {
        _enemyTypes = new Dictionary<string, EnemyData>();
        foreach (var data in EnemyRegistry.GetAll())
        {
            _enemyTypes[data.Id] = data;
        }
    }

    /// <summary>
    /// Spawn enemies at the supplied points and append them to <paramref name="target"/>.
    /// Unknown enemy ids are logged and skipped.
    /// </summary>
    /// <param name="points">Spawn entries (enemyId, world position).</param>
    /// <param name="target">List that will receive the new enemy entities.</param>
    public void SpawnAll(IEnumerable<(string id, Vector2 pos)> points, List<EnemyEntity> target)
    {
        foreach (var (enemyId, position) in points)
        {
            if (_enemyTypes.TryGetValue(enemyId, out var data))
            {
                var enemy = new EnemyEntity(data, position);
                target.Add(enemy);
                Console.WriteLine($"[EnemySpawner] Spawned {data.Name} at ({position.X}, {position.Y})");
            }
            else
            {
                Console.WriteLine($"[EnemySpawner] WARNING: Unknown enemy type '{enemyId}'");
            }
        }
    }

    /// <summary>
    /// Clear the supplied enemy list and respawn fresh ones from the supplied points.
    /// Used for FarmScene's day-advance respawn.
    /// </summary>
    /// <param name="points">Spawn entries to repopulate from.</param>
    /// <param name="enemies">Enemy list to clear and refill.</param>
    public void Respawn(IEnumerable<(string id, Vector2 pos)> points, List<EnemyEntity> enemies)
    {
        enemies.Clear();
        SpawnAll(points, enemies);
        Console.WriteLine($"[EnemySpawner] Respawned {enemies.Count} enemies");
    }

    /// <summary>
    /// Spawn the Skeleton King boss at the supplied position.
    /// </summary>
    /// <param name="position">World position for the boss spawn.</param>
    /// <returns>New BossEntity at the given position.</returns>
    public BossEntity SpawnBoss(Vector2 position)
    {
        var boss = new BossEntity(position);
        Console.WriteLine($"[EnemySpawner] Spawned Skeleton King boss at ({position.X}, {position.Y})");
        return boss;
    }
}
