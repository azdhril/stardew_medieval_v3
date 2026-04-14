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
}
