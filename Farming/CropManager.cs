using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Farming;

/// <summary>
/// Manages crop lifecycle: planting, day-tick growth, wilt checking.
/// </summary>
public class CropManager
{
    private readonly GridManager _grid;
    private readonly List<CropData> _availableCrops;
    private int _selectedCropIndex;

    public CropManager(GridManager grid, List<CropData> availableCrops)
    {
        _grid = grid;
        _availableCrops = availableCrops;
    }

    public CropData? GetSelectedCrop()
    {
        if (_availableCrops.Count == 0) return null;
        return _availableCrops[_selectedCropIndex];
    }

    public void CycleSelectedCrop()
    {
        if (_availableCrops.Count == 0) return;
        _selectedCropIndex = (_selectedCropIndex + 1) % _availableCrops.Count;
        Console.WriteLine($"[CropManager] Selected: {GetSelectedCrop()?.Name}");
    }

    public bool TryPlant(Point tile)
    {
        var cell = _grid.GetCell(tile);
        if (cell == null || !cell.IsTilled)
        {
            Console.WriteLine("[CropManager] Cannot plant: not tilled");
            return false;
        }

        if (cell.Crop != null)
        {
            Console.WriteLine("[CropManager] Cannot plant: crop already exists");
            return false;
        }

        var cropData = GetSelectedCrop();
        if (cropData == null)
        {
            Console.WriteLine("[CropManager] No crop selected");
            return false;
        }

        cell.Crop = new CropInstance(cropData);
        Console.WriteLine($"[CropManager] Planted {cropData.Name} at ({tile.X}, {tile.Y})");
        return true;
    }

    /// <summary>
    /// Called on day advance. Advances watered crops, checks wilt on ripe crops.
    /// </summary>
    public void OnDayAdvanced()
    {
        foreach (var (tile, cell) in _grid.GetAllCells())
        {
            if (cell.Crop == null) continue;

            var crop = cell.Crop;

            // Watered, non-ripe, non-wilted: advance
            if (cell.IsWatered && !crop.IsRipe && !crop.IsWilted)
            {
                bool changed = crop.AdvanceDay();
                if (changed)
                    Console.WriteLine($"[CropManager] {crop.Data.Name} at ({tile.X},{tile.Y}) grew to stage {crop.Data.GetStageIndex(crop.DayCount)}");
            }

            // Ripe crops: accumulate overripe days (every day, regardless of watering)
            if (crop.IsRipe && !crop.IsWilted)
            {
                crop.AdvanceDay(); // counts toward wilt threshold
                if (crop.CheckWilt())
                    Console.WriteLine($"[CropManager] {crop.Data.Name} at ({tile.X},{tile.Y}) WILTED!");
            }
        }
    }

    public void RemoveCrop(Point tile)
    {
        var cell = _grid.GetCell(tile);
        if (cell != null)
        {
            cell.Crop = null;
        }
    }
}
