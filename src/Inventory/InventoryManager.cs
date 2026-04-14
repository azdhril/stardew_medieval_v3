using System;
using System.Collections.Generic;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Inventory;

/// <summary>
/// Manages 20 inventory slots, reference-based hotbar (8 slots), consumable refs (2 slots),
/// and 7 defensive equipment slots. Hotbar/consumable slots store item ID references that
/// point to items in the inventory grid. References break when the item leaves inventory
/// (dropped, stored in chest) but persist when items are rearranged within inventory.
/// </summary>
public class InventoryManager
{
    /// <summary>Total number of inventory slots.</summary>
    public const int SlotCount = 20;

    /// <summary>Number of hotbar reference slots.</summary>
    public const int HotbarSize = 8;

    /// <summary>Number of consumable quick-slots (Q only, E kept free for actions).</summary>
    public const int ConsumableSlotCount = 1;

    private readonly ItemStack?[] _slots = new ItemStack?[SlotCount];
    private readonly string?[] _hotbarRefs = new string?[HotbarSize];
    private readonly string?[] _consumableRefs = new string?[ConsumableSlotCount];

    /// <summary>Currently selected hotbar slot index (0-7).</summary>
    public int ActiveHotbarIndex { get; private set; } = 0;

    /// <summary>Player's current gold balance. Mutated only via SetGold/AddGold/TrySpendGold.</summary>
    public int Gold { get; private set; } = 0;

    /// <summary>Fired whenever <see cref="Gold"/> changes.</summary>
    public event Action? OnGoldChanged;

    /// <summary>Set gold to an absolute value (clamped to >= 0). Fires OnGoldChanged.</summary>
    public void SetGold(int value)
    {
        Gold = Math.Max(0, value);
        OnGoldChanged?.Invoke();
    }

