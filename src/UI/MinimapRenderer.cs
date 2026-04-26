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
    /// Live render — re-draws the world (tile layers + decor) into the minimap's
    /// RenderTarget every frame at 0.5× zoom centered on the player. Shows
    /// <see cref="LiveViewTilesWide"/> tiles around the player so the minimap reveals
    /// roughly 20% MORE area than the regular game viewport (the whole point of a
    /// minimap is to show what's just off-screen).
    ///
    /// Per design, monsters/boss are NOT plotted — the minimap is a navigation aid
    /// for terrain/structure, not a combat radar.
    ///
    /// Must be called BEFORE any backbuffer drawing so SetRenderTarget(null) doesn't
    /// discard the result.
    /// </summary>
    public void PreRender(
        GraphicsDevice device,
        SpriteBatch spriteBatch,
        TileMap map,
        PlayerEntity player,
        float gameZoom)
    {
        _preRendered = false;
        if (_rt == null || _circleMask == null) return;

        // Snapshot viewport before SetRenderTarget mutates it. Restored at the end so
        // the caller's screen-space sb.Begin uses the correct backbuffer dimensions —
        // some MonoGame paths don't auto-restore reliably.
        var prevViewport = device.Viewport;

        // Tiles visible in the GAME viewport at the current zoom — minimap reveals
        // <see cref="MinimapBufferRatio"/>× that many so it always shows MORE than
        // what's on screen (the whole point of a minimap is to peek ahead). The
        // larger of width/height is used so the minimap covers the longer game axis.
        float gameViewTilesW = prevViewport.Width  / (TileMap.TileSize * Math.Max(gameZoom, 0.01f));
        float gameViewTilesH = prevViewport.Height / (TileMap.TileSize * Math.Max(gameZoom, 0.01f));
        float tilesWide = Math.Max(gameViewTilesW, gameViewTilesH) * MinimapBufferRatio;
        // Floor at 16 tiles so very small windows still show something useful.
        if (tilesWide < 16f) tilesWide = 16f;
        float scale = RtSize / (tilesWide * TileMap.TileSize);

        // Clamp the camera center so we don't render past the map edges (would show
        // empty void). When the map is smaller than the view, just center on the map.
        float halfViewWorld = (tilesWide / 2f) * TileMap.TileSize;
        float mapWorldW = map.Width * TileMap.TileSize;
        float mapWorldH = map.Height * TileMap.TileSize;
        float cx = mapWorldW <= halfViewWorld * 2f
            ? mapWorldW / 2f
            : MathHelper.Clamp(player.Position.X, halfViewWorld, mapWorldW - halfViewWorld);
        float cy = mapWorldH <= halfViewWorld * 2f
            ? mapWorldH / 2f
            : MathHelper.Clamp(player.Position.Y, halfViewWorld, mapWorldH - halfViewWorld);

        // World rect that the minimap shows — used by TileMap.Draw for culling.
        int viewSidePx = (int)Math.Ceiling(tilesWide * TileMap.TileSize);
        var viewArea = new Rectangle(
            (int)(cx - halfViewWorld), (int)(cy - halfViewWorld),
            viewSidePx, viewSidePx);

        // Camera transform: shift world so cx,cy lands at RtSize/2 after scaling.
        var transform =
            Matrix.CreateTranslation(-cx, -cy, 0) *
            Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(RtSize / 2f, RtSize / 2f, 0);

        device.SetRenderTarget(_rt);
        device.Clear(new Color(20, 20, 25)); // dark base for "outside the map" area

        // World pass — tile layers + decor with the minimap transform. Decor's
        // DrawBeforePlayer/AfterPlayer have player-relative Y-sort logic; passing
        // null player makes them just draw the full sprite (no Y-sort fade).
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            null, null, null, transform);
        map.Draw(spriteBatch, viewArea);
        // DrawBeforePlayer with null player short-circuits to DrawFull (no Y-sort fade),
        // which is exactly what we want for the minimap — every decor sprite drawn whole.
        foreach (var decor in map.Decor)
            decor.DrawBeforePlayer(spriteBatch, null);
        spriteBatch.End();

        // Player marker — small bright dot in screen-space (RT space). Not the player
        // sprite because at 0.5× scale the 16×16 sprite reads as a soft blob; a 4×4
        // marker is much clearer for the "you are here" cue.
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        var markerSize = new Point(4, 4);
        int markerX = (int)((player.Position.X - cx) * scale + RtSize / 2f) - markerSize.X / 2;
        int markerY = (int)((player.Position.Y - cy) * scale + RtSize / 2f) - markerSize.Y / 2;
        spriteBatch.Draw(_pixel, new Rectangle(markerX - 1, markerY - 1, markerSize.X + 2, markerSize.Y + 2), Color.Black);
        spriteBatch.Draw(_pixel, new Rectangle(markerX, markerY, markerSize.X, markerSize.Y), new Color(255, 244, 183));
        spriteBatch.End();

        // Multiply by circle mask to zero out alpha outside the circle.
        var multiplyBlend = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.SourceAlpha,
        };
        var maskArea = new Rectangle(0, 0, RtSize, RtSize);
        spriteBatch.Begin(SpriteSortMode.Deferred, multiplyBlend);
        spriteBatch.Draw(_circleMask, maskArea, Color.White);
        spriteBatch.End();

        device.SetRenderTarget(null);
        // Defensive viewport restore — some driver paths don't auto-restore correctly,
        // which would leave the GameplayScene's next ApplyFitZoom seeing a 192×192
        // viewport and computing camera bounds wrong (player drifts off-screen).
        device.Viewport = prevViewport;
        _preRendered = true;
    }

    /// <summary>
    /// Ratio of minimap-visible tiles to game-viewport tiles. The minimap is a
    /// CIRCLE inscribed in the square RT, so the corners (~22% of total area) are
    /// masked out — a 1.3× ratio leaves only ~8% effective buffer on the visible
    /// axis. 2.0 (=double) compensates for the mask + gives a real "scout ahead"
    /// feel where the minimap shows world the player hasn't reached yet.
    /// Resolution-independent: scales with viewport and current camera zoom.
    /// </summary>
    private const float MinimapBufferRatio = 2.0f;

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
        // Auto-color sample: render the minimap as a downscaled top-down view of the
        // actual map by averaging each tile's pixels. Caches per-gid so each unique
        // tile is decoded once even on a 50×50 map. Falls back to terrain green when
        // the cell is empty or sampling fails.
        var sampled = map.SampleTileColor(x, y);
        return sampled.A == 0 ? new Color(58, 97, 54) : sampled;
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
