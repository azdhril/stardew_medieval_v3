using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.World;

/// <summary>
/// Named AABB zone loaded from a TMX object layer. Scenes use these for map transitions,
/// dialogue triggers, and other spatial events.
/// </summary>
/// <param name="Name">Identifier declared on the Tiled object (e.g., "ToVillage").</param>
/// <param name="Bounds">Axis-aligned rectangle in world pixels.</param>
public record TriggerZone(string Name, Rectangle Bounds)
{
    /// <summary>True when the given world-space point lies inside <see cref="Bounds"/>.</summary>
    public bool ContainsPoint(Vector2 p) => Bounds.Contains((int)p.X, (int)p.Y);
}
