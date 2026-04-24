namespace stardew_medieval_v3.Data;

/// <summary>
/// A bag the player wears. Defines the inventory capacity, the display name
/// shown as the grid subtitle, and the next bag in the upgrade chain (or null
/// when maxed out). Data-only; registered in <see cref="BagRegistry"/>.
/// </summary>
public sealed record BagDefinition(
    string Id,
    string Name,
    int Capacity,
    string? NextId);
