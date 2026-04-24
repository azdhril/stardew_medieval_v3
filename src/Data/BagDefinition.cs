namespace stardew_medieval_v3.Data;

/// <summary>
/// A bag the player wears. Defines the inventory capacity, the display name
/// shown as the grid subtitle, the next bag in the upgrade chain (or null
/// when maxed out), the gold price to purchase at a merchant (0 for the
/// starter bag which can't be bought), and the sprite index into the shared
/// <c>bags_upgrades.png</c> spritesheet for shop / UI rendering.
/// Data-only; registered in <see cref="BagRegistry"/>.
/// </summary>
public sealed record BagDefinition(
    string Id,
    string Name,
    int Capacity,
    string? NextId,
    int ShopPrice = 0,
    int SpriteIndex = -1);
