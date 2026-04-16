using stardew_medieval_v3.Progression;

namespace StardewMedieval.Tests.Progression;

/// <summary>
/// Unit tests for the XP curve and enemy XP table.
/// </summary>
public class XPTableTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void Level1_Requires50XP()
    {
        Assert.Equal(50, XPTable.XPToNextLevel(1));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Level2_Requires61XP()
    {
        // floor(50 * 1.22^1) = floor(61.0) = 61
        Assert.Equal(61, XPTable.XPToNextLevel(2));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Level10_ReturnsApprox293XP()
    {
        // floor(50 * 1.22^9) = floor(293.xx)
        int xp = XPTable.XPToNextLevel(10);
        Assert.InRange(xp, 280, 300);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Level0_ClampsTo50()
    {
        Assert.Equal(50, XPTable.XPToNextLevel(0));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Level100_ReturnsIntMaxValue()
    {
        Assert.Equal(int.MaxValue, XPTable.XPToNextLevel(100));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void XPPerEnemy_ContainsExpectedKeys()
    {
        Assert.Equal(10, XPTable.XPPerEnemy["Skeleton"]);
        Assert.Equal(15, XPTable.XPPerEnemy["DarkMage"]);
        Assert.Equal(25, XPTable.XPPerEnemy["Golem"]);
        Assert.Equal(150, XPTable.XPPerEnemy["SkeletonKing"]);
    }
}
