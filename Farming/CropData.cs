using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Farming;

/// <summary>
/// Static definition of a crop type.
/// Spritesheet layout: N columns (stages) laid out horizontally.
/// Each stage = 16px wide. Height varies per crop (16, 32, or 48).
/// Some sheets have multiple rows (variants) — use SourceY to pick the row.
/// </summary>
public class CropData
{
    public string Name { get; init; } = "";
    public int DaysPerStage { get; init; } = 1;
    public int StageCount { get; init; } = 7;
    public int DaysToWilt { get; init; } = 2;
    public string YieldItemName { get; init; } = "";
    public int YieldQuantity { get; init; } = 1;

    public Texture2D? GrowthSheet { get; set; }

    /// <summary>Height of one stage frame in pixels (16, 32, or 48).</summary>
    public int SpriteHeight { get; init; } = 16;

    /// <summary>Y offset in the spritesheet (for sheets with multiple variant rows).</summary>
    public int SourceY { get; init; } = 0;

    public int TotalGrowthDays => DaysPerStage * StageCount;

    public bool IsRipe(int dayCount) => dayCount >= TotalGrowthDays;

    public bool IsWilted(int dayCount) => dayCount >= TotalGrowthDays + DaysToWilt;

    public int GetStageIndex(int dayCount)
    {
        if (DaysPerStage <= 0) return 0;
        int stage = dayCount / DaysPerStage;
        return System.Math.Clamp(stage, 0, StageCount - 1);
    }

    public Rectangle GetSourceRect(int dayCount, bool isWilted)
    {
        int stage = isWilted ? StageCount - 1 : GetStageIndex(dayCount);
        return new Rectangle(stage * 16, SourceY, 16, SpriteHeight);
    }
}
