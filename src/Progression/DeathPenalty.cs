using System;
using System.Collections.Generic;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.Progression;

/// <summary>
/// Calculates and applies death penalties: 10% gold loss (floor) and
/// probabilistic item/equipment loss (15% lose 2, 25% lose 1, 60% nothing).
/// Called BEFORE respawn/HP restore so the penalty snapshot reflects pre-death state.
/// </summary>
public static class DeathPenalty
{
    /// <summary>Result of applying a death penalty.</summary>
    /// <param name="GoldLost">Amount of gold deducted.</param>
    /// <param name="ItemsLost">Item IDs that were removed (inventory + equipment).</param>
    public record PenaltyResult(int GoldLost, List<string> ItemsLost);

    /// <summary>
    /// Apply death penalty to the given inventory.
    /// 1. Deduct 10% gold (floor, minimum 0).
    /// 2. Roll item loss: 15% chance lose 2, 25% chance lose 1, 60% nothing.
    /// 3. Items are picked from a unified pool of inventory slots + equipped items.
    /// 4. Prune broken hotbar/consumable references after removal.
    /// </summary>
    /// <param name="inv">Player inventory to mutate.</param>
    /// <param name="rng">Random instance for deterministic testing.</param>
    /// <returns>Summary of what was lost.</returns>
    public static PenaltyResult Apply(InventoryManager inv, Random rng)
    {
        var itemsLost = new List<string>();

        // --- Gold penalty (D-14): 10% floor ---
        int goldLost = (int)Math.Floor(inv.Gold * 0.10);
        if (goldLost > 0)
            inv.SetGold(inv.Gold - goldLost);

        // --- Item loss roll (D-15) ---
        double roll = rng.NextDouble();
        int itemsToLose = roll < 0.15 ? 2 : roll < 0.40 ? 1 : 0;

        if (itemsToLose > 0)
        {
            // Build unified pool: (source, slotIndex, equipSlot?, itemId)
            var pool = new List<(string source, int index, EquipSlot? slot, string itemId)>();

            for (int i = 0; i < InventoryManager.SlotCount; i++)
            {
                var stack = inv.GetSlot(i);
                if (stack != null)
                    pool.Add(("inv", i, null, stack.ItemId));
            }

            foreach (var kvp in inv.GetAllEquipment())
                pool.Add(("equip", -1, kvp.Key, kvp.Value));

            // Remove items from pool (no double-pick)
            for (int n = 0; n < itemsToLose && pool.Count > 0; n++)
            {
                int pick = rng.Next(pool.Count);
                var entry = pool[pick];
                itemsLost.Add(entry.itemId);

                if (entry.source == "inv")
                    inv.RemoveAt(entry.index);
                else if (entry.source == "equip" && entry.slot.HasValue)
                    inv.ForceRemoveEquipment(entry.slot.Value);

                pool.RemoveAt(pick);
            }

            // Prune stale hotbar/consumable refs
            inv.PruneBrokenReferences();
        }

        Console.WriteLine(
            $"[DeathPenalty] Lost {goldLost}g + {itemsLost.Count} items: " +
            $"{(itemsLost.Count > 0 ? string.Join(", ", itemsLost) : "none")}");

        return new PenaltyResult(goldLost, itemsLost);
    }
}
