using System.Text.Json;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.World;

namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Tests for the Plan 03 boss-victory contract: Quest.Complete fires on boss
/// death, DungeonState.BossDefeated is a persistent milestone, and re-entry
/// after victory does NOT respawn the boss (enforced via BossSpawnGate).
/// </summary>
public class BossVictoryTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void OnBossDeath_QuestCompleted()
    {
        // Simulate the DungeonScene boss-victory code path without a full
        // MonoGame harness: just exercise the two observable state effects.
        var quest = new MainQuest();
        var dungeon = new DungeonState();
        dungeon.BeginRun();
        Assert.False(dungeon.BossDefeated);
        Assert.NotEqual(MainQuestState.Complete, quest.State);

        // Victory handler body (from DungeonScene.OnPreUpdate):
        dungeon.BossDefeated = true;
        quest.Complete();

        Assert.True(dungeon.BossDefeated);
        Assert.Equal(MainQuestState.Complete, quest.State);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BossVictory_Persists_AcrossSaveRoundtrip()
    {
        // Boss defeat must survive save/load. Uses JsonSerializer directly so
        // the test is hermetic (no LocalApplicationData I/O).
        var dungeon = new DungeonState();
        dungeon.BeginRun();
        dungeon.BossDefeated = true;

        var state = new GameState { SaveVersion = 8, Dungeon = dungeon.ToSnapshot() };
        string json = JsonSerializer.Serialize(state);
        var reloaded = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.Dungeon);
        Assert.True(reloaded.Dungeon.BossDefeated);

        // Rehydrate into a live DungeonState and confirm the flag round-trips.
        var restored = new DungeonState();
        restored.LoadFromSnapshot(reloaded.Dungeon);
        Assert.True(restored.BossDefeated);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void ReEntry_AfterDefeat_DoesNotRespawnBoss()
    {
        // BossSpawnGate is the single source of truth DungeonScene consults on
        // load to decide whether to call EnemySpawner.SpawnBoss. Once the
        // milestone is set, the gate must refuse further spawns forever.
        var dungeon = new DungeonState();
        dungeon.BeginRun();
        Assert.True(BossSpawnGate.ShouldSpawn(dungeon));

        dungeon.BossDefeated = true;
        Assert.False(BossSpawnGate.ShouldSpawn(dungeon));

        // Even after a fresh BeginRun, BossDefeated persists so the gate stays
        // closed (see Plan 03 Task 2 decision + DungeonState.BeginRun xmldoc).
        dungeon.BeginRun();
        Assert.False(BossSpawnGate.ShouldSpawn(dungeon));

        // Null state is treated as "never defeated" so dev/jump tooling works.
        Assert.True(BossSpawnGate.ShouldSpawn(null));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void BeginRun_DoesNotClear_BossDefeatedMilestone()
    {
        // Directly lock the D-14 contract: BeginRun clears rooms/chests/loot
        // (D-13) but NEVER clears the boss milestone. If this test ever goes
        // red, re-read Plan 03 Task 2 before flipping it — the game depends on
        // this invariant.
        var dungeon = new DungeonState();
        dungeon.BeginRun();
        dungeon.MarkCleared("r1");
        dungeon.BossDefeated = true;

        dungeon.BeginRun();

        Assert.Empty(dungeon.ClearedRooms);
        Assert.True(dungeon.BossDefeated);
    }
}
