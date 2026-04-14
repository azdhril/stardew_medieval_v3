using System.IO;
using System.Reflection;
using System.Text.Json;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.World;

namespace StardewMedieval.Tests.Save;

/// <summary>
/// Tests for save schema migration v7 -> v8 (DungeonState defaults) and v8 roundtrip.
/// </summary>
public class SaveV7ToV8MigrationTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void V7Save_LoadsWithDefaultDungeonState()
    {
        // Simulate a v7 save JSON missing the Dungeon field entirely.
        string v7Json = "{ \"SaveVersion\": 7, \"DayNumber\": 5, \"Gold\": 100 }";
        var state = JsonSerializer.Deserialize<GameState>(v7Json);
        Assert.NotNull(state);

        // Invoke private MigrateIfNeeded via reflection (mirrors load path).
        var method = typeof(SaveManager).GetMethod("MigrateIfNeeded",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        method!.Invoke(null, new object?[] { state });

        Assert.Equal(8, state!.SaveVersion);
        Assert.NotNull(state.Dungeon);
        Assert.Empty(state.Dungeon.ClearedRooms);
        Assert.Empty(state.Dungeon.OpenedChestIds);
        Assert.False(state.Dungeon.BossDefeated);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void V8Save_RoundtripsDungeonState()
    {
        var original = new GameState
        {
            SaveVersion = 8,
            DayNumber = 3,
            Dungeon = new DungeonStateSnapshot
            {
                ClearedRooms = new() { "r1", "r2" },
                OpenedChestIds = new() { "dungeon_r3a_chest" },
                BossDefeated = true,
                RunSeed = 12345,
                IsRunActive = true,
                ChestContents = new()
                {
                    new DungeonChestContentSnapshot
                    {
                        ChestId = "dungeon_r3a_chest",
                        Items = new() { "Health_Potion", "Iron_Sword" },
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(restored);
        Assert.Equal(8, restored!.SaveVersion);
        Assert.Equal(2, restored.Dungeon.ClearedRooms.Count);
        Assert.Contains("r1", restored.Dungeon.ClearedRooms);
        Assert.Contains("r2", restored.Dungeon.ClearedRooms);
        Assert.True(restored.Dungeon.BossDefeated);
        Assert.Equal(12345, restored.Dungeon.RunSeed);
        Assert.Single(restored.Dungeon.ChestContents);
        Assert.Equal("dungeon_r3a_chest", restored.Dungeon.ChestContents[0].ChestId);
        Assert.Equal(2, restored.Dungeon.ChestContents[0].Items.Count);
    }
}
