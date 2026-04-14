namespace stardew_medieval_v3.Data;

/// <summary>
/// Drop rule for a harvestable world resource.
/// </summary>
public class ResourceDropData
{
    public string ItemId { get; init; } = "";
    public int MinQuantity { get; init; } = 1;
    public int MaxQuantity { get; init; } = 1;
}
