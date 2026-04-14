using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Shared definition for a harvestable world resource such as a tree or ore vein.
/// </summary>
public class ResourceNodeData
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public ResourceToolKind RequiredTool { get; init; } = ResourceToolKind.None;
    public int MaxHits { get; init; } = 1;
    public Texture2D? Sheet { get; init; }
    public Rectangle SourceRect { get; init; }
    public Rectangle DepletedSourceRect { get; init; }
    public Point CollisionSize { get; init; } = new(16, 10);
    public Point CollisionOffset { get; init; } = new(0, -5);
    public Point DepletedCollisionSize { get; init; } = new(12, 8);
    public Point DepletedCollisionOffset { get; init; } = new(0, -3);
    public int OcclusionStartY { get; init; }
    public float OverlayFadeAlpha { get; init; } = 0.55f;
    public List<ResourceDropData> Drops { get; init; } = new();
}
