using System.Collections.Generic;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Maps sprite identifiers to (texture, source rectangle) pairs. Supports multiple
/// backing textures in a single atlas so tools, items, and UI icons can share one
/// lookup API even when they live in separate spritesheet files.
/// </summary>
public class SpriteAtlas
{
    private readonly struct Region
    {
        public readonly Texture2D Texture;
        public readonly Rectangle Rect;
        public Region(Texture2D t, Rectangle r) { Texture = t; Rect = r; }
    }

    private readonly Dictionary<string, Region> _regions = new();

    /// <summary>The primary (items) spritesheet texture. Used as fallback for unknown sprites.</summary>
    public Texture2D Texture { get; }

    public SpriteAtlas(Texture2D primarySheet)
    {
        Texture = primarySheet;
    }

    /// <summary>Register a sprite region on the primary (items) texture.</summary>
    public void Register(string spriteId, int col, int row, int width = 16, int height = 16)
    {
        _regions[spriteId] = new Region(Texture, new Rectangle(col * width, row * height, width, height));
    }

    /// <summary>Register a sprite region on an arbitrary texture (e.g. tool sheet, hand icon).</summary>
    public void RegisterOn(Texture2D sheet, string spriteId, int col, int row, int width = 16, int height = 16)
    {
        _regions[spriteId] = new Region(sheet, new Rectangle(col * width, row * height, width, height));
    }

    /// <summary>Register a sprite region using an explicit rectangle on any texture.</summary>
    public void RegisterRect(Texture2D sheet, string spriteId, Rectangle rect)
    {
        _regions[spriteId] = new Region(sheet, rect);
    }

    /// <summary>Get the source rectangle for a sprite identifier.</summary>
    public Rectangle GetRect(string spriteId)
    {
        if (_regions.TryGetValue(spriteId, out var r)) return r.Rect;
        return new Rectangle(0, 0, 16, 16);
    }

    /// <summary>Get the texture backing a sprite identifier (primary texture if unregistered).</summary>
    public Texture2D GetTexture(string spriteId)
    {
        if (_regions.TryGetValue(spriteId, out var r)) return r.Texture;
        return Texture;
    }

    /// <summary>
    /// Register all sprites on the Pickup_Items.png spritesheet (14 cols).
    /// NOTE: the sheet has 1-row vertical gaps between content bands.
    /// Actual row layout:
    ///   Rows 0-2: crops + rotten pairs (packed, no gaps)
    ///   Row 3:    empty gap
    ///   Row 4:    tree fruits (apple/orange/banana/lime — not implemented)
    ///   Row 5:    fish (cols 0-3, skip), boot (col 4), magic staff (col 5)
    ///   Row 6:    empty gap
    ///   Row 7:    wood logs & stars (not implemented, skipped)
    /// </summary>
    public static SpriteAtlas CreateDefault(Texture2D itemSheet)
    {
        var atlas = new SpriteAtlas(itemSheet);

        // --- Row 0: crops + rotten variants (cabbage, radish, carrot, strawberry, wheat, pepper, beet) ---
        atlas.Register("crop_cabbage",            0, 0);
        atlas.Register("crop_cabbage_rotten",     1, 0);
        atlas.Register("crop_radish",             2, 0);
        atlas.Register("crop_radish_rotten",      3, 0);
        atlas.Register("crop_carrot",             4, 0);
        atlas.Register("crop_carrot_rotten",      5, 0);
        atlas.Register("crop_strawberry",         6, 0);
        atlas.Register("crop_strawberry_rotten",  7, 0);
        atlas.Register("crop_wheat",              8, 0);
        atlas.Register("crop_wheat_rotten",       9, 0);
        atlas.Register("crop_pepper",            10, 0);
        atlas.Register("crop_pepper_rotten",     11, 0);
        atlas.Register("crop_beet",              12, 0);  // no Beet crop yet, registered for future
        atlas.Register("crop_beet_rotten",       13, 0);

        // --- Row 1: shitake, onion, cauliflower, corn, tomato, grape_purple ---
        atlas.Register("crop_shitake",            0, 1);  // no Shitake crop yet
        atlas.Register("crop_shitake_rotten",     1, 1);
        atlas.Register("crop_onion",              2, 1);
        atlas.Register("crop_onion_rotten",       3, 1);
        atlas.Register("crop_cauliflower",        4, 1);
        atlas.Register("crop_cauliflower_rotten", 5, 1);
        atlas.Register("crop_corn",               6, 1);
        atlas.Register("crop_corn_rotten",        7, 1);
        atlas.Register("crop_tomato",             8, 1);
        atlas.Register("crop_tomato_rotten",      9, 1);
        atlas.Register("crop_grape_purple",      10, 1);
        atlas.Register("crop_grape_purple_rotten", 11, 1);
        // cols 12-13: "unknown fruit" — intentionally skipped per design

        // --- Row 2: coffee, cucumber, pumpkin, pineapple, watermelon ---
        atlas.Register("crop_coffee",             0, 2);
        atlas.Register("crop_coffee_rotten",      1, 2);
        atlas.Register("crop_cucumber",           2, 2);  // no Cucumber crop yet
        atlas.Register("crop_cucumber_rotten",    3, 2);
        atlas.Register("crop_pumpkin",            4, 2);
        atlas.Register("crop_pumpkin_rotten",     5, 2);
        atlas.Register("crop_pineapple",          6, 2);
        atlas.Register("crop_pineapple_rotten",   7, 2);
        atlas.Register("crop_watermelon",         8, 2);
        atlas.Register("crop_watermelon_rotten",  9, 2);
        // --- Row 3: gap ---
        // --- Row 4: tree fruits (apple / orange / banana / lime) — not implemented ---
        // --- Row 5: gap ---
        // --- Row 6: equipment / loot ---
        atlas.Register("armor_leather_boots",     4, 6);
        atlas.Register("weapon_magic_staff",      5, 6);
        atlas.Register("loot_bones",              3, 6);
        atlas.Register("loot_stone",              2, 8);
        atlas.Register("loot_mana_crystal",       4, 8);
        atlas.Register("loot_wood",               0, 8);

        return atlas;
    }

