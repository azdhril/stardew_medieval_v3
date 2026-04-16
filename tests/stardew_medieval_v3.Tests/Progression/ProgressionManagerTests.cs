using System;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Progression;

namespace StardewMedieval.Tests.Progression;

/// <summary>
/// Unit tests for ProgressionManager XP/level-up logic and gold rolling.
/// Tests use a headless PlayerEntity (no texture) to avoid GraphicsDevice dependency.
/// </summary>
public class ProgressionManagerTests
{
    private static (ProgressionManager pm, PlayerEntity player, PlayerStats stats) Create()
    {
        var player = new PlayerEntity();
        var stats = player.Stats;
        var pm = new ProgressionManager(player, stats);
        return (pm, player, stats);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void AwardXP_Skeleton_Adds10XP()
    {
        var (pm, _, _) = Create();
        pm.AwardXP("Skeleton");
        Assert.Equal(10, pm.XP);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void AwardXP_UnknownEnemy_Adds5XPDefault()
    {
        var (pm, _, _) = Create();
        pm.AwardXP("UnknownBeast");
        Assert.Equal(5, pm.XP);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void LevelUp_TriggersAt50XP()
    {
        var (pm, player, stats) = Create();
        int levelUpFired = 0;
        int levelUpValue = 0;
        pm.OnLevelUp += (lvl) => { levelUpFired++; levelUpValue = lvl; };

        // Award 49 XP (4 skeletons + 1 unknown = 10*4 + 5*1 = 45, need more)
        // Simply set via multiple awards
        for (int i = 0; i < 4; i++) pm.AwardXP("Skeleton"); // 40 XP
        Assert.Equal(1, pm.Level); // Not yet leveled

        pm.AwardXP("Skeleton"); // 50 XP total -> level up, remainder 0
        Assert.Equal(2, pm.Level);
        Assert.Equal(0, pm.XP);
        Assert.Equal(1, levelUpFired);
        Assert.Equal(2, levelUpValue);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void LevelUp_CorrectRemainder()
    {
        var (pm, _, _) = Create();
        // 49 XP accumulated, then +10 = 59. Level 1 needs 50. Remainder = 9.
        // Award 49 via smaller chunks: 4 skeletons (40) + 1 golem (25) = 65 -> should level up
        // Actually let's do it simpler:
        // Award 4*10 = 40, then award DarkMage (15) = 55. Level up at 50, remainder = 5.
        for (int i = 0; i < 4; i++) pm.AwardXP("Skeleton"); // 40
        pm.AwardXP("DarkMage"); // +15 = 55 total. Need 50 for level 2. Remainder = 5.
        Assert.Equal(2, pm.Level);
        Assert.Equal(5, pm.XP);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void LevelUp_PushesStats()
    {
        var (pm, player, stats) = Create();
        // player starts at MaxHP=100, MaxStamina=100
        Assert.Equal(100f, player.MaxHP);
        Assert.Equal(100f, stats.MaxStamina);

        // Force level up
        for (int i = 0; i < 5; i++) pm.AwardXP("Skeleton"); // 50 XP -> level 2
        Assert.Equal(2, pm.Level);
        Assert.Equal(110f, player.MaxHP); // +10
        Assert.Equal(105f, stats.MaxStamina); // +5
        Assert.Equal(1, pm.BaseDamageBonus); // +1

        // HP and stamina should be fully restored
        Assert.Equal(player.MaxHP, player.HP);
        Assert.Equal(stats.MaxStamina, stats.CurrentStamina);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void OnXPGained_Fires()
    {
        var (pm, _, _) = Create();
        int gainedAmount = 0;
        pm.OnXPGained += (amount) => gainedAmount = amount;

        pm.AwardXP("Skeleton");
        Assert.Equal(10, gainedAmount);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void RollGold_Skeleton_InExpectedRange()
    {
        // Skeleton base = 5, +/-30% variance = [3..7]
        var rng = new Random(42);
        for (int i = 0; i < 50; i++)
        {
            int gold = ProgressionManager.RollGold("Skeleton", rng);
            Assert.InRange(gold, 3, 7);
        }
    }

    [Fact]
    [Trait("Category", "quick")]
    public void RollGold_SkeletonKing_InExpectedRange()
    {
        // SkeletonKing base = 100, +/-30% variance = [70..130]
        var rng = new Random(42);
        for (int i = 0; i < 50; i++)
        {
            int gold = ProgressionManager.RollGold("SkeletonKing", rng);
            Assert.InRange(gold, 70, 130);
        }
    }

    [Fact]
    [Trait("Category", "quick")]
    public void RollGold_UnknownEnemy_ReturnsAtLeast1()
    {
        var rng = new Random(42);
        int gold = ProgressionManager.RollGold("UnknownBeast", rng);
        Assert.True(gold >= 1);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void LoadFromState_RestoresFields()
    {
        var (pm, player, stats) = Create();
        var state = new stardew_medieval_v3.Core.GameState
        {
            XP = 25,
            Level = 3,
            BaseDamageBonus = 2,
            MaxHP = 120,
            MaxStamina = 110
        };

        pm.LoadFromState(state);

        Assert.Equal(25, pm.XP);
        Assert.Equal(3, pm.Level);
        Assert.Equal(2, pm.BaseDamageBonus);
        Assert.Equal(120f, player.MaxHP);
        Assert.Equal(110f, stats.MaxStamina);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void SaveToState_WritesFields()
    {
        var (pm, player, stats) = Create();
        // Award XP to change state
        for (int i = 0; i < 5; i++) pm.AwardXP("Skeleton"); // level 2

        var state = new stardew_medieval_v3.Core.GameState();
        pm.SaveToState(state);

        Assert.Equal(pm.XP, state.XP);
        Assert.Equal(pm.Level, state.Level);
        Assert.Equal(pm.BaseDamageBonus, state.BaseDamageBonus);
        Assert.Equal((int)player.MaxHP, state.MaxHP);
        Assert.Equal((int)stats.MaxStamina, state.MaxStamina);
    }
}
