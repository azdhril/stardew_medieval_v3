using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.World;

/// <summary>
/// Owns all chest instances in the active map and handles save/load snapshots.
/// </summary>
public class ChestManager
{
    private readonly List<ChestInstance> _chests = new();

    public IReadOnlyList<ChestInstance> All => _chests;

    public void Add(ChestInstance chest)
    {
        _chests.Add(chest);
    }

    public void Update(float deltaTime)
    {
        foreach (var chest in _chests)
            chest.Update(deltaTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var chest in _chests)
            chest.Draw(spriteBatch);
    }

    public void DrawBeforePlayer(SpriteBatch spriteBatch, PlayerEntity player)
    {
        foreach (var chest in _chests)
            if (!chest.ShouldDrawAfterPlayer(player))
                chest.Draw(spriteBatch);
    }

    public void DrawAfterPlayer(SpriteBatch spriteBatch, PlayerEntity player)
    {
        foreach (var chest in _chests)
            if (chest.ShouldDrawAfterPlayer(player))
                chest.Draw(spriteBatch);
    }

    public ChestInstance? GetChestAtFacingTile(Point facingTile)
    {
        foreach (var chest in _chests)
            if (chest.IntersectsFacingTile(facingTile))
                return chest;
        return null;
    }

    public List<ChestSaveData> GetSaveData()
    {
        var data = new List<ChestSaveData>(_chests.Count);
        foreach (var chest in _chests)
        {
            data.Add(new ChestSaveData
            {
                InstanceId = chest.InstanceId,
                VariantId = chest.VariantId,
                TileX = chest.Tile.X,
                TileY = chest.Tile.Y,
                Capacity = chest.Container.Capacity,
                Contents = chest.Container.GetSaveData(),
            });
        }
        return data;
    }

    public void LoadFrom(List<ChestSaveData> data)
    {
        _chests.Clear();
        foreach (var save in data)
        {
            var chest = new ChestInstance(
                save.InstanceId,
                save.VariantId,
                new Point(save.TileX, save.TileY),
                save.Capacity > 0 ? save.Capacity : ChestInstance.DefaultCapacity);
            chest.Container.LoadFrom(save.Contents ?? new List<ItemStack?>());
            _chests.Add(chest);
        }
    }
}
