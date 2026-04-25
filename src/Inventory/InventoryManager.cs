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
public class InventoryManager : IItemSlotCollection
{
    /// <summary>
    /// Absolute upper bound on inventory size — equals the largest registered bag tier.
    /// The backing slot array is sized to this so bag upgrades never need to resize memory;
    /// <see cref="Capacity"/> is the currently-accessible subset.
    /// </summary>
    public static readonly int SlotCount = BagRegistry.MaxCapacity;

    /// <summary>Number of hotbar reference slots.</summary>
    public const int HotbarSize = 8;

    /// <summary>Number of consumable quick-slots (Q only, E kept free for actions).</summary>
    public const int ConsumableSlotCount = 1;

    private readonly ItemStack?[] _slots = new ItemStack?[SlotCount];
    private readonly string?[] _hotbarRefs = new string?[HotbarSize];
    private readonly string?[] _consumableRefs = new string?[ConsumableSlotCount];

    /// <summary>
    /// Identifier of the currently-equipped bag. Drives <see cref="Capacity"/> and
    /// the inventory subtitle shown in the Inventory overlay. Persisted in the save.
    /// </summary>
    public string BagId { get; private set; } = BagRegistry.StarterId;

    /// <summary>Max water charges a single watering can holds.</summary>
    public const int MaxWateringCanCharges = 20;

    /// <summary>
    /// Current water charges in the player's watering can. Persisted in the save.
    /// Refilled to <see cref="MaxWateringCanCharges"/> when the player uses the can on a
    /// water tile (lake/river); decremented on every successful TryWater call.
    /// </summary>
    public int WateringCanCharges { get; private set; } = 0;

