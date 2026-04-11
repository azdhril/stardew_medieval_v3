using System;
using System.Collections.Generic;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Pure data class managing 20 inventory slots, hotbar selection, and equipment.
/// The first 8 slots double as the hotbar.
/// </summary>
public class InventoryManager
{
    /// <summary>Total number of inventory slots.</summary>
    public const int SlotCount = 20;

    /// <summary>Number of hotbar slots (first N inventory slots).</summary>
    public const int HotbarSize = 8;

    private readonly ItemStack?[] _slots = new ItemStack?[SlotCount];

    /// <summary>Currently active hotbar slot index (0-7).</summary>
    public int ActiveHotbarIndex { get; private set; } = 0;

    /// <summary>Currently equipped weapon ItemId, or null if none.</summary>
    public string? WeaponId { get; private set; }

    /// <summary>Currently equipped armor ItemId, or null if none.</summary>
    public string? ArmorId { get; private set; }

    /// <summary>Fired whenever inventory contents change (add, remove, move, equip).</summary>
    public event Action? OnInventoryChanged;

    /// <summary>
    /// Get the item stack at a specific slot index.
    /// </summary>
    /// <param name="index">Slot index (0-19).</param>
    /// <returns>The ItemStack at that slot, or null if empty.</returns>
    public ItemStack? GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return null;
        return _slots[index];
    }

    /// <summary>
    /// Try to add items to inventory. First fills existing stacks, then empty slots.
    /// </summary>
    /// <param name="itemId">The item definition Id to add.</param>
    /// <param name="quantity">Number of items to add.</param>
    /// <returns>Remaining quantity that could not be added (0 = all added).</returns>
    public int TryAdd(string itemId, int quantity = 1)
    {
        var def = ItemRegistry.Get(itemId);
        if (def == null)
        {
            Console.WriteLine($"[InventoryManager] Unknown item: {itemId}");
            return quantity;
        }

        int remaining = quantity;
        int stackLimit = def.StackLimit;

        // Pass 1: fill existing stacks of the same item
        for (int i = 0; i < SlotCount && remaining > 0; i++)
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

        // Pass 2: place remainder in first empty slot(s)
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_slots[i] == null)
            {
                int toAdd = Math.Min(remaining, stackLimit);
                _slots[i] = new ItemStack { ItemId = itemId, Quantity = toAdd };
                remaining -= toAdd;
            }
        }

        if (remaining < quantity)
            OnInventoryChanged?.Invoke();

        return remaining;
    }

    /// <summary>
    /// Remove and return the stack at a given slot index.
    /// </summary>
    /// <param name="index">Slot index to remove from.</param>
    /// <returns>The removed ItemStack, or null if slot was empty.</returns>
    public ItemStack? RemoveAt(int index)
    {
        if (index < 0 || index >= SlotCount)
            return null;

        var stack = _slots[index];
        if (stack == null)
            return null;

        _slots[index] = null;
        OnInventoryChanged?.Invoke();
        return stack;
    }

    /// <summary>
    /// Move (swap) contents between two slots. If same itemId and stackable, merge up to StackLimit.
    /// </summary>
    /// <param name="fromSlot">Source slot index.</param>
    /// <param name="toSlot">Destination slot index.</param>
    public void MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= SlotCount || toSlot < 0 || toSlot >= SlotCount)
            return;
        if (fromSlot == toSlot)
            return;

        var from = _slots[fromSlot];
        var to = _slots[toSlot];

        // If both have the same item, try to merge
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
                    // Full stack, swap instead
                    _slots[fromSlot] = to;
                    _slots[toSlot] = from;
                }
            }
        }
        else
        {
            // Different items or one is empty: swap
            _slots[fromSlot] = to;
            _slots[toSlot] = from;
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Set the active hotbar slot index (clamped 0-7).
    /// </summary>
    /// <param name="index">Hotbar index to activate.</param>
    public void SetActiveHotbar(int index)
    {
        ActiveHotbarIndex = Math.Clamp(index, 0, HotbarSize - 1);
    }

    /// <summary>
    /// Get the item stack in the currently active hotbar slot.
    /// </summary>
    /// <returns>The active hotbar ItemStack, or null if empty.</returns>
    public ItemStack? GetActiveHotbarItem()
    {
        return GetSlot(ActiveHotbarIndex);
    }

    /// <summary>
    /// Equip the item at a given slot index. If the slot contains a Weapon or Armor,
    /// equip it and put the previously equipped item (if any) back into that slot.
    /// </summary>
    /// <param name="slotIndex">Slot index containing the item to equip.</param>
    /// <returns>True if an item was equipped, false otherwise.</returns>
    public bool TryEquip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return false;

        var stack = _slots[slotIndex];
        if (stack == null)
            return false;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def == null)
            return false;

        if (def.Type == ItemType.Weapon)
        {
            string? oldWeapon = WeaponId;
            WeaponId = stack.ItemId;
            _slots[slotIndex] = null;

            // Put old weapon back in the slot
            if (oldWeapon != null)
                _slots[slotIndex] = new ItemStack { ItemId = oldWeapon, Quantity = 1 };

            OnInventoryChanged?.Invoke();
            return true;
        }
        else if (def.Type == ItemType.Armor)
        {
            string? oldArmor = ArmorId;
            ArmorId = stack.ItemId;
            _slots[slotIndex] = null;

            // Put old armor back in the slot
            if (oldArmor != null)
                _slots[slotIndex] = new ItemStack { ItemId = oldArmor, Quantity = 1 };

            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unequip a weapon or armor and place it in the target slot.
    /// </summary>
    /// <param name="equipType">"weapon" or "armor".</param>
    /// <param name="targetSlot">Slot index to place the unequipped item.</param>
    /// <returns>True if the item was unequipped successfully.</returns>
    public bool TryUnequip(string equipType, int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= SlotCount)
            return false;

        string? itemId = equipType == "weapon" ? WeaponId : equipType == "armor" ? ArmorId : null;
        if (itemId == null)
            return false;

        // Check if target slot is empty
        if (_slots[targetSlot] != null)
        {
            // Try stacking if same item
            if (_slots[targetSlot]!.ItemId == itemId)
            {
                var def = ItemRegistry.Get(itemId);
                if (def != null && _slots[targetSlot]!.Quantity < def.StackLimit)
                {
                    _slots[targetSlot]!.Quantity += 1;
                }
                else
                {
                    return false; // Can't stack, slot full
                }
            }
            else
            {
                return false; // Slot occupied with different item
            }
        }
        else
        {
            _slots[targetSlot] = new ItemStack { ItemId = itemId, Quantity = 1 };
        }

        if (equipType == "weapon")
            WeaponId = null;
        else
            ArmorId = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Populate inventory from a saved GameState.
    /// </summary>
    /// <param name="state">The GameState to load from.</param>
    public void LoadFromState(GameState state)
    {
        // Clear all slots
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = null;

        // Restore saved inventory
        for (int i = 0; i < state.Inventory.Count && i < SlotCount; i++)
        {
            var saved = state.Inventory[i];
            if (saved != null && !string.IsNullOrEmpty(saved.ItemId))
                _slots[i] = new ItemStack { ItemId = saved.ItemId, Quantity = saved.Quantity };
        }

        WeaponId = state.WeaponId;
        ArmorId = state.ArmorId;

        // Set active hotbar from first non-null in HotbarSlots, or 0
        ActiveHotbarIndex = 0;
        if (state.HotbarSlots != null)
        {
            for (int i = 0; i < state.HotbarSlots.Count && i < HotbarSize; i++)
            {
                if (state.HotbarSlots[i] != null)
                {
                    ActiveHotbarIndex = i;
                    break;
                }
            }
        }

        Console.WriteLine($"[InventoryManager] Loaded {state.Inventory.Count} items from save");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Save current inventory state to a GameState object.
    /// </summary>
    /// <param name="state">The GameState to write to.</param>
    public void SaveToState(GameState state)
    {
        state.Inventory.Clear();
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] != null)
                state.Inventory.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity });
        }

        state.WeaponId = WeaponId;
        state.ArmorId = ArmorId;

        // Populate HotbarSlots from first 8 slot ItemIds
        state.HotbarSlots = new List<string?>(new string?[HotbarSize]);
        for (int i = 0; i < HotbarSize; i++)
        {
            state.HotbarSlots[i] = _slots[i]?.ItemId;
        }
    }
}
