using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static registry of all dungeon rooms. Mirrors the CropRegistry/ChestRegistry
/// pattern: a single in-memory dictionary populated at type init.
/// </summary>
public static class DungeonRegistry
{
    /// <summary>All rooms keyed by room id. Static for global access from scenes.</summary>
    public static readonly Dictionary<string, DungeonRoomData> Rooms = BuildRooms();

    /// <summary>Get a room by id, throwing if missing.</summary>
    public static DungeonRoomData Get(string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var room))
            throw new KeyNotFoundException($"[DungeonRegistry] Unknown room id '{roomId}'");
        return room;
    }

    /// <summary>Enumerate all configured rooms.</summary>
    public static IEnumerable<DungeonRoomData> GetAll() => Rooms.Values;

    private static Dictionary<string, DungeonRoomData> BuildRooms()
    {
        var rooms = new Dictionary<string, DungeonRoomData>
        {
            ["r1"] = new DungeonRoomData
            {
                Id = "r1",
                TmxPath = "assets/Maps/dungeon_r1.tmx",
                Spawns = new()
                {
                    ("Skeleton", new Vector2(160, 160)),
                    ("Skeleton", new Vector2(240, 200)),
                },
                HasGatedExit = true,
                Exits = new()
                {
                    ["exit_r1_to_r2"] = new ExitData(RoomId: "r2", RequiresCleared: true),
                    ["exit_r1_to_village"] = new ExitData(LeaveDungeon: true, TargetScene: "village", TargetTrigger: "dungeon_entrance"),
                },
            },
            ["r2"] = new DungeonRoomData
            {
                Id = "r2",
                TmxPath = "assets/Maps/dungeon_r2.tmx",
                Spawns = new()
                {
                    ("Skeleton", new Vector2(160, 160)),
                    ("Skeleton", new Vector2(220, 180)),
                    ("Skeleton", new Vector2(280, 240)),
                },
                HasGatedExit = true,
                Exits = new()
                {
                    ["exit_r2_to_r3"] = new ExitData(RoomId: "r3", RequiresCleared: true),
                    ["exit_r2_to_r1"] = new ExitData(RoomId: "r1"),
                },
            },
            ["r3"] = new DungeonRoomData
            {
                Id = "r3",
                TmxPath = "assets/Maps/dungeon_r3.tmx",
                Spawns = new()
                {
                    ("Skeleton", new Vector2(160, 160)),
                    ("DarkMage", new Vector2(260, 200)),
                },
                HasGatedExit = true,
                Exits = new()
                {
                    ["exit_r3_to_r4"] = new ExitData(RoomId: "r4", RequiresCleared: true),
                    ["exit_r3_to_r3a"] = new ExitData(RoomId: "r3a"),
                    ["exit_r3_to_r2"] = new ExitData(RoomId: "r2"),
                },
            },
            ["r3a"] = new DungeonRoomData
            {
                Id = "r3a",
                TmxPath = "assets/Maps/dungeon_r3a.tmx",
                IsOptional = true,
                Chests = new()
                {
                    ("dungeon_r3a_chest", new Point(6, 6), "chest_wood"),
                },
                Exits = new()
                {
                    ["exit_r3a_to_r3"] = new ExitData(RoomId: "r3"),
                },
            },
            ["r4"] = new DungeonRoomData
            {
                Id = "r4",
                TmxPath = "assets/Maps/dungeon_r4.tmx",
                Spawns = new()
                {
                    ("Golem", new Vector2(200, 200)),
                    ("DarkMage", new Vector2(280, 220)),
                },
                HasGatedExit = true,
                Exits = new()
                {
                    ["exit_r4_to_boss"] = new ExitData(RoomId: "boss", RequiresCleared: true),
                    ["exit_r4_to_r4a"] = new ExitData(RoomId: "r4a"),
                    ["exit_r4_to_r3"] = new ExitData(RoomId: "r3"),
                },
            },
            ["r4a"] = new DungeonRoomData
            {
                Id = "r4a",
                TmxPath = "assets/Maps/dungeon_r4a.tmx",
                IsOptional = true,
                Spawns = new()
                {
                    ("Skeleton", new Vector2(180, 180)),
                    ("Skeleton", new Vector2(260, 200)),
                },
                Chests = new()
                {
                    ("dungeon_r4a_chest", new Point(7, 6), "chest_wood"),
                },
                Exits = new()
                {
                    ["exit_r4a_to_r4"] = new ExitData(RoomId: "r4"),
                },
            },
            ["boss"] = new DungeonRoomData
            {
                Id = "boss",
                TmxPath = "assets/Maps/dungeon_boss.tmx",
                IsBossRoom = true,
                Exits = new()
                {
                    // Per D-14: returning to village near castle door so the
                    // King quest-complete dialogue (NPC-04) can be exercised.
                    ["exit_boss_to_village"] = new ExitData(
                        LeaveDungeon: true,
                        TargetScene: "village",
                        TargetTrigger: "castle_door"),
                },
            },
        };

        Console.WriteLine($"[DungeonRegistry] Loaded {rooms.Count} rooms");
        return rooms;
    }
}