    /// <summary>Refill the watering can to its maximum capacity.</summary>
    public void RefillWateringCan()
    {
        if (WateringCanCharges == MaxWateringCanCharges) return;
        WateringCanCharges = MaxWateringCanCharges;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Consume one water charge. Returns true on success, false if the can is empty
    /// (callers should suppress the watering action — the player must refill at water).
    /// </summary>
    public bool TryConsumeWateringCharge()
    {
        if (WateringCanCharges <= 0) return false;
        WateringCanCharges--;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Display name of the equipped bag (e.g. "Bolsa de Couro").</summary>
    public string BagName => BagRegistry.Get(BagId).Name;

    /// <summary>The bag definition for the next upgrade, or null when maxed out.</summary>
    public BagDefinition? NextBag => BagRegistry.Next(BagRegistry.Get(BagId));

    public int Capacity => BagRegistry.Get(BagId).Capacity;

    /// <summary>
    /// Swap the equipped bag for <paramref name="bagId"/>. Returns true if the id
    /// is valid and different from the current bag. Slots beyond the new capacity
    /// retain their contents in memory (not lost), but become inaccessible until
    /// an upgrade restores capacity — keeps downgrades safe against item loss.
    /// </summary>
    public bool TryEquipBag(string bagId)
    {
        var next = BagRegistry.Get(bagId);
        if (next.Id == BagId) return false;
        BagId = next.Id;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Shortcut — upgrade to the next bag tier, if one exists.</summary>
    public bool TryUpgradeBag()
    {
        var next = NextBag;
        return next != null && TryEquipBag(next.Id);
    }

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

    public void SetSlot(int index, ItemStack? stack)
    {
        if (index < 0 || index >= SlotCount) return;
        _slots[index] = stack == null ? null : new ItemStack { ItemId = stack.ItemId, Quantity = stack.Quantity, Quality = stack.Quality };
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Try to add items to inventory. Returns remaining that couldn't fit.</summary>
    public int TryAdd(string itemId, int quantity = 1) => TryAdd(itemId, quantity, 0);

    /// <summary>
    /// Try to add items to inventory with a specific quality grade. Stacks only
    /// merge with existing stacks of the same itemId AND same quality, so a
    /// 2-star Trout drop never collapses into a 1-star Trout stack.
    /// </summary>
    public int TryAdd(string itemId, int quantity, int quality)
    {
        var def = ItemRegistry.Get(itemId);
        if (def == null)
        {
            Console.WriteLine($"[InventoryManager] Unknown item: {itemId}");
            return quantity;
        }

        int remaining = quantity;
        int stackLimit = def.StackLimit;
        int accessible = Capacity;

        // Top off existing stacks first — but ONLY within accessible capacity so new items never
        // land in a slot the player can't reach after a bag downgrade. Same itemId + same Quality.
        for (int i = 0; i < accessible && remaining > 0; i++)
        {
            if (_slots[i] != null && _slots[i]!.ItemId == itemId && _slots[i]!.Quality == quality)
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

        for (int i = 0; i < accessible && remaining > 0; i++)
        {
            if (_slots[i] == null)
            {
                int toAdd = Math.Min(remaining, stackLimit);
                _slots[i] = new ItemStack { ItemId = itemId, Quantity = toAdd, Quality = quality };
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

        if (from != null && to != null && from.ItemId == to.ItemId && from.Quality == to.Quality)
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

    /// <summary>
    /// Clears hotbar and consumable refs that point to items no longer present.
    /// Useful after moving the last stack of an item into a chest or other container.
    /// </summary>
    public void PruneBrokenReferences()
    {
        bool changed = false;

        for (int i = 0; i < _hotbarRefs.Length; i++)
        {
            var itemId = _hotbarRefs[i];
            if (itemId != null && !HasItem(itemId))
            {
                _hotbarRefs[i] = null;
                changed = true;
            }
        }

        for (int i = 0; i < _consumableRefs.Length; i++)
        {
            var itemId = _consumableRefs[i];
            if (itemId != null && !HasItem(itemId))
            {
                _consumableRefs[i] = null;
                changed = true;
            }
        }

        if (changed)
            OnInventoryChanged?.Invoke();
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

    /// <summary>
    /// Move the active hotbar selection by the given step count with wrap-around.
    /// Positive values move right; negative values move left.
    /// </summary>
    public void CycleActiveHotbar(int step)
    {
        if (HotbarSize <= 0 || step == 0) return;

        int next = (ActiveHotbarIndex + step) % HotbarSize;
        if (next < 0)
            next += HotbarSize;

        ActiveHotbarIndex = next;
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
    /// Use a consumable from a quick-slot. Decrements quantity in inventory and
    /// returns the effect payload for the caller to apply.
    /// </summary>
    public ConsumableUseResult UseConsumable(int index)
    {
        var itemId = GetConsumableRef(index);
        if (itemId == null) return ConsumableUseResult.None;

        int slot = FindSlot(itemId);
        if (slot < 0) return ConsumableUseResult.None;

        var def = ItemRegistry.Get(itemId);
        if (def == null) return ConsumableUseResult.None;

        float healAmount = 0f;
        if (def.Stats.TryGetValue("heal", out float heal))
            healAmount = heal;

        float staminaRestorePct = 0f;
        if (def.Stats.TryGetValue("stamina_restore_pct", out float rawPct))
            staminaRestorePct = rawPct > 1f ? rawPct / 100f : rawPct;

        _slots[slot]!.Quantity--;
        if (_slots[slot]!.Quantity <= 0)
            _slots[slot] = null;

        // Auto-clear ref if no more of this item in inventory
        if (!HasItem(itemId))
            _consumableRefs[index] = null;

        OnInventoryChanged?.Invoke();
        return new ConsumableUseResult(true, itemId, healAmount, staminaRestorePct);
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

    /// <summary>
    /// Force-remove equipment from a slot without returning it to inventory.
    /// Used by death penalty system. Returns true if an item was actually removed.
    /// </summary>
    public bool ForceRemoveEquipment(EquipSlot slot)
    {
        bool removed = _equipment.Remove(slot);
        if (removed) OnInventoryChanged?.Invoke();
        return removed;
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
        // Migrate pre-bag saves (BagId is null) to the starter bag.
        BagId = state.BagId ?? BagRegistry.StarterId;
        WateringCanCharges = Math.Clamp(state.WateringCanCharges, 0, MaxWateringCanCharges);
        for (int i = 0; i < SlotCount; i++) _slots[i] = null;

        for (int i = 0; i < state.Inventory.Count && i < SlotCount; i++)
        {
            var saved = state.Inventory[i];
            if (saved != null && !string.IsNullOrEmpty(saved.ItemId))
                _slots[i] = new ItemStack { ItemId = saved.ItemId, Quantity = saved.Quantity, Quality = saved.Quality };
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
        state.BagId = BagId;
        state.WateringCanCharges = WateringCanCharges;
        state.Inventory.Clear();
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] != null)
                state.Inventory.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity, Quality = _slots[i]!.Quality });

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

    /// <summary>
    /// Sort inventory slots by type, then rarity, then name. Empty slots go to the end.
    /// Hotbar references re-resolve via item id, so slot order changing does not break
    /// them as long as the item id is still somewhere in inventory.
    /// </summary>
    public void SortByDefault()
    {
        var items = new List<ItemStack>();
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
                items.Add(new ItemStack { ItemId = _slots[i]!.ItemId, Quantity = _slots[i]!.Quantity, Quality = _slots[i]!.Quality });
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

            int idCompare = string.Compare(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase);
            if (idCompare != 0) return idCompare;

            // Higher quality first within identical itemId so 3⭐ trout sit before 1⭐ trout.
            return b.Quality.CompareTo(a.Quality);
        });

        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = i < items.Count ? items[i] : null;
    }

    private static int GetTypeOrder(ItemType type) => type switch
    {
        ItemType.Tool => 0,
        ItemType.Weapon => 1,
        ItemType.Armor => 2,
        ItemType.Consumable => 3,
        ItemType.Seed => 4,
        ItemType.Crop => 5,
        ItemType.Fish => 6,
        ItemType.Loot => 7,
        _ => 99,
    };

    private static int GetRarityOrder(Rarity rarity) => rarity switch
    {
        Rarity.Rare => 2,
        Rarity.Uncommon => 1,
        _ => 0,
    };
}
