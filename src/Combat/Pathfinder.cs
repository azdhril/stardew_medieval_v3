using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// A* pathfinder on a tile-based walkability grid. Built once per map load
/// from TileMap collision data. Enemies query FindPath() to navigate around
/// obstacles instead of walking in straight lines that get stuck on walls.
/// Uses octile heuristic with 8-directional movement and diagonal blocking
/// to prevent corner-cutting through walls.
/// </summary>
public class Pathfinder
{
    private bool[,] _walkable = null!;
    private int _width;
    private int _height;

    // 8-directional neighbors: cardinals [0..3], diagonals [4..7]
    private static readonly Point[] Neighbors =
    {
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0),
        new(1, -1), new(1, 1), new(-1, 1), new(-1, -1)
    };

    private static readonly float[] NeighborCosts =
    {
        1f, 1f, 1f, 1f,
        1.414f, 1.414f, 1.414f, 1.414f
    };

    /// <summary>
    /// Build walkability grid by probing TileMap collision at each tile.
    /// Call once after map load. A slightly inset rectangle avoids false
    /// positives from tile-edge collision polygons.
    /// </summary>
    public void BuildGrid(TileMap map)
    {
        _width = map.Width;
        _height = map.Height;
        _walkable = new bool[_width, _height];

        int ts = TileMap.TileSize;
        int inset = 2;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var testRect = new Rectangle(
                    x * ts + inset,
                    y * ts + inset,
                    ts - inset * 2,
                    ts - inset * 2);
                _walkable[x, y] = !map.CheckCollision(testRect);
            }
        }

        Console.WriteLine($"[Pathfinder] Built {_width}x{_height} walkability grid");
    }

    /// <summary>Check if a tile coordinate is within bounds and walkable.</summary>
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return false;
        return _walkable[x, y];
    }

    /// <summary>
    /// Find shortest path from start to goal using A* with octile heuristic.
    /// Returns list of tile Points (start to goal inclusive), or null if
    /// unreachable within the maxNodes exploration budget.
    /// </summary>
    public List<Point>? FindPath(Point start, Point goal, int maxNodes = 500)
    {
        if (!IsWalkable(goal.X, goal.Y))
        {
            goal = FindNearestWalkable(goal);
            if (goal.X < 0) return null;
        }

        if (!IsWalkable(start.X, start.Y))
        {
            start = FindNearestWalkable(start);
            if (start.X < 0) return null;
        }

        if (start == goal)
            return new List<Point> { start };

        var openSet = new PriorityQueue<Point, float>();
        var cameFrom = new Dictionary<Point, Point>();
        var gScore = new Dictionary<Point, float> { [start] = 0f };

        openSet.Enqueue(start, Heuristic(start, goal));
        int explored = 0;

        while (openSet.Count > 0 && explored < maxNodes)
        {
            var current = openSet.Dequeue();
            explored++;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            float currentG = gScore[current];

            for (int i = 0; i < Neighbors.Length; i++)
            {
                int nx = current.X + Neighbors[i].X;
                int ny = current.Y + Neighbors[i].Y;
                var nb = new Point(nx, ny);

                if (!IsWalkable(nx, ny)) continue;

                // Block diagonal if either adjacent cardinal wall would be clipped
                if (i >= 4)
                {
                    if (!IsWalkable(current.X + Neighbors[i].X, current.Y) ||
                        !IsWalkable(current.X, current.Y + Neighbors[i].Y))
                        continue;
                }

                float tentG = currentG + NeighborCosts[i];

                if (!gScore.TryGetValue(nb, out float oldG) || tentG < oldG)
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentG;
                    openSet.Enqueue(nb, tentG + Heuristic(nb, goal));
                }
            }
        }

        return null;
    }

    /// <summary>Octile distance heuristic for 8-directional movement.</summary>
    private static float Heuristic(Point a, Point b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy) + 0.414f * Math.Min(dx, dy);
    }

    private static List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Add(prev);
            current = prev;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Find the nearest walkable tile to center (BFS expanding ring).
    /// Returns Point(-1,-1) if none found within 10 tiles.
    /// </summary>
    private Point FindNearestWalkable(Point center)
    {
        for (int r = 1; r <= 10; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                    int nx = center.X + dx;
                    int ny = center.Y + dy;
                    if (IsWalkable(nx, ny))
                        return new Point(nx, ny);
                }
            }
        }
        return new Point(-1, -1);
    }

    /// <summary>
    /// Count walkable tiles along a ray from worldPos in direction.
    /// Used by ranged enemies to choose flee directions with open space.
    /// </summary>
    public int ScoreDirectionOpenness(Vector2 worldPos, Vector2 direction, int maxSteps = 5)
    {
        int ts = TileMap.TileSize;
        int count = 0;
        for (int step = 1; step <= maxSteps; step++)
        {
            var check = worldPos + direction * (step * ts);
            int tx = (int)MathF.Floor(check.X / ts);
            int ty = (int)MathF.Floor(check.Y / ts);
            if (!IsWalkable(tx, ty)) break;
            count++;
        }
        return count;
    }
}
