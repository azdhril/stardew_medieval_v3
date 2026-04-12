using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Quest;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Groups shared game services passed to Scene constructors.
/// Not a DI framework -- just a dependency bag.
/// </summary>
public class ServiceContainer
{
    public required GraphicsDevice GraphicsDevice { get; init; }
    public required SpriteBatch SpriteBatch { get; init; }
    public required InputManager Input { get; init; }
    public required TimeManager Time { get; init; }
    public required Camera Camera { get; init; }
    public required ContentManager Content { get; init; }

    /// <summary>
    /// Set after SceneManager is created (circular reference resolved via setter).
    /// </summary>
    public SceneManager SceneManager { get; set; } = null!;

    /// <summary>
    /// Shared inventory instance, set by FarmScene after creation.
    /// Allows other scenes (e.g. InventoryScene) to access inventory via Services.
    /// </summary>
    public InventoryManager? Inventory { get; set; }

    /// <summary>Main quest container. Set by FarmScene after construction.</summary>
    public MainQuest? Quest { get; set; }

    /// <summary>
    /// Shared player entity. Set by FarmScene after creation so other scenes
    /// (Village, Castle, Shop) can preserve player state across transitions (WLD-04).
    /// </summary>
    public PlayerEntity? Player { get; set; }

    /// <summary>
    /// Shared player sprite sheet, loaded by FarmScene. Re-bound to Player on each
    /// scene entry if needed (textures survive scene swaps since they live in Services).
    /// </summary>
    public Texture2D? PlayerSpriteSheet { get; set; }

    /// <summary>
    /// Loaded GameState reference, set by FarmScene. Scenes update CurrentScene on
    /// entry so the next auto-save captures the last active scene name.
    /// </summary>
    public GameState? GameState { get; set; }
}
