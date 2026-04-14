using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Registry of chest variants loaded from assets/Sprites/Items/Containers/chests.png.
/// Sheet is 288x128: 9 variant columns x 4 frame rows (32x32 cells), with art
/// sizes 18x16 (closed) and 18x20 (opened) bottom-aligned inside each cell.
/// </summary>
public static class ChestRegistry
{
    public const int VariantCount = 9;
    public const int FrameCount = 4;
    public const int CellWidth = 32;
    public const int CellHeight = 32;
    public const int ArtWidth = 18;
    public const int ArtHeightClosed = 16;
    public const int ArtHeightOpened = 20;

    /// <summary>Frame index representing a fully closed chest.</summary>
    public const int FrameClosed = 0;
    /// <summary>Frame index representing a fully open chest.</summary>
    public const int FrameOpen = 3;

    /// <summary>Seconds per frame when playing the open/close animation.</summary>
    public const float FrameDuration = 0.08f;

    private static readonly Dictionary<string, ChestData> _chests = new();
    private static Texture2D? _sheet;

    public static Texture2D? Sheet => _sheet;
    public static Dictionary<string, ChestData> All => _chests;

    public static void Initialize(GraphicsDevice device)
    {
        const string path = "assets/Sprites/Items/Containers/chests.png";
        _sheet = LoadTexture(device, path);

        // Variant names are placeholders based on visual color; rename as design firms up.
        Register(0, "chest_wood",   "Wooden Chest");
        Register(1, "chest_iron",   "Iron Chest");
        Register(2, "chest_ruby",   "Ruby Chest");
        Register(3, "chest_jade",   "Jade Chest");
        Register(4, "chest_oak",    "Oak Chest");
        Register(5, "chest_sapphire","Sapphire Chest");
        Register(6, "chest_gold",   "Gold Chest");
        Register(7, "chest_shadow", "Shadow Chest");
        Register(8, "chest_cursed", "Cursed Chest");
    }

    private static void Register(int variantIndex, string id, string displayName)
    {
        _chests[id] = new ChestData
        {
            Id = id,
            DisplayName = displayName,
            VariantIndex = variantIndex,
            Sheet = _sheet,
        };
    }

    public static ChestData? Get(string id)
    {
        _chests.TryGetValue(id, out var data);
        return data;
    }

    public static ChestData? GetByIndex(int variantIndex)
    {
        foreach (var c in _chests.Values)
            if (c.VariantIndex == variantIndex) return c;
        return null;
    }

    public static List<ChestData> GetAllChests() => new(_chests.Values);

    private static Texture2D? LoadTexture(GraphicsDevice device, string path)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            return Texture2D.FromStream(device, stream);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[ChestRegistry] Failed to load {path}: {ex.Message}");
            return null;
        }
    }
}
