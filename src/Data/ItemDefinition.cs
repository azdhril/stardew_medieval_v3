using System.Collections.Generic;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Unified item definition. All game items (crops, tools, weapons, etc.) share this model.
/// Loaded from items.json via ItemRegistry.
/// </summary>
public class ItemDefinition
{
    /// <summary>Unique item identifier (e.g. "Cabbage_Seed", "Hoe").</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name shown in UI.</summary>
    public string Name { get; set; } = "";

    /// <summary>Short tooltip text shown in compact UI surfaces.</summary>
    public string Description { get; set; } = "";

    /// <summary>Item category.</summary>
    public ItemType Type { get; set; }

    /// <summary>Rarity tier for drop rates and visual styling.</summary>
    public Rarity Rarity { get; set; } = Rarity.Common;

    /// <summary>Maximum stack size in inventory.</summary>
    public int StackLimit { get; set; } = 99;

    /// <summary>
    /// Base economic value used by the shop. Sell price defaults to BasePrice/2.
    /// Shop buy price lives in <c>ShopStock</c> (may differ for markup). A value
    /// of 0 means the item is non-economic (quest/loot-only, cannot be sold).
    /// </summary>
    public int BasePrice { get; set; } = 0;

    /// <summary>Sprite identifier for rendering.</summary>
    public string SpriteId { get; set; } = "";

    /// <summary>Flexible stat bag (e.g. "damage": 10, "heal": 5).</summary>
    public Dictionary<string, float> Stats { get; set; } = new();

    /// <summary>Which equipment slot this item goes in, or null if not equippable.</summary>
    public EquipSlot? EquipSlot { get; set; }
}
