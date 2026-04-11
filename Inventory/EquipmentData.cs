using System.Collections.Generic;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Utility class for calculating combined equipment stats from all equipped items.
/// </summary>
public static class EquipmentData
{
    /// <summary>
    /// Get the combined attack and defense stats from all equipped items.
    /// </summary>
    public static (float attack, float defense) GetEquipmentStats(IReadOnlyDictionary<EquipSlot, string> equipment)
    {
        float attack = 0f;
        float defense = 0f;

        foreach (var kvp in equipment)
        {
            var def = ItemRegistry.Get(kvp.Value);
            if (def?.Stats == null) continue;

            if (def.Stats.TryGetValue("damage", out float dmg))
                attack += dmg;
            if (def.Stats.TryGetValue("defense", out float def2))
                defense += def2;
        }

        return (attack, defense);
    }
}
