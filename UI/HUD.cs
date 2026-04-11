using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Heads-Up Display: stamina bar, clock, day counter, equipped tool.
/// Drawn in screen space (not affected by camera).
/// </summary>
public class HUD
{
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;

    private readonly TimeManager _time;
    private readonly PlayerStats _stats;
    private readonly ToolController _tools;

    public HUD(TimeManager time, PlayerStats stats, ToolController tools)
    {
        _time = time;
        _stats = stats;
        _tools = tools;
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

        // === Top-right: Equipped Tool & Selected Crop ===
        string toolText = $"[{_tools.EquippedTool}]";
        var toolSize = _font.MeasureString(toolText);
        spriteBatch.DrawString(_font, toolText, new Vector2(screenWidth - toolSize.X - 12, 10), Color.Yellow);

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

        // Controls hint removed -- hotbar now occupies bottom-center area
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }
}
