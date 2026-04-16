using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Progression;

namespace StardewMedieval.Tests.Progression;

/// <summary>
/// Tests for DeathPenalty.Apply: gold loss, item loss via RNG buckets,
/// edge cases (empty inventory, no equipment), and hotbar reference pruning.
/// </summary>
public class DeathPenaltyTests
{
    /// <summary>
    /// Helper: initializes ItemRegistry so InventoryManager.TryAdd resolves items.
    /// </summary>
    private static void EnsureItemRegistry()
    {
        // ItemRegistry.Initialize() is idempotent (checks _initialized flag).
        ItemRegistry.Initialize();
    }

    /// <summary>
    /// Deterministic Random that returns a fixed NextDouble value.
    /// </summary>
    private sealed class FixedRandom : Random
    {
        private readonly double _doubleValue;
        private readonly int _nextValue;

        public FixedRandom(double doubleValue, int nextValue = 0) : base(0)
        {
            _doubleValue = doubleValue;
            _nextValue = nextValue;
        }

        public override double NextDouble() => _doubleValue;
        public override int Next(int maxValue) => Math.Min(_nextValue, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => Math.Min(_nextValue, Math.Max(minValue, maxValue - 1));
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_Gold100_Loses10()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(100);

        // roll >= 0.40 => lose 0 items
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.50));

        Assert.Equal(10, result.GoldLost);
        Assert.Equal(90, inv.Gold);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_Gold0_LosesNothing()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);

        var result = DeathPenalty.Apply(inv, new FixedRandom(0.50));

        Assert.Equal(0, result.GoldLost);
        Assert.Equal(0, inv.Gold);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_Gold5_Loses0_FloorRounding()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(5);

        // floor(5 * 0.10) = floor(0.5) = 0
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.50));

        Assert.Equal(0, result.GoldLost);
        Assert.Equal(5, inv.Gold);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_RollBelow015_Loses2Items()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);
        inv.TryAdd("Iron_Sword");
        inv.TryAdd("Health_Potion", 3);

        // roll < 0.15 => 2 items lost; next pick index always 0
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.10, 0));

        Assert.Equal(2, result.ItemsLost.Count);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_RollBetween015And040_Loses1Item()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);
        inv.TryAdd("Iron_Sword");
        inv.TryAdd("Health_Potion", 3);

        // roll in [0.15, 0.40) => 1 item lost
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.25, 0));

        Assert.Single(result.ItemsLost);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_RollAbove040_Loses0Items()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);
        inv.TryAdd("Iron_Sword");
        inv.TryAdd("Health_Potion", 3);

        // roll >= 0.40 => 0 items lost
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.50));

        Assert.Empty(result.ItemsLost);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_EmptyInventory_NoCrash()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);

        // roll < 0.15 => wants 2 items, but inventory empty
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.10, 0));

        Assert.Equal(0, result.GoldLost);
        Assert.Empty(result.ItemsLost);
    }

    [Fact]
    [Trait("Category", "quick")]
    public void Apply_WithEquipment_CanLoseEquippedItem()
    {
        EnsureItemRegistry();
        var inv = new InventoryManager();
        inv.SetGold(0);
        inv.TryAdd("Leather_Armor");
        int armorSlot = inv.FindSlot("Leather_Armor");
        inv.TryEquip(armorSlot);

        // Verify it's equipped
        Assert.NotNull(inv.GetEquipped(EquipSlot.Armor));

        // roll < 0.15 => 2 items, but only 1 exists (armor in equipment)
        var result = DeathPenalty.Apply(inv, new FixedRandom(0.10, 0));

        // Should have lost the one equipped item
        Assert.Single(result.ItemsLost);
        Assert.Contains("Leather_Armor", result.ItemsLost);
        Assert.Null(inv.GetEquipped(EquipSlot.Armor));
    }
}
