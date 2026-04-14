using System;
using System.Collections.Generic;
using System.Linq;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static registry of all item definitions. Loaded from items.json at startup.
/// Provides lookup by Id and filtering by ItemType.
/// </summary>
public static class ItemRegistry
{
    private static readonly Dictionary<string, ItemDefinition> _items = new();

    /// <summary>
    /// Load all item definitions from a JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to items.json file.</param>
    public static void Initialize(string jsonPath = "src/Data/items.json")
    {
        _items.Clear();
        try
        {
            var json = System.IO.File.ReadAllText(jsonPath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var wrapper = System.Text.Json.JsonSerializer.Deserialize<ItemsFile>(json, options);
            if (wrapper?.Items != null)
            {
                foreach (var item in wrapper.Items)
                    _items[item.Id] = item;
            }
            Console.WriteLine($"[ItemRegistry] Loaded {_items.Count} items");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ItemRegistry] Failed to load: {ex.Message}");
        }
    }

    /// <summary>
    /// Get an item definition by its unique Id.
    /// </summary>
    /// <param name="id">Item Id (e.g. "Cabbage", "Hoe").</param>
    /// <returns>The ItemDefinition, or null if not found.</returns>
    public static ItemDefinition? Get(string id)
    {
        _items.TryGetValue(id, out var item);
        return item;
    }

    /// <summary>
    /// Get all items of a specific type.
    /// </summary>
    /// <param name="type">The ItemType to filter by.</param>
    /// <returns>List of matching ItemDefinitions.</returns>
    public static List<ItemDefinition> GetByType(ItemType type)
        => _items.Values.Where(i => i.Type == type).ToList();

    /// <summary>
    /// All loaded item definitions indexed by Id.
    /// </summary>
    public static Dictionary<string, ItemDefinition> All => _items;

    private class ItemsFile
    {
        public string Version { get; set; } = "";
        public List<ItemDefinition> Items { get; set; } = new();
    }
}
