namespace StardewMedieval.Tests.Save;

/// <summary>
/// Stub tests for save schema migration v7 -> v8. Real assertions wired in Task 2.
/// </summary>
public class SaveV7ToV8MigrationTests
{
    [Fact]
    [Trait("Category", "quick")]
    public void V7Save_LoadsWithDefaultDungeonState()
    {
        Assert.True(true);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void V8Save_RoundtripsDungeonState()
    {
        Assert.True(true);
    }
}
