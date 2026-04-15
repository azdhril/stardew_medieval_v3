using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TiledCS;

namespace stardew_medieval_v3.World;

/// <summary>
/// Loads and renders Tiled TMX maps. Handles multiple layers, collision via object polygons, and farm zones.
/// </summary>
public class TileMap
{
    public const int TileSize = 16;

    public int Width { get; private set; }
    public int Height { get; private set; }

    private TiledMap _map = null!;
    private Dictionary<int, TiledTileset> _tilesets = new();
    private Dictionary<int, Texture2D> _tilesetTextures = new();

    // Layer references (by name)
    private TiledLayer? _groundLayer;
    private TiledLayer? _waterLayer;
    private TiledLayer? _farmZoneLayer;

    // Collision polygons from Tiled object layer
    private readonly List<Vector2[]> _collisionPolygons = new();

    // Trigger zones from Tiled "Triggers" object layer
    private readonly List<TriggerZone> _triggers = new();

    /// <summary>Named rectangular trigger zones loaded from the TMX "Triggers" object group.</summary>
    public IReadOnlyList<TriggerZone> Triggers => _triggers;

    public void Load(string tmxPath, GraphicsDevice device)
    {
        _map = new TiledMap(tmxPath);
        Width = _map.Width;
        Height = _map.Height;

        var mapDir = Path.GetDirectoryName(tmxPath)!;

        // Load all tilesets
        foreach (var mapTileset in _map.Tilesets)
        {
            var tsxPath = Path.Combine(mapDir, mapTileset.source);
            var tileset = new TiledTileset(tsxPath);
            _tilesets[mapTileset.firstgid] = tileset;

            var imgPath = Path.Combine(mapDir, tileset.Image.source);
            using var stream = File.OpenRead(imgPath);
            var texture = Texture2D.FromStream(device, stream);
            _tilesetTextures[mapTileset.firstgid] = texture;
        }

        // Find tile layers by name
        foreach (var layer in _map.Layers)
        {
            switch (layer.name.ToLower())
            {
                case "ground": _groundLayer = layer; break;
                case "water": _waterLayer = layer; break;
                case "farmzone": _farmZoneLayer = layer; break;
            }
        }

        // Load collision polygons from object groups
        LoadCollisionObjects();

        // Load trigger zones from "Triggers" object group (optional — many maps won't have one)
        LoadTriggerObjects();

        Console.WriteLine($"[TileMap] Loaded {Width}x{Height} map, {_map.Layers.Length} layers, {_collisionPolygons.Count} collision polygons, {_triggers.Count} trigger zones");
    }

    /// <summary>
    /// Parses any ObjectLayer named "Triggers" (case-insensitive) into <see cref="TriggerZone"/> records.
    /// Missing layers, missing names, and missing objects are all tolerated silently.
    /// </summary>
    private void LoadTriggerObjects()
    {
        _triggers.Clear();

        if (_map.Layers == null) return;

        foreach (var layer in _map.Layers)
        {
            if (layer.type != TiledLayerType.ObjectLayer) continue;
            if (!layer.name.Equals("Triggers", StringComparison.OrdinalIgnoreCase)) continue;

            var objects = layer.objects ?? Array.Empty<TiledObject>();
            foreach (var obj in objects)
            {
                if (obj.width <= 0 || obj.height <= 0) continue;
                var rect = new Rectangle(
                    (int)obj.x,
                    (int)obj.y,
                    (int)obj.width,
                    (int)obj.height);
                _triggers.Add(new TriggerZone(obj.name ?? "", rect));
            }
        }

        if (_triggers.Count > 0)
            Console.WriteLine($"[TileMap] Loaded {_triggers.Count} trigger zones");
    }

    /// <summary>
    /// A single TMX object parsed from an object group: name, AABB (world px),
    /// an optional Point (object center), and its custom properties map.
    /// </summary>
    public record TmxObject(
        string Name,
        Rectangle Bounds,
        Vector2 Point,
        Dictionary<string, string> Properties);

    /// <summary>
    /// Return all TMX objects in the object group with the given name
    /// (case-insensitive). Empty list if the group is missing — callers
    /// must tolerate absence (Pitfall 7: DungeonScene falls back to
    /// DungeonRegistry.Spawns when "EnemySpawns" is empty).
    /// </summary>
    public List<TmxObject> GetObjectGroup(string groupName)
    {
        var results = new List<TmxObject>();
        if (_map?.Layers == null) return results;

        foreach (var layer in _map.Layers)
        {
            if (layer.type != TiledLayerType.ObjectLayer) continue;
            if (!string.Equals(layer.name, groupName, StringComparison.OrdinalIgnoreCase)) continue;

            var objects = layer.objects ?? Array.Empty<TiledObject>();
            foreach (var obj in objects)
            {
                var bounds = new Rectangle(
                    (int)obj.x,
                    (int)obj.y,
                    (int)obj.width,
                    (int)obj.height);
                var point = new Vector2(
                    obj.x + obj.width / 2f,
                    obj.y + obj.height / 2f);

                var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (obj.properties != null)
                {
                    foreach (var p in obj.properties)
                    {
                        if (p.name == null) continue;
                        props[p.name] = p.value ?? "";
                    }
                }

                results.Add(new TmxObject(obj.name ?? "", bounds, point, props));
            }
        }

        return results;
    }

