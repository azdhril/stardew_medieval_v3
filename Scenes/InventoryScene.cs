using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Inventory overlay scene with two tabs: a 20-slot grid view and a Tibia-style
/// equipment view. Opened via I key, closed via I or Escape.
/// Drawn on top of the game world with a semi-transparent dark background.
/// </summary>
public class InventoryScene : Scene
{
    private const int PanelWidth = 280;
    private const int PanelHeight = 260;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;

    private InventoryGridRenderer _gridRenderer = null!;
    private EquipmentRenderer _equipRenderer = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    /// <summary>Active tab: 0 = Grid, 1 = Equipment.</summary>
    private int _activeTab;

    /// <summary>
    /// Create a new InventoryScene overlay.
    /// </summary>
    /// <param name="services">Shared service container.</param>
    /// <param name="inventory">The InventoryManager to display and manipulate.</param>
    /// <param name="atlas">The SpriteAtlas for item icon rendering.</param>
    public InventoryScene(ServiceContainer services, InventoryManager inventory, SpriteAtlas atlas)
        : base(services)
    {
        _inventory = inventory;
        _atlas = atlas;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

        // 1x1 pixel for background dimming and panel
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Initialize sub-renderers
        _gridRenderer = new InventoryGridRenderer(_inventory, _atlas);
        _gridRenderer.LoadContent(device, _font);

        _equipRenderer = new EquipmentRenderer(_inventory, _atlas);
        _equipRenderer.LoadContent(device, _font);

        _activeTab = 0;

        Console.WriteLine("[InventoryScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        // Close inventory on I or Escape (instant, no fade)
        if (input.IsKeyPressed(Keys.I) || input.IsKeyPressed(Keys.Escape))
        {
            Services.SceneManager.PopImmediate();
            return;
        }

        // Tab switch on Tab key
        if (input.IsKeyPressed(Keys.Tab))
        {
            _activeTab = _activeTab == 0 ? 1 : 0;
            _gridRenderer.ClearSelection();
            Console.WriteLine($"[InventoryScene] Switched to tab {_activeTab}");
        }

        // Click handling
        if (input.IsLeftClickPressed)
        {
            var viewport = Services.GraphicsDevice.Viewport;
            int panelX = (viewport.Width - PanelWidth) / 2;
            int panelY = (viewport.Height - PanelHeight) / 2;
            int contentX = panelX + 10;
            int contentY = panelY + 30;

            if (_activeTab == 0)
            {
                // Grid tab: click to select/move items
                _gridRenderer.HandleClick(input.MousePosition, contentX, contentY);
            }
            else
            {
                // Equipment tab: click to equip/unequip
                var clickResult = _equipRenderer.HandleClick(input.MousePosition, contentX, contentY);
                if (clickResult != null)
                {
                    int selectedSlot = _gridRenderer.SelectedSlot;
                    if (selectedSlot >= 0)
                    {
                        // Have a selected inventory slot: try to equip from it
                        _inventory.TryEquip(selectedSlot);
                        _gridRenderer.ClearSelection();
                    }
                    else
                    {
                        // No selection: try to unequip to first empty slot
                        int firstEmpty = FindFirstEmptySlot();
                        if (firstEmpty >= 0)
                        {
                            _inventory.TryUnequip(clickResult, firstEmpty);
                        }
                    }
                }
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        int screenWidth = viewport.Width;
        int screenHeight = viewport.Height;

        // Semi-transparent dark background overlay
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.6f);
        spriteBatch.End();

        // Panel background
        int panelX = (screenWidth - PanelWidth) / 2;
        int panelY = (screenHeight - PanelHeight) / 2;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Panel solid background
        spriteBatch.Draw(_pixel,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            Color.DarkSlateGray * 0.9f);

        // Tab buttons at top
        string itemsLabel = "Items";
        string equipLabel = "Equipment";
        var itemsSize = _font.MeasureString(itemsLabel);
        var equipSize = _font.MeasureString(equipLabel);

        int tabY = panelY + 5;
        int tabItemsX = panelX + 20;
        int tabEquipX = panelX + PanelWidth / 2 + 10;

        // Active tab in white, inactive in gray
        spriteBatch.DrawString(_font, itemsLabel,
            new Vector2(tabItemsX, tabY),
            _activeTab == 0 ? Color.White : Color.Gray);
        spriteBatch.DrawString(_font, equipLabel,
            new Vector2(tabEquipX, tabY),
            _activeTab == 1 ? Color.White : Color.Gray);

        // Underline for active tab
        if (_activeTab == 0)
        {
            spriteBatch.Draw(_pixel,
                new Rectangle(tabItemsX, tabY + (int)itemsSize.Y + 1, (int)itemsSize.X, 2),
                Color.White);
        }
        else
        {
            spriteBatch.Draw(_pixel,
                new Rectangle(tabEquipX, tabY + (int)equipSize.Y + 1, (int)equipSize.X, 2),
                Color.White);
        }

        // Content area
        int contentX = panelX + 10;
        int contentY = panelY + 30;

        if (_activeTab == 0)
        {
            // Grid tab
            _gridRenderer.Draw(spriteBatch, contentX, contentY);

            // Show selected item name at bottom of panel
            int selectedSlot = _gridRenderer.SelectedSlot;
            if (selectedSlot >= 0)
            {
                var stack = _inventory.GetSlot(selectedSlot);
                if (stack != null)
                {
                    var def = ItemRegistry.Get(stack.ItemId);
                    if (def != null)
                    {
                        string infoText = $"{def.Name} ({def.Rarity})";
                        var infoSize = _font.MeasureString(infoText);
                        spriteBatch.DrawString(_font, infoText,
                            new Vector2(panelX + (PanelWidth - infoSize.X) / 2, panelY + PanelHeight - 20),
                            Color.LightGoldenrodYellow);
                    }
                }
            }
        }
        else
        {
            // Equipment tab
            _equipRenderer.Draw(spriteBatch, contentX, contentY);
        }

        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[InventoryScene] Unloaded");
    }

    /// <summary>
    /// Find the first empty inventory slot index, or -1 if full.
    /// </summary>
    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < InventoryManager.SlotCount; i++)
        {
            if (_inventory.GetSlot(i) == null)
                return i;
        }
        return -1;
    }
}
