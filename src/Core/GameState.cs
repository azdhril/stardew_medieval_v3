using System.Collections.Generic;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Core;

/// <summary>
/// All persistent game data, serialized to JSON.
/// </summary>
public class GameState
{
    // === Existing base save data ===
    public int SaveVersion { get; set; } = 10;
    public int DayNumber { get; set; } = 1;
    public int Season { get; set; } // 0=Spring
    public float StaminaCurrent { get; set; } = 100f;
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float GameTime { get; set; }
    public List<FarmCellSaveData> FarmCells { get; set; } = new();

    // === New (v3) per D-10 ===
    public List<ItemStack> Inventory { get; set; } = new();
    public int Gold { get; set; } = 0;
    public int XP { get; set; } = 0;
    public int Level { get; set; } = 1;
    public string CurrentScene { get; set; } = "Farm";
    public int QuestState { get; set; } = 0; // 0=None, serialized as int for JSON compat
    public string? WeaponId { get; set; } // Legacy, migrated to Equipment
    public string? ArmorId { get; set; } // Legacy, migrated to Equipment
    public Dictionary<string, string> Equipment { get; set; } = new();
    public List<string?> ConsumableRefs { get; set; } = new();
    public List<string?> HotbarSlots { get; set; } = new(new string?[8]);

    // === New (v4) per D-23: Boss tracking ===
    public bool BossKilled { get; set; } = false;

    // === New (v6): dynamic world containers ===
    public List<ChestSaveData> Chests { get; set; } = new();

    // === New (v7): dynamic harvestable resources ===
    public List<ResourceSaveData> Resources { get; set; } = new();

    // === New (v8): dungeon run state ===
    public DungeonStateSnapshot Dungeon { get; set; } = new();

    // === New (v9): progression stats ===
    public int MaxHP { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    public int BaseDamageBonus { get; set; } = 0;

    // === New (v10): per-scene position ===
    /// <summary>
    /// Per-scene player position snapshot. Populated by
    /// <see cref="GameStateSnapshot.SaveNow"/> on save and consumed by
    /// <c>GameplayScene.LoadContent</c> during boot-time restore so each scene
    /// (Farm/Village/Castle/Shop) remembers its own last-known position.
    /// Dungeon scenes are skipped — their coordinates are intra-run only.
    /// </summary>
    public Dictionary<string, ScenePosition> PositionByScene { get; set; } = new();
}

/// <summary>
/// Serializable player position for a given scene. Stored in
/// <see cref="GameState.PositionByScene"/> so each scene remembers its own spot.
/// </summary>
public class ScenePosition
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class FarmCellSaveData
{
    public int CellX { get; set; }
    public int CellY { get; set; }
    public bool IsTilled { get; set; }
    public bool IsWatered { get; set; }
    public bool HasCrop { get; set; }
    public string CropDataName { get; set; } = "";
    public int CropDayCount { get; set; }
    public bool IsWilted { get; set; }
}

public class ChestSaveData
{
    public string InstanceId { get; set; } = "";
    public string VariantId { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int Capacity { get; set; } = 12;
    public List<ItemStack?> Contents { get; set; } = new();
}

public class ResourceSaveData
{
    public string InstanceId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int HitsRemaining { get; set; }
}
