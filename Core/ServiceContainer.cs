using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Inventory;
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
}
