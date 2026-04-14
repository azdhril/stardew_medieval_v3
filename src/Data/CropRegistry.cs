using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Farming;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Registry of all crop types. Equivalent to ScriptableObject assets in Unity.
/// </summary>
public static class CropRegistry
{
    private static readonly Dictionary<string, CropData> _crops = new();

    public static Dictionary<string, CropData> All => _crops;

    public static void Initialize(GraphicsDevice device)
    {
        const string path = "assets/Sprites/Crops/";

        // === 16px tall crops (small, ground-level) ===

        Register(new CropData
        {
            Name = "Cabbage", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Cabbage", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Cabbage_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Carrot", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Carrot", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Carrot_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Cauliflower", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Cauliflower", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Cauliflower_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Cotton", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Cotton", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Cotton_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Onion", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Onion", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Onion_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Pepper", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Pepper", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Pepper_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Radish", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Radish", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Radish_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Strawberry", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Strawberry", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Strawberry_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Wheat", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, YieldItemName = "Wheat", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Wheat_Growth_Stages_16x16.png")
        });

        // Turnip = top row (row 0)
        Register(new CropData
        {
            Name = "Turnip", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, SourceY = 0, YieldItemName = "Turnip", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Turnip_Growth_Stages_16x16.png")
        });

        // Cosmic Purple Carrot = bottom row of turnip sheet (row 1, sourceY=16)
        Register(new CropData
        {
            Name = "Cosmic Carrot", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 16, SourceY = 16, YieldItemName = "Cosmic Carrot", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Turnip_Growth_Stages_16x16.png")
        });

        // === 32px tall crops (medium height) ===

        Register(new CropData
        {
            Name = "Coffee", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Coffee", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Coffee_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Corn", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Corn", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Corn_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Pineapple", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Pineapple", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Pineapple_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Pumpkin", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Pumpkin", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Pumpkin_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Tomato", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Tomato", YieldQuantity = 2,
            GrowthSheet = LoadTexture(device, path + "Tomato_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Watermelon", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Watermelon", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Watermelon_Growth_Stages_16x16.png")
        });

        Register(new CropData
        {
            Name = "Zucchini", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, YieldItemName = "Zucchini", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Zuchini_Growth_Stages_16x16.png")
        });

        // Grape Purple = top row
        Register(new CropData
        {
            Name = "Grape Purple", StageCount = 5, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, SourceY = 0, YieldItemName = "Grape", YieldQuantity = 2,
            GrowthSheet = LoadTexture(device, path + "Grape_Growth_Stages_16x16.png")
        });

        // Grape Pink = bottom row (sourceY = 32 since each row is 32px tall; but sheet is 96... let me check)
        // Sheet is 112x96. 5 stages of 32px tall. Top row grape purple at Y=0, bottom row grape pink.
        // If each variant is 32px tall: row0=0..31, then maybe gap, row1=48..79? Or row1=32..63?
        // 96 / 3 = 32, so: row0=0, row1=32, row2=64. Two grape variants = row0 and row1.
        Register(new CropData
        {
            Name = "Grape Pink", StageCount = 5, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 32, SourceY = 32, YieldItemName = "Grape", YieldQuantity = 2,
            GrowthSheet = LoadTexture(device, path + "Grape_Growth_Stages_16x16.png")
        });

        // === 48px tall crops (very tall) ===

        Register(new CropData
        {
            Name = "Prickly Pear", StageCount = 7, DaysPerStage = 1, DaysToWilt = 2,
            SpriteHeight = 48, YieldItemName = "Prickly Pear", YieldQuantity = 1,
            GrowthSheet = LoadTexture(device, path + "Prickly_Pear_Growth_Stages_16x16.png")
        });
    }

    private static void Register(CropData crop)
    {
        _crops[crop.Name] = crop;
    }

    public static CropData? Get(string name)
    {
        _crops.TryGetValue(name, out var data);
        return data;
    }

    public static List<CropData> GetAllCrops() => new(_crops.Values);

    private static Texture2D? LoadTexture(GraphicsDevice device, string path)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            return Texture2D.FromStream(device, stream);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[CropRegistry] Failed to load {path}: {ex.Message}");
            return null;
        }
    }
}
