using System.Collections.Generic;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Curated list of items the village shopkeeper offers. Prices may differ from
/// <see cref="ItemDefinition.BasePrice"/> because of per-stock markup.
/// See CONTEXT D-08 (seeds + potions + starter weapon + starter armor).
/// </summary>
public static class ShopStock
{
    /// <summary>
    /// A single shop listing. For regular items, <paramref name="ItemId"/> is a
    /// valid <see cref="ItemRegistry"/> key. For the bag-upgrade entry,
    /// <paramref name="IsBagUpgrade"/> is true and <paramref name="BagId"/>
    /// identifies the <see cref="BagDefinition"/> being sold; clicking Buy in
    /// that case applies <see cref="InventoryManager.TryEquipBag"/> instead of
    /// adding an item stack to the inventory.
    /// </summary>
    public record Entry(
        string ItemId,
        int Price,
        bool IsBagUpgrade = false,
        string? BagId = null);

    /// <summary>
    /// The shopkeeper's regular items (seeds, potions, weapons, armor). For the
    /// per-player dynamic Buy list — including any pending bag upgrade — use
    /// <see cref="GetBuyItems"/> instead.
    /// </summary>
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
    /// Compose the Buy tab for <paramref name="inv"/>: if the player has a next
    /// bag upgrade available, prepend it as a synthetic entry at the top of the
    /// list (so it's always visible as the first purchase). Otherwise returns
    /// the regular stock unchanged.
    /// </summary>
    public static IReadOnlyList<Entry> GetBuyItems(InventoryManager inv)
    {
        var next = inv.NextBag;
        if (next == null || next.ShopPrice <= 0) return Items;

        var list = new List<Entry>(Items.Count + 1)
        {
            new Entry(ItemId: next.Id, Price: next.ShopPrice, IsBagUpgrade: true, BagId: next.Id),
        };
        list.AddRange(Items);
        return list;
    }

    /// <summary>
    /// Sell price for a given item: BasePrice / 2 (integer division, floor).
    /// Returns 0 for unknown items or items with no BasePrice.
    /// </summary>
    public static int GetSellPrice(ItemDefinition? def) => def == null ? 0 : def.BasePrice / 2;
}
