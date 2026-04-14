using System.Linq;
using stardew_medieval_v3.Data;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Tests for DungeonRegistry static config integrity: room count, exits, gating.
/// </summary>
public class DungeonRegistryTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void AllRooms_LoadWithoutThrow()
    {
        var rooms = DungeonRegistry.GetAll().ToList();

        Assert.Equal(7, rooms.Count);

        string[] expected = { "r1", "r2", "r3", "r3a", "r4", "r4a", "boss" };
        foreach (var id in expected)
            Assert.True(DungeonRegistry.Rooms.ContainsKey(id), $"Missing room id '{id}'");
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BossExit_RequiresAllMainRoomsCleared()
    {
        // The route into the boss room (exit_r4_to_boss in r4) must require clearing.
        var r4 = DungeonRegistry.Get("r4");
        Assert.True(r4.Exits.ContainsKey("exit_r4_to_boss"));
        Assert.True(r4.Exits["exit_r4_to_boss"].RequiresCleared);

        // And every main room (r1, r2, r3, r4) must gate its forward exit.
        var r1 = DungeonRegistry.Get("r1");
        Assert.True(r1.Exits["exit_r1_to_r2"].RequiresCleared);
        var r2 = DungeonRegistry.Get("r2");
        Assert.True(r2.Exits["exit_r2_to_r3"].RequiresCleared);
        var r3 = DungeonRegistry.Get("r3");
        Assert.True(r3.Exits["exit_r3_to_r4"].RequiresCleared);

        // Boss room itself is the boss arena.
        var boss = DungeonRegistry.Get("boss");
        Assert.True(boss.IsBossRoom);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void NoOrphanTriggerNames()
    {
        // Every intra-dungeon Exit.RoomId must reference an existing registry room.
        foreach (var room in DungeonRegistry.GetAll())
        {
            foreach (var (triggerName, exit) in room.Exits)
            {
                if (exit.LeaveDungeon)
                {
                    Assert.False(string.IsNullOrEmpty(exit.TargetScene),
                        $"Exit {triggerName} in {room.Id} marked LeaveDungeon but missing TargetScene");
                    continue;
                }

                Assert.False(string.IsNullOrEmpty(exit.RoomId),
                    $"Exit {triggerName} in {room.Id} has no RoomId and is not LeaveDungeon");
                Assert.True(DungeonRegistry.Rooms.ContainsKey(exit.RoomId!),
                    $"Exit {triggerName} in {room.Id} points at unknown room '{exit.RoomId}'");
            }
        }
    }
}
