using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Combat;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Renders a lightweight screen-space minimap using one pixel per world tile.
/// Static terrain is cached into a texture and dynamic actors are drawn each frame.
/// </summary>
public sealed class MinimapRenderer : IDisposable
{
    private const int PanelPadding = 2;
    private const int BorderThickness = 2;
    private const int ViewTilesWide = 10;
    private const int ViewTilesHigh = 10;
    private const int CachePixelsPerTile = 8;

    private Texture2D _pixel = null!;
    private Texture2D? _staticMapTexture;
    private Point _mapTileSize;

    public void LoadContent(GraphicsDevice device)
    {
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Rebuilds the static terrain texture. Call when entering a new map.
    /// </summary>
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

    public void Draw(
        SpriteBatch spriteBatch,
        Rectangle panelArea,
        TileMap map,
        Camera camera,
        PlayerEntity player,
        IEnumerable<EnemyEntity> enemies,
        BossEntity? boss,
        GridManager? grid = null)
    {
        if (_staticMapTexture == null)
            return;

        DrawRect(spriteBatch, panelArea, Color.Black * 0.9f);
        DrawRect(
            spriteBatch,
            new Rectangle(
                panelArea.X + BorderThickness,
                panelArea.Y + BorderThickness,
                panelArea.Width - (BorderThickness * 2),
                panelArea.Height - (BorderThickness * 2)),
            new Color(48, 31, 22));

        var mapArea = GetMapArea(panelArea);
        var sourceArea = GetSourcePixelArea(map, player.Position);
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
    }

    public void Dispose()
    {
        _staticMapTexture?.Dispose();
        _pixel?.Dispose();
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

    private Rectangle GetMapArea(Rectangle panelArea)
    {
        int innerX = panelArea.X + BorderThickness + PanelPadding;
        int innerY = panelArea.Y + BorderThickness + PanelPadding;
        int innerW = panelArea.Width - ((BorderThickness + PanelPadding) * 2);
        int innerH = panelArea.Height - ((BorderThickness + PanelPadding) * 2);

        float mapAspect = _mapTileSize.X / (float)_mapTileSize.Y;
        float areaAspect = innerW / (float)innerH;

        if (mapAspect > areaAspect)
        {
            int drawH = (int)(innerW / mapAspect);
            int offsetY = (innerH - drawH) / 2;
            return new Rectangle(innerX, innerY + offsetY, innerW, drawH);
        }

        int drawW = (int)(innerH * mapAspect);
        int offsetX = (innerW - drawW) / 2;
        return new Rectangle(innerX + offsetX, innerY, drawW, innerH);
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
