using System.Reflection;
using System.Text.Json;
using stardew_medieval_v3.Core;

namespace StardewMedieval.Tests.Save;

/// <summary>
/// Tests for save schema migration v8 -> v9 (progression fields) and v9 roundtrip.
/// </summary>
public class SaveV8ToV9MigrationTests
{
    /// <summary>
    /// Helper: invoke private MigrateIfNeeded via reflection.
    /// </summary>
    private static void Migrate(GameState state)
    {
        var method = typeof(SaveManager).GetMethod("MigrateIfNeeded",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        method!.Invoke(null, new object?[] { state });
    }

    [Fact]
    [Trait("Category", "quick")]
    public void V8Save_Level1_MigratesToV9WithDefaults()
    {
        string v8Json = "{ \"SaveVersion\": 8, \"DayNumber\": 5, \"Level\": 1 }";
        var state = JsonSerializer.Deserialize<GameState>(v8Json);
        Assert.NotNull(state);

        Migrate(state!);

        Assert.Equal(9, state!.SaveVersion);
        Assert.Equal(100, state.MaxHP);
        Assert.Equal(100, state.MaxStamina);
        Assert.Equal(0, state.BaseDamageBonus);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void V8Save_Level5_MigratesToV9WithScaledStats()
    {
        string v8Json = "{ \"SaveVersion\": 8, \"DayNumber\": 10, \"Level\": 5 }";
        var state = JsonSerializer.Deserialize<GameState>(v8Json);
        Assert.NotNull(state);

        Migrate(state!);

        Assert.Equal(9, state!.SaveVersion);
        // Level 5: MaxHP = 100 + (5-1)*10 = 140
        Assert.Equal(140, state.MaxHP);
        // Level 5: MaxStamina = 100 + (5-1)*5 = 120
        Assert.Equal(120, state.MaxStamina);
        // Level 5: BaseDamageBonus = 5-1 = 4
        Assert.Equal(4, state.BaseDamageBonus);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void V9Save_RoundtripsProgressionFields()
    {
        var original = new GameState
        {
            SaveVersion = 9,
            DayNumber = 3,
            MaxHP = 150,
            MaxStamina = 130,
            BaseDamageBonus = 5,
            XP = 42,
            Level = 6
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(restored);
        Assert.Equal(9, restored!.SaveVersion);
        Assert.Equal(150, restored.MaxHP);
        Assert.Equal(130, restored.MaxStamina);
        Assert.Equal(5, restored.BaseDamageBonus);
        Assert.Equal(42, restored.XP);
        Assert.Equal(6, restored.Level);
    }
}
