using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Registry of harvestable world resource definitions. Mirrors ChestRegistry so
/// future resources can be added data-first without baking scene-specific logic.
/// </summary>
public static class ResourceRegistry
{
    private static readonly Dictionary<string, ResourceNodeData> _nodes = new();
    private static Texture2D? _sheet;

    public static IReadOnlyDictionary<string, ResourceNodeData> All => _nodes;

    public static void Initialize(GraphicsDevice device)
    {
        if (_sheet == null)
            _sheet = LoadTexture(device, "assets/Sprites/Scenario/6_Trees_16x16.png");

        _nodes.Clear();

        // Large standalone leafy tree from the top-left block of the sheet.
        Register(new ResourceNodeData
        {
            Id = "tree_broadleaf_large",
            DisplayName = "Broadleaf Tree",
            RequiredTool = ResourceToolKind.Axe,
            MaxHits = 4,
            Sheet = _sheet,
            SourceRect = new Rectangle(129, 36, 79, 92),
            DepletedSourceRect = new Rectangle(154, 145, 34, 31),
            CollisionSize = new Point(24, 16),
            CollisionOffset = new Point(0, -4),
            DepletedCollisionSize = new Point(18, 10),
            DepletedCollisionOffset = new Point(0, -2),
            OcclusionStartY = 0,
            OverlayFadeAlpha = 0.45f,
            Drops = new List<ResourceDropData>
            {
                new() { ItemId = "Wood", MinQuantity = 3, MaxQuantity = 5 }
            }
        });
    }

    public static ResourceNodeData? Get(string id)
    {
        _nodes.TryGetValue(id, out var data);
        return data;
    }

    private static void Register(ResourceNodeData data) => _nodes[data.Id] = data;

    private static Texture2D? LoadTexture(GraphicsDevice device, string path)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            return Texture2D.FromStream(device, stream);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[ResourceRegistry] Failed to load {path}: {ex.Message}");
            return null;
        }
    }
}
