using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

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

    /// <summary>
    /// One-shot scene restore target set by Game1.LoadContent from the loaded save.
    /// FarmScene reads this at end of OnLoad and, if non-null and not "Farm", issues
    /// a TransitionTo on the next frame. Consumer MUST set to null after consumption.
    /// </summary>
    public string? PendingRestoreScene { get; set; }

    /// <summary>
    /// One-shot restore position for a specific scene. Set by Game1 from
    /// <see cref="GameState.PositionByScene"/> for whichever scene we're about to enter.
    /// Consumed by GameplayScene.LoadContent when SceneName matches
    /// <see cref="PendingRestoreSceneName"/>. Use <c>null</c> to indicate no restore.
    /// </summary>
    public Vector2? PendingRestorePosition { get; set; }

    /// <summary>Name of the scene whose position is queued in <see cref="PendingRestorePosition"/>.</summary>
    public string? PendingRestoreSceneName { get; set; }

    /// <summary>
    /// Shared sprite atlas (item/tool icons). Set by FarmScene after atlas
    /// construction so overlay scenes (e.g. ShopOverlayScene) can render icons
    /// without rebuilding registrations.
    /// </summary>
    public SpriteAtlas? Atlas { get; set; }

    /// <summary>
    /// Shared font service (runtime TTF rasterizer via FontStashSharp). Created
    /// lazily by the first gameplay scene that needs it in LoadContent; reused
    /// across all scenes. Replaces the old SpriteFont content-pipeline fonts
    /// (see quick task 260423-tu6).
    /// </summary>
    public FontService? Fonts { get; set; }

    /// <summary>
    /// Shared HUD renderer. Set by FarmScene so hub scenes (Village/Castle/Shop)
    /// can draw the same HUD on transition without re-instantiating.
    /// </summary>
    public HUD? Hud { get; set; }

    /// <summary>
    /// Shared hotbar renderer. Set by FarmScene so hub scenes can draw it.
    /// </summary>
    public HotbarRenderer? Hotbar { get; set; }

    /// <summary>
    /// Shared UI theme (9-slice textures + icons). Lazily created by the first
    /// overlay scene that needs it; reused by subsequent overlays to avoid
    /// reloading textures.
    /// </summary>
    public UITheme? Theme { get; set; }

    /// <summary>
    /// Per-scene chest manager. Owned by whichever scene currently renders chests
    /// (FarmScene, DungeonScene). Read by GameStateSnapshot.SaveNow so manual
    /// saves capture chest contents from any scene.
    /// </summary>
    public ChestManager? ChestManager { get; set; }

    /// <summary>
    /// Per-scene resource manager. Same lifecycle as ChestManager — owned by the
    /// currently active world scene; read by SaveNow.
    /// </summary>
    public ResourceManager? ResourceManager { get; set; }

    /// <summary>
    /// Run-scoped dungeon state singleton. Persists across DungeonScene transitions
    /// so room-cleared / chest-opened flags survive door traversal. Reset on death
    /// or fresh dungeon entry via <see cref="DungeonState.BeginRun"/>.
    /// </summary>
    public DungeonState? Dungeon { get; set; }

    /// <summary>
    /// Progression manager tracking XP, level, and damage bonus.
    /// Set by FarmScene after player creation; read by GameStateSnapshot.
    /// </summary>
    public Progression.ProgressionManager? Progression { get; set; }

    /// <summary>
    /// Shared toast renderer. Persists across scene transitions so death penalty
    /// messages queued in DungeonScene display after the FarmScene transition.
    /// Created lazily by the first gameplay scene that needs it.
    /// </summary>
    public Toast? Toast { get; set; }

    /// <summary>Wired by Game1 so menus can toggle fullscreen without holding a GraphicsDeviceManager.</summary>
    public System.Action? ToggleFullscreen { get; set; }

    /// <summary>Wired by Game1 so menus can request a clean exit.</summary>
    public System.Action? QuitGame { get; set; }

    /// <summary>
    /// Drops an item into the active gameplay scene's world at <paramref name="worldPosition"/>.
    /// Wired by FarmScene / DungeonScene so overlays (InventoryScene, ChestScene) can throw
    /// items to the floor when the user drags them outside the panel. Items spawned via this
    /// hook are flagged "DroppedByPlayer" so they don't auto-magnet back into the inventory
    /// until the player walks away and returns (or presses E nearby for manual pickup).
    /// </summary>
    public System.Action<string, int, Vector2>? SpawnItemDrop { get; set; }
}
