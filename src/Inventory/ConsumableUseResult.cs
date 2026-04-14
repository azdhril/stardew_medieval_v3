namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Result of consuming an item from a quick-slot.
/// Supports multiple effect types so foods, potions, and future buffs
/// can share the same use flow.
/// </summary>
public readonly record struct ConsumableUseResult(
    bool Consumed,
    string? ItemId,
    float HealAmount,
    float StaminaRestorePct)
{
    public static ConsumableUseResult None => new(false, null, 0f, 0f);
}
