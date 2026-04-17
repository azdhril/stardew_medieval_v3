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
    private SpriteFont? _smallFont;
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

    public void LoadContent(GraphicsDevice device, SpriteFont font, Microsoft.Xna.Framework.Content.ContentManager? content = null)
    {
        _font = font;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        if (content != null)
        {
            try { _smallFont = content.Load<SpriteFont>("NotoSerifSmall"); }
            catch { Console.WriteLine("[HUD] NotoSerifSmall not found, using main font for bar text"); }
        }

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
        // === Top-left: Day and Time (stretched PanelTitle + hourglass icon) ===
        string dayText = $"Day {_time.DayNumber}  {_time.GetDisplayHour()}";
        var daySize = _font.MeasureString(dayText);
        int panelPad = 12;
        int iconSize = 16;
        int contentW = iconSize + 4 + (int)daySize.X + panelPad * 2;
        int contentH = Math.Max(iconSize, (int)daySize.Y) + panelPad + 8;
        // 15% bigger, maintain texture aspect ratio (no distortion)
        int clockPanelW = (int)(contentW * 1.15f);
        int clockPanelH = (int)(contentH * 1.15f);
        if (_theme?.PanelTitle != null && _theme.PanelTitle.Width > 1)
        {
            float texRatio = (float)_theme.PanelTitle.Width / _theme.PanelTitle.Height;
            int ratioH = (int)(clockPanelW / texRatio);
            if (ratioH >= clockPanelH)
                clockPanelH = ratioH;
            else
                clockPanelW = (int)(clockPanelH * texRatio);
        }
        var clockPanelRect = new Rectangle(8, 6, clockPanelW, clockPanelH);

        if (_theme?.PanelTitle != null)
            NineSlice.DrawStretched(spriteBatch, _theme.PanelTitle, clockPanelRect);

        // Hourglass icon at left edge of background (25% bigger, nudged 30% right)
        bool hasClockIcon = _theme?.ClockIcon != null && _theme.ClockIcon.Width > 1;
        if (hasClockIcon)
        {
            int bigIcon = (int)(iconSize * 1.25f);
            int iconLeftPad = 8 + (int)(iconSize * 0.45f);
            spriteBatch.Draw(_theme!.ClockIcon,
                new Rectangle(8 + iconLeftPad, 6 + (clockPanelH - bigIcon) / 2, bigIcon, bigIcon),
                Color.White);
        }

        // Day text centered in panel (independent of icon position)
        int clockTextX = 8 + (clockPanelW - (int)daySize.X) / 2;
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
        DrawBarIcon(spriteBatch, barX, nextBarY, _theme?.IconHeart, hpDrawn ? HpBarScale : 0, hpDrawn ? ScaledBgHeight(HpBarScale) : fallbackH);
        int hpBarW = hpDrawn && _barBg != null ? (int)(_barBg.Width * HpBarScale) : fallbackW;
        int hpBarH = hpDrawn ? ScaledBgHeight(HpBarScale) : fallbackH;
        int hpFillX = barX + (int)(FillOffsetXNative * HpBarScale);
        int hpFillW = hpBarW - (int)(FillOffsetXNative * HpBarScale);
        var barFont =  _font;
        var smallBarFont = _smallFont ?? _font;
        string hpText = $"{(int)_player.HP}/{(int)_player.MaxHP}";
        var hpTextSz = barFont.MeasureString(hpText);
        float hpTextY = nextBarY + (hpBarH - hpTextSz.Y) / 2 - hpBarH * 0.07f;
        spriteBatch.DrawString(barFont, hpText,
            new Vector2(hpFillX + (hpFillW - hpTextSz.X) / 2, hpTextY),
            new Color(220, 80, 100)); // light pinkish red
        nextBarY += hpBarH + barSpacing;

        // --- Mana Bar (placeholder -- no mana system yet, shows full blue) ---
        float manaFill = 1.0f;
        float manaMax = 100f;
        float manaCur = manaMax;
        bool manaDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillMana, manaFill, ManaBarScale);
        if (!manaDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, new Color(40, 40, 40));
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, Color.Blue);
        }
        DrawBarIcon(spriteBatch, barX, nextBarY, _theme?.IconMana, manaDrawn ? ManaBarScale : 0, manaDrawn ? ScaledBgHeight(ManaBarScale) : fallbackH);
        int manaBarW = manaDrawn && _barBg != null ? (int)(_barBg.Width * ManaBarScale) : fallbackW;
        int manaBarH = manaDrawn ? ScaledBgHeight(ManaBarScale) : fallbackH;
        int manaFillX = barX + (int)(FillOffsetXNative * ManaBarScale);
        int manaFillW = manaBarW - (int)(FillOffsetXNative * ManaBarScale);
        string manaText = $"{(int)manaCur}/{(int)manaMax}";
        var manaTextSz = smallBarFont.MeasureString(manaText);
        float manaTextY = nextBarY + (manaBarH - manaTextSz.Y) / 2 - manaBarH * 0.07f;
        spriteBatch.DrawString(smallBarFont, manaText,
            new Vector2(manaFillX + (manaFillW - manaTextSz.X) / 2, manaTextY),
            new Color(140, 200, 245)); // sky blue
        nextBarY += manaBarH + barSpacing;

        // --- Stamina Bar ---
        float staFill = _stats.CurrentStamina / _stats.MaxStamina;
        bool staDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillStamina, staFill, StaminaBarScale);
        if (!staDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            Color barColor = staFill > 0.5f ? Color.LimeGreen : staFill > 0.25f ? Color.Yellow : Color.Red;
            DrawRect(spriteBatch, barX, nextBarY, (int)(fallbackW * staFill), fallbackH, barColor);
        }
        DrawBarIcon(spriteBatch, barX, nextBarY, _theme?.IconStamina, staDrawn ? StaminaBarScale : 0, staDrawn ? ScaledBgHeight(StaminaBarScale) : fallbackH);
        int staBarW = staDrawn && _barBg != null ? (int)(_barBg.Width * StaminaBarScale) : fallbackW;
        int staBarH = staDrawn ? ScaledBgHeight(StaminaBarScale) : fallbackH;
        int staFillX = barX + (int)(FillOffsetXNative * StaminaBarScale);
        int staFillW = staBarW - (int)(FillOffsetXNative * StaminaBarScale);
        string staText = $"{(int)_stats.CurrentStamina}/{(int)_stats.MaxStamina}";
        var staTextSz = smallBarFont.MeasureString(staText);
        float staTextY = nextBarY + (staBarH - staTextSz.Y) / 2 - staBarH * 0.07f;
        spriteBatch.DrawString(smallBarFont, staText,
            new Vector2(staFillX + (staFillW - staTextSz.X) / 2, staTextY),
            new Color(130, 210, 90)); // light green
        nextBarY += staBarH + barSpacing + 2;

        // === Gold label with coin icon + stretched PanelCurrency ===
        int gold = _inventory?.Gold ?? 0;
        string goldStr = gold.ToString("N0");  // 1,000  100,000  1,000,000
        var goldSize = _font.MeasureString(goldStr);
        int coinIconSz = 18;
        int goldPanelW = coinIconSz + 6 + (int)goldSize.X + 32;
        int goldPanelH = 34;
        // Maintain texture aspect ratio to reduce distortion
        if (_theme?.PanelCurrency != null && _theme.PanelCurrency.Width > 1)
        {
            float texRatio = (float)_theme.PanelCurrency.Width / _theme.PanelCurrency.Height;
            int ratioH = (int)(goldPanelW / texRatio);
            if (ratioH >= goldPanelH)
                goldPanelH = ratioH;
            else
                goldPanelW = (int)(goldPanelH * texRatio);
        }
        var goldPanelRect = new Rectangle(barX, nextBarY, goldPanelW, goldPanelH);

        if (_theme?.PanelCurrency != null)
            NineSlice.DrawStretched(spriteBatch, _theme.PanelCurrency, goldPanelRect);

        // Coin icon
        int coinX = barX + 8;
        int coinY = nextBarY + (goldPanelH - coinIconSz) / 2;
        if (_theme?.GoldIcon != null && _theme.GoldIcon.Width > 1)
            spriteBatch.Draw(_theme.GoldIcon, new Rectangle(coinX, coinY, coinIconSz, coinIconSz), Color.White);

        // Gold text (cream color, nudged 30% right from coin)
        int goldTextNudge = (int)(coinIconSz * 0.30f);
        spriteBatch.DrawString(_font, goldStr,
            new Vector2(coinX + coinIconSz + 4 + goldTextNudge, nextBarY + (goldPanelH - (int)goldSize.Y) / 2),
            new Color(255, 248, 220));

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

        int xpBarH = 20;
        int xpBarY = hotbarTopY - xpBarH - 18;
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
        const int PanelW = 315;
        const int PanelH = 45;
        const int MarginRight = 12;
        const int MarginTop = 12;
        int panelX = screenWidth - PanelW - MarginRight;
        int panelY = MarginTop;

        // Background panel (stretched PanelTitle when available, flat rect fallback)
        if (theme?.PanelTitle != null)
        {
            NineSlice.DrawStretched(sb, theme.PanelTitle,
                new Rectangle(panelX, panelY, PanelW, PanelH));
        }
        else
        {
            sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, PanelW + 2, PanelH + 2), Color.Black);
            sb.Draw(pixel, new Rectangle(panelX, panelY, PanelW, PanelH), new Color(60, 40, 30));
        }

        int textY = panelY + (PanelH - (int)font.MeasureString("A").Y) / 2;

        switch (state)
        {
            case MainQuestState.NotStarted:
            {
                string s = "Quest: (none)";
                var size = font.MeasureString(s);
                float x = panelX + (PanelW - size.X) / 2f;
                sb.DrawString(font, s, new Vector2(x, textY), Color.Gray * 0.7f);
                break;
            }
            case MainQuestState.Active:
            {
                string prefix = "Quest:";
                string obj = " Clear the Dungeon";
                var pfxSize = font.MeasureString(prefix);
                var objSize = font.MeasureString(obj);
                float totalW = pfxSize.X + objSize.X;
                float totalX = panelX + (PanelW - totalW) / 2f;
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
                float totalW = pfxSize.X + objSize.X + chkSize.X;
                float totalX = panelX + (PanelW - totalW) / 2f;
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

    /// <summary>
    /// Draws a small icon inside the bar's left icon square area.
    /// The icon area spans 0..FillOffsetXNative pixels at native resolution.
    /// </summary>
    private void DrawBarIcon(SpriteBatch sb, int barX, int barY, Texture2D? icon, float barScale, int barH)
    {
        if (icon == null || icon.Width <= 1) return;
        int areaW = barScale > 0 ? (int)(FillOffsetXNative * barScale) : 20;
        int iconSz = Math.Min(16, Math.Min(areaW - 2, barH - 2));
        if (iconSz < 4) return;
        int ix = barX + (areaW - iconSz) / 2;
        int iy = barY + (barH - iconSz) / 2;
        sb.Draw(icon, new Rectangle(ix, iy, iconSz, iconSz), Color.White);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
