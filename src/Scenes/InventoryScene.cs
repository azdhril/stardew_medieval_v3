using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.UI.Widgets;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Inventory overlay scene: equipment silhouette on the left, 20-slot item grid on the right.
/// Dragging items to the hotbar at the bottom of the screen sets hotbar references.
/// The hotbar itself is rendered by FarmScene underneath this overlay.
///
/// Per quick 260424-2af: this scene has NO standalone clickable buttons — only
/// the drag-driven slot grid plus a slot-hover tooltip. Left-click EDGE detection
/// uses <see cref="InputManager.IsLeftClickPressed"/> while press-HELD detection
/// (for drag continuation) continues via <see cref="Mouse.GetState"/>, matching
/// the ChestScene pattern. <c>_wasMouseDown</c> is retained solely for MouseUp
/// edge detection in the drag state machine (legitimate per RESEARCH audit).
/// </summary>
public class InventoryScene : Scene
{
    private const int PanelWidth = 735;
    private const int PanelHeight = 447;

    private readonly InventoryManager _inventory;
    private readonly SpriteAtlas _atlas;
    private readonly HotbarRenderer _hotbar;

    // Content layout (mirrors ChestScene — panel chrome + two cream sub-panes).
    private const int ContentMarginX = 18;
    private const int ContentTop = 72;
    private const int ContentBottom = 44;
    private const int SeparatorW = 14;
    private const int EquipPaneW = 230;
    private const int CloseButtonSize = 32;
    private const int PanePadding = 12;
    private const int PaneTitleHeight = 22;

    private InventoryGridRenderer _gridRenderer = null!;
    private SpriteFontBase _font = null!;
    private SpriteFontBase _titleFont = null!;
    private Texture2D _pixel = null!;
    private UITheme _theme = null!;
    private CloseButton _closeBtn = null!;
    private IconButton _sortBtn = null!;

    // Drag state-machine tracker — legitimate use per RESEARCH audit.
    private bool _wasMouseDown;

    public InventoryScene(ServiceContainer services, InventoryManager inventory, SpriteAtlas atlas, HotbarRenderer hotbar)
        : base(services)
    {
        _inventory = inventory;
        _atlas = atlas;
        _hotbar = hotbar;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Fonts!.GetFont(FontRole.Body, 18);
        _titleFont = Services.Fonts!.GetFont(FontRole.Bold, 24);

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        if (Services.Theme == null)
        {
            Services.Theme = new UITheme();
            Services.Theme.LoadContent(Services.GraphicsDevice);
        }
        _theme = Services.Theme;

        _gridRenderer = new InventoryGridRenderer(_inventory, _atlas);
        _gridRenderer.LoadContent(device, _font);
        _gridRenderer.SetTheme(_theme);

        var viewport = device.Viewport;
        _gridRenderer.SetHotbar(_hotbar, viewport.Width, viewport.Height);

        _closeBtn = new CloseButton(_theme.BtnIconX)
        {
            OnClickAction = () =>
            {
                _gridRenderer.CancelDrag();
                Services.SceneManager.PopImmediate();
            },
            Tooltip = "Close",
        };
        Ui.Register(_closeBtn);

        // Reorder (broom) — bare icon, no background chrome, matches ChestScene's sort button.
        _sortBtn = new IconButton(_theme.IconSort)
        {
            OnClickAction = () => _inventory.SortByDefault(),
            Tooltip = "Reorganizar",
        };
        Ui.Register(_sortBtn);

        _wasMouseDown = false;
        Console.WriteLine("[InventoryScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        if (input.IsKeyPressed(Keys.I) || input.IsKeyPressed(Keys.Escape))
        {
            _gridRenderer.CancelDrag();
            Services.SceneManager.PopImmediate();
            return;
        }

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY, out _, out Rectangle gridPaneRect, out int equipX, out int equipY, out int gridX, out int gridY);

        // Refresh close button bounds each frame (viewport-responsive).
        _closeBtn.Bounds = GetCloseButtonRect(panelX, panelY);
        _sortBtn.Bounds = GetSortButtonRect(gridPaneRect);

        // Widget layer FIRST — consumes the click if close X was hit.
        if (Ui.Update(deltaTime, input))
            return;

        // Press-held detection for drag continuation. Edge detection flows via
        // InputManager.IsLeftClickPressed for the initial press; MouseUp edge
        // is detected by comparing the current held state to _wasMouseDown.
        var ms = Mouse.GetState();
        bool mouseDown = ms.LeftButton == ButtonState.Pressed;
        Point mousePos = input.MousePosition;

        if (input.IsLeftClickPressed)
            _gridRenderer.HandleMouseDown(mousePos, gridX, gridY, equipX, equipY);
        else if (mouseDown && _wasMouseDown)
            _gridRenderer.UpdateDrag(mousePos);
        else if (!mouseDown && _wasMouseDown)
        {
            // First chance: drag released OUTSIDE the modal panel → toss the grid item to
            // the world (via the gameplay scene's SpawnItemDrop hook). If consumed, skip
            // the regular drop-target dispatch.
            var panelRect = new Rectangle(panelX, panelY, PanelWidth, PanelHeight);
            bool tossed = _gridRenderer.TryDropOutsidePanel(mousePos, panelRect,
                (id, qty) => Services.SpawnItemDrop?.Invoke(id, qty, Services.Player?.GetFootPosition() ?? Microsoft.Xna.Framework.Vector2.Zero));
            if (!tossed)
                _gridRenderer.HandleMouseUp(mousePos, gridX, gridY, equipX, equipY);
        }

        // _wasMouseDown retained SOLELY for drag MouseUp edge detection
        // (per RESEARCH audit — legitimate drag-state use, not button click tracking).
        _wasMouseDown = mouseDown;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        int screenWidth = viewport.Width;
        int screenHeight = viewport.Height;

        // Dim background
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.55f);
        spriteBatch.End();

