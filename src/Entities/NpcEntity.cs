using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Entities;

/// <summary>
/// Base class for interactive NPCs. Carries an identifier, an optional portrait texture
/// for dialogue UI, and a proximity check used by interaction prompts.
/// </summary>
public class NpcEntity : Entity
{
    /// <summary>Maximum distance (in world pixels) at which the player can interact with this NPC.</summary>
    public const float InteractRange = 28f;

    /// <summary>Stable string identifier used to look up dialogue/shop data.</summary>
    public string NpcId { get; }

    /// <summary>Portrait drawn in the dialogue box; null if none supplied.</summary>
    public Texture2D? Portrait { get; }

    private readonly Texture2D _sprite;

    /// <summary>
    /// Creates a new NPC.
    /// </summary>
    /// <param name="npcId">Stable identifier (e.g., "King", "Shopkeeper").</param>
    /// <param name="sprite">World sprite drawn at <see cref="Entity.Position"/>.</param>
    /// <param name="portrait">Optional dialogue portrait.</param>
    /// <param name="position">Initial world position (sprite center).</param>
    public NpcEntity(string npcId, Texture2D sprite, Texture2D? portrait, Vector2 position)
    {
        NpcId = npcId;
        _sprite = sprite;
        Portrait = portrait;
        Position = position;
        FrameWidth = 32;
        FrameHeight = 32;
        Console.WriteLine($"[NpcEntity] Spawned {npcId} at ({position.X}, {position.Y})");
    }

    /// <summary>
    /// True when <paramref name="playerPos"/> is within <see cref="InteractRange"/> of this NPC.
    /// </summary>
    public bool IsInInteractRange(Vector2 playerPos)
    {
        return Vector2.Distance(Position, playerPos) <= InteractRange;
    }

    /// <summary>Draws the NPC sprite as a 32x32 quad centered on <see cref="Entity.Position"/>.</summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_sprite == null) return;
        var dest = new Rectangle(
            (int)Position.X - 16,
            (int)Position.Y - 16,
            32,
            32);
        spriteBatch.Draw(_sprite, dest, Color.White);
    }
}
