using System;
using System.Collections.Generic;
using System.Linq;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.World;

/// <summary>
/// Deterministic chest-content roller for a dungeon run. Called once from
/// <see cref="Scenes.VillageScene"/> when the player enters the dungeon: it
/// walks every room in <see cref="DungeonRegistry"/>, rolls a per-chest
/// <see cref="LootTable"/> using a fresh <see cref="Random"/> seeded from
/// <see cref="DungeonState.RunSeed"/>, and stores the resulting item ids in
/// <see cref="DungeonState.ChestContents"/>. Re-entering a room reads that map
/// instead of re-rolling (Pitfall 5 idempotency, D-10).
/// </summary>
public static class DungeonChestSeeder
{
    /// <summary>
    /// Seed <see cref="DungeonState.ChestContents"/> for every chest declared in
    /// <see cref="DungeonRegistry"/>. Idempotent per run: safe to call again
    /// during <see cref="DungeonState.BeginRun"/> but only meaningful on fresh
    /// run state (BeginRun just cleared ChestContents).
    /// </summary>
    public static void Seed(ServiceContainer svc)
    {
        var dungeon = svc.Dungeon
            ?? throw new InvalidOperationException("[DungeonChestSeeder] ServiceContainer.Dungeon is null");
        Seed(dungeon);
    }

    /// <summary>
    /// Test-friendly overload that works directly on a <see cref="DungeonState"/>
    /// without requiring a full <see cref="ServiceContainer"/> (MonoGame
    /// GraphicsDevice, etc.). Behavior is identical to the ServiceContainer
    /// overload.
    /// </summary>
    public static void Seed(DungeonState dungeon)
    {
        var rng = new Random(dungeon.RunSeed);
        var table = new LootTable(new List<LootDrop>
        {
            new("Health_Potion", 1.0f),
            new("Iron_Sword",    0.25f),
            new("Mana_Crystal",  0.4f),
            new("Bones",         0.9f),
        });

        int chestCount = 0;
        foreach (var room in DungeonRegistry.GetAll())
        {
            foreach (var (chestId, _, _) in room.Chests)
            {
                if (dungeon.IsChestOpened(chestId))
                {
                    // Chest was already collected in a prior run -- leave it empty permanently.
                    // Hydration path in DungeonScene.OnLoad detects this via IsChestOpened and
                    // renders the chest in its opened/empty sprite state.
                    dungeon.ChestContents[chestId] = new List<string>();
                }
                else
                {
                    var drops = table.Roll(rng);
                    dungeon.ChestContents[chestId] = drops.Select(d => d.itemId).ToList();
                }
                chestCount++;
            }
        }

        Console.WriteLine($"[DungeonChestSeeder] Seeded {chestCount} chest(s) with seed {dungeon.RunSeed}");
    }
}
