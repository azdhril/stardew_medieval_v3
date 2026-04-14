using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Minimal slot-based item container contract shared by player inventory
/// and future world containers such as chests.
/// </summary>
public interface IItemSlotCollection
{
    int Capacity { get; }
    ItemStack? GetSlot(int index);
    void SetSlot(int index, ItemStack? stack);
}
