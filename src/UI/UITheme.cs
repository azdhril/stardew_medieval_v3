using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Central loader that owns every UI Texture2D used by the 3 HUDs (Inventory, Chest, Shop),
/// plus per-texture 9-slice metadata. Loaded once on first overlay open and reused across
/// scenes via <c>ServiceContainer.Theme</c>. Missing/corrupt assets fall back to a 1x1 solid
/// color texture so the game never crashes on a bad file.
/// </summary>
public sealed class UITheme
{
    // Window chrome
    public Texture2D PanePopup      { get; private set; } = null!;
    public Texture2D PanelTitle     { get; private set; } = null!;
    public Texture2D PanelThumbnail { get; private set; } = null!;

    // Slots
    public Texture2D SlotSelected   { get; private set; } = null!;

    // Chest buttons
    public Texture2D CommonBtn      { get; private set; } = null!;
    public Texture2D CommonBtnDim   { get; private set; } = null!;

    // Shop tabs
    public Texture2D TabOn          { get; private set; } = null!;
    public Texture2D TabOff         { get; private set; } = null!;

    // Shop list
    public Texture2D ListItemHover  { get; private set; } = null!;

    // Shop buttons
    public Texture2D SquareBtn      { get; private set; } = null!;
    public Texture2D SquareBtnHover { get; private set; } = null!;
    public Texture2D YellowBtnSmall { get; private set; } = null!;
    public Texture2D BtnIconX       { get; private set; } = null!;

    // Shop gold pouch
    public Texture2D PanelCurrency  { get; private set; } = null!;

    // Divider
    public Texture2D ImageDeco      { get; private set; } = null!;

    // Cream slot pane background (goes under a whole slot grid)
    public Texture2D PanelSlotPane  { get; private set; } = null!;

    // Small circle button (used for qty +/- steppers)
    public Texture2D BtnCircleSmall { get; private set; } = null!;

    // Glyph icons overlaid on buttons / stats
    public Texture2D IconPlus       { get; private set; } = null!;
    public Texture2D IconMinus      { get; private set; } = null!;
    public Texture2D IconAttack     { get; private set; } = null!;
    public Texture2D IconDefense    { get; private set; } = null!;

    // Equipment slot watermark placeholders (Tibia-style faint glyphs on empty slots).
    // These are generic icons repurposed until dedicated equipment silhouettes exist.
    public Texture2D IconEquipHelmet   { get; private set; } = null!;
    public Texture2D IconEquipNecklace { get; private set; } = null!;
    public Texture2D IconEquipArmor    { get; private set; } = null!;
    public Texture2D IconEquipShield   { get; private set; } = null!;
    public Texture2D IconEquipRing     { get; private set; } = null!;
    public Texture2D IconEquipLegs     { get; private set; } = null!;
    public Texture2D IconEquipBoots    { get; private set; } = null!;

    // HUD icons and progress bars
    public Texture2D GoldIcon      { get; private set; } = null!;
    public Texture2D ClockIcon     { get; private set; } = null!;
    public Texture2D XPBarBg       { get; private set; } = null!;
    public Texture2D XPBarFill     { get; private set; } = null!;
    public Texture2D PanelSmall    { get; private set; } = null!;
    public Texture2D IconHeart     { get; private set; } = null!;
    public Texture2D IconMana      { get; private set; } = null!;
    public Texture2D IconStamina   { get; private set; } = null!;

    // 9-slice insets (tune these during visual review; start with conservative defaults
    // suitable for typical pixel-art medieval UI assets).
    public NineSlice.Insets PanePopupInsets      = new(11, 28, 11, 31);
    public NineSlice.Insets PanelTitleInsets     = new(32, 24, 32, 24);
    public NineSlice.Insets CommonBtnInsets      = NineSlice.Insets.Uniform(12);
    public NineSlice.Insets TabInsets            = new(12, 8, 12, 4);
    public NineSlice.Insets ListItemHoverInsets  = NineSlice.Insets.Uniform(8);
    public NineSlice.Insets SquareBtnInsets      = NineSlice.Insets.Uniform(8);
    public NineSlice.Insets YellowBtnSmallInsets = NineSlice.Insets.Uniform(10);
    public NineSlice.Insets PanelCurrencyInsets  = new(24, 16, 24, 16);
    public NineSlice.Insets PanelSlotPaneInsets  = NineSlice.Insets.Uniform(16);
    public NineSlice.Insets PanelSmallInsets     = NineSlice.Insets.Uniform(8);

    // HUD-specific insets (smaller than overlay counterparts for compact HUD panels)
    public NineSlice.Insets HudPanelTitleInsets    = new(8, 6, 8, 6);
    public NineSlice.Insets HudPanelCurrencyInsets = new(10, 6, 10, 6);

    private bool _loaded;

