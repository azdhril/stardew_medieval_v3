using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
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

    private readonly TimeManager _time;
    private readonly PlayerStats _stats;
    private readonly ToolController _tools;
    private readonly PlayerEntity _player;
    private readonly CombatManager _combat;

    public HUD(TimeManager time, PlayerStats stats, ToolController tools,
        PlayerEntity player, CombatManager combat)
    {
        _time = time;
        _stats = stats;
        _tools = tools;
        _player = player;
        _combat = combat;
    }

    public void LoadContent(GraphicsDevice device, SpriteFont font)
    {
        _font = font;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        // === Top-left: Day and Time ===
        string dayText = $"Day {_time.DayNumber}  {_time.GetDisplayHour()}";
        spriteBatch.DrawString(_font, dayText, new Vector2(12, 10), Color.White);

        // (Equipped tool label removed — tools are now hotbar items)

        // === Top-left below day: Player HP Bar (per D-12) ===
        int hpBarX = 12;
        int hpBarY = 32;
        int hpBarWidth = 104;
        int hpBarHeight = 12;

        // Background
        DrawRect(spriteBatch, hpBarX - 1, hpBarY - 1, hpBarWidth + 2, hpBarHeight + 2, Color.Black);
        DrawRect(spriteBatch, hpBarX, hpBarY, hpBarWidth, hpBarHeight, new Color(40, 40, 40));

        // HP fill (red)
        float hpFill = _player.MaxHP > 0 ? _player.HP / _player.MaxHP : 0f;
        DrawRect(spriteBatch, hpBarX, hpBarY, (int)(hpBarWidth * hpFill), hpBarHeight, Color.Red);

        // HP label
        string hpText = $"HP: {_player.HP:F0}/{_player.MaxHP:F0}";
        spriteBatch.DrawString(_font, hpText, new Vector2(hpBarX + 4, hpBarY - 1), Color.White);

        // === Bottom-left: Stamina Bar ===
        int barX = 12;
        int barY = screenHeight - 30;
        int barWidth = 120;
        int barHeight = 16;

        // Background
        DrawRect(spriteBatch, barX - 1, barY - 1, barWidth + 2, barHeight + 2, Color.Black);

        // Fill
        float fill = _stats.CurrentStamina / _stats.MaxStamina;
        Color barColor = fill > 0.5f ? Color.LimeGreen : fill > 0.25f ? Color.Yellow : Color.Red;
        DrawRect(spriteBatch, barX, barY, (int)(barWidth * fill), barHeight, barColor);

        // Label
        string staminaText = $"STA: {_stats.CurrentStamina:F0}/{_stats.MaxStamina:F0}";
        spriteBatch.DrawString(_font, staminaText, new Vector2(barX + 4, barY), Color.White);

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