    private void LoadCollisionObjects()
    {
        _collisionPolygons.Clear();

        foreach (var group in _map.Groups)
        {
            if (!group.name.Equals("Collision", StringComparison.OrdinalIgnoreCase)) continue;
            LoadObjectsFromGroup(group.objects, 0, 0);
        }

        // Also check top-level object layers (TiledCS stores them differently)
        if (_map.Layers != null)
        {
            foreach (var layer in _map.Layers)
            {
                if (layer.type == TiledLayerType.ObjectLayer && layer.name.Equals("Collision", StringComparison.OrdinalIgnoreCase))
                {
                    if (layer.objects != null)
                        LoadObjectsFromGroup(layer.objects, 0, 0);
                }
            }
        }
    }

    private void LoadObjectsFromGroup(TiledObject[] objects, float offsetX, float offsetY)
    {
        foreach (var obj in objects)
        {
            float ox = obj.x + offsetX;
            float oy = obj.y + offsetY;

            if (obj.polygon != null && obj.polygon.points != null)
            {
                // Polygon object: points are float[] pairs [x0,y0,x1,y1,...] relative to object position
                var points = ParseFloatPoints(obj.polygon.points, ox, oy);
                if (points.Length >= 3)
                {
                    _collisionPolygons.Add(points);
                    Console.WriteLine($"[TileMap] Loaded collision polygon with {points.Length} points at ({ox:F1}, {oy:F1})");
                }
            }
            else
            {
                // Rectangle object (no polygon): use x, y, width, height as a box
                if (obj.width > 0 && obj.height > 0)
                {
                    var rect = new Vector2[]
                    {
                        new(ox, oy),
                        new(ox + obj.width, oy),
                        new(ox + obj.width, oy + obj.height),
                        new(ox, oy + obj.height)
                    };
                    _collisionPolygons.Add(rect);
                    Console.WriteLine($"[TileMap] Loaded collision rect at ({ox:F1}, {oy:F1}) size {obj.width}x{obj.height}");
                }
            }
        }
    }