    /// <summary>Load all textures. Safe to call multiple times (no-op after first).</summary>
    public void LoadContent(GraphicsDevice device)
    {
        if (_loaded) return;
        PanePopup      = Load(device, "Panel/UI_Pane_PopupPixelArt.png",  "PanePopup",      new Color(60, 40, 30));
        PanelTitle     = Load(device, "Panel/UI_Panel_Title.png",        "PanelTitle",     new Color(90, 60, 45));
        PanelThumbnail = Load(device, "Panel/UI_Panel_Thumbnail.png",    "PanelThumbnail", new Color(40, 30, 25));
        SlotSelected   = Load(device, "Slot/UI_Slot_Selected.png",       "SlotSelected",   Color.Gold);
        CommonBtn      = Load(device, "Buttons/UI_Common_Btn.png",       "CommonBtn",      new Color(78, 58, 44));
        CommonBtnDim   = Load(device, "Buttons/UI_Common_Btn_dim.png",   "CommonBtnDim",   new Color(50, 40, 32));
        TabOn          = Load(device, "Tabs/UI_tab_On.png",              "TabOn",          Color.Gold);
        TabOff         = Load(device, "Tabs/UI_tab_Off.png",             "TabOff",         new Color(78, 58, 44));
        ListItemHover  = Load(device, "Lists/UI_ListItem_Hover.png",     "ListItemHover",  Color.White * 0.12f);
        SquareBtn      = Load(device, "Buttons/UI_Square_Btn.png",       "SquareBtn",      new Color(78, 58, 44));
        SquareBtnHover = Load(device, "Buttons/UI_Square_Btn_hover.png", "SquareBtnHover", new Color(120, 90, 60));
        YellowBtnSmall = Load(device, "Buttons/UI_Btn_yellow_small.png", "YellowBtnSmall", Color.Gold);
        BtnIconX       = Load(device, "Buttons/UI_BtnIcon_x.png",        "BtnIconX",       Color.White);
        PanelCurrency  = Load(device, "Panel/UI_Panel_Currency.png",     "PanelCurrency",  new Color(60, 45, 25));
        ImageDeco      = Load(device, "Decorations/UI_Image_Deco.png",   "ImageDeco",      new Color(140, 110, 70));
        PanelSlotPane  = Load(device, "Panel/UI_Panel_SlotPane.png",     "PanelSlotPane",  new Color(215, 190, 140));
        BtnCircleSmall = Load(device, "Buttons/UI_Btn_Circle_small.png", "BtnCircleSmall", new Color(78, 58, 44));

        IconPlus       = Load(device, "Icons/Icon_plus.png",             "IconPlus",       Color.White);
        IconMinus      = Load(device, "Icons/Icon_minus.png",            "IconMinus",      Color.White);
        IconAttack     = Load(device, "Icons/System/UI_Icon_Sys_Attack.png",  "IconAttack",  new Color(220, 80, 60));
        IconDefense    = Load(device, "Icons/System/UI_Icon_Sys_Defense.png", "IconDefense", new Color(90, 140, 210));

        // Equipment watermark placeholders — generic icons re-used until dedicated art exists.
        IconEquipHelmet   = Load(device, "Icons/Icon_shield-check.png", "IconEquipHelmet",   Color.White);
        IconEquipNecklace = Load(device, "Icons/Icon_star.png",         "IconEquipNecklace", Color.White);
        IconEquipArmor    = Load(device, "Icons/Icon_shield.png",       "IconEquipArmor",    Color.White);
        IconEquipShield   = Load(device, "Icons/Icon_shield-slash.png", "IconEquipShield",   Color.White);
        IconEquipRing     = Load(device, "Icons/Icon_dot.png",          "IconEquipRing",     Color.White);
        IconEquipLegs     = Load(device, "Icons/Icon_columns.png",      "IconEquipLegs",     Color.White);
        IconEquipBoots    = Load(device, "Icons/Icon_chevron-down.png", "IconEquipBoots",    Color.White);

        // HUD icons and XP progress bar
        GoldIcon    = Load(device, "Icons/System/UI_Icon_Sys_Gold.png",                "GoldIcon",    Color.Gold);
        ClockIcon   = Load(device, "Icons/General/UI_Icon_Hourglass.png",              "ClockIcon",   Color.White);
        XPBarBg     = Load(device, "Bars/Progress/UI_Progress_Style1_Bg.png",          "XPBarBg",     new Color(40, 30, 25));
        XPBarFill   = Load(device, "Bars/Progress/UI_Progress_Style1_Fill_Yellow.png", "XPBarFill",   Color.Gold);
        PanelSmall  = Load(device, "Panel/UI_Panel_Title.png",                         "PanelSmall",  new Color(60, 40, 30));
        IconHeart   = Load(device, "Icons/Icon_heart.png",                             "IconHeart",   Color.Red);
        IconMana    = Load(device, "Icons/Icon_sparkles.png",                          "IconMana",    Color.Blue);
        IconStamina = Load(device, "Icons/Icon_stamina.png",                           "IconStamina", Color.Green);

        _loaded = true;
        Console.WriteLine("[UITheme] Content loaded");
    }

    private static Texture2D Load(GraphicsDevice device, string relative, string tag, Color fallback)
    {
        string path = Path.Combine("assets", "Sprites", "System", "UI Elements", relative);
        try
        {
            using var s = File.OpenRead(path);
            return Texture2D.FromStream(device, s);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UITheme] Failed to load {tag} from {path}: {ex.Message}");
            var tex = new Texture2D(device, 1, 1);
            tex.SetData(new[] { fallback });
            return tex;
        }
    }
}
