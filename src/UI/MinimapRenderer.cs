using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a circular screen-space minimap with an ornate frame overlay.
/// Pre-renders map content to a RenderTarget2D with circular alpha mask,
/// then composites the result + frame during the HUD pass.
/// </summary>
public sealed class MinimapRenderer : IDisposable
{
    private const int ViewTilesWide = 10;
    private const int ViewTilesHigh = 10;
    private const int CachePixelsPerTile = 8;
    private const int RtSize = 192;

    private Texture2D _pixel = null!;
    private Texture2D? _staticMapTexture;
    private Texture2D? _frameTexture;
    private Texture2D? _circleMask;
    private RenderTarget2D? _rt;
    private Point _mapTileSize;

    // Snapshot of last PreRender params so Draw can composite without re-supplying them.
    private bool _preRendered;

    public void LoadContent(GraphicsDevice device)
    {
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            using var frameStream = File.OpenRead("assets/Sprites/System/UI Elements/Frame/UI_Frame_Map.png");
            _frameTexture = Texture2D.FromStream(device, frameStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MinimapRenderer] Failed to load UI_Frame_Map: {ex.Message}");
        }

        BuildCircleMask(device, RtSize);

        _rt = new RenderTarget2D(device, RtSize, RtSize, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
    }

    private void BuildCircleMask(GraphicsDevice device, int size)
    {
        var data = new Color[size * size];
        float center = size / 2f;
        float radius = center - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                    data[y * size + x] = Color.White;
                else if (dist <= radius + 1.5f)
                {
                    float alpha = 1f - (dist - radius) / 1.5f;
                    data[y * size + x] = Color.White * alpha;
                }
                else
                    data[y * size + x] = Color.Transparent;
            }
        }

        _circleMask = new Texture2D(device, size, size);
        _circleMask.SetData(data);
    }

    public void Rebuild(TileMap map, GraphicsDevice device)
    {
        _staticMapTexture?.Dispose();
        _mapTileSize = new Point(map.Width, map.Height);

        int texWidth = map.Width * CachePixelsPerTile;
        int texHeight = map.Height * CachePixelsPerTile;
        var data = new Color[texWidth * texHeight];
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                FillTileBlock(data, texWidth, x, y, GetTileColor(map, x, y));
            }
        }

        _staticMapTexture = new Texture2D(device, texWidth, texHeight);
        _staticMapTexture.SetData(data);
    }

    /// <summary>
    /// Pre-render minimap content into a circular RenderTarget. Must be called
    /// BEFORE any backbuffer drawing so SetRenderTarget(null) doesn't discard it.
    /// </summary>
    public void PreRender(
        GraphicsDevice device,
        SpriteBatch spriteBatch,
        TileMap map,
        PlayerEntity player,
        IEnumerable<EnemyEntity> enemies,
        BossEntity? boss,
        GridManager? grid = null)
    {
        _preRendered = false;
        if (_staticMapTexture == null || _rt == null || _circleMask == null)
            return;

        var sourceArea = GetSourcePixelArea(map, player.Position);
        var mapArea = new Rectangle(0, 0, RtSize, RtSize);

        device.SetRenderTarget(_rt);
        device.Clear(Color.Transparent);

        // Draw map content normally
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        spriteBatch.Draw(_staticMapTexture, mapArea, sourceArea, Color.White);

        if (grid != null)
            DrawFarmCells(spriteBatch, mapArea, sourceArea, grid);

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            DrawWorldMarker(spriteBatch, mapArea, sourceArea, enemy.Position, new Point(3, 3), new Color(191, 47, 47));
        }

        if (boss != null && boss.IsAlive)
            DrawWorldMarker(spriteBatch, mapArea, sourceArea, boss.Position, new Point(5, 5), new Color(255, 140, 64));

        DrawWorldMarker(spriteBatch, mapArea, sourceArea, player.Position, new Point(4, 4), new Color(255, 244, 183));

        spriteBatch.End();

        // Multiply by circle mask to zero out alpha outside the circle
        var multiplyBlend = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.SourceAlpha,
        };

        spriteBatch.Begin(SpriteSortMode.Deferred, multiplyBlend);
        spriteBatch.Draw(_circleMask, mapArea, Color.White);
        spriteBatch.End();

        device.SetRenderTarget(null);
        _preRendered = true;
    }

    /// <summary>
    /// Composite the pre-rendered circular minimap + frame onto the screen.
    /// Called during the HUD SpriteBatch pass.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Rectangle panelArea)
    {
        if (!_preRendered || _rt == null)
            return;

        int frameMargin = 8;
        var frameRect = new Rectangle(
            panelArea.X - frameMargin,
            panelArea.Y - frameMargin,
            panelArea.Width + frameMargin * 2,
            panelArea.Height + frameMargin * 2);

        spriteBatch.Draw(_rt, panelArea, Color.White);

        if (_frameTexture != null)
            spriteBatch.Draw(_frameTexture, frameRect, Color.White);
    }

    public void Dispose()
    {
        _staticMapTexture?.Dispose();
        _pixel?.Dispose();
        _circleMask?.Dispose();
        _rt?.Dispose();
    }

    private static Color GetTileColor(TileMap map, int x, int y)
    {
        if (map.IsWater(x, y))
            return new Color(28, 86, 132);

        if (map.IsFarmZone(x, y))
            return new Color(121, 92, 52);

        return new Color(58, 97, 54);
    }

    private static void FillTileBlock(Color[] data, int textureWidth, int tileX, int tileY, Color color)
    {
        int startX = tileX * CachePixelsPerTile;
        int startY = tileY * CachePixelsPerTile;

        for (int py = 0; py < CachePixelsPerTile; py++)
        {
            int row = (startY + py) * textureWidth;
            for (int px = 0; px < CachePixelsPerTile; px++)
                data[row + startX + px] = color;
        }
    }

    private Rectangle GetSourcePixelArea(TileMap map, Vector2 playerWorldPosition)
    {
        float playerTileX = playerWorldPosition.X / TileMap.TileSize;
        float playerTileY = playerWorldPosition.Y / TileMap.TileSize;

        int width = Math.Min(ViewTilesWide, map.Width);
        int height = Math.Min(ViewTilesHigh, map.Height);

        float leftTiles = playerTileX - (width / 2f);
        float topTiles = playerTileY - (height / 2f);

        leftTiles = Math.Clamp(leftTiles, 0f, Math.Max(0, map.Width - width));
        topTiles = Math.Clamp(topTiles, 0f, Math.Max(0, map.Height - height));

        return new Rectangle(
            (int)MathF.Round(leftTiles * CachePixelsPerTile),
            (int)MathF.Round(topTiles * CachePixelsPerTile),
            width * CachePixelsPerTile,
            height * CachePixelsPerTile);
    }

    private void DrawFarmCells(SpriteBatch spriteBatch, Rectangle mapArea, Rectangle sourceArea, GridManager grid)
    {
        float leftTile = sourceArea.X / (float)CachePixelsPerTile;
        float topTile = sourceArea.Y / (float)CachePixelsPerTile;
        float rightTile = leftTile + (sourceArea.Width / (float)CachePixelsPerTile);
        float bottomTile = topTile + (sourceArea.Height / (float)CachePixelsPerTile);

        foreach (var (tile, cell) in grid.GetAllCells())
        {
            if (!cell.IsTilled && cell.Crop == null)
                continue;

            float tileLeft = tile.X;
            float tileTop = tile.Y;
            if (tileLeft + 1f <= leftTile || tileLeft >= rightTile || tileTop + 1f <= topTile || tileTop >= bottomTile)
                continue;

            Color color = cell.Crop != null
                ? new Color(114, 201, 102)
                : cell.IsWatered
                    ? new Color(90, 122, 181)
                    : new Color(150, 108, 70);

            var rect = GetTileRect(mapArea, sourceArea, tile.X, tile.Y);
            DrawRect(spriteBatch, rect, color);
        }
    }

    private void DrawWorldMarker(
        SpriteBatch spriteBatch,
        Rectangle mapArea,
        Rectangle sourceArea,
        Vector2 worldPosition,
        Point size,
        Color color)
    {
        float tileX = worldPosition.X / TileMap.TileSize;
        float tileY = worldPosition.Y / TileMap.TileSize;
        float leftTile = sourceArea.X / (float)CachePixelsPerTile;
        float topTile = sourceArea.Y / (float)CachePixelsPerTile;
        float widthTiles = sourceArea.Width / (float)CachePixelsPerTile;
        float heightTiles = sourceArea.Height / (float)CachePixelsPerTile;

        if (tileX < leftTile || tileX >= leftTile + widthTiles || tileY < topTile || tileY >= topTile + heightTiles)
            return;

        float normalizedX = (tileX - leftTile) / widthTiles;
        float normalizedY = (tileY - topTile) / heightTiles;

        int x = mapArea.X + (int)(normalizedX * mapArea.Width) - (size.X / 2);
        int y = mapArea.Y + (int)(normalizedY * mapArea.Height) - (size.Y / 2);

        DrawRect(spriteBatch, new Rectangle(x, y, size.X, size.Y), color);
    }

    private Rectangle GetTileRect(Rectangle mapArea, Rectangle sourceArea, int tileX, int tileY)
    {
        float leftTile = sourceArea.X / (float)CachePixelsPerTile;
        float topTile = sourceArea.Y / (float)CachePixelsPerTile;
        float widthTiles = sourceArea.Width / (float)CachePixelsPerTile;
        float heightTiles = sourceArea.Height / (float)CachePixelsPerTile;

        int x = mapArea.X + (int)(((tileX - leftTile) / widthTiles) * mapArea.Width);
        int y = mapArea.Y + (int)(((tileY - topTile) / heightTiles) * mapArea.Height);
        int w = Math.Max(1, (int)Math.Ceiling(mapArea.Width / widthTiles));
        int h = Math.Max(1, (int)Math.Ceiling(mapArea.Height / heightTiles));
        return new Rectangle(x, y, w, h);
    }

    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        spriteBatch.Draw(_pixel, rect, color);
    }
}