    /// <summary>
    /// Register sprites on the Food_Icons.png spritesheet (8 cols). Seeds for existing
    /// crops live here (paired with their crop icon). We only register seed ids for
    /// crops that exist in items.json.
    /// Layout (col,row) for seeds:
    ///   Row 3: seed_wheat(1), seed_watermelon(3), seed_pepper(5)
    ///   Row 4: seed_tomato(1), seed_carrot(3)
    ///   Row 5: seed_grape_purple(1), seed_cabbage(3)
    ///   Row 6: seed_pumpkin(1)
    ///   Row 7: seed_corn(1), seed_turnip(3)
    ///   Row 8: seed_radish(3)
    ///   Row 9: seed_onion(3)
    ///   Row 11: seed_strawberry(1)
    /// </summary>
    public void RegisterFoodIcons(Texture2D foodSheet)
    {
        RegisterOn(foodSheet, "seed_wheat",        1, 3);
        RegisterOn(foodSheet, "seed_watermelon",   3, 3);
        RegisterOn(foodSheet, "seed_pepper",       5, 3);
        RegisterOn(foodSheet, "seed_tomato",       1, 4);
        RegisterOn(foodSheet, "seed_carrot",       3, 4);
        RegisterOn(foodSheet, "seed_grape_purple", 1, 5);
        RegisterOn(foodSheet, "seed_cabbage",      3, 5);
        RegisterOn(foodSheet, "seed_pumpkin",      1, 6);
        RegisterOn(foodSheet, "seed_corn",         1, 7);
        RegisterOn(foodSheet, "seed_turnip",       3, 7);
        RegisterOn(foodSheet, "seed_radish",       3, 8);
        RegisterOn(foodSheet, "seed_onion",        3, 9);
        RegisterOn(foodSheet, "seed_strawberry",   1, 11);

        // Sample edible foods used by the stamina-recovery system.
        RegisterOn(foodSheet, "food_smoked_meat",  0, 0);
        RegisterOn(foodSheet, "food_steak",        1, 0);
        RegisterOn(foodSheet, "food_melon_slice",  2, 0);
    }