    /// <summary>Add gold. Negative amounts are rejected and logged. Fires OnGoldChanged on success.</summary>
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Console.WriteLine($"[InventoryManager] AddGold rejected negative {amount}");
            return;
        }
        Gold += amount;
        OnGoldChanged?.Invoke();
    }

    /// <summary>
    /// Attempt to debit <paramref name="amount"/> from gold.
    /// Returns false if amount is negative or exceeds current gold; true on success.
    /// Fires OnGoldChanged only on successful debit.
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || amount > Gold) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke();
        return true;
    }

    /// <summary>Equipment slots: maps EquipSlot to equipped ItemId.</summary>
    private readonly Dictionary<EquipSlot, string> _equipment = new();

    /// <summary>Fired whenever inventory contents change.</summary>
    public event Action? OnInventoryChanged;

    // === Inventory grid ===

    /// <summary>Get the item stack at a specific inventory slot index (0-19).</summary>
    public ItemStack? GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return null;
        return _slots[index];
    }

    /// <summary>Try to add items to inventory. Returns remaining that couldn't fit.</summary>
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
    /// Remove up to <paramref name="quantity"/> units of the given item from inventory
    /// (searches stacks left-to-right). Clears the slot when a stack reaches zero.
    /// Returns how many units were actually removed.
    /// </summary>
    public int TryConsume(string itemId, int quantity = 1)
    {
        int remaining = quantity;
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            var s = _slots[i];
            if (s == null || s.ItemId != itemId) continue;
            int take = Math.Min(remaining, s.Quantity);
            s.Quantity -= take;
            remaining -= take;
            if (s.Quantity <= 0) _slots[i] = null;
        }
        int consumed = quantity - remaining;
        if (consumed > 0) OnInventoryChanged?.Invoke();
        return consumed;
    }

    /// <summary>Remove and return the stack at a given slot.</summary>
    public ItemStack? RemoveAt(int index)
    {
        if (index < 0 || index >= SlotCount) return null;
        var stack = _slots[index];
        if (stack == null) return null;

        _slots[index] = null;
        OnInventoryChanged?.Invoke();
        return stack;
    }

    /// <summary>
    /// Remove <paramref name="quantity"/> units from the stack at <paramref name="index"/>.
    /// If quantity >= stack.Quantity, the slot is cleared (delegates to RemoveAt).
    /// Returns the removed portion (ItemId + removed quantity), or null if the slot is empty/invalid.
    /// </summary>
    public ItemStack? RemoveQuantity(int index, int quantity)
    {
        if (index < 0 || index >= SlotCount || quantity <= 0) return null;
        var stack = _slots[index];
        if (stack == null) return null;
        if (quantity >= stack.Quantity) return RemoveAt(index);

        stack.Quantity -= quantity;
        var removed = new ItemStack { ItemId = stack.ItemId, Quantity = quantity };
        OnInventoryChanged?.Invoke();
        Console.WriteLine($"[InventoryManager] Removed {quantity}x {stack.ItemId} from slot {index} (remaining: {stack.Quantity})");
        return removed;
    }

    /// <summary>Move/swap contents between two inventory slots.</summary>
    public void MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= SlotCount || toSlot < 0 || toSlot >= SlotCount) return;
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
                    if (from.Quantity <= 0) _slots[fromSlot] = null;
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

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Check if inventory contains at least one stack of the given item.</summary>
    public bool HasItem(string itemId)
    {
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] != null && _slots[i]!.ItemId == itemId)
                return true;
        return false;
    }

    /// <summary>Find the first inventory slot containing the given item ID. Returns -1 if not found.</summary>
    public int FindSlot(string itemId)
    {
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] != null && _slots[i]!.ItemId == itemId)
                return i;
        return -1;
    }

    // === Hotbar references ===

    /// <summary>Get the hotbar reference (item ID) at a given hotbar slot.</summary>
    public string? GetHotbarRef(int index)
    {
        if (index < 0 || index >= HotbarSize) return null;
        return _hotbarRefs[index];
    }

    /// <summary>Set a hotbar reference to an item ID (or null to clear).</summary>
    public void SetHotbarRef(int index, string? itemId)
    {
        if (index < 0 || index >= HotbarSize) return;
        _hotbarRefs[index] = itemId;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Swap two hotbar references.</summary>
    public void SwapHotbarRefs(int a, int b)
    {
        if (a < 0 || a >= HotbarSize || b < 0 || b >= HotbarSize || a == b) return;
        (_hotbarRefs[a], _hotbarRefs[b]) = (_hotbarRefs[b], _hotbarRefs[a]);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Get the actual ItemStack from inventory for a hotbar slot.
    /// Returns null if the ref is empty or item no longer in inventory.
    /// </summary>
    public ItemStack? GetHotbarStack(int index)
    {
        var itemId = GetHotbarRef(index);
        if (itemId == null) return null;
        int slot = FindSlot(itemId);
        return slot >= 0 ? _slots[slot] : null;
    }

    /// <summary>Set the active hotbar slot index (clamped 0-7).</summary>
    public void SetActiveHotbar(int index)
    {
        ActiveHotbarIndex = Math.Clamp(index, 0, HotbarSize - 1);
    }

    /// <summary>Get the item stack for the currently active hotbar slot.</summary>
    public ItemStack? GetActiveHotbarItem() => GetHotbarStack(ActiveHotbarIndex);

    // === Consumable references ===

    /// <summary>Get the consumable reference (item ID) at a given consumable slot.</summary>
    public string? GetConsumableRef(int index)
    {
        if (index < 0 || index >= ConsumableSlotCount) return null;
        return _consumableRefs[index];
    }

    /// <summary>Set a consumable reference. Only accepts Consumable type items.</summary>
    public bool SetConsumableRef(int index, string? itemId)
    {
        if (index < 0 || index >= ConsumableSlotCount) return false;
        if (itemId != null)
        {
            var def = ItemRegistry.Get(itemId);
            if (def == null || def.Type != ItemType.Consumable) return false;
        }
        _consumableRefs[index] = itemId;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Swap two consumable references.</summary>
    public void SwapConsumableRefs(int a, int b)
    {
        if (a < 0 || a >= ConsumableSlotCount || b < 0 || b >= ConsumableSlotCount || a == b) return;
        (_consumableRefs[a], _consumableRefs[b]) = (_consumableRefs[b], _consumableRefs[a]);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Get the actual ItemStack from inventory for a consumable slot.
    /// Returns null if empty or item no longer in inventory.
    /// </summary>
    public ItemStack? GetConsumableStack(int index)
    {
        var itemId = GetConsumableRef(index);
        if (itemId == null) return null;
        int slot = FindSlot(itemId);
        return slot >= 0 ? _slots[slot] : null;
    }

    /// <summary>
    /// Use a consumable from a quick-slot. Decrements quantity in inventory.
    /// Returns the heal/effect value, or 0 if nothing to use.
    /// </summary>
    public float UseConsumable(int index)
    {
        var itemId = GetConsumableRef(index);
        if (itemId == null) return 0;

        int slot = FindSlot(itemId);
        if (slot < 0) return 0;

        var def = ItemRegistry.Get(itemId);
        if (def == null) return 0;

        float value = 0;
        if (def.Stats.TryGetValue("heal", out float heal))
            value = heal;

        _slots[slot]!.Quantity--;
        if (_slots[slot]!.Quantity <= 0)
            _slots[slot] = null;

        // Auto-clear ref if no more of this item in inventory
        if (!HasItem(itemId))
            _consumableRefs[index] = null;

        OnInventoryChanged?.Invoke();
        return value;
    }

    /// <summary>Debug: wipe all slots, hotbar refs, consumable refs, and equipment.</summary>
    public void ClearAll()
    {
        for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
        for (int i = 0; i < _hotbarRefs.Length; i++) _hotbarRefs[i] = null;
        for (int i = 0; i < _consumableRefs.Length; i++) _consumableRefs[i] = null;
        _equipment.Clear();
        OnInventoryChanged?.Invoke();
    }

    // === Equipment ===

    /// <summary>Get the equipped item Id for a given slot, or null.</summary>
    public string? GetEquipped(EquipSlot slot)
        => _equipment.TryGetValue(slot, out var id) ? id : null;

    /// <summary>Get all equipped items as a readonly snapshot.</summary>
    public IReadOnlyDictionary<EquipSlot, string> GetAllEquipment() => _equipment;

    /// <summary>Equip item from inventory slot into its designated EquipSlot.</summary>
    public bool TryEquip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return false;
        var stack = _slots[slotIndex];
        if (stack == null) return false;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def?.EquipSlot == null) return false;

        var equipSlot = def.EquipSlot.Value;
        string? oldItemId = GetEquipped(equipSlot);

        _equipment[equipSlot] = stack.ItemId;
        _slots[slotIndex] = null;
        if (oldItemId != null)
            _slots[slotIndex] = new ItemStack { ItemId = oldItemId, Quantity = 1 };

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Equip item from inventory into a specific equipment slot (validates match).</summary>
    public bool TryEquipToSlot(int slotIndex, EquipSlot targetSlot)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return false;
        var stack = _slots[slotIndex];
        if (stack == null) return false;

        var def = ItemRegistry.Get(stack.ItemId);
        if (def?.EquipSlot == null || def.EquipSlot.Value != targetSlot) return false;

        string? oldItemId = GetEquipped(targetSlot);
        _equipment[targetSlot] = stack.ItemId;
        _slots[slotIndex] = null;
        if (oldItemId != null)
            _slots[slotIndex] = new ItemStack { ItemId = oldItemId, Quantity = 1 };

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Unequip item from equipment slot to a target inventory slot.</summary>
    public bool TryUnequip(EquipSlot equipSlot, int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= SlotCount) return false;
        if (!_equipment.TryGetValue(equipSlot, out var itemId)) return false;

        if (_slots[targetSlot] != null)
        {
            var targetStack = _slots[targetSlot]!;
            var targetDef = ItemRegistry.Get(targetStack.ItemId);
            if (targetDef?.EquipSlot == equipSlot)
            {
                _slots[targetSlot] = new ItemStack { ItemId = itemId, Quantity = 1 };
                _equipment[equipSlot] = targetStack.ItemId;
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        _slots[targetSlot] = new ItemStack { ItemId = itemId, Quantity = 1 };
        _equipment.Remove(equipSlot);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Unequip item to the first empty inventory slot.</summary>
    public bool TryUnequipToEmpty(EquipSlot equipSlot)
    {
        if (!_equipment.ContainsKey(equipSlot)) return false;
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] == null)
                return TryUnequip(equipSlot, i);
        return false;
    }

    // === Save/Load ===

    /// <summary>Populate inventory from a saved GameState.</summary>
    public void LoadFromState(GameState state)
    {
        Gold = Math.Max(0, state.Gold);
        for (int i = 0; i < SlotCount; i++) _slots[i] = null;

        for (int i = 0; i < state.Inventory.Count && i < SlotCount; i++)
        {
            var saved = state.Inventory[i];
            if (saved != null && !string.IsNullOrEmpty(saved.ItemId))
                _slots[i] = new ItemStack { ItemId = saved.ItemId, Quantity = saved.Quantity };
        }

        _equipment.Clear();
        if (state.Equipment != null)
        {
            foreach (var kvp in state.Equipment)
                if (Enum.TryParse<EquipSlot>(kvp.Key, out var slot))
                    _equipment[slot] = kvp.Value;
        }

        // Legacy migration
        if (state.ArmorId != null && !_equipment.ContainsKey(EquipSlot.Armor))
        {
            _equipment[EquipSlot.Armor] = state.ArmorId;
            state.ArmorId = null;
        }

        // Load hotbar refs
        for (int i = 0; i < HotbarSize; i++) _hotbarRefs[i] = null;
        if (state.HotbarSlots != null)
            for (int i = 0; i < state.HotbarSlots.Count && i < HotbarSize; i++)
                _hotbarRefs[i] = state.HotbarSlots[i];

        // Load consumable refs
        for (int i = 0; i < ConsumableSlotCount; i++) _consumableRefs[i] = null;
        if (state.ConsumableRefs != null)
            for (int i = 0; i < state.ConsumableRefs.Count && i < ConsumableSlotCount; i++)
                _consumableRefs[i] = state.ConsumableRefs[i];

        ActiveHotbarIndex = 0;
        Console.WriteLine($"[InventoryManager] Loaded {state.Inventory.Count} items, {_equipment.Count} equipment");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Save current inventory state to a GameState.</summary>
    public void SaveToState(GameState state)
    {
        state.Gold = Gold;
        state.Inventory.Clear();
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] != null)
                state.Inventory.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity });

        state.Equipment = new Dictionary<string, string>();
        foreach (var kvp in _equipment)
            state.Equipment[kvp.Key.ToString()] = kvp.Value;

        state.HotbarSlots = new List<string?>(new string?[HotbarSize]);
        for (int i = 0; i < HotbarSize; i++)
            state.HotbarSlots[i] = _hotbarRefs[i];

        state.ConsumableRefs = new List<string?>(new string?[ConsumableSlotCount]);
        for (int i = 0; i < ConsumableSlotCount; i++)
            state.ConsumableRefs[i] = _consumableRefs[i];

        state.WeaponId = null;
        state.ArmorId = null;
    }
}
