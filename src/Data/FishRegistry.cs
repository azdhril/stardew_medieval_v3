using System;
using System.Collections.Generic;
using System.Linq;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static registry of fishing parameters loaded from <c>src/Data/fish.json</c>.
/// Mirrors <see cref="ItemRegistry"/> but only holds fishing-specific data;
/// the player-visible item (name/rarity/price/sprite) lives in items.json
/// keyed by the same <see cref="FishData.Id"/>.
/// </summary>
public static class FishRegistry
{
    private static readonly Dictionary<string, FishData> _fishes = new();

    public static IReadOnlyDictionary<string, FishData> All => _fishes;

    /// <summary>Load fish definitions from the given json file.</summary>
    public static void Initialize(string jsonPath = "src/Data/fish.json")
    {
        _fishes.Clear();
        try
        {
            var json = System.IO.File.ReadAllText(jsonPath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var wrapper = System.Text.Json.JsonSerializer.Deserialize<FishesFile>(json, options);
            if (wrapper?.Fishes != null)
            {
                foreach (var fish in wrapper.Fishes)
                    _fishes[fish.Id] = fish;
            }
            Console.WriteLine($"[FishRegistry] Loaded {_fishes.Count} fish entries");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FishRegistry] Failed to load: {ex.Message}");
        }
    }

    /// <summary>Get fishing data for the given item id, or null if unregistered.</summary>
    public static FishData? Get(string id)
    {
        _fishes.TryGetValue(id, out var fish);
        return fish;
    }

    /// <summary>
    /// Filter the roster by biome / season / time-of-day / weather. Used by
    /// the fishing roll once the player gets a bite. Returned list is suitable
    /// for weighted random selection on <see cref="FishData.Weight"/>.
    /// </summary>
    public static List<FishData> GetCandidates(FishBiome biome, string season, string timeOfDay, string weather)
    {
        return _fishes.Values.Where(f =>
            f.Biome == biome
            && (f.Seasons.Count == 0 || f.Seasons.Contains(season, StringComparer.OrdinalIgnoreCase))
            && (f.TimeOfDay == "Any" || f.TimeOfDay.Equals(timeOfDay, StringComparison.OrdinalIgnoreCase))
            && (f.Weather == "Any" || f.Weather.Equals(weather, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    private class FishesFile
    {
        public string Version { get; set; } = "";
        public List<FishData> Fishes { get; set; } = new();
    }
}
