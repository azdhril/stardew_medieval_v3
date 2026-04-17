using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.World;

/// <summary>
/// Dynamic world chest instance with animated open/close frames and its own storage.
/// </summary>
public class ChestInstance : Entity
{
    public const int DefaultCapacity = 8;
    public const int MaxCapacity = 24;
    private const int CollisionSidePadding = 2;

    private float _frameTimer;
    private bool _opening;
    private bool _closing;

    public string InstanceId { get; }
    public string VariantId { get; }
    public Point Tile { get; }
    public ItemContainer Container { get; }

    /// <summary>Current animation frame (0 closed, 3 open).</summary>
    public int Frame { get; private set; }

    /// <summary>Bottom-center world anchor used for drawing and prompts.</summary>
    public Vector2 WorldAnchor => new(Tile.X * TileMap.TileSize + TileMap.TileSize / 2f, (Tile.Y + 1) * TileMap.TileSize);

    public bool IsOpen => !_opening && !_closing && Frame >= ChestRegistry.FrameOpen;
    public bool IsClosed => !_opening && !_closing && Frame <= ChestRegistry.FrameClosed;
    public bool IsAnimating => _opening || _closing;

    /// <summary>
    /// Chest blocks movement on its visible body, not just the bottom edge.
    /// Keeping the box a bit taller avoids the player "entering" the upper half
    /// when approaching from top to bottom.
    /// </summary>
    public override Rectangle CollisionBox => new(
        (int)WorldAnchor.X - (ChestRegistry.ArtWidth + CollisionSidePadding * 2) / 2,
        (int)WorldAnchor.Y - ChestRegistry.ArtHeightOpened,
        ChestRegistry.ArtWidth + CollisionSidePadding * 2,
        ChestRegistry.ArtHeightOpened);

    public ChestInstance(string instanceId, string variantId, Point tile, int capacity = DefaultCapacity)
    {
        InstanceId = instanceId;
        VariantId = variantId;
        Tile = tile;
        Container = new ItemContainer(MathHelper.Clamp(capacity, 1, MaxCapacity));
        Frame = ChestRegistry.FrameClosed;
        Position = WorldAnchor;
    }

    public override void Update(float deltaTime)
    {
        if (!_opening && !_closing)
            return;

        _frameTimer += deltaTime;
        while (_frameTimer >= ChestRegistry.FrameDuration)
        {
            _frameTimer -= ChestRegistry.FrameDuration;

            if (_opening)
            {
                Frame++;
                if (Frame >= ChestRegistry.FrameOpen)
                {
                    Frame = ChestRegistry.FrameOpen;
                    _opening = false;
                    break;
                }
            }
            else if (_closing)
            {
                Frame--;
                if (Frame <= ChestRegistry.FrameClosed)
                {
                    Frame = ChestRegistry.FrameClosed;
                    _closing = false;
                    break;
                }
            }
        }
    }

    public void BeginOpen()
    {
        _closing = false;
        _opening = true;
    }

    public void SetOpenedInstant()
    {
        _opening = false;
        _closing = false;
        Frame = ChestRegistry.FrameOpen;
    }

    public void BeginClose()
    {
        _opening = false;
        _closing = true;
    }

    public bool IntersectsFacingTile(Point facingTile) => facingTile == Tile;

    public Rectangle GetInteractionBounds() =>
        new(Tile.X * TileMap.TileSize, Tile.Y * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize);

    public bool ShouldDrawAfterPlayer(PlayerEntity player) =>
        player.GetFootPosition().Y < WorldAnchor.Y;

    public override void Draw(SpriteBatch spriteBatch)
    {
        var data = ChestRegistry.Get(VariantId);
        if (data?.Sheet == null)
            return;

        var src = data.GetFrameRect(Frame);
        var dest = new Rectangle(
            (int)WorldAnchor.X - src.Width / 2,
            (int)WorldAnchor.Y - src.Height,
            src.Width,
            src.Height);

        spriteBatch.Draw(data.Sheet, dest, src, Color.White);
    }
}
