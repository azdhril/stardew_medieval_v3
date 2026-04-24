using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.UI.Widgets;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Abstract base class for game scenes. Scenes inherit and override
/// LoadContent, Update, Draw, UnloadContent.
/// </summary>
public abstract class Scene
{
    protected ServiceContainer Services { get; }

    /// <summary>
    /// Per-scene UI widget orchestrator. Scenes register clickable widgets in
    /// <see cref="LoadContent"/>, call <c>Ui.Update(dt, Services.Input)</c> first
    /// in <see cref="Update"/> to consume widget clicks before scene-level input,
    /// and <c>Ui.Draw(...)</c> last in <see cref="Draw"/> to overlay focus outline
    /// and tooltip on top of widget chrome. The manager is auto-created so scenes
    /// can use it unconditionally; non-migrated scenes simply never call Register
    /// and pay zero overhead. Cleared by the base <see cref="UnloadContent"/>,
    /// which also restores the cursor to <see cref="MouseCursor.Arrow"/> to avoid
    /// the stale Hand cursor leak on scene pop (Pitfall 2).
    /// </summary>
    protected UIManager Ui { get; } = new UIManager();

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

    /// <summary>
    /// Base UnloadContent clears widget registrations and restores the cursor
    /// to <see cref="MouseCursor.Arrow"/> (net-fix for the latent cursor-leak
    /// bug described in RESEARCH Pitfall 2). Derived overrides SHOULD call
    /// <c>base.UnloadContent()</c> to inherit this cleanup.
    /// </summary>
    public virtual void UnloadContent()
    {
        Ui.Clear();
        Mouse.SetCursor(MouseCursor.Arrow);
    }
}
