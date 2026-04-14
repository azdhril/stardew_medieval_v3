using stardew_medieval_v3.Data;
using stardew_medieval_v3.World;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Tests for the room-cleared contract that DungeonScene.OnPreUpdate relies on:
/// MarkCleared is idempotent (one-shot) and only gated rooms participate.
/// </summary>
public class RoomClearedTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void MarkCleared_IsIdempotent_AcrossMultipleCalls()
    {
        var state = new DungeonState();
        state.BeginRun();

        state.MarkCleared("r1");
        state.MarkCleared("r1");
        state.MarkCleared("r1");

        Assert.True(state.IsCleared("r1"));
        Assert.Single(state.ClearedRooms);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void MarkCleared_AfterBeginRun_ResetsClearedSet()
    {
        var state = new DungeonState();
        state.BeginRun();
        state.MarkCleared("r1");
        state.MarkCleared("r2");
        Assert.Equal(2, state.ClearedRooms.Count);

        state.BeginRun();

        Assert.False(state.IsCleared("r1"));
        Assert.False(state.IsCleared("r2"));
        Assert.Empty(state.ClearedRooms);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void NonGatedRoom_HasGatedExitIsFalse_SoCleanupSkipped()
    {
        // Contract: DungeonScene only fires room-cleared when HasGatedExit is true.
        // Optional side rooms (r3a, r4a) must NOT be gated.
        var r3a = DungeonRegistry.Get("r3a");
        var r4a = DungeonRegistry.Get("r4a");

        Assert.True(r3a.IsOptional);
        Assert.False(r3a.HasGatedExit);
        Assert.True(r4a.IsOptional);
        Assert.False(r4a.HasGatedExit);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void GatedRoom_RequiresClearedExit_IsBlockedUntilMarked()
    {
        // Contract: gated-exit lookup in DungeonScene.HandleTrigger uses
        // ExitData.RequiresCleared AND DungeonState.IsCleared(roomId).
        var r1 = DungeonRegistry.Get("r1");
        Assert.True(r1.HasGatedExit);

        // At least one r1 exit should require cleared.
        bool anyGated = false;
        foreach (var exit in r1.Exits.Values)
            if (exit.RequiresCleared) { anyGated = true; break; }
        Assert.True(anyGated, "r1 should have at least one RequiresCleared exit");

        var state = new DungeonState();
        state.BeginRun();
        Assert.False(state.IsCleared("r1"));

        state.MarkCleared("r1");
        Assert.True(state.IsCleared("r1"));
    }
}
