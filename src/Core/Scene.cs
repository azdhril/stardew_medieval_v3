using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Abstract base class for game scenes. Scenes inherit and override
/// LoadContent, Update, Draw, UnloadContent.
/// </summary>
public abstract class Scene
{
    protected ServiceContainer Services { get; }

    protected Scene(ServiceContainer services)
    {
        Services = services;
    }

    /// <summary>Called once when scene is pushed onto the stack.</summary>
    public virtual void LoadContent() { }

    /// <summary>Called every frame for the active (top) scene.</summary>
    public virtual void Update(float deltaTime) { }

    /// <summary>Called every frame for all scenes in the stack (bottom to top).</summary>
    public virtual void Draw(SpriteBatch spriteBatch) { }

    /// <summary>Called when scene is popped from the stack. Release resources here.</summary>
    public virtual void UnloadContent() { }
}
