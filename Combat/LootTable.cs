using System;
using System.Collections.Generic;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Represents a single possible drop from an enemy.
/// </summary>
/// <param name="ItemId">Item identifier matching Data/items.json.</param>
/// <param name="DropChance">Probability of dropping (0.0 to 1.0, where 1.0 = guaranteed).</param>
public record LootDrop(string ItemId, float DropChance);

/// <summary>
/// Defines a set of possible item drops for an enemy type.
/// Each drop is rolled independently against its DropChance.
/// </summary>
public class LootTable
{
    /// <summary>List of possible drops with their probabilities.</summary>
    public List<LootDrop> Drops { get; }

    /// <summary>
    /// Create a new LootTable with the given drop definitions.
    /// </summary>
    /// <param name="drops">List of possible drops.</param>
    public LootTable(List<LootDrop> drops)
    {
        Drops = drops;
    }

    /// <summary>
    /// Roll all drops against their DropChance and return items that passed.
    /// Quantity is always 1 per drop for v1.
    /// </summary>
    /// <param name="rng">Random number generator for deterministic testing.</param>
    /// <returns>List of (itemId, quantity) pairs for drops that passed the roll.</returns>
    public List<(string itemId, int quantity)> Roll(Random rng)
    {
        var results = new List<(string itemId, int quantity)>();

        foreach (var drop in Drops)
        {
            float roll = (float)rng.NextDouble();
            if (roll < drop.DropChance)
            {
                results.Add((drop.ItemId, 1));
            }
        }

        return results;
    }
}
