using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Farming;

public enum ToolType { Hands, Hoe, WateringCan, Seeds, Scythe }

/// <summary>
/// Dispatches farming actions based on whatever is in the player's active hotbar slot.
/// No keybinds for tool switching — tools are inventory items placed on the hotbar.
/// Active slot empty → Hands. Seed item → Seeds. Tool item → corresponding ToolType.
/// </summary>
public class ToolController
{
    public ToolType EquippedTool => DeriveEquippedTool();

    /// <summary>The item id currently in the active hotbar slot (e.g. "Hoe", "Cabbage_Seed"), or null.</summary>
    public string? ActiveItemId => _inventory.GetHotbarRef(_inventory.ActiveHotbarIndex);

    private readonly GridManager _grid;
    private readonly CropManager _cropManager;
    private readonly PlayerEntity _player;
    private readonly InventoryManager _inventory;
    private readonly Action<string, int, Vector2> _spawnDrop;

    public ToolController(GridManager grid, CropManager cropManager, PlayerEntity player,
        InventoryManager inventory, Action<string, int, Vector2> spawnDrop)
    {
        _grid = grid;
        _cropManager = cropManager;
        _player = player;
        _inventory = inventory;
        _spawnDrop = spawnDrop;
    }

    private ToolType DeriveEquippedTool()
    {
        var id = ActiveItemId;
        if (id == null) return ToolType.Hands;
        switch (id)
        {
            case "Hoe": return ToolType.Hoe;
            case "Watering_Can": return ToolType.WateringCan;
            case "Scythe": return ToolType.Scythe;
        }
        var def = ItemRegistry.Get(id);
        if (def != null && def.Type == ItemType.Seed) return ToolType.Seeds;
        return ToolType.Hands;
    }

    public void Update(InputManager input)
    {
        // Cycle crops with Tab when the active slot is a seed
        if (input.IsKeyPressed(Keys.Tab) && EquippedTool == ToolType.Seeds)
        {
            _cropManager.CycleSelectedCrop();
        }

        // E and LMB both trigger the tool action on the facing tile.
        // LMB is suppressed when the active item is a weapon so CombatManager
        // can handle the click as a melee swing instead.
        bool actionPressed = input.InteractPressed
            || (input.IsLeftClickPressed && !IsActiveItemWeapon());

        if (actionPressed)
        {
            try
            {
                var tile = _player.GetFacingTile();
                DoAction(tile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ToolController] EXCEPTION during DoAction: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    private bool IsActiveItemWeapon()
    {
        var id = ActiveItemId;
        if (id == null) return false;
        var def = ItemRegistry.Get(id);
        return def?.Type == ItemType.Weapon;
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
