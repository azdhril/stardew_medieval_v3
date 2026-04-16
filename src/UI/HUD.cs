using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Progression;
using stardew_medieval_v3.Quest;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Heads-Up Display: HP bar, mana bar, stamina bar, clock panel, gold panel,
/// XP bar, quest tracker, and magic cooldown. Drawn in screen space (not
/// affected by camera). Uses NineSlice panels for a polished pixel-art look.
/// </summary>
public class HUD
{
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    private Texture2D? _barBg;
    private Texture2D? _barFillHP;
    private Texture2D? _barFillMana;
    private Texture2D? _barFillStamina;

    private readonly TimeManager _time;
    private readonly PlayerStats _stats;
    private readonly ToolController _tools;
    private readonly PlayerEntity _player;
    private readonly CombatManager _combat;

    private InventoryManager? _inventory;
    private ProgressionManager? _progression;
    private UITheme? _theme;
    private MainQuest? _quest;

    public HUD(TimeManager time, PlayerStats stats, ToolController tools,
        PlayerEntity player, CombatManager combat, InventoryManager? inventory = null)
    {
        _time = time;
        _stats = stats;
        _tools = tools;
        _player = player;
        _combat = combat;
        _inventory = inventory;
    }

    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var bgStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Bg.png");
            _barBg = Texture2D.FromStream(device, bgStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HUD] Failed to load UI_StatusBar_Bg: {ex.Message}");
            _barBg = null;
        }

        try
        {
            using var hpStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_HP.png");
            _barFillHP = Texture2D.FromStream(device, hpStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HUD] Failed to load UI_StatusBar_Fill_HP: {ex.Message}");
            _barFillHP = null;
        }

        try
        {
            using var manaStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_Blue.png");
            _barFillMana = Texture2D.FromStream(device, manaStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HUD] Failed to load UI_StatusBar_Fill_Blue: {ex.Message}");
            _barFillMana = null;
        }

        try
        {
            using var staStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_Green.png");
            _barFillStamina = Texture2D.FromStream(device, staStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HUD] Failed to load UI_StatusBar_Fill_Green: {ex.Message}");
            _barFillStamina = null;
        }
    }

    /// <summary>
    /// Draws a sprite-based status bar. Returns true if rendered with sprites,
    /// false if either texture is missing (caller should use flat-rect fallback).
    /// </summary>
    private const float HpBarScale = 0.50f;
    private const float ManaBarScale = 0.44f;
    private const float StaminaBarScale = 0.44f;

    private const int FillOffsetXNative = 59;
    private const int FillOffsetYNative = 10;

    private bool DrawSpriteBar(SpriteBatch sb, int x, int y, Texture2D? bg, Texture2D? fill, float pct, float scale)
    {
        if (bg == null || fill == null) return false;

        int bgW = (int)(bg.Width * scale);
        int bgH = (int)(bg.Height * scale);
        sb.Draw(bg, new Rectangle(x, y, bgW, bgH), Color.White);

        int padX = (int)(FillOffsetXNative * scale);
        int padY = (int)(FillOffsetYNative * scale);
        int fillFullW = (int)(fill.Width * scale);
        int fillH = (int)(fill.Height * scale);

        int fillW = (int)MathHelper.Clamp(fillFullW * pct, 0, fillFullW);
        if (fillW > 0)
        {
            int srcW = (int)MathHelper.Clamp(fill.Width * pct, 0, fill.Width);
            var src = new Rectangle(0, 0, srcW, fill.Height);
            var dest = new Rectangle(x + padX, y + padY, fillW, fillH);
            sb.Draw(fill, dest, src, Color.White);
        }
        return true;
    }

    private int ScaledBgHeight(float scale) => _barBg != null ? (int)((_barBg.Height) * scale) : 16;

    public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        // === Top-left: Day and Time (NineSlice panel + clock icon) ===
        string dayText = $"Day {_time.DayNumber}  {_time.GetDisplayHour()}";
        var daySize = _font.MeasureString(dayText);
        int panelPad = 8;
        int iconSize = 16;
        int clockPanelW = iconSize + 4 + (int)daySize.X + panelPad * 2;
        int clockPanelH = Math.Max(iconSize, (int)daySize.Y) + panelPad;
        var clockPanelRect = new Rectangle(8, 6, clockPanelW, clockPanelH);

        if (_theme?.PanelSmall != null)
            NineSlice.Draw(spriteBatch, _theme.PanelSmall, clockPanelRect, _theme.PanelSmallInsets);

        // Draw clock icon (if available and not a 1x1 fallback)
        if (_theme?.ClockIcon != null && _theme.ClockIcon.Width > 1)
            spriteBatch.Draw(_theme.ClockIcon,
                new Rectangle(8 + panelPad, 6 + (clockPanelH - iconSize) / 2, iconSize, iconSize),
                Color.White);

        // Draw day text after icon
        int clockTextX = 8 + panelPad + (_theme?.ClockIcon != null && _theme.ClockIcon.Width > 1 ? iconSize + 4 : 0);
        int clockTextY = 6 + (clockPanelH - (int)daySize.Y) / 2;
        spriteBatch.DrawString(_font, dayText, new Vector2(clockTextX, clockTextY), Color.White);

        // (Equipped tool label removed -- tools are now hotbar items)

        // === Top-left stacked bars: HP -> Mana -> Stamina (no numeric text) ===
        int barX = 12;
        int barSpacing = 2;
        int nextBarY = 6 + clockPanelH + 4;
        int fallbackW = 104;
        int fallbackH = 12;

        // --- HP Bar ---
        float hpFill = _player.MaxHP > 0 ? _player.HP / _player.MaxHP : 0f;
        bool hpDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillHP, hpFill, HpBarScale);
        if (!hpDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, new Color(40, 40, 40));
            DrawRect(spriteBatch, barX, nextBarY, (int)(fallbackW * hpFill), fallbackH, Color.Red);
        }
        nextBarY += (hpDrawn ? ScaledBgHeight(HpBarScale) : fallbackH) + barSpacing;

        // --- Mana Bar (placeholder -- no mana system yet, shows full blue) ---
        float manaFill = 1.0f;
        bool manaDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillMana, manaFill, ManaBarScale);
        if (!manaDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, new Color(40, 40, 40));
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, Color.Blue);
        }
        nextBarY += (manaDrawn ? ScaledBgHeight(ManaBarScale) : fallbackH) + barSpacing;

        // --- Stamina Bar ---
        float staFill = _stats.CurrentStamina / _stats.MaxStamina;
        bool staDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillStamina, staFill, StaminaBarScale);
        if (!staDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            Color barColor = staFill > 0.5f ? Color.LimeGreen : staFill > 0.25f ? Color.Yellow : Color.Red;
            DrawRect(spriteBatch, barX, nextBarY, (int)(fallbackW * staFill), fallbackH, barColor);
        }
        nextBarY += (staDrawn ? ScaledBgHeight(StaminaBarScale) : fallbackH) + barSpacing + 2;

        // === Gold label with NineSlice panel + coin icon ===
        int gold = _inventory?.Gold ?? 0;
        string goldStr = gold.ToString();
        var goldSize = _font.MeasureString(goldStr);
        int goldIconSize = 14;
        bool hasGoldIcon = _theme?.GoldIcon != null && _theme.GoldIcon.Width > 1;
        int goldIconSpace = hasGoldIcon ? goldIconSize + 4 : 0;
        int goldPanelW = goldIconSpace + (int)goldSize.X + 12;
        int goldPanelH = Math.Max(goldIconSize, (int)goldSize.Y) + 8;
        var goldPanelRect = new Rectangle(barX, nextBarY, goldPanelW, goldPanelH);

        if (_theme?.PanelSmall != null)
            NineSlice.Draw(spriteBatch, _theme.PanelSmall, goldPanelRect, _theme.PanelSmallInsets);

        if (hasGoldIcon)
            spriteBatch.Draw(_theme!.GoldIcon,
                new Rectangle(barX + 6, nextBarY + (goldPanelH - goldIconSize) / 2, goldIconSize, goldIconSize),
                Color.White);

        spriteBatch.DrawString(_font, goldStr,
            new Vector2(barX + 6 + goldIconSpace, nextBarY + (goldPanelH - (int)goldSize.Y) / 2),
            Color.Gold);

        // === XP bar above hotbar ===
        DrawXPBar(spriteBatch, screenWidth, screenHeight);

        // === Magic cooldown indicator (per D-09) ===
        DrawFireballCooldown(spriteBatch, screenWidth, screenHeight);

        // === Top-right: Quest tracker (HUD-05 / D-13) ===
        var questState = _quest?.State ?? MainQuestState.NotStarted;
        DrawQuestTracker(spriteBatch, _font, _pixel, questState, screenWidth, _theme);
    }

    /// <summary>
    /// Draws the XP progress bar just above the hotbar panel with a "Lv X" label.
    /// </summary>
    private void DrawXPBar(SpriteBatch sb, int screenWidth, int screenHeight)
    {
        // Calculate hotbar position (mirrors HotbarRenderer layout)
        int hotbarSlotSize = 50;
        int hotbarBottomMargin = 5;
        int hotbarSize = 8; // InventoryManager.HotbarSize
        int hotbarPadding = 0;
        int hotbarWidth = hotbarSize * hotbarSlotSize + (hotbarSize - 1) * hotbarPadding;
        int hotbarStartX = (screenWidth - hotbarWidth) / 2;
        int hotbarTopY = screenHeight - hotbarSlotSize - hotbarBottomMargin;

        int xpBarH = 10;
        int xpBarY = hotbarTopY - xpBarH - 6;
        int xpBarX = hotbarStartX - 8;
        int xpBarW = hotbarWidth + 16;

        // Draw XP bar background
        if (_theme?.XPBarBg != null)
        {
            sb.Draw(_theme.XPBarBg, new Rectangle(xpBarX, xpBarY, xpBarW, xpBarH), Color.White);
        }
        else
        {
            DrawRect(sb, xpBarX, xpBarY, xpBarW, xpBarH, new Color(40, 30, 25));
        }

        // Draw XP bar fill
        float xpFill = _progression != null && _progression.XPToNext > 0
            ? (float)_progression.XP / _progression.XPToNext : 0f;
        xpFill = MathHelper.Clamp(xpFill, 0f, 1f);

        if (_theme?.XPBarFill != null && xpFill > 0)
        {
            int fillW = (int)(xpBarW * xpFill);
            int srcW = (int)(_theme.XPBarFill.Width * xpFill);
            sb.Draw(_theme.XPBarFill, new Rectangle(xpBarX, xpBarY, fillW, xpBarH),
                new Rectangle(0, 0, srcW, _theme.XPBarFill.Height), Color.White);
        }
        else if (xpFill > 0)
        {
            int fillW = (int)(xpBarW * xpFill);
            DrawRect(sb, xpBarX, xpBarY, fillW, xpBarH, Color.Gold);
        }

        // Draw "Lv X" label left of the XP bar
        int level = _progression?.Level ?? 1;
        string lvText = $"Lv {level}";
        var lvSize = _font.MeasureString(lvText);
        sb.DrawString(_font, lvText,
            new Vector2(xpBarX - lvSize.X - 4, xpBarY + (xpBarH - lvSize.Y) / 2),
            Color.Gold);
    }

    /// <summary>
    /// Binds the main-quest reference used by the quest tracker. Optional: if unset,
    /// the tracker renders the NotStarted "Quest: (none)" dim label.
    /// </summary>
    public void SetQuest(MainQuest? quest)
    {
        _quest = quest;
    }

    /// <summary>
    /// Binds the progression manager for XP bar and level display.
    /// </summary>
    public void SetProgression(ProgressionManager? prog)
    {
        _progression = prog;
    }

    /// <summary>
    /// Binds the UI theme for NineSlice panels and icon textures.
    /// </summary>
    public void SetTheme(UITheme? theme)
    {
        _theme = theme;
    }

    /// <summary>
    /// Binds the inventory manager for gold display.
    /// </summary>
    public void SetInventory(InventoryManager? inv)
    {
        _inventory = inv;
    }

    /// <summary>
    /// Draws the quest tracker in the top-right corner with NineSlice panel
    /// background when a theme is available, falling back to flat rectangles.
    /// Static so scenes without a full HUD (e.g. CastleScene shell) can call
    /// it directly. See UI-SPEC for exact copy and colors.
    /// </summary>
    public static void DrawQuestTracker(SpriteBatch sb, SpriteFont font, Texture2D pixel,
        MainQuestState state, int screenWidth, UITheme? theme = null)
    {
        const int PanelW = 200;
        const int PanelH = 20;
        const int MarginRight = 12;
        const int MarginTop = 12;
        int panelX = screenWidth - PanelW - MarginRight;
        int panelY = MarginTop;

        // Background panel (NineSlice when theme available, flat rect fallback)
        if (theme?.PanelSmall != null)
        {
            NineSlice.Draw(sb, theme.PanelSmall,
                new Rectangle(panelX - 1, panelY - 1, PanelW + 2, PanelH + 2),
                theme.PanelSmallInsets);
        }
        else
        {
            sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, PanelW + 2, PanelH + 2), Color.Black);
            sb.Draw(pixel, new Rectangle(panelX, panelY, PanelW, PanelH), new Color(60, 40, 30));
        }

        int textY = panelY + 3;

        switch (state)
        {
            case MainQuestState.NotStarted:
            {
                string s = "Quest: (none)";
                var size = font.MeasureString(s);
                float x = panelX + PanelW - size.X - 6;
                sb.DrawString(font, s, new Vector2(x, textY), Color.Gray * 0.7f);
                break;
            }
            case MainQuestState.Active:
            {
                string prefix = "Quest:";
                string obj = " Clear the Dungeon";
                var pfxSize = font.MeasureString(prefix);
                var objSize = font.MeasureString(obj);
                float totalX = panelX + PanelW - (pfxSize.X + objSize.X) - 6;
                sb.DrawString(font, prefix, new Vector2(totalX, textY), Color.Gold);
                sb.DrawString(font, obj, new Vector2(totalX + pfxSize.X, textY), Color.White);
                break;
            }
            case MainQuestState.Complete:
            {
                string prefix = "Quest:";
                string obj = " Clear the Dungeon ";
                string check = "v"; // ASCII checkmark (SpriteFont may lack special chars)
                var pfxSize = font.MeasureString(prefix);
                var objSize = font.MeasureString(obj);
                var chkSize = font.MeasureString(check);
                float totalX = panelX + PanelW - (pfxSize.X + objSize.X + chkSize.X) - 6;
                sb.DrawString(font, prefix, new Vector2(totalX, textY), Color.Gold);
                sb.DrawString(font, obj, new Vector2(totalX + pfxSize.X, textY), Color.White);
                sb.DrawString(font, check, new Vector2(totalX + pfxSize.X + objSize.X, textY), Color.LimeGreen);
                break;
            }
        }
    }

    /// <summary>
    /// Draw the fireball cooldown indicator near the hotbar area.
    /// Shows an icon area that darkens when on cooldown with vertical fill progress.
    /// </summary>
    private void DrawFireballCooldown(SpriteBatch sb, int screenWidth, int screenHeight)
    {
        float remaining = _combat.FireballCooldownRemaining;
        float max = _combat.FireballCooldownMax;

        // Only render while a spell cast is actively cooling down. Otherwise stay hidden
        // (there is no RMB fireball anymore -- fireballs are cast by equipping a staff).
        if (remaining <= 0 || max <= 0) return;

        int cdIconSize = 16;
        int iconX = screenWidth / 2 + 140;
        int iconY = screenHeight - 28;

        DrawRect(sb, iconX - 1, iconY - 1, cdIconSize + 2, cdIconSize + 2, Color.Black);
        DrawRect(sb, iconX, iconY, cdIconSize, cdIconSize, new Color(40, 40, 40));
        float progress = 1f - (remaining / max);
        int fillHeight = (int)(cdIconSize * progress);
        DrawRect(sb, iconX, iconY + (cdIconSize - fillHeight), cdIconSize, fillHeight, Color.Orange * 0.5f);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
