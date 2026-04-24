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
        ["bag_cloth"]      = new("bag_cloth",      "Bolsa de Pano",     8,  "bag_leather"),
        ["bag_leather"]    = new("bag_leather",    "Bolsa de Couro",    12, "bag_reinforced"),
        ["bag_reinforced"] = new("bag_reinforced", "Bolsa Reforçada",   20, "bag_traveler"),
        ["bag_traveler"]   = new("bag_traveler",   "Bolsa do Viajante", 28, null),
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
