namespace StardewMedieval.Tests.Dungeon;

/// <summary>
/// Stub tests for room-cleared event detection. Real assertions wired in Task 3.
/// </summary>
public class RoomClearedTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void EmptyEnemyList_FiresEventOnce()
    {
        Assert.True(true);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void NonGatedRoom_DoesNotMarkCleared()
    {
        Assert.True(true);
    }
}
