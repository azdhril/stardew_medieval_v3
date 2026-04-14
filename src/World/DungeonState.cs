using System;
using System.Collections.Generic;

namespace stardew_medieval_v3.World;

/// <summary>
/// Run-scoped dungeon state: cleared rooms, opened chests, deterministic loot RNG seed,
/// boss-defeat flag, and frozen chest contents (rolled once per run for D-10 idempotency).
/// Lives as a singleton on ServiceContainer.Dungeon across scene transitions.
/// </summary>
public class DungeonState
{
    /// <summary>Set of room ids cleared this run.</summary>
    public HashSet<string> ClearedRooms { get; } = new();

    /// <summary>Set of chest ids opened this run (so re-entry shows opened sprite).</summary>
    public HashSet<string> OpenedChestIds { get; } = new();

    /// <summary>Per-chest contents rolled once on BeginRun and persisted across re-entry.</summary>
    public Dictionary<string, List<string>> ChestContents { get; } = new();

    /// <summary>True once the boss has been defeated this run.</summary>
    public bool BossDefeated { get; set; }

    /// <summary>Deterministic seed for this run's loot rolls.</summary>
    public int RunSeed { get; set; }

    /// <summary>True between BeginRun and EndRun. False at boot and after death/clear.</summary>
    public bool IsRunActive { get; private set; }

    /// <summary>
    /// Reset all per-run flags and assign a new RunSeed. Called on dungeon entry,
    /// on death, and defensively if DungeonScene loads with no active run.
    /// </summary>
    public void BeginRun()
    {
        ClearedRooms.Clear();
        OpenedChestIds.Clear();
        ChestContents.Clear();
        BossDefeated = false;
        RunSeed = new Random().Next();
        IsRunActive = true;
        Console.WriteLine($"[DungeonState] Run started seed={RunSeed}");
    }

    /// <summary>End the active run (called on boss kill / leave dungeon).</summary>
    public void EndRun()
    {
        IsRunActive = false;
        Console.WriteLine("[DungeonState] Run ended");
    }

    /// <summary>True if the room id has been cleared this run.</summary>
    public bool IsCleared(string roomId) => ClearedRooms.Contains(roomId);

    /// <summary>Mark a room as cleared. Idempotent.</summary>
    public void MarkCleared(string roomId)
    {
        if (ClearedRooms.Add(roomId))
            Console.WriteLine($"[DungeonState] Room {roomId} marked cleared");
    }

    /// <summary>True if the chest id has been opened this run.</summary>
    public bool IsChestOpened(string chestId) => OpenedChestIds.Contains(chestId);

    /// <summary>Mark a chest as opened. Idempotent.</summary>
    public void MarkChestOpened(string chestId)
    {
        if (OpenedChestIds.Add(chestId))
            Console.WriteLine($"[DungeonState] Chest {chestId} marked opened");
    }

    /// <summary>Build a serializable snapshot of this run's state.</summary>
    public DungeonStateSnapshot ToSnapshot()
    {
        return new DungeonStateSnapshot
        {
            ClearedRooms = new List<string>(ClearedRooms),
            OpenedChestIds = new List<string>(OpenedChestIds),
            BossDefeated = BossDefeated,
            RunSeed = RunSeed,
            IsRunActive = IsRunActive,
            ChestContents = SerializeChestContents(),
        };
    }

    /// <summary>Restore run state from a previously persisted snapshot.</summary>
    public void LoadFromSnapshot(DungeonStateSnapshot snapshot)
    {
        ClearedRooms.Clear();
        if (snapshot.ClearedRooms != null)
            foreach (var id in snapshot.ClearedRooms) ClearedRooms.Add(id);

        OpenedChestIds.Clear();
        if (snapshot.OpenedChestIds != null)
            foreach (var id in snapshot.OpenedChestIds) OpenedChestIds.Add(id);

        ChestContents.Clear();
        if (snapshot.ChestContents != null)
            foreach (var entry in snapshot.ChestContents)
                ChestContents[entry.ChestId] = new List<string>(entry.Items ?? new List<string>());

        BossDefeated = snapshot.BossDefeated;
        RunSeed = snapshot.RunSeed;
        IsRunActive = snapshot.IsRunActive;
    }

    private List<DungeonChestContentSnapshot> SerializeChestContents()
    {
        var list = new List<DungeonChestContentSnapshot>(ChestContents.Count);
        foreach (var kvp in ChestContents)
        {
            list.Add(new DungeonChestContentSnapshot
            {
                ChestId = kvp.Key,
                Items = new List<string>(kvp.Value),
            });
        }
        return list;
    }
}

/// <summary>
/// Plain-data DTO for DungeonState JSON serialization. Mirrors the runtime
/// fields with JSON-friendly types (Lists instead of HashSet/Dictionary).
/// </summary>
public class DungeonStateSnapshot
{
    public List<string> ClearedRooms { get; set; } = new();
    public List<string> OpenedChestIds { get; set; } = new();
    public List<DungeonChestContentSnapshot> ChestContents { get; set; } = new();
    public bool BossDefeated { get; set; }
    public int RunSeed { get; set; }
    public bool IsRunActive { get; set; }
}

/// <summary>Per-chest serialized contents: chest id and the list of item ids it holds.</summary>
public class DungeonChestContentSnapshot
{
    public string ChestId { get; set; } = "";
    public List<string> Items { get; set; } = new();
}
