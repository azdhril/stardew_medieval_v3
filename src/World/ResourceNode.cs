using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.World;

/// <summary>
/// Runtime instance of a harvestable world resource.
/// Keeps scene placement dynamic while sharing behavior through ResourceNodeData.
/// </summary>
public class ResourceNode : Entity
{
    private static readonly Random _rng = new();

    public string InstanceId { get; }
    public string NodeId => Data.Id;
    public Point Tile { get; }
    public ResourceNodeData Data { get; }
    public int HitsRemaining { get; private set; }
    public bool IsDepleted => HitsRemaining <= 0;

    public Vector2 WorldAnchor => new(
        Tile.X * TileMap.TileSize + TileMap.TileSize / 2f,
        (Tile.Y + 1) * TileMap.TileSize);

    public override Rectangle CollisionBox
    {
        get
        {
            Point size = IsDepleted ? Data.DepletedCollisionSize : Data.CollisionSize;
            Point offset = IsDepleted ? Data.DepletedCollisionOffset : Data.CollisionOffset;
            int x = (int)WorldAnchor.X - size.X / 2 + offset.X;
            int y = (int)WorldAnchor.Y - size.Y + offset.Y;
            return new Rectangle(x, y, size.X, size.Y);
        }
    }

    public ResourceNode(string instanceId, string nodeId, Point tile, int? hitsRemaining = null)
    {
        InstanceId = instanceId;
        Tile = tile;
        Data = ResourceRegistry.Get(nodeId)
            ?? throw new InvalidOperationException($"Unknown resource node '{nodeId}'.");

        HitsRemaining = Math.Clamp(hitsRemaining ?? Data.MaxHits, 0, Data.MaxHits);
        Position = WorldAnchor;
        HP = MaxHP = Data.MaxHits;
    }

    public bool OccupiesTile(Point tile) => Tile == tile;

    public bool TryGather(ResourceToolKind tool, Action<string, int, Vector2> spawnDrop)
    {
        if (IsDepleted || tool != Data.RequiredTool)
            return false;

        HitsRemaining = Math.Max(0, HitsRemaining - 1);
        HP = HitsRemaining;
        _flashTimer = FlashDuration;

        if (IsDepleted)
            SpawnDrops(spawnDrop);

        return true;
    }

    public override void Update(float deltaTime)
    {
        UpdateFlash(deltaTime);
        Position = WorldAnchor;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Data.Sheet == null)
            return;

        var src = IsDepleted ? Data.DepletedSourceRect : Data.SourceRect;
        var dest = new Rectangle(
            (int)(WorldAnchor.X - src.Width / 2f),
            (int)(WorldAnchor.Y - src.Height),
            src.Width,
            src.Height);

        spriteBatch.Draw(Data.Sheet, dest, src, IsFlashing ? Color.IndianRed : Color.White);
    }

    public ResourceSaveData ToSaveData() => new()
    {
        InstanceId = InstanceId,
        NodeId = NodeId,
        TileX = Tile.X,
        TileY = Tile.Y,
        HitsRemaining = HitsRemaining
    };

    private void SpawnDrops(Action<string, int, Vector2> spawnDrop)
    {
        foreach (var drop in Data.Drops)
        {
            int qty = _rng.Next(drop.MinQuantity, drop.MaxQuantity + 1);
            if (qty > 0)
                spawnDrop(drop.ItemId, qty, WorldAnchor);
        }
    }
}
