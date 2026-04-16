using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Quest;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Heads-Up Display: HP bar, stamina bar, clock, day counter, equipped tool, magic cooldown.
/// Drawn in screen space (not affected by camera).
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
        // === Top-left: Day and Time ===
        string dayText = $"Day {_time.DayNumber}  {_time.GetDisplayHour()}";
        spriteBatch.DrawString(_font, dayText, new Vector2(12, 10), Color.White);

        // (Equipped tool label removed — tools are now hotbar items)

        // === Top-left stacked bars: HP → Mana → Stamina ===
        int barX = 12;
        int barSpacing = 2;
        int nextBarY = 32;
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
        string hpText = $"HP: {_player.HP:F0}/{_player.MaxHP:F0}";
        int hpTextY = hpDrawn ? nextBarY + (ScaledBgHeight(HpBarScale) / 2) - 7 : nextBarY - 1;
        spriteBatch.DrawString(_font, hpText, new Vector2(barX + 8, hpTextY), Color.White);
        nextBarY += (hpDrawn ? ScaledBgHeight(HpBarScale) : fallbackH) + barSpacing;

        // --- Mana Bar (placeholder — no mana system yet, shows full blue) ---
        float manaFill = 1.0f;
        bool manaDrawn = DrawSpriteBar(spriteBatch, barX, nextBarY, _barBg, _barFillMana, manaFill, ManaBarScale);
        if (!manaDrawn)
        {
            DrawRect(spriteBatch, barX - 1, nextBarY - 1, fallbackW + 2, fallbackH + 2, Color.Black);
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, new Color(40, 40, 40));
            DrawRect(spriteBatch, barX, nextBarY, fallbackW, fallbackH, Color.Blue);
        }
        string manaText = "MP: ---/---";
        int manaTextY = manaDrawn ? nextBarY + (ScaledBgHeight(ManaBarScale) / 2) - 7 : nextBarY - 1;
        spriteBatch.DrawString(_font, manaText, new Vector2(barX + 8, manaTextY), Color.White);
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
        string staminaText = $"STA: {_stats.CurrentStamina:F0}/{_stats.MaxStamina:F0}";
        int staTextY = staDrawn ? nextBarY + (ScaledBgHeight(StaminaBarScale) / 2) - 7 : nextBarY;
        spriteBatch.DrawString(_font, staminaText, new Vector2(barX + 8, staTextY), Color.White);

        // === Gold label (under stamina bar, matches bar column) ===
        int gold = _inventory?.Gold ?? 0;
        string goldText = $"Gold: {gold}";
        int goldY = nextBarY + (staDrawn ? ScaledBgHeight(StaminaBarScale) : fallbackH) + barSpacing + 2;
        // Subtle black outline for readability on bright tiles: draw text at (+1,+1) in black, then gold on top.
        spriteBatch.DrawString(_font, goldText, new Vector2(barX + 1, goldY + 1), Color.Black);
        spriteBatch.DrawString(_font, goldText, new Vector2(barX, goldY), Color.Gold);

        // === Magic cooldown indicator (per D-09) ===
        DrawFireballCooldown(spriteBatch, screenWidth, screenHeight);

        // === Top-right: Quest tracker (HUD-05 / D-13) ===
        var questState = _quest?.State ?? MainQuestState.NotStarted;
        DrawQuestTracker(spriteBatch, _font, _pixel, questState, screenWidth);
    }

    private MainQuest? _quest;

    /// <summary>
    /// Binds the main-quest reference used by the quest tracker. Optional: if unset,
    /// the tracker renders the NotStarted "Quest: (none)" dim label.
    /// </summary>
    public void SetQuest(MainQuest? quest)
    {
        _quest = quest;
    }

    /// <summary>
    /// Draws the plain-text quest tracker in the top-right corner. Static so scenes
    /// without a full HUD (e.g. CastleScene shell) can call it directly. See UI-SPEC
    /// §Copywriting §Empty states for exact copy and colors.
    /// </summary>
    public static void DrawQuestTracker(SpriteBatch sb, SpriteFont font, Texture2D pixel,
        MainQuestState state, int screenWidth)
    {
        const int PanelW = 200;
        const int PanelH = 20;
        const int MarginRight = 12;
        const int MarginTop = 12;
        int panelX = screenWidth - PanelW - MarginRight;
        int panelY = MarginTop;

        // Background panel
        sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, PanelW + 2, PanelH + 2), Color.Black);
        sb.Draw(pixel, new Rectangle(panelX, panelY, PanelW, PanelH), new Color(60, 40, 30));

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
                string check = "v"; // ASCII checkmark (SpriteFont may lack ✓)
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
        // (there is no RMB fireball anymore — fireballs are cast by equipping a staff).
        if (remaining <= 0 || max <= 0) return;

        int iconSize = 16;
        int iconX = screenWidth / 2 + 140;
        int iconY = screenHeight - 28;

        DrawRect(sb, iconX - 1, iconY - 1, iconSize + 2, iconSize + 2, Color.Black);
        DrawRect(sb, iconX, iconY, iconSize, iconSize, new Color(40, 40, 40));
        float progress = 1f - (remaining / max);
        int fillHeight = (int)(iconSize * progress);
        DrawRect(sb, iconX, iconY + (iconSize - fillHeight), iconSize, fillHeight, Color.Orange * 0.5f);
        sb.DrawString(_font, "F", new Vector2(iconX + 3, iconY - 1), Color.White);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
