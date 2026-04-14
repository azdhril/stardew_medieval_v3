using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using stardew_medieval_v3.Data;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Tests for <see cref="DungeonRegistry"/> static config integrity: room count,
/// exits, gating. Plan 02 adds a TMX cross-check proving every intra-dungeon
/// exit name declared in the registry exists as a Trigger object in the target
/// room's TMX file (no orphan trigger names).
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
        var r4 = DungeonRegistry.Get("r4");
        Assert.True(r4.Exits.ContainsKey("exit_r4_to_boss"));
        Assert.True(r4.Exits["exit_r4_to_boss"].RequiresCleared);

        var r1 = DungeonRegistry.Get("r1");
        Assert.True(r1.Exits["exit_r1_to_r2"].RequiresCleared);
        var r2 = DungeonRegistry.Get("r2");
        Assert.True(r2.Exits["exit_r2_to_r3"].RequiresCleared);
        var r3 = DungeonRegistry.Get("r3");
        Assert.True(r3.Exits["exit_r3_to_r4"].RequiresCleared);

        var boss = DungeonRegistry.Get("boss");
        Assert.True(boss.IsBossRoom);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BossRoom_RequiresAllMainRoomsCleared()
    {
        // DungeonScene.HandleTrigger enforces an additional check when the
        // target room is "boss": every main room r1..r4 must be cleared.
        // Locks both halves of that contract at the registry level: r4 has a
        // RequiresCleared gated exit to boss, and all four main rooms are
        // registered so the foreach loop can find them.
        var r4 = DungeonRegistry.Get("r4");
        Assert.True(r4.Exits.ContainsKey("exit_r4_to_boss"));
        Assert.True(r4.Exits["exit_r4_to_boss"].RequiresCleared);
        Assert.Equal("boss", r4.Exits["exit_r4_to_boss"].RoomId);

        foreach (var id in new[] { "r1", "r2", "r3", "r4" })
            Assert.True(DungeonRegistry.Rooms.ContainsKey(id), $"Main room {id} missing");
    }

    [Fact]
    [Trait("Category", "quick")]
    public void NoOrphanTriggerNames()
    {
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

    [Fact]
    [Trait("Category", "quick")]
    public void EveryExit_HasMatchingTriggerInSourceTmx()
    {
        // Contract: for each room R, every key in R.Exits must be the `name`
        // attribute of some <object> inside an <objectgroup name="Triggers"> in
        // R's TMX file. Proves Plan 02 authored triggers that actually match
        // the registry.
        string repoRoot = FindRepoRoot();

        foreach (var room in DungeonRegistry.GetAll())
        {
            // Plan 03 authors dungeon_boss.tmx so the boss room is now included
            // in the cross-check along with the other six rooms.
            string tmxPath = Path.Combine(repoRoot, room.TmxPath);
            Assert.True(File.Exists(tmxPath), $"TMX missing for {room.Id}: {tmxPath}");

            var doc = XDocument.Load(tmxPath);
            var triggerNames = doc
                .Descendants("objectgroup")
                .Where(g => string.Equals((string?)g.Attribute("name"), "Triggers",
                    System.StringComparison.OrdinalIgnoreCase))
                .Descendants("object")
                .Select(o => (string?)o.Attribute("name") ?? "")
                .ToHashSet();

            foreach (var triggerName in room.Exits.Keys)
            {
                Assert.True(triggerNames.Contains(triggerName),
                    $"Room {room.Id} TMX is missing Trigger '{triggerName}' (declared in registry)");
            }
        }
    }

    /// <summary>
    /// Walk up from the test assembly location until we find the repo root
    /// (the directory that contains `assets/Maps`). Tests run from
    /// tests/.../bin/Debug/net8.0, so we may need to climb several levels.
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "assets", "Maps")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate repo root (no ancestor contains assets/Maps).");
    }
}
