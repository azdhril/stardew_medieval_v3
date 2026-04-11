using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a Tibia-style equipment panel with weapon and armor slots
/// around a character silhouette, plus combined stat display.
/// </summary>
public class EquipmentRenderer
{
    private const int SlotWidth = 40;
    private const int SlotHeight = 40;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private Texture2D _slotNormal = null!;
    private Texture2D _slotSelected = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    /// <summary>
    /// Create a new EquipmentRenderer.
    /// </summary>
    /// <param name="inventory">The InventoryManager to read equipment state from.</param>
    /// <param name="atlas">The SpriteAtlas for item icon lookups.</param>
    public EquipmentRenderer(InventoryManager inventory, SpriteAtlas atlas)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    /// <summary>
    /// Load slot textures, font, and pixel texture for rendering.
    /// </summary>
    /// <param name="device">Graphics device for texture loading.</param>
    /// <param name="font">SpriteFont for labels and stat text.</param>
    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;

        // 1x1 pixel texture for silhouette and stat backgrounds
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var normalStream = File.OpenRead("Content/Sprites/System/UI Elements/Slot/UI_Slot_Normal.png");
            _slotNormal = Texture2D.FromStream(device, normalStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EquipmentRenderer] Failed to load UI_Slot_Normal: {ex.Message}");
            _slotNormal = new Texture2D(device, 1, 1);
            _slotNormal.SetData(new[] { new Color(60, 40, 30) });
        }

        try
        {
            using var selectedStream = File.OpenRead("Content/Sprites/System/UI Elements/Slot/UI_Slot_Selected.png");
            _slotSelected = Texture2D.FromStream(device, selectedStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EquipmentRenderer] Failed to load UI_Slot_Selected: {ex.Message}");
            _slotSelected = new Texture2D(device, 1, 1);
            _slotSelected.SetData(new[] { Color.Gold });
        }

        Console.WriteLine("[EquipmentRenderer] Content loaded");
    }

    /// <summary>
    /// Handle a mouse click on equipment slots.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen coordinates.</param>
    /// <param name="offsetX">Panel X offset on screen.</param>
    /// <param name="offsetY">Panel Y offset on screen.</param>
    /// <returns>"weapon" or "armor" if a slot was clicked, null otherwise.</returns>
    public string? HandleClick(Point mousePos, int offsetX, int offsetY)
    {
        var weaponRect = GetWeaponRect(offsetX, offsetY);
        var armorRect = GetArmorRect(offsetX, offsetY);

        if (weaponRect.Contains(mousePos))
            return "weapon";
        if (armorRect.Contains(mousePos))
            return "armor";

        return null;
    }

    /// <summary>
    /// Draw the equipment panel with silhouette, weapon/armor slots, icons, and stats.
    /// </summary>
    /// <param name="sb">SpriteBatch (must already be in a Begin/End block).</param>
    /// <param name="offsetX">Panel X offset on screen.</param>
    /// <param name="offsetY">Panel Y offset on screen.</param>
    public void Draw(SpriteBatch sb, int offsetX, int offsetY)
    {
        var weaponRect = GetWeaponRect(offsetX, offsetY);
        var armorRect = GetArmorRect(offsetX, offsetY);

        // Draw character silhouette (gray rectangle representing body)
        var silhouetteRect = new Rectangle(offsetX + 55, offsetY + 10, 60, 100);
        sb.Draw(_pixel, silhouetteRect, Color.Gray * 0.4f);

        // Draw silhouette head (small circle approximation)
        var headRect = new Rectangle(offsetX + 70, offsetY - 5, 30, 30);
        sb.Draw(_pixel, headRect, Color.Gray * 0.4f);

        // Weapon slot label and texture
        string weaponLabel = "Weapon";
        var weaponLabelSize = _font.MeasureString(weaponLabel);
        sb.DrawString(_font, weaponLabel,
            new Vector2(weaponRect.X + (SlotWidth - weaponLabelSize.X) / 2, weaponRect.Y - weaponLabelSize.Y - 2),
            Color.LightGray);
        sb.Draw(_slotNormal, weaponRect, Color.White);

        // Armor slot label and texture
        string armorLabel = "Armor";
        var armorLabelSize = _font.MeasureString(armorLabel);
        sb.DrawString(_font, armorLabel,
            new Vector2(armorRect.X + (SlotWidth - armorLabelSize.X) / 2, armorRect.Y - armorLabelSize.Y - 2),
            Color.LightGray);
        sb.Draw(_slotNormal, armorRect, Color.White);

        // Draw equipped weapon icon
        if (_inventory.WeaponId != null)
        {
            var weaponDef = ItemRegistry.Get(_inventory.WeaponId);
            if (weaponDef != null)
            {
                var srcRect = _atlas.GetRect(weaponDef.SpriteId);
                var destRect = new Rectangle(
                    weaponRect.X + 4, weaponRect.Y + 4,
                    SlotWidth - 8, SlotHeight - 8);
                sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);
            }
        }

        // Draw equipped armor icon
        if (_inventory.ArmorId != null)
        {
            var armorDef = ItemRegistry.Get(_inventory.ArmorId);
            if (armorDef != null)
            {
                var srcRect = _atlas.GetRect(armorDef.SpriteId);
                var destRect = new Rectangle(
                    armorRect.X + 4, armorRect.Y + 4,
                    SlotWidth - 8, SlotHeight - 8);
                sb.Draw(_atlas.Texture, destRect, srcRect, Color.White);
            }
        }

        // Draw stat summary below the silhouette
        var (attack, defense) = EquipmentData.GetEquipmentStats(_inventory.WeaponId, _inventory.ArmorId);
        int statsY = silhouetteRect.Bottom + 15;

        sb.DrawString(_font, $"ATK: {attack:F0}", new Vector2(offsetX + 20, statsY), Color.OrangeRed);
        sb.DrawString(_font, $"DEF: {defense:F0}", new Vector2(offsetX + 20, statsY + 18), Color.CornflowerBlue);
    }

    /// <summary>Get the weapon slot rectangle at the given panel offset.</summary>
    private Rectangle GetWeaponRect(int offsetX, int offsetY)
        => new Rectangle(offsetX + 20, offsetY + 60, SlotWidth, SlotHeight);

    /// <summary>Get the armor slot rectangle at the given panel offset.</summary>
    private Rectangle GetArmorRect(int offsetX, int offsetY)
        => new Rectangle(offsetX + 80, offsetY + 40, SlotWidth, SlotHeight);
}
