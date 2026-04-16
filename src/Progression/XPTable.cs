using System;
using System.Collections.Generic;

namespace stardew_medieval_v3.Progression;

/// <summary>
/// Exponential XP curve and enemy XP value lookup.
/// Level 1 requires 50 XP; each subsequent level scales by 1.22x.
/// Level 100+ is capped at int.MaxValue (effectively unreachable).
/// </summary>
public static class XPTable
{
    /// <summary>
    /// Returns the total XP required to advance from the given level to the next.
    /// Formula: floor(50 * 1.22^(level-1)). Clamped: level &lt; 1 returns 50; level &gt;= 100 returns int.MaxValue.
    /// </summary>
    public static int XPToNextLevel(int level)
    {
        if (level < 1) return 50;
        if (level >= 100) return int.MaxValue;
        return (int)Math.Floor(50.0 * Math.Pow(1.22, level - 1));
    }

    /// <summary>
    /// XP awarded per enemy type on kill. Keys match EnemyData.Id values.
    /// </summary>
    public static readonly Dictionary<string, int> XPPerEnemy = new()
    {
        { "Skeleton", 10 },
        { "DarkMage", 15 },
        { "Golem", 25 },
        { "SkeletonKing", 150 }
    };
}
