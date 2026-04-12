using System.Collections.Generic;
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
    /// Create a default SpriteAtlas with all known item SpriteIds mapped to their
    /// grid positions in the 7_Pickup_Items_16x16.png spritesheet.
    /// </summary>
    public static SpriteAtlas CreateDefault(Texture2D itemSheet)
    {
        var atlas = new SpriteAtlas(itemSheet);

        // Row 0: Vegetables / Crops
        atlas.Register("crop_cabbage", 0, 0);
        atlas.Register("crop_carrot", 1, 0);
        atlas.Register("crop_cauliflower", 2, 0);
        atlas.Register("crop_pepper", 3, 0);
        atlas.Register("crop_radish", 4, 0);
        atlas.Register("crop_strawberry", 5, 0);
        atlas.Register("crop_turnip", 6, 0);
        atlas.Register("crop_onion", 7, 0);
        atlas.Register("crop_cotton", 8, 0);
        atlas.Register("crop_grape_purple", 9, 0);
        atlas.Register("crop_grape_pink", 10, 0);

        // Row 1: More crops and fruits
        atlas.Register("crop_tomato", 0, 1);
        atlas.Register("crop_corn", 1, 1);
        atlas.Register("crop_pumpkin", 2, 1);
        atlas.Register("crop_watermelon", 3, 1);
        atlas.Register("crop_pineapple", 4, 1);
        atlas.Register("crop_zucchini", 5, 1);
        atlas.Register("crop_wheat", 6, 1);
        atlas.Register("crop_coffee", 7, 1);
        atlas.Register("crop_cosmic_carrot", 8, 1);
        atlas.Register("crop_prickly_pear", 9, 1);

        // Row 2: Seeds
        atlas.Register("seed_cabbage", 0, 2);
        atlas.Register("seed_carrot", 1, 2);
        atlas.Register("seed_cauliflower", 2, 2);
        atlas.Register("seed_pepper", 3, 2);
        atlas.Register("seed_radish", 4, 2);
        atlas.Register("seed_strawberry", 5, 2);
        atlas.Register("seed_turnip", 6, 2);
        atlas.Register("seed_onion", 7, 2);
        atlas.Register("seed_cotton", 8, 2);
        atlas.Register("seed_grape_purple", 9, 2);
        atlas.Register("seed_grape_pink", 10, 2);

        // Row 3: More seeds
        atlas.Register("seed_tomato", 0, 3);
        atlas.Register("seed_corn", 1, 3);
        atlas.Register("seed_pumpkin", 2, 3);
        atlas.Register("seed_watermelon", 3, 3);
        atlas.Register("seed_pineapple", 4, 3);
        atlas.Register("seed_zucchini", 5, 3);
        atlas.Register("seed_wheat", 6, 3);
        atlas.Register("seed_coffee", 7, 3);
        atlas.Register("seed_cosmic_carrot", 8, 3);
        atlas.Register("seed_prickly_pear", 9, 3);

        // Weapons (placeholders on items sheet)
        atlas.Register("weapon_iron_sword", 0, 4);
        atlas.Register("weapon_steel_sword", 1, 4);
        atlas.Register("weapon_flame_blade", 2, 4);

        // Armor
        atlas.Register("armor_leather", 3, 4);
        atlas.Register("armor_iron", 4, 4);
        atlas.Register("armor_dragon", 5, 4);

        // Consumables
        atlas.Register("consumable_health_potion", 6, 4);

        return atlas;
    }

    /// <summary>
    /// Register tool sprites on the Tool_Icons_NO_Outline.png sheet (160x16, 10 cols).
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
}
