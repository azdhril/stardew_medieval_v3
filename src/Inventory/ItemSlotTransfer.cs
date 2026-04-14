using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Shared move/merge logic for drag-and-drop transfers between slot collections.
/// </summary>
public static class ItemSlotTransfer
{
    public static void MoveOrSwap(IItemSlotCollection source, int sourceIndex, IItemSlotCollection target, int targetIndex)
    {
        var from = source.GetSlot(sourceIndex);
        if (from == null) return;

        if (ReferenceEquals(source, target))
            return;

        var to = target.GetSlot(targetIndex);
        if (to != null && to.ItemId == from.ItemId)
        {
            var def = ItemRegistry.Get(from.ItemId);
            if (def != null)
            {
                int space = def.StackLimit - to.Quantity;
                if (space > 0)
                {
                    int moved = System.Math.Min(space, from.Quantity);
                    target.SetSlot(targetIndex, new ItemStack { ItemId = to.ItemId, Quantity = to.Quantity + moved });

                    int remaining = from.Quantity - moved;
                    source.SetSlot(sourceIndex, remaining > 0
                        ? new ItemStack { ItemId = from.ItemId, Quantity = remaining }
                        : null);
                    return;
                }
            }
        }

        source.SetSlot(sourceIndex, to == null ? null : new ItemStack { ItemId = to.ItemId, Quantity = to.Quantity });
        target.SetSlot(targetIndex, new ItemStack { ItemId = from.ItemId, Quantity = from.Quantity });
    }
}
