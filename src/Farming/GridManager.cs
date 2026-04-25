using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Farming;

/// <summary>
/// Owns the grid-cell farming state dictionary. Handles tilling, watering, visual overlays.
/// </summary>
public class GridManager
{
    private readonly Dictionary<Point, CellData> _cells = new();
    private readonly TileMap _map;

    // Overlay textures (generated at runtime - simple colored rectangles)
    private Texture2D _tilledTexture = null!;
    private Texture2D _wateredTexture = null!;

    public GridManager(TileMap map)
    {
        _map = map;
    }

    public void LoadContent(GraphicsDevice device)
    {
        // Create simple overlay textures
        _tilledTexture = CreateSolidTexture(device, new Color(101, 67, 33, 160)); // brown
        _wateredTexture = CreateSolidTexture(device, new Color(60, 40, 20, 200)); // dark brown
    }

    private static Texture2D CreateSolidTexture(GraphicsDevice device, Color color)
    {
        var tex = new Texture2D(device, TileMap.TileSize, TileMap.TileSize);
        var data = new Color[TileMap.TileSize * TileMap.TileSize];
        Array.Fill(data, color);
        tex.SetData(data);
        return tex;
    }

    public CellData GetOrCreateCell(Point tile)
    {
        if (!_cells.TryGetValue(tile, out var cell))
        {
            cell = new CellData
            {
                IsTillable = _map.IsFarmZone(tile.X, tile.Y)
            };
            _cells[tile] = cell;
        }
        return cell;
    }

    public CellData? GetCell(Point tile)
    {
        _cells.TryGetValue(tile, out var cell);
        return cell;
    }

    public bool TryTill(Point tile, PlayerStats stats)
    {
        if (!_map.IsFarmZone(tile.X, tile.Y))
        {
            Console.WriteLine("[GridManager] Cannot till: not in farm zone");
            return false;
        }

        // Peek an existing cell without creating one — a failed till must NOT leave an
        // empty CellData behind, otherwise DrawFarmZoneHint stops tinting the tile and
        // the player sees a pale "ghost" square where nothing happened.
        _cells.TryGetValue(tile, out var existing);
        if (existing?.IsTilled == true)
        {
            Console.WriteLine("[GridManager] Already tilled");
            return false;
        }

        if (!stats.TrySpendStamina(5))
        {
            Console.WriteLine("[GridManager] Not enough stamina to till");
            return false;
        }

        var cell = existing ?? GetOrCreateCell(tile);
        cell.IsTilled = true;
        Console.WriteLine($"[GridManager] Tilled ({tile.X}, {tile.Y})");
        return true;
    }

    public bool TryWater(Point tile, PlayerStats stats)
    {
        var cell = GetCell(tile);
        if (cell == null || !cell.IsTilled)
        {
            Console.WriteLine("[GridManager] Cannot water: cell is not tilled");
            return false;
        }

        if (cell.IsWatered)
        {
            Console.WriteLine("[GridManager] Already watered");
            return false;
        }

        if (!stats.TrySpendStamina(3))
        {
            Console.WriteLine("[GridManager] Not enough stamina to water");
            return false;
        }

        cell.IsWatered = true;
        Console.WriteLine($"[GridManager] Watered ({tile.X}, {tile.Y})");
        return true;
    }

    /// <summary>
    /// Called on day advance: reset all watering flags.
    /// </summary>
    public void OnDayAdvanced()
    {
        foreach (var cell in _cells.Values)
        {
            cell.IsWatered = false;
        }
    }

    public List<FarmCellSaveData> GetSaveData()
    {
        return _cells.Select(kvp => new FarmCellSaveData
        {
            CellX = kvp.Key.X,
            CellY = kvp.Key.Y,
            IsTilled = kvp.Value.IsTilled,
            IsWatered = kvp.Value.IsWatered,
            HasCrop = kvp.Value.Crop != null,
            CropDataName = kvp.Value.Crop?.Data.Name ?? "",
            CropDayCount = kvp.Value.Crop?.DayCount ?? 0,
            IsWilted = kvp.Value.Crop?.IsWilted ?? false
        }).ToList();
    }

    public void LoadFromSaveData(List<FarmCellSaveData> data, Dictionary<string, CropData> cropRegistry)
    {
        _cells.Clear();
        foreach (var save in data)
        {
            var tile = new Point(save.CellX, save.CellY);
            var cell = new CellData
            {
                IsTillable = _map.IsFarmZone(tile.X, tile.Y),
                IsTilled = save.IsTilled,
                IsWatered = save.IsWatered
            };

            if (save.HasCrop && cropRegistry.TryGetValue(save.CropDataName, out var cropData))
            {
                var crop = new CropInstance(cropData);
                crop.SetState(save.CropDayCount, save.IsWilted);
                cell.Crop = crop;
            }

            _cells[tile] = cell;
        }
    }

    public void ClearAllCells() => _cells.Clear();

    /// <summary>
    /// Draw soil overlays (tilled/watered).
    /// </summary>
    public void DrawOverlays(SpriteBatch spriteBatch, Rectangle viewArea)
    {
        foreach (var (tile, cell) in _cells)
        {
            if (!cell.IsTilled) continue;

            var destRect = new Rectangle(
                tile.X * TileMap.TileSize,
                tile.Y * TileMap.TileSize,
                TileMap.TileSize,
                TileMap.TileSize
            );

            // Only draw if in view
            if (!viewArea.Intersects(destRect)) continue;

            if (cell.IsWatered)
                spriteBatch.Draw(_wateredTexture, destRect, Color.White);
            else
                spriteBatch.Draw(_tilledTexture, destRect, Color.White);
        }
    }

    /// <summary>
    /// Draw crops on top of soil.
    /// </summary>
    public void DrawCrops(SpriteBatch spriteBatch, Rectangle viewArea)
    {
        foreach (var (tile, cell) in _cells)
        {
            if (cell.Crop == null) continue;

            var crop = cell.Crop;
            if (crop.Data.GrowthSheet == null) continue;

            var srcRect = crop.Data.GetSourceRect(crop.DayCount, crop.IsWilted);
            int spriteH = crop.Data.SpriteHeight;

            // Crop is drawn anchored at bottom of tile (grows upward)
            var destRect = new Rectangle(
                tile.X * TileMap.TileSize,
                tile.Y * TileMap.TileSize + TileMap.TileSize - spriteH,
                16,
                spriteH
            );

            if (!viewArea.Intersects(destRect)) continue;

            spriteBatch.Draw(crop.Data.GrowthSheet, destRect, srcRect, Color.White);
        }
    }

    public IEnumerable<KeyValuePair<Point, CellData>> GetAllCells() => _cells;
}
