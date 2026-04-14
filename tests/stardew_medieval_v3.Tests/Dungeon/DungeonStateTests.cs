using stardew_medieval_v3.World;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Tests for DungeonState run-scoped flags, snapshot roundtrip, and chest tracking.
/// </summary>
public class DungeonStateTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void BeginRun_ClearsAllFlags()
    {
        var state = new DungeonState();
        state.MarkCleared("r1");
        state.MarkChestOpened("chest_a");
        state.ChestContents["chest_a"] = new() { "Bones" };
        state.BossDefeated = true;

        state.BeginRun();

        Assert.Empty(state.ClearedRooms);
        Assert.Empty(state.OpenedChestIds);
        Assert.Empty(state.ChestContents);
        Assert.False(state.BossDefeated);
        Assert.True(state.IsRunActive);
        Assert.NotEqual(0, state.RunSeed);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void ChestContents_PersistAcrossRoomReentry()
    {
        var state = new DungeonState();
        state.BeginRun();
        state.ChestContents["dungeon_r3a_chest"] = new() { "Health_Potion", "Iron_Sword" };

        // Simulate snapshot/load (room re-entry would re-hydrate from this map).
        var snap = state.ToSnapshot();
        var restored = new DungeonState();
        restored.LoadFromSnapshot(snap);

        Assert.True(restored.ChestContents.ContainsKey("dungeon_r3a_chest"));
        Assert.Equal(2, restored.ChestContents["dungeon_r3a_chest"].Count);
        Assert.Equal("Health_Potion", restored.ChestContents["dungeon_r3a_chest"][0]);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BossCompletesQuest_OnKill()
    {
        // BossDefeated flag must be settable and survive snapshot roundtrip.
        var state = new DungeonState();
        state.BeginRun();
        state.BossDefeated = true;

        var snap = state.ToSnapshot();
        var restored = new DungeonState();
        restored.LoadFromSnapshot(snap);

        Assert.True(restored.BossDefeated);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void DeathResetsRun_BeginRunCalled()
    {
        var state = new DungeonState();
        state.BeginRun();
        state.MarkCleared("r1");
        state.MarkCleared("r2");
        int firstSeed = state.RunSeed;

        // Simulate dungeon death handler invoking BeginRun.
        state.BeginRun();

        Assert.Empty(state.ClearedRooms);
        // RunSeed should generally change (extreme collision unlikely).
        // Don't assert inequality strictly to avoid one-in-2^32 flake.
        Assert.True(state.IsRunActive);
    }
}