        GetPanelPosition(out int panelX, out int panelY);
        GetLayout(panelX, panelY,
            out Rectangle equipPaneRect, out Rectangle gridPaneRect,
            out int equipX, out int equipY, out int gridX, out int gridY);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Panel frame — 9-slice pixel art (consistent with ChestScene).
        NineSlice.Draw(spriteBatch, _theme.PanePopup,
            new Rectangle(panelX, panelY, PanelWidth, PanelHeight),
            _theme.PanePopupInsets);

        // Title — shared helper, identical visual to Chest/Shop modal titles.
        WidgetHelpers.DrawPanelTitle(spriteBatch, _titleFont, "Inventario",
            new Rectangle(panelX + 28, panelY - 3, PanelWidth - 56, 50),
            Color.LightGoldenrodYellow);

        // Close X at top-right — sits on the panel chrome, no pane overlap risk.
        _closeBtn.Draw(spriteBatch);

        // Cream slot-pane backgrounds — same look as Bolsa/Baú in ChestScene.
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, equipPaneRect, _theme.PanelSlotPaneInsets);
        NineSlice.Draw(spriteBatch, _theme.PanelSlotPane, gridPaneRect, _theme.PanelSlotPaneInsets);

        // Sort broom — drawn AFTER the cream pane so it isn't covered by the pane chrome.
        _sortBtn.Draw(spriteBatch);

        // Sub-pane subtitles — left = "Equipamentos", right = current bag's display name.
        // Style matches ChestScene's "Bolsa"/"Baú" (smaller letter spacing, no shadow).
        WidgetHelpers.DrawPanelTitle(spriteBatch, _titleFont, "Equipamentos",
            new Rectangle(
                equipPaneRect.X + PanePadding,
                equipPaneRect.Y + 10,
                equipPaneRect.Width - PanePadding * 2,
                PaneTitleHeight),
            Color.LightGoldenrodYellow, letterSpacing: 1f, shadow: false);
        WidgetHelpers.DrawPanelTitle(spriteBatch, _titleFont, _inventory.BagName,
            new Rectangle(
                gridPaneRect.X + PanePadding,
                gridPaneRect.Y + 10,
                gridPaneRect.Width - PanePadding * 2,
                PaneTitleHeight),
            Color.LightGoldenrodYellow, letterSpacing: 1f, shadow: false);

        // Equipment cluster + inventory grid on top of the cream panes.
        _gridRenderer.Draw(spriteBatch, gridX, gridY, equipX, equipY);

        // ATK / DEF badges anchored to the equipment pane's bottom corners
        // (left = attack, right = defense). Independent of cluster position.
        DrawEquipmentStats(spriteBatch, equipPaneRect);

        // Item tooltip — shared floating-panel style (matches ChestScene).
        DrawHoverItemTooltip(spriteBatch, gridX, gridY, screenWidth, screenHeight);

        // Focus outline + widget tooltip overlay.
        Ui.Draw(spriteBatch, _pixel, _font, screenWidth, screenHeight);

        spriteBatch.End();
    }

    /// <summary>
    /// Compute the two cream sub-pane rects and the equipment / grid content origins
    /// inside them. Mirrors ChestScene.GetLayout so both overlays feel the same.
    /// Also sets the grid's Columns dynamically from the grid pane width so the
    /// inventory fills horizontally and wraps naturally (supports future bag upgrades).
    /// </summary>
    private void GetLayout(
        int panelX, int panelY,
        out Rectangle equipPaneRect, out Rectangle gridPaneRect,
        out int equipX, out int equipY, out int gridX, out int gridY)
    {
        int contentX = panelX + ContentMarginX;
        int contentY = panelY + ContentTop;
        int contentW = PanelWidth - ContentMarginX * 2;
        int contentH = PanelHeight - ContentTop - ContentBottom;
        int gridPaneW = contentW - EquipPaneW - SeparatorW;

        equipPaneRect = new Rectangle(contentX, contentY, EquipPaneW, contentH);
        gridPaneRect  = new Rectangle(contentX + EquipPaneW + SeparatorW, contentY, gridPaneW, contentH);

        // Equipment cluster: 3 cols × 4 rows of EquipSlotSize slots with EquipGap. Center in pane.
        int equipDisplayW = 3 * InventoryGridRenderer.EquipSlotSize + 2 * InventoryGridRenderer.EquipGap;
        equipX = equipPaneRect.X + (equipPaneRect.Width - equipDisplayW) / 2;
        equipY = equipPaneRect.Y + PanePadding + PaneTitleHeight + 8;

        // Inventory grid: dynamic Columns computed from pane inner width (leaves 16px margin each side).
        const int sidePadding = 16;
        int usableW = gridPaneRect.Width - sidePadding * 2;
        int cols = Math.Max(1, usableW / InventoryGridRenderer.SlotSize);
        _gridRenderer.Columns = cols;
        int gridDisplayW = cols * InventoryGridRenderer.SlotSize;
        gridX = gridPaneRect.X + (gridPaneRect.Width - gridDisplayW) / 2;
        gridY = gridPaneRect.Y + PanePadding + PaneTitleHeight + 8;
    }

    /// <summary>
    /// Sort button: 32×32, top-right of the bag pane aligned with the grid's right edge
    /// (matches ChestScene sort button placement). Called each frame from Update.
    /// </summary>
    private Rectangle GetSortButtonRect(Rectangle gridPaneRect)
    {
        const int size = 32;
        int gridW = _gridRenderer.Columns * InventoryGridRenderer.SlotSize;
        int right = gridPaneRect.X + (gridPaneRect.Width + gridW) / 2 - 7;
        return new Rectangle(right - size, gridPaneRect.Y + 10, size, size);
    }

    private static Rectangle GetCloseButtonRect(int panelX, int panelY) => new(
        panelX + PanelWidth - 54,
        panelY + 10,
        CloseButtonSize,
        CloseButtonSize);

    public override void UnloadContent()
    {
        base.UnloadContent();
        _pixel?.Dispose();
        Console.WriteLine("[InventoryScene] Unloaded");
    }

    private void GetPanelPosition(out int panelX, out int panelY)
    {
        var viewport = Services.GraphicsDevice.Viewport;
        panelX = (viewport.Width - PanelWidth) / 2;
        // Center vertically but shift up a bit so hotbar below is visible
        panelY = (viewport.Height - PanelHeight) / 2 - 30;
    }

    /// <summary>
    /// Draw ATK (bottom-left) and DEF (bottom-right) badges anchored to the
    /// equipment pane's inner corners — independent of the cluster position so
    /// the badges look balanced regardless of slot size changes.
    /// </summary>
    private void DrawEquipmentStats(SpriteBatch sb, Rectangle paneRect)
    {
        var (attack, defense) = EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment());
        const int iconSz = 22;
        const int pad = 16;
        int badgeY = paneRect.Bottom - pad - iconSz;

        int leftX = paneRect.X + pad;
        int rightAnchor = paneRect.Right - pad;

        sb.Draw(_theme.IconAttack, new Rectangle(leftX, badgeY, iconSz, iconSz), Color.White);
        sb.DrawString(_font, $"{attack:F0}",
            new Vector2(leftX + iconSz + 4, badgeY + 2),
            new Color(255, 205, 100));

        string defText = $"{defense:F0}";
        var defTextSz = _font.MeasureString(defText);
        int defTextX = rightAnchor - (int)defTextSz.X;
        int defIconX = defTextX - 4 - iconSz;
        sb.Draw(_theme.IconDefense, new Rectangle(defIconX, badgeY, iconSz, iconSz), Color.White);
        sb.DrawString(_font, defText, new Vector2(defTextX, badgeY + 2), new Color(190, 220, 255));
    }

    /// <summary>
    /// Render a floating item tooltip (rounded dark panel + golden border) for the
    /// slot currently under the cursor. Uses the same visual style as ChestScene's
    /// slot tooltip — shared via <see cref="WidgetHelpers.DrawTooltipPanel"/>.
    /// </summary>
    private void DrawHoverItemTooltip(SpriteBatch sb, int gridX, int gridY, int viewportW, int viewportH)
    {
        var mousePos = Services.Input.MousePosition;
        ItemStack? hoveredStack = null;
        Rectangle anchorRect = Rectangle.Empty;

        int cols = _gridRenderer.Columns;
        for (int i = 0; i < _inventory.Capacity; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = gridX + col * InventoryGridRenderer.SlotSize;
            int y = gridY + row * InventoryGridRenderer.SlotSize;
            var slotRect = new Rectangle(x, y, InventoryGridRenderer.SlotSize, InventoryGridRenderer.SlotSize);
            if (slotRect.Contains(mousePos))
            {
                hoveredStack = _inventory.GetSlot(i);
                anchorRect = slotRect;
                break;
            }
        }

        if (hoveredStack == null) return;
        var def = ItemRegistry.Get(hoveredStack.ItemId);
        if (def == null) return;

        string name = string.IsNullOrWhiteSpace(def.Name) ? def.Id : def.Name;
        string rarityLine = def.Rarity.ToString();

        var nameSize = _font.MeasureString(name);
        var raritySize = _font.MeasureString(rarityLine);

        const int pad = 9;
        const int gap = 3;
        int width = (int)Math.Ceiling(Math.Max(nameSize.X, raritySize.X)) + pad * 2;
        int height = (int)Math.Ceiling(nameSize.Y + gap + raritySize.Y) + pad * 2;

        int tipX = Math.Clamp(anchorRect.X + anchorRect.Width / 2 - width / 2, 4, viewportW - width - 4);
        int tipY = anchorRect.Y - 8 - height;
        if (tipY < 4) tipY = anchorRect.Bottom + 8;

        var tipRect = new Rectangle(tipX, tipY, width, height);
        WidgetHelpers.DrawTooltipPanel(sb, _pixel, tipRect);

        var textPos = new Vector2(tipRect.X + pad, tipRect.Y + pad);
        sb.DrawString(_font, name, textPos, Color.LightGoldenrodYellow);
        textPos.Y += nameSize.Y + gap;
        sb.DrawString(_font, rarityLine, textPos, new Color(226, 214, 184));
    }
}
