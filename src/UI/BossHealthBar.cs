using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Screen-space boss HP bar displayed at the top center of the screen.
/// Uses the same status-bar sprites as the player HUD, scaled 30% larger.
/// </summary>
public static class BossHealthBar
{
    private const float Scale = 0.65f;
    private const int TopMargin = 12;

    private static Texture2D? _barBg;
    private static Texture2D? _barFillHP;
    private static Texture2D? _pixel;
    private static Texture2D? _iconAlert;

    private const int FillOffsetXNative = 58;
    private const int FillOffsetYNative = 13;

    public static void LoadContent(GraphicsDevice device)
    {
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var bgStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Bg.png");
            _barBg = Texture2D.FromStream(device, bgStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BossHealthBar] Failed to load UI_StatusBar_Bg: {ex.Message}");
        }

        try
        {
            using var hpStream = File.OpenRead("assets/Sprites/System/UI Elements/Bars/Status/UI_StatusBar_Fill_HP.png");
            _barFillHP = Texture2D.FromStream(device, hpStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BossHealthBar] Failed to load UI_StatusBar_Fill_HP: {ex.Message}");
        }

        try
        {
            using var iconStream = File.OpenRead("assets/Sprites/System/UI Elements/Icons/Icon_alert-fill.png");
            _iconAlert = Texture2D.FromStream(device, iconStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BossHealthBar] Failed to load Icon_alert-fill: {ex.Message}");
        }
    }

    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font,
        string bossName, float hp, float maxHp, int screenWidth, int screenHeight)
    {
        if (hp <= 0 || maxHp <= 0) return;

        float fill = MathHelper.Clamp(hp / maxHp, 0f, 1f);

        if (_barBg != null && _barFillHP != null)
        {
            int bgW = (int)(_barBg.Width * Scale);
            int bgH = (int)(_barBg.Height * Scale);
            int barX = (screenWidth - bgW) / 2;
            int barY = TopMargin;

            spriteBatch.Draw(_barBg, new Rectangle(barX, barY, bgW, bgH), Color.White);

            // Draw alert icon in the square area left of the fill
            int areaW = (int)(FillOffsetXNative * Scale);
            if (_iconAlert != null && _iconAlert.Width > 1)
            {
                int iconSz = Math.Min(30, Math.Min(areaW - 4, bgH - 4));
                int ix = barX + (areaW - iconSz) / 2;
                int iy = barY + (bgH - iconSz) / 2;
                spriteBatch.Draw(_iconAlert, new Rectangle(ix, iy, iconSz, iconSz), Color.White);
            }

            int padX = (int)(FillOffsetXNative * Scale);
            int padY = (int)(FillOffsetYNative * Scale);
            int fillFullW = (int)(_barFillHP.Width * Scale);
            int fillH = (int)(_barFillHP.Height * Scale);
            int fillW = (int)MathHelper.Clamp(fillFullW * fill, 0, fillFullW);

            if (fillW > 0)
            {
                int srcW = (int)MathHelper.Clamp(_barFillHP.Width * fill, 0, _barFillHP.Width);
                var src = new Rectangle(0, 0, srcW, _barFillHP.Height);
                var dest = new Rectangle(barX + padX, barY + padY, fillW, fillH);
                spriteBatch.Draw(_barFillHP, dest, src, Color.White);
            }

            var nameSize = font.MeasureString(bossName);
            float nameX = barX + (bgW - nameSize.X) / 2f;
            float nameY = barY + (bgH - nameSize.Y) / 2.4f;
            spriteBatch.DrawString(font, bossName, new Vector2(nameX, nameY), Color.White * 0.7f);
        }
        else
        {
            // Flat-rect fallback
            int barWidth = 300;
            int barHeight = 12;
            int barX = (screenWidth - barWidth) / 2;
            int barY = TopMargin;

            spriteBatch.Draw(pixel, new Rectangle(barX - 1, barY - 1, barWidth + 2, barHeight + 2), new Color(100, 100, 100));
            spriteBatch.Draw(pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(30, 30, 30));
            int fillWidth = (int)(barWidth * fill);
            if (fillWidth > 0)
                spriteBatch.Draw(pixel, new Rectangle(barX, barY, fillWidth, barHeight), new Color(180, 20, 20));

            var nameSize = font.MeasureString(bossName);
            float nameX = barX + (barWidth - nameSize.X) / 2f;
            float nameY = barY - nameSize.Y - 4;
            spriteBatch.DrawString(font, bossName, new Vector2(nameX, nameY), Color.White);
        }
    }
}
