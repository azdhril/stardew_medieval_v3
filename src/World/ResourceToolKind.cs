namespace stardew_medieval_v3.World;

/// <summary>
/// Tool families that can harvest world resource nodes.
/// This stays separate from concrete inventory items so future tools can share behavior.
/// </summary>
public enum ResourceToolKind
{
    None,
    Axe,
    Pickaxe
}
