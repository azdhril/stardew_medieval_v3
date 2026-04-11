using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Maps sprite identifiers to source rectangles within a spritesheet texture.
/// Used for rendering item icons in inventory and hotbar UI.
/// </summary>
public class SpriteAtlas
{
    private readonly Dictionary<string, Rectangle> _regions = new();

    /// <summary>The spritesheet texture containing all item icons.</summary>
    public Texture2D Texture { get; }

    /// <summary>
    /// Create a new SpriteAtlas for the given spritesheet.
    /// </summary>
    /// <param name="spriteSheet">The spritesheet texture.</param>
    public SpriteAtlas(Texture2D spriteSheet)
    {
        Texture = spriteSheet;
    }

    /// <summary>
    /// Register a sprite region by its identifier and grid position.
    /// </summary>
    /// <param name="spriteId">Unique sprite identifier (e.g. "crop_cabbage").</param>
    /// <param name="col">Column index in the grid.</param>
    /// <param name="row">Row index in the grid.</param>
    /// <param name="width">Cell width in pixels (default 16).</param>
    /// <param name="height">Cell height in pixels (default 16).</param>
    public void Register(string spriteId, int col, int row, int width = 16, int height = 16)
    {
        _regions[spriteId] = new Rectangle(col * width, row * height, width, height);
    }

    /// <summary>
    /// Get the source rectangle for a sprite identifier.
    /// </summary>
    /// <param name="spriteId">The sprite identifier to look up.</param>
    /// <returns>The source rectangle, or a fallback (0,0,16,16) if not found.</returns>
    public Rectangle GetRect(string spriteId)
    {
        if (_regions.TryGetValue(spriteId, out var rect))
            return rect;
        return new Rectangle(0, 0, 16, 16);
    }

    /// <summary>
    /// Create a default SpriteAtlas with all known item SpriteIds mapped to their
    /// grid positions in the 7_Pickup_Items_16x16.png spritesheet.
    /// </summary>
    /// <param name="itemSheet">The item spritesheet texture.</param>
    /// <returns>A configured SpriteAtlas instance.</returns>
    public static SpriteAtlas CreateDefault(Texture2D itemSheet)
    {
        var atlas = new SpriteAtlas(itemSheet);

        // Row 0: Vegetables / Crops (left to right based on spritesheet inspection)
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

        // Row 2: Seeds (mapping seeds to similar positions)
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

        // Row 4: Misc items (nuts, mushrooms, etc.)
        // Mapping tools/weapons/armor to existing visible sprites as placeholders
        // until dedicated weapon/armor spritesheets are added
        atlas.Register("tool_hoe", 0, 2);       // seed packet placeholder
        atlas.Register("tool_watering_can", 1, 2);
        atlas.Register("tool_scythe", 2, 2);

        // Weapons — use distinct row 4 sprites as placeholders
        atlas.Register("weapon_iron_sword", 0, 4);
        atlas.Register("weapon_steel_sword", 1, 4);
        atlas.Register("weapon_flame_blade", 2, 4);

        // Armor — use row 4 sprites as placeholders
        atlas.Register("armor_leather", 3, 4);
        atlas.Register("armor_iron", 4, 4);
        atlas.Register("armor_dragon", 5, 4);

        // Consumables
        atlas.Register("consumable_health_potion", 6, 4);

        return atlas;
    }
}
