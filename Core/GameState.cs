using System.Collections.Generic;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Core;

/// <summary>
/// All persistent game data, serialized to JSON.
/// </summary>
public class GameState
{
    // === Existing (v2) ===
    public int SaveVersion { get; set; } = 3;
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
    public string? WeaponId { get; set; }
    public string? ArmorId { get; set; }
    public List<string?> HotbarSlots { get; set; } = new(new string?[8]);
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
