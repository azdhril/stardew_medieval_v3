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

    /// <summary>Item category.</summary>
    public ItemType Type { get; set; }

    /// <summary>Rarity tier for drop rates and visual styling.</summary>
    public Rarity Rarity { get; set; } = Rarity.Common;

    /// <summary>Maximum stack size in inventory.</summary>
    public int StackLimit { get; set; } = 99;

    /// <summary>Sprite identifier for rendering.</summary>
    public string SpriteId { get; set; } = "";

    /// <summary>Flexible stat bag (e.g. "damage": 10, "heal": 5).</summary>
    public Dictionary<string, float> Stats { get; set; } = new();

    /// <summary>Which equipment slot this item goes in, or null if not equippable.</summary>
    public EquipSlot? EquipSlot { get; set; }
}
