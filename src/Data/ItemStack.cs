namespace stardew_medieval_v3.Data;

/// <summary>
/// A stack of items in inventory. References an ItemDefinition by Id.
/// </summary>
public class ItemStack
{
    /// <summary>ItemDefinition.Id this stack refers to.</summary>
    public string ItemId { get; set; } = "";

    /// <summary>Current quantity in this stack.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional quality grade for fish/crops (0 = none, 1 = ⭐, 2 = ⭐⭐, 3 = ⭐⭐⭐).
    /// Stacks with different Quality DO NOT merge, so a 2-star Trout sits in a
    /// different slot than a 1-star Trout. Default 0 keeps existing items unaffected.
    /// </summary>
    public int Quality { get; set; } = 0;
}
