using System.Collections.Generic;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Curated list of items the village shopkeeper offers. Prices may differ from
/// <see cref="ItemDefinition.BasePrice"/> because of per-stock markup.
/// See CONTEXT D-08 (seeds + potions + starter weapon + starter armor).
/// </summary>
public static class ShopStock
{
    /// <summary>A single shop listing: the item Id and the shop's buy price in gold.</summary>
    public record Entry(string ItemId, int Price);

    /// <summary>The shopkeeper's curated stock (6-10 items). Item Ids MUST resolve via ItemRegistry.</summary>
    public static IReadOnlyList<Entry> Items { get; } = new List<Entry>
    {
        new("Cabbage_Seed",    25),
        new("Carrot_Seed",     25),
        new("Strawberry_Seed", 40),
        new("Pumpkin_Seed",    40),
        new("Health_Potion",   75),
        new("Bread",           25),
        new("Iron_Sword",     220),
        new("Leather_Armor",  160),
    };

    /// <summary>
    /// Sell price for a given item: BasePrice / 2 (integer division, floor).
    /// Returns 0 for unknown items or items with no BasePrice.
    /// </summary>
    public static int GetSellPrice(ItemDefinition? def) => def == null ? 0 : def.BasePrice / 2;
}
