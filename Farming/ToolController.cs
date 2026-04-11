using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Farming;

public enum ToolType { Hands, Hoe, WateringCan, Seeds, Scythe }

/// <summary>
/// Manages tool selection and dispatches farming actions.
/// Same keybinds as Unity version: H=Hoe, G=WateringCan, R=Seeds, F=Hands, Tab=CycleCrop
/// </summary>
public class ToolController
{
    public ToolType EquippedTool { get; private set; } = ToolType.Hands;

    private readonly GridManager _grid;
    private readonly CropManager _cropManager;
    private readonly PlayerEntity _player;
    private readonly Action<string, int, Vector2> _spawnDrop;

    public ToolController(GridManager grid, CropManager cropManager, PlayerEntity player, Action<string, int, Vector2> spawnDrop)
    {
        _grid = grid;
        _cropManager = cropManager;
        _player = player;
        _spawnDrop = spawnDrop;
    }

    public void Update(InputManager input)
    {
        // Tool switching
        if (input.IsKeyPressed(Keys.H))
        {
            EquippedTool = ToolType.Hoe;
            Console.WriteLine("[ToolController] Equipped: Hoe");
        }
        if (input.IsKeyPressed(Keys.G))
        {
            EquippedTool = ToolType.WateringCan;
            Console.WriteLine("[ToolController] Equipped: WateringCan");
        }
        if (input.IsKeyPressed(Keys.R))
        {
            EquippedTool = ToolType.Seeds;
            Console.WriteLine("[ToolController] Equipped: Seeds");
        }
        if (input.IsKeyPressed(Keys.F))
        {
            EquippedTool = ToolType.Hands;
            Console.WriteLine("[ToolController] Equipped: Hands");
        }
        if (input.IsKeyPressed(Keys.C))
        {
            EquippedTool = ToolType.Scythe;
            Console.WriteLine("[ToolController] Equipped: Scythe");
        }

        // Cycle crops with Tab when Seeds equipped
        if (input.IsKeyPressed(Keys.Tab) && EquippedTool == ToolType.Seeds)
        {
            _cropManager.CycleSelectedCrop();
        }

        // Interact with E — FARM-01 fix: target the facing tile, not the standing tile
        if (input.InteractPressed)
        {
            var tile = _player.GetFacingTile();
            DoAction(tile);
        }
    }

    private void DoAction(Point tile)
    {
        switch (EquippedTool)
        {
            case ToolType.Hoe:
                _grid.TryTill(tile, _player.Stats);
                break;
            case ToolType.WateringCan:
                _grid.TryWater(tile, _player.Stats);
                break;
            case ToolType.Seeds:
                _cropManager.TryPlant(tile);
                break;
            case ToolType.Hands:
            case ToolType.Scythe:
                TryHarvest(tile);
                break;
        }
    }

    private void TryHarvest(Point tile)
    {
        var cell = _grid.GetCell(tile);
        if (cell?.Crop == null)
        {
            Console.WriteLine("[Harvest] No crop here");
            return;
        }

        var crop = cell.Crop;

        if (crop.IsWilted)
        {
            // Clear wilted crop, reset cell (Stardew style)
            _cropManager.RemoveCrop(tile);
            cell.IsTilled = false;
            cell.IsWatered = false;
            Console.WriteLine($"[Harvest] Cleared wilted {crop.Data.Name}");
            return;
        }

        if (!crop.IsRipe)
        {
            Console.WriteLine("[Harvest] Crop not ripe yet");
            return;
        }

        // Harvest: spawn item drop at crop tile center (FARM-03)
        string yieldItem = crop.Data.YieldItemName;
        int yieldQty = crop.Data.YieldQuantity;
        Vector2 worldPos = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8);
        _spawnDrop(yieldItem, yieldQty, worldPos);
        Console.WriteLine($"[Harvest] Spawned {yieldQty}x {yieldItem} as item drop");

        // Remove crop and clear tilled state (Stardew style - must re-till)
        _cropManager.RemoveCrop(tile);
        cell.IsTilled = false;
        cell.IsWatered = false;
    }
}
