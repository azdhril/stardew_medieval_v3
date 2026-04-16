using System.Collections.Generic;
using System.Linq;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.World;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Deterministic-seed tests for <see cref="DungeonChestSeeder"/>. Two runs with
/// the same RunSeed must produce identical ChestContents. Re-entering a room is
/// read-only against the Dict so the Take-All + re-enter flow is idempotent.
/// </summary>
public class LootRollTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void Seed_IsDeterministic_ForSameSeed()
    {
        var a = new DungeonState { RunSeed = 12345 };
        var b = new DungeonState { RunSeed = 12345 };

        DungeonChestSeeder.Seed(a);
        DungeonChestSeeder.Seed(b);

        Assert.Equal(a.ChestContents.Count, b.ChestContents.Count);
        foreach (var kvp in a.ChestContents)
        {
            Assert.True(b.ChestContents.ContainsKey(kvp.Key), $"Chest {kvp.Key} missing in run B");
            Assert.Equal(kvp.Value, b.ChestContents[kvp.Key]);
        }
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Seed_DiffersForDifferentSeeds()
    {
        var a = new DungeonState { RunSeed = 1 };
        var b = new DungeonState { RunSeed = 999_999 };

        DungeonChestSeeder.Seed(a);
        DungeonChestSeeder.Seed(b);

        // Very unlikely both seeds produce identical rolls across all chests.
        bool anyDifference = false;
        foreach (var kvp in a.ChestContents)
        {
            if (!b.ChestContents.TryGetValue(kvp.Key, out var other) ||
                !kvp.Value.SequenceEqual(other))
            {
                anyDifference = true;
                break;
            }
        }
        Assert.True(anyDifference, "Different seeds must produce at least one differing chest");
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Seed_PopulatesAllRegisteredChests()
    {
        var state = new DungeonState { RunSeed = 42 };

        DungeonChestSeeder.Seed(state);

        // Every chest declared in the registry must appear as a key. The value
        // list may be empty if no drop passed the Roll threshold -- that's okay.
        foreach (var room in DungeonRegistry.GetAll())
        {
            foreach (var (chestId, _, _) in room.Chests)
            {
                Assert.True(state.ChestContents.ContainsKey(chestId),
                    $"ChestContents missing entry for {chestId}");
            }
        }
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Seed_SkipsOpenedChests_WritesEmptyContents()
    {
        var state = new DungeonState { RunSeed = 12345 };
        // Pick a real chest id from the registry so the loop hits it.
        state.OpenedChestIds.Add("dungeon_r3a_chest");

        DungeonChestSeeder.Seed(state);

        Assert.True(state.ChestContents.ContainsKey("dungeon_r3a_chest"));
        Assert.Empty(state.ChestContents["dungeon_r3a_chest"]);

        // Another registered chest (not opened) should have loot rolled.
        Assert.True(state.ChestContents.ContainsKey("dungeon_r4a_chest"));
        // At least one drop -- Health_Potion is weight 1.0 so it always lands.
        Assert.NotEmpty(state.ChestContents["dungeon_r4a_chest"]);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BeginRun_PreservesOpenedChestIds()
    {
        var state = new DungeonState();
        state.OpenedChestIds.Add("foo");
        state.OpenedChestIds.Add("bar");
        state.ClearedRooms.Add("r1");
        state.ChestContents["foo"] = new List<string> { "Health_Potion" };

        state.BeginRun();

        Assert.Contains("foo", state.OpenedChestIds);
        Assert.Contains("bar", state.OpenedChestIds);
        Assert.Empty(state.ClearedRooms);
        Assert.Empty(state.ChestContents);
        Assert.NotEqual(0, state.RunSeed);
        Assert.True(state.IsRunActive);
    }
}