    /// <summary>
    /// Register tool sprites on the Tool_Icons.png sheet (160x16, 10 cols).
    /// Order: arco(0), flecha(1), picareta(2), machado(3), espada(4), scythe(5),
    /// regador(6), vara de pesca(7), lamparina(8), tocha(9).
    /// Hoe is mapped to lamparina until a dedicated hoe icon exists.
    /// </summary>
    public void RegisterTools(Texture2D toolSheet)
    {
        RegisterOn(toolSheet, "tool_hoe", 8, 0);            // lamparina (placeholder)
        RegisterOn(toolSheet, "tool_watering_can", 6, 0);   // regador
        RegisterOn(toolSheet, "tool_scythe", 5, 0);         // scythe
        RegisterOn(toolSheet, "tool_pickaxe", 2, 0);
        RegisterOn(toolSheet, "tool_axe", 3, 0);
        RegisterOn(toolSheet, "tool_bow", 0, 0);
        RegisterOn(toolSheet, "tool_arrow", 1, 0);
        RegisterOn(toolSheet, "tool_sword", 4, 0);
        RegisterOn(toolSheet, "tool_fishing_rod", 7, 0);
        RegisterOn(toolSheet, "tool_lantern", 8, 0);
        RegisterOn(toolSheet, "tool_torch", 9, 0);
    }

    /// <summary>Register the hand icon on a standalone 16x16 texture.</summary>
    public void RegisterHand(Texture2D handTexture)
    {
        RegisterOn(handTexture, "tool_hand", 0, 0);
    }

    /// <summary>Register consumable potion sprites from potions.png (21×15 grid, 16×16 cells).</summary>
    public void RegisterPotions(Texture2D potionSheet)
    {
        RegisterOn(potionSheet, "consumable_health_potion", 3, 0);
    }

    /// <summary>Register the Gold_Coin icon from a standalone texture (full rect).</summary>
    public void RegisterGoldCoin(Texture2D goldTexture)
    {
        RegisterRect(goldTexture, "Gold_Coin", new Rectangle(0, 0, goldTexture.Width, goldTexture.Height));
    }

    /// <summary>
    /// Register the 6 upgrade bags from <c>bags_upgrades.png</c> (96×16, horizontal layout).
    /// Each 16×16 cell maps to a sprite id of the form <c>bag_upgrade_{index}</c> where
    /// index matches <see cref="BagDefinition.SpriteIndex"/>.
    /// </summary>
    public void RegisterBagUpgrades(Texture2D bagsSheet)
    {
        for (int i = 0; i < 6; i++)
            RegisterOn(bagsSheet, $"bag_upgrade_{i}", i, 0);
    }

    /// <summary>
    /// Register fishing-related sprites from <c>Tools/fishing.png</c> (144×96, 9×6 cells of 16px).
    /// Row 2 col 0 = the equipped fishing rod (overrides the placeholder from <see cref="RegisterTools"/>).
    /// Row 1 (cols 0..8) = bobbers/floats — registered as <c>fishing_bobber_{0..8}</c> for the
    /// future fishing minigame's water-line indicator.
    /// </summary>
    public void RegisterFishingTools(Texture2D fishingSheet)
    {
        RegisterOn(fishingSheet, "tool_fishing_rod", 0, 2);
        for (int i = 0; i < 9; i++)
            RegisterOn(fishingSheet, $"fishing_bobber_{i}", i, 1);
    }

    /// <summary>
    /// Register fish sprites on <c>fish_all.png</c> (160×160, 10 cols × 10 rows of 16×16 cells).
    /// Walks <see cref="FishRegistry"/> and registers each entry under the sprite id
    /// <c>fish_{lowercase_id}</c> — must run AFTER <see cref="FishRegistry.Initialize"/>.
    /// </summary>
    public void RegisterFishes(Texture2D fishSheet)
    {
        foreach (var fish in FishRegistry.All.Values)
        {
            string spriteId = fish.Id.ToLowerInvariant();
            RegisterOn(fishSheet, spriteId, fish.SpriteCol, fish.SpriteRow);
        }
    }

    /// <summary>
    /// Register the big-bobber arcade-joystick sprite sheet (<c>big_bobber.png</c>,
    /// 48×48 = 3 cols × 3 rows of 16×16 cells). Layout: row 0 = up animation
    /// (3 frames), row 1 = up-left diagonal, row 2 = left. Other 5 directions
    /// (down, right, down-right, down-left, up-right) are derived at draw time
    /// via horizontal/vertical sprite flips so the asset stays compact. The
    /// frame index (0..2) maps to the joystick's lean magnitude so harder pushes
    /// show more lean.
    /// </summary>
    public void RegisterBigBobber(Texture2D sheet)
    {
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                RegisterOn(sheet, $"big_bobber_{row}_{col}", col, row);
    }
}
