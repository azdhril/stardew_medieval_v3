using System.Collections.Generic;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static registry of bag tiers the player can equip. Ordered as an upgrade
/// chain via <see cref="BagDefinition.NextId"/>; <see cref="Next"/> returns the
/// next bag the player can buy/upgrade to (or null when already at the top).
/// </summary>
public static class BagRegistry
{
    /// <summary>The default bag a brand-new player starts with.</summary>
    public const string StarterId = "bag_cloth";

    private static readonly Dictionary<string, BagDefinition> _all = new()
    {
        // Starter bag — given to every new player, not for sale.
        ["bag_cloth"]       = new("bag_cloth",       "Bolsa de Pano",         8,  NextId: "bag_worn"),

        // 5 upgrade tiers — cap at 28 slots (the most the inventory panel can show
        // at 7 columns × 4 rows without overflowing). SpriteIndex maps to
        // bags_upgrades.png (horizontal — 0..5 left→right); idx 5 reserved for a
        // future "hero" tier if the panel grows.
        ["bag_worn"]        = new("bag_worn",        "Bolsa Surrada",        12, NextId: "bag_leather",    ShopPrice:   1_000,   SpriteIndex: 0),
        ["bag_leather"]     = new("bag_leather",     "Bolsa de Couro",       16, NextId: "bag_reinforced", ShopPrice:   5_000,   SpriteIndex: 1),
        ["bag_reinforced"]  = new("bag_reinforced",  "Bolsa Reforçada",      20, NextId: "bag_traveler",   ShopPrice:  25_000,   SpriteIndex: 2),
        ["bag_traveler"]    = new("bag_traveler",    "Bolsa do Viajante",    24, NextId: "bag_adventurer", ShopPrice:  75_000,   SpriteIndex: 3),
        ["bag_adventurer"]  = new("bag_adventurer",  "Bolsa do Aventureiro", 28, NextId: null,             ShopPrice: 250_000,   SpriteIndex: 4),
    };

    /// <summary>
    /// Return the bag with the given id. Falls back to the starter bag if the
    /// id is unknown — keeps save migrations forgiving when a content pack
    /// removes a bag tier the player had equipped.
    /// </summary>
    public static BagDefinition Get(string? id)
    {
        if (id != null && _all.TryGetValue(id, out var bag)) return bag;
        return _all[StarterId];
    }

    /// <summary>Return the next upgrade in the chain, or null when maxed out.</summary>
    public static BagDefinition? Next(BagDefinition current)
        => current.NextId != null && _all.TryGetValue(current.NextId, out var next) ? next : null;

    /// <summary>All bag tiers (iteration order is insertion order = upgrade order).</summary>
    public static IEnumerable<BagDefinition> All => _all.Values;

    /// <summary>The largest Capacity across every registered bag — used to size the underlying slot array.</summary>
    public static int MaxCapacity
    {
        get
        {
            int max = 0;
            foreach (var b in _all.Values)
                if (b.Capacity > max) max = b.Capacity;
            return max;
        }
    }
}
