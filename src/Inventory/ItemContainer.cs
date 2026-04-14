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
}
