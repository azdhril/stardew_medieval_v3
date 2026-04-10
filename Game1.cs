using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
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
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 960;
        _graphics.PreferredBackBufferHeight = 540;
    }

    protected override void Initialize()
    {
        Window.Title = "Stardew Medieval v3 (MonoGame)";
        _input = new InputManager();
        base.Initialize();
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

        // Push initial scene
        _sceneManager.PushImmediate(new FarmScene(_services));
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _input.Update();

        if (_input.IsKeyPressed(Keys.Escape))
            Exit();

        _sceneManager.Update(dt);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }
}
