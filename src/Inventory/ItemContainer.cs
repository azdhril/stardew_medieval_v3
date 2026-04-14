using System;
using System.Collections.Generic;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Generic slot-based item container for chests and other storage objects.
/// </summary>
public class ItemContainer : IItemSlotCollection
{
    private readonly ItemStack?[] _slots;

    public int Capacity => _slots.Length;

    public event Action? OnChanged;

    public ItemContainer(int capacity)
    {
        _slots = new ItemStack?[Math.Max(1, capacity)];
    }

    public ItemStack? GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return null;
        return _slots[index];
    }

    public void SetSlot(int index, ItemStack? stack)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index] = stack == null ? null : new ItemStack { ItemId = stack.ItemId, Quantity = stack.Quantity };
        OnChanged?.Invoke();
    }

    public void MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= _slots.Length || toSlot < 0 || toSlot >= _slots.Length) return;
        if (fromSlot == toSlot) return;

        var from = _slots[fromSlot];
        var to = _slots[toSlot];

        if (from != null && to != null && from.ItemId == to.ItemId)
        {
            var def = ItemRegistry.Get(from.ItemId);
            if (def != null)
            {
                int space = def.StackLimit - to.Quantity;
                if (space > 0)
                {
                    int toMove = Math.Min(from.Quantity, space);
                    to.Quantity += toMove;
                    from.Quantity -= toMove;
                    if (from.Quantity <= 0)
                        _slots[fromSlot] = null;
                }
                else
                {
                    _slots[fromSlot] = to;
                    _slots[toSlot] = from;
                }
            }
        }
        else
        {
            _slots[fromSlot] = to;
            _slots[toSlot] = from;
        }

        OnChanged?.Invoke();
    }

    public int TryAdd(string itemId, int quantity = 1)
    {
        var def = ItemRegistry.Get(itemId);
        if (def == null)
            return quantity;

        int remaining = quantity;
        int stackLimit = def.StackLimit;

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i] != null && _slots[i]!.ItemId == itemId)
            {
                int space = stackLimit - _slots[i]!.Quantity;
                if (space > 0)
                {
                    int toAdd = Math.Min(remaining, space);
                    _slots[i]!.Quantity += toAdd;
                    remaining -= toAdd;
                }
            }
        }

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i] == null)
            {
                int toAdd = Math.Min(remaining, stackLimit);
                _slots[i] = new ItemStack { ItemId = itemId, Quantity = toAdd };
                remaining -= toAdd;
            }
        }

        if (remaining < quantity)
            OnChanged?.Invoke();

        return remaining;
    }

    public List<ItemStack?> GetSaveData()
    {
        var data = new List<ItemStack?>(_slots.Length);
        for (int i = 0; i < _slots.Length; i++)
        {
            var stack = _slots[i];
            data.Add(stack == null
                ? null
                : new ItemStack { ItemId = stack.ItemId, Quantity = stack.Quantity });
        }
        return data;
    }

    public void LoadFrom(List<ItemStack?> data)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < data.Count && data[i] != null)
                _slots[i] = new ItemStack { ItemId = data[i]!.ItemId, Quantity = data[i]!.Quantity };
            else
                _slots[i] = null;
        }
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Sort items by type, then rarity, then display name. Empty slots stay at the end.
    /// </summary>
    public void SortByDefault()
    {
        var items = new List<ItemStack>();
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
                items.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity });
        }

        items.Sort(static (a, b) =>
        {
            var defA = ItemRegistry.Get(a.ItemId);
            var defB = ItemRegistry.Get(b.ItemId);

            int typeCompare = GetTypeOrder(defA?.Type ?? ItemType.Loot).CompareTo(GetTypeOrder(defB?.Type ?? ItemType.Loot));
            if (typeCompare != 0) return typeCompare;

            int rarityCompare = GetRarityOrder(defB?.Rarity ?? Rarity.Common).CompareTo(GetRarityOrder(defA?.Rarity ?? Rarity.Common));
            if (rarityCompare != 0) return rarityCompare;

            int nameCompare = string.Compare(defA?.Name ?? a.ItemId, defB?.Name ?? b.ItemId, StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0) return nameCompare;

            return string.Compare(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase);
        });

        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = i < items.Count ? items[i] : null;

        OnChanged?.Invoke();
    }

    private static int GetTypeOrder(ItemType type) => type switch
    {
        ItemType.Tool => 0,
        ItemType.Weapon => 1,
        ItemType.Armor => 2,
        ItemType.Consumable => 3,
        ItemType.Seed => 4,
        ItemType.Crop => 5,
        ItemType.Loot => 6,
        _ => 99,
    };

    private static int GetRarityOrder(Rarity rarity) => rarity switch
    {
        Rarity.Rare => 2,
        Rarity.Uncommon => 1,
        _ => 0,
    };
}
