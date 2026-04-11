using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Manages enemy spawn positions and day-advance respawning.
/// Per D-20: enemies respawn when a new day starts.
/// Spawn positions are hardcoded away from the farm zone (top-left area).
/// </summary>
public class EnemySpawner
{
    /// <summary>
    /// Predefined spawn points: (EnemyId, Position).
    /// Placed in the open area away from the farm zone.
    /// </summary>
    private static readonly (string enemyId, Vector2 position)[] SpawnPoints = new[]
    {
        ("Skeleton", new Vector2(400, 200)),
        ("Skeleton", new Vector2(500, 350)),
        ("DarkMage", new Vector2(600, 250)),
        ("Golem", new Vector2(450, 400))
    };

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
    /// Spawn all enemies at their predefined positions.
    /// </summary>
    /// <returns>List of newly created enemy entities.</returns>
    public List<EnemyEntity> SpawnAll()
    {
        var enemies = new List<EnemyEntity>();

        foreach (var (enemyId, position) in SpawnPoints)
        {
            if (_enemyTypes.TryGetValue(enemyId, out var data))
            {
                var enemy = new EnemyEntity(data, position);
                enemies.Add(enemy);
                Console.WriteLine($"[EnemySpawner] Spawned {data.Name} at ({position.X}, {position.Y})");
            }
            else
            {
                Console.WriteLine($"[EnemySpawner] WARNING: Unknown enemy type '{enemyId}'");
            }
        }

        return enemies;
    }

    /// <summary>
    /// Clear existing enemies and spawn fresh ones. Called on day advance.
    /// Per D-20: enemies respawn when a new day starts.
    /// </summary>
    /// <param name="enemies">Enemy list to clear and refill.</param>
    public void Respawn(List<EnemyEntity> enemies)
    {
        enemies.Clear();
        enemies.AddRange(SpawnAll());
        Console.WriteLine($"[EnemySpawner] Respawned {enemies.Count} enemies for new day");
    }
}
