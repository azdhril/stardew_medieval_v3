using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Quest;
using stardew_medieval_v3.Scenes;

namespace stardew_medieval_v3;

/// <summary>
/// Game entry point. Thin shell that delegates all logic to SceneManager.
/// </summary>
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private InputManager _input = null!;
    private SceneManager _sceneManager = null!;
    private ServiceContainer _services = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "assets";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        Window.Title = "Stardew Medieval v3 (MonoGame)";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        _input = new InputManager();
        base.Initialize();
    }

    private int _windowedW = 1280;
    private int _windowedH = 720;

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        // Don't resync backbuffer while fullscreen — would clobber the restore size.
        if (_graphics.IsFullScreen) return;

        int w = Math.Max(320, Window.ClientBounds.Width);
        int h = Math.Max(180, Window.ClientBounds.Height);
        if (_graphics.PreferredBackBufferWidth == w &&
            _graphics.PreferredBackBufferHeight == h) return;

        _graphics.PreferredBackBufferWidth = w;
        _graphics.PreferredBackBufferHeight = h;
        _graphics.ApplyChanges();
        GraphicsDevice.Viewport = new Viewport(0, 0, w, h);
        _windowedW = w;
        _windowedH = h;
        _services?.Camera.FitZoomToViewport(3.0f);
        _services?.Camera.Reclamp();
    }

    private void ToggleFullscreen()
    {
        if (!_graphics.IsFullScreen)
        {
            _windowedW = _graphics.PreferredBackBufferWidth;
            _windowedH = _graphics.PreferredBackBufferHeight;
            _graphics.HardwareModeSwitch = false;
            _graphics.PreferredBackBufferWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
        }
        else
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = _windowedW;
            _graphics.PreferredBackBufferHeight = _windowedH;
        }
        _graphics.ApplyChanges();

        GraphicsDevice.Viewport = new Viewport(
            0, 0,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);
        _services?.Camera.FitZoomToViewport(3.0f);
        _services?.Camera.Reclamp();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create shared services
        var time = new TimeManager { DayDurationSeconds = 120f };
        var camera = new Camera(GraphicsDevice);

        _sceneManager = new SceneManager();
        _sceneManager.Initialize(GraphicsDevice);

        _services = new ServiceContainer
        {
            GraphicsDevice = GraphicsDevice,
            SpriteBatch = _spriteBatch,
            Input = _input,
            Time = time,
            Camera = camera,
            Content = Content,
        };
        _services.SceneManager = _sceneManager;
        _services.ToggleFullscreen = ToggleFullscreen;
        _services.QuitGame = Exit;

        // Pre-read save so boot routing knows which scene to land on.
        // Always push FarmScene first (bootstrap): it constructs Player, Inventory, Atlas,
        // Hotbar, Progression, Quest. If the saved scene is non-Farm (Village/Castle/Shop),
        // FarmScene will TransitionTo it on the next frame after its OnLoad completes.
        // Dungeon:* saved scenes are intentionally normalized to Farm during v9->v10 migration.
        var bootSave = SaveManager.Load();
        if (bootSave != null)
        {
            string target = bootSave.CurrentScene ?? "Farm";
            if (string.IsNullOrEmpty(target) || target.StartsWith("Dungeon:"))
                target = "Farm";

            _services.PendingRestoreScene = target;
            _services.PendingRestoreSceneName = target;
            if (bootSave.PositionByScene != null
                && bootSave.PositionByScene.TryGetValue(target, out var scenePos))
            {
                _services.PendingRestorePosition = new Vector2(scenePos.X, scenePos.Y);
                Console.WriteLine($"[Game1] Boot restore queued: scene={target} pos=({scenePos.X},{scenePos.Y})");
            }
            else
            {
                Console.WriteLine($"[Game1] Boot restore queued: scene={target} (no position entry — will use spawn default)");
            }
        }

        // Push initial scene
        _sceneManager.PushImmediate(new FarmScene(_services));
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Pass IsActive so the InputManager ignores OS-wide clicks/keystrokes
        // while the window is unfocused or minimized. Without this, Mouse.GetState()
        // returns global desktop state and clicks in other apps would fire ghost
        // casts in the fishing system, hotbar selection, etc.
        _input.Update(IsActive);

        if (_input.IsKeyPressed(Keys.F11))
            ToggleFullscreen();

#if DEBUG
        // F9: force-advance main quest state (D-12 dev hook).
        if (_input.IsKeyPressed(Keys.F9) && _services?.Quest != null)
        {
            if (_services.Quest.State == MainQuestState.NotStarted)
                _services.Quest.Activate();
            else if (_services.Quest.State == MainQuestState.Active)
                _services.Quest.Complete();
            Console.WriteLine($"[DEBUG] F9 pressed, quest state -> {_services.Quest.State}");
        }
#endif

        _sceneManager.Update(dt);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Dark grey (not pure black) so a future "all-transparent tiles" map
        // regression is visible rather than pitch black. See .planning/debug/
        // dungeon-dark-and-damage.md for context.
        GraphicsDevice.Clear(new Color(24, 24, 28));
        _sceneManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }
}
