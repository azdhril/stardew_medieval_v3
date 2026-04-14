using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Definition of a chest variant on the shared chests.png spritesheet.
/// Layout: 9 columns (one per variant) x 4 rows (animation frames, top=closed,
/// bottom=open). Each cell is 32x32 padded; the actual art is 18x16 closed and
/// 18x20 opened, centered horizontally and bottom-aligned inside the cell.
/// </summary>
public class ChestData
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int VariantIndex { get; init; }
    public Texture2D? Sheet { get; init; }

    /// <summary>Source rect for a given animation frame (0=closed, 3=fully open).</summary>
    public Rectangle GetFrameRect(int frame)
    {
        int f = System.Math.Clamp(frame, 0, ChestRegistry.FrameCount - 1);
        return new Rectangle(
            VariantIndex * ChestRegistry.CellWidth,
            f * ChestRegistry.CellHeight,
            ChestRegistry.CellWidth,
            ChestRegistry.CellHeight);
    }

    /// <summary>Tight art rect (drops the transparent padding) for the given frame.</summary>
    public Rectangle GetArtRect(int frame)
    {
        int f = System.Math.Clamp(frame, 0, ChestRegistry.FrameCount - 1);
        bool opened = f == ChestRegistry.FrameCount - 1;
        int artW = ChestRegistry.ArtWidth;
        int artH = opened ? ChestRegistry.ArtHeightOpened : ChestRegistry.ArtHeightClosed;
        int cellX = VariantIndex * ChestRegistry.CellWidth;
        int cellY = f * ChestRegistry.CellHeight;
        int offsetX = (ChestRegistry.CellWidth - artW) / 2;
        int offsetY = ChestRegistry.CellHeight - artH; // bottom-aligned
        return new Rectangle(cellX + offsetX, cellY + offsetY, artW, artH);
    }
}