    /// <summary>
    /// TiledCS polygon.points is a flat float array: [x0, y0, x1, y1, ...]
    /// </summary>
    private static Vector2[] ParseFloatPoints(float[] rawPoints, float originX, float originY)
    {
        int count = rawPoints.Length / 2;
        var points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector2(originX + rawPoints[i * 2], originY + rawPoints[i * 2 + 1]);
        }
        return points;
    }

    public bool IsFarmZone(int x, int y)
    {
        if (_farmZoneLayer == null) return false;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return GetGid(_farmZoneLayer, x, y) != 0;
    }

    public bool IsWater(int x, int y)
    {
        if (_waterLayer == null) return false;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return GetGid(_waterLayer, x, y) != 0;
    }

    /// <summary>
    /// Check if a world-space rectangle collides with any collision polygon or map edges.
    /// </summary>
    public bool CheckCollision(Rectangle worldRect)
    {
        var center = new Vector2(worldRect.Center.X, worldRect.Center.Y);
        float radius = Math.Min(worldRect.Width, worldRect.Height) / 2f;
        return CheckCircleCollision(center, radius);
    }

    /// <summary>
    /// Circle-based collision: slides smoothly along polygon edges instead of catching on corners.
    /// </summary>
    public bool CheckCircleCollision(Vector2 center, float radius)
    {
        // Map boundary
        var bounds = GetWorldBounds();
        if (center.X - radius < bounds.Left || center.X + radius > bounds.Right ||
            center.Y - radius < bounds.Top || center.Y + radius > bounds.Bottom)
            return true;

        // Check if circle overlaps any collision polygon
        foreach (var polygon in _collisionPolygons)
        {
            if (CircleIntersectsPolygon(center, radius, polygon))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Ray-casting algorithm to test if a point is inside a polygon.
    /// </summary>
    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int n = polygon.Length;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Check if a circle overlaps a polygon. Tests: center inside, or circle touches any edge.
    /// </summary>
    private static bool CircleIntersectsPolygon(Vector2 center, float radius, Vector2[] polygon)
    {
        // If center is inside polygon, definitely colliding
        if (PointInPolygon(center, polygon))
            return true;

        // Check distance from circle center to each polygon edge
        float rSq = radius * radius;
        int n = polygon.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float distSq = PointToSegmentDistanceSq(center, polygon[j], polygon[i]);
            if (distSq <= rSq)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Squared distance from point P to line segment AB.
    /// </summary>
    private static float PointToSegmentDistanceSq(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLenSq = ab.LengthSquared();
        if (abLenSq < 0.0001f) return Vector2.DistanceSquared(p, a);

        float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / abLenSq, 0f, 1f);
        Vector2 closest = a + ab * t;
        return Vector2.DistanceSquared(p, closest);
    }

    public Rectangle GetWorldBounds() =>
        new(0, 0, Width * TileSize, Height * TileSize);

    /// <summary>
    /// Draws collision polygons as outlined edges for F3 debug overlay.
    /// </summary>
    public void DrawCollisionDebug(SpriteBatch spriteBatch, Texture2D pixel, Color color)
    {
        foreach (var polygon in _collisionPolygons)
        {
            int n = polygon.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
                DrawDebugLine(spriteBatch, pixel, polygon[j], polygon[i], color);
        }
    }

    private static void DrawDebugLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < 0.0001f) return;
        float angle = (float)Math.Atan2(delta.Y, delta.X);
        sb.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(length, 1f), SpriteEffects.None, 0f);
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle viewArea)
    {
        if (_map.Layers == null) return;
        foreach (var layer in _map.Layers)
        {
            if (layer.type != TiledLayerType.TileLayer) continue;
            if (!layer.visible) continue;
            // farmzone is a gameplay tag layer (drives IsFarmZone), never rendered
            if (string.Equals(layer.name, "farmzone", StringComparison.OrdinalIgnoreCase)) continue;
            DrawLayer(spriteBatch, layer, viewArea);
        }
    }

    private void DrawLayer(SpriteBatch spriteBatch, TiledLayer layer, Rectangle viewArea)
    {
        int offX = (int)MathF.Round((float)layer.offsetX);
        int offY = (int)MathF.Round((float)layer.offsetY);

        // Shift viewArea into layer-local space so culling bounds account for the offset.
        int localLeft = viewArea.Left - offX;
        int localTop = viewArea.Top - offY;
        int localRight = viewArea.Right - offX;
        int localBottom = viewArea.Bottom - offY;

        int startX = Math.Max(0, localLeft / TileSize - 1);
        int startY = Math.Max(0, localTop / TileSize - 1);
        int endX = Math.Min(Width - 1, localRight / TileSize + 1);
        int endY = Math.Min(Height - 1, localBottom / TileSize + 1);

        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        {
            int gid = GetGid(layer, x, y);
            if (gid == 0) continue;

            var destRect = new Rectangle(
                x * TileSize + offX,
                y * TileSize + offY,
                TileSize,
                TileSize);
            DrawTileByGid(spriteBatch, gid, destRect);
        }
    }

    private void DrawTileByGid(SpriteBatch spriteBatch, int rawGid, Rectangle dest)
    {
        int gid = rawGid & 0x1FFFFFFF;

        int firstGid = 0;
        TiledTileset? tileset = null;
        Texture2D? texture = null;

        foreach (var kvp in _tilesets)
        {
            if (gid >= kvp.Key && kvp.Key > firstGid)
            {
                firstGid = kvp.Key;
                tileset = kvp.Value;
                texture = _tilesetTextures[kvp.Key];
            }
        }

        if (tileset == null || texture == null) return;

        int localId = gid - firstGid;
        int columns = tileset.Columns;
        if (columns <= 0) return;

        int srcX = (localId % columns) * TileSize;
        int srcY = (localId / columns) * TileSize;
        var srcRect = new Rectangle(srcX, srcY, TileSize, TileSize);

        var effects = SpriteEffects.None;
        if ((rawGid & 0x80000000) != 0) effects |= SpriteEffects.FlipHorizontally;
        if ((rawGid & 0x40000000) != 0) effects |= SpriteEffects.FlipVertically;

        spriteBatch.Draw(texture, dest, srcRect, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

    private int GetGid(TiledLayer layer, int x, int y)
    {
        int index = y * Width + x;
        if (index < 0 || index >= layer.data.Length) return 0;
        return layer.data[index];
    }

    public static Point WorldToTile(Vector2 worldPos) =>
        new((int)MathF.Floor(worldPos.X / TileSize), (int)MathF.Floor(worldPos.Y / TileSize));

    public static Vector2 TileToWorld(int x, int y) =>
        new(x * TileSize, y * TileSize);

    public static Vector2 TileCenterWorld(int x, int y) =>
        new(x * TileSize + TileSize / 2f, y * TileSize + TileSize / 2f);
}
