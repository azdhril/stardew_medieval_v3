using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static configuration for a single dungeon room: TMX path, spawn list,
/// chest list, and named exit map. Mutable lists are populated once at
/// registry init time.
/// </summary>
public class DungeonRoomData
{
    /// <summary>Stable room id, e.g. "r1", "boss".</summary>
    public string Id { get; init; } = "";

    /// <summary>Path to the TMX map for this room.</summary>
    public string TmxPath { get; init; } = "";

    /// <summary>Enemy spawn entries (enemyId, world position).</summary>
    public List<(string enemyId, Vector2 position)> Spawns { get; init; } = new();

    /// <summary>Chest entries seeded into this room (chestId, tile, sprite variant).</summary>
    public List<(string chestId, Point tile, string spriteId)> Chests { get; init; } = new();

    /// <summary>Map of trigger zone name to exit configuration.</summary>
    public Dictionary<string, ExitData> Exits { get; init; } = new();

    /// <summary>True if exits remain locked until the room is cleared (D-04/D-05).</summary>
    public bool HasGatedExit { get; init; }

    /// <summary>True if this room is the boss arena (no respawn on re-entry once boss is dead).</summary>
    public bool IsBossRoom { get; init; }

    /// <summary>True if this room is an optional side room (no clear-gate entry).</summary>
    public bool IsOptional { get; init; }
}

/// <summary>
/// Configuration for a single dungeon exit (door / trigger zone).
/// Either references another room id (intra-dungeon) or leaves the dungeon entirely.
/// </summary>
/// <param name="RoomId">Target room id when staying in the dungeon.</param>
/// <param name="RequiresCleared">If true, exit blocks until the source room is marked cleared.</param>
/// <param name="LeaveDungeon">If true, this exit returns the player to TargetScene.</param>
/// <param name="TargetScene">"farm" or "village" (only used when LeaveDungeon).</param>
/// <param name="TargetTrigger">Spawn anchor name in the target scene (only used when LeaveDungeon).</param>
public record ExitData(
    string? RoomId = null,
    bool RequiresCleared = false,
    bool LeaveDungeon = false,
    string? TargetScene = null,
    string? TargetTrigger = null);
