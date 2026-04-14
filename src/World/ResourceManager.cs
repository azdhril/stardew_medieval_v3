using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.World;

/// <summary>
/// Owns the live resource nodes for a scene and provides save/load helpers.
/// </summary>
public class ResourceManager
{
    private readonly List<ResourceNode> _nodes = new();

    public IReadOnlyList<ResourceNode> All => _nodes;

    public void Add(ResourceNode node) => _nodes.Add(node);

    public void Clear() => _nodes.Clear();

    public void Update(float deltaTime)
    {
        foreach (var node in _nodes)
            node.Update(deltaTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var node in _nodes)
            node.Draw(spriteBatch);
    }

    public ResourceNode? GetNodeAtTile(Point tile)
    {
        foreach (var node in _nodes)
            if (node.OccupiesTile(tile))
                return node;

        return null;
    }

    public void LoadFrom(List<ResourceSaveData> state)
    {
        _nodes.Clear();
        foreach (var saved in state)
            _nodes.Add(new ResourceNode(saved.InstanceId, saved.NodeId, new Point(saved.TileX, saved.TileY), saved.HitsRemaining));
    }

    public List<ResourceSaveData> GetSaveData()
    {
        var data = new List<ResourceSaveData>(_nodes.Count);
        foreach (var node in _nodes)
            data.Add(node.ToSaveData());
        return data;
    }
}
