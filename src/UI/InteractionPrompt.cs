using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Small floating prompt (e.g. "Press E to talk") rendered above an entity or
/// trigger in screen space. Caller converts world position to screen position
/// via the camera transform before calling <see cref="Draw"/>. UI-SPEC §Component
/// Inventory: ~120x24 panel, 20px above entity.
/// </summary>
public class InteractionPrompt
{
    private const int Height = 24;
    private const int Padding = 8;
    private const int VerticalOffset = 20;

    private static readonly Color PanelFill = new Color(60, 40, 30);
    private static readonly Color PanelOutline = Color.Black;

    /// <summary>
    /// Draws the prompt panel centered horizontally on <paramref name="screenPos"/>,
    /// offset 20px upward. Caller owns SpriteBatch.Begin/End.
    /// </summary>
    /// <param name="sb">Open SpriteBatch.</param>
    /// <param name="font">Font for the prompt text.</param>
    /// <param name="pixel">1x1 white texture.</param>
    /// <param name="screenPos">Screen-space anchor (entity head in screen coords).</param>
    /// <param name="text">Prompt copy, e.g. "Press E to talk".</param>
    public void Draw(SpriteBatch sb, SpriteFontBase font, Texture2D pixel, Vector2 screenPos, string text)
    {
        var textSize = font.MeasureString(text);
        int panelW = (int)textSize.X + Padding * 2;
        int panelH = Height;
        int panelX = (int)(screenPos.X - panelW / 2f);
        int panelY = (int)(screenPos.Y - VerticalOffset - panelH);

        // Outline
        sb.Draw(pixel, new Rectangle(panelX - 1, panelY - 1, panelW + 2, panelH + 2), PanelOutline);
        // Fill
        sb.Draw(pixel, new Rectangle(panelX, panelY, panelW, panelH), PanelFill);

        int textX = panelX + (panelW - (int)textSize.X) / 2;
        int textY = panelY + (panelH - (int)textSize.Y) / 2;
        sb.DrawString(font, text, new Vector2(textX, textY), Color.White);
    }
}
