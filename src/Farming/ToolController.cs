using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Farming;

public enum ToolType { Hands, Hoe, WateringCan, Seeds, Scythe, Axe, Pickaxe }

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
    private readonly Func<ToolType, Point, bool>? _handleWorldToolAction;

    public ToolController(GridManager grid, CropManager cropManager, PlayerEntity player,
        InventoryManager inventory, Action<string, int, Vector2> spawnDrop,
        Func<ToolType, Point, bool>? handleWorldToolAction = null)
    {
        _grid = grid;
        _cropManager = cropManager;
        _player = player;
        _inventory = inventory;
        _spawnDrop = spawnDrop;
        _handleWorldToolAction = handleWorldToolAction;
    }

    private ToolType DeriveEquippedTool()
    {
        var id = ActiveItemId;
        if (id == null) return ToolType.Hands;
        switch (id)
        {
            case "Hoe": return ToolType.Hoe;
            case "Axe": return ToolType.Axe;
            case "Watering_Can": return ToolType.WateringCan;
            case "Scythe": return ToolType.Scythe;
            case "Pickaxe": return ToolType.Pickaxe;
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
                TryPlantActiveSeed(tile);
                break;
            case ToolType.Axe:
            case ToolType.Pickaxe:
                _handleWorldToolAction?.Invoke(EquippedTool, tile);
                break;
            case ToolType.Hands:
            case ToolType.Scythe:
                TryHarvest(tile);
                break;
        }
    }

    /// <summary>
    /// Plant the crop matching the seed currently in the active hotbar slot
    /// (e.g. "Cabbage_Seed" -> crop "Cabbage"). Consumes 1 seed on success and
    /// clears the hotbar reference if the stack runs out.
    /// </summary>
    private void TryPlantActiveSeed(Point tile)
    {
        string? seedId = ActiveItemId;
        if (seedId == null || !seedId.EndsWith("_Seed"))
        {
            _cropManager.TryPlant(tile);
            return;
        }
        // Convention: "Cabbage_Seed" -> "Cabbage", "Cosmic_Carrot_Seed" -> "Cosmic Carrot"
        string cropName = seedId.Substring(0, seedId.Length - "_Seed".Length).Replace('_', ' ');
        if (_cropManager.TryPlant(tile, cropName))
        {
            int consumed = _inventory.TryConsume(seedId, 1);
            if (consumed > 0 && !_inventory.HasItem(seedId))
            {
                // Stack emptied — clear any hotbar/consumable refs pointing at it
                for (int i = 0; i < InventoryManager.HotbarSize; i++)
                {
                    if (_inventory.GetHotbarRef(i) == seedId)
                        _inventory.SetHotbarRef(i, null);
                }
            }
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
            // Wilted harvest: if a rotten variant exists in the registry, drop 1x of it.
            // Otherwise just clear the tile (Stardew style). Rotten id convention:
            // "{YieldItemName}_Rotten" (e.g. Cabbage -> Cabbage_Rotten).
            string rottenId = crop.Data.YieldItemName + "_Rotten";
            if (ItemRegistry.Get(rottenId) != null)
            {
                Vector2 rotPos = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8);
                _spawnDrop(rottenId, 1, rotPos);
                Console.WriteLine($"[Harvest] Spawned 1x {rottenId} from wilted {crop.Data.Name}");
            }
            else
            {
                Console.WriteLine($"[Harvest] Cleared wilted {crop.Data.Name} (no rotten variant)");
            }
            _cropManager.RemoveCrop(tile);
            cell.IsTilled = false;
            cell.IsWatered = false;
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
