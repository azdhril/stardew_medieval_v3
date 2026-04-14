namespace stardew_medieval_v3.Farming;

/// <summary>
/// Per-cell farming state.
/// </summary>
public class CellData
{
    public bool IsTillable { get; set; }
    public bool IsTilled { get; set; }
    public bool IsWatered { get; set; }
    public CropInstance? Crop { get; set; }
}
