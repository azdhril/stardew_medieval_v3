using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Utility class for calculating combined equipment stats from weapon and armor.
/// </summary>
public static class EquipmentData
{
    /// <summary>
    /// Get the combined attack and defense stats from equipped weapon and armor.
    /// </summary>
    /// <param name="weaponId">Equipped weapon ItemId, or null.</param>
    /// <param name="armorId">Equipped armor ItemId, or null.</param>
    /// <returns>Tuple of (attack, defense) values, defaulting to 0f for missing.</returns>
    public static (float attack, float defense) GetEquipmentStats(string? weaponId, string? armorId)
    {
        float attack = 0f;
        float defense = 0f;

        if (weaponId != null)
        {
            var weaponDef = ItemRegistry.Get(weaponId);
            if (weaponDef?.Stats != null && weaponDef.Stats.TryGetValue("damage", out float dmg))
                attack = dmg;
        }

        if (armorId != null)
        {
            var armorDef = ItemRegistry.Get(armorId);
            if (armorDef?.Stats != null && armorDef.Stats.TryGetValue("defense", out float def))
                defense = def;
        }

        return (attack, defense);
    }
}
