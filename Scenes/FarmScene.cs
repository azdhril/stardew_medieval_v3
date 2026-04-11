using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.UI;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Main farm gameplay scene. Extracted from Game1.cs.
/// Contains: TileMap, Player, Farming systems, HUD, day/night cycle.
/// </summary>
public class FarmScene : Scene
{
    private TileMap _map = null!;
    private PlayerEntity _player = null!;
    private GridManager _gridManager = null!;
    private CropManager _cropManager = null!;
    private ToolController _toolController = null!;
    private HUD _hud = null!;
    private Texture2D _pixel = null!;
    private InventoryManager _inventory = null!;
    private SpriteAtlas _spriteAtlas = null!;
    private HotbarRenderer _hotbar = null!;

    public FarmScene(ServiceContainer services) : base(services) { }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        var content = Services.Content;

        // Pixel texture for overlays
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Map
        _map = new TileMap();
        _map.Load("Content/Maps/test_farm.tmx", device);

        // Player
        _player = new PlayerEntity
        {
            Position = TileMap.TileCenterWorld(10, 10)
        };
        var playerTex = LoadTexture(device, "Content/Sprites/Player/player_spritesheet.png");
        _player.LoadContent(playerTex);

        // Crop registry (loads textures)
        CropRegistry.Initialize(device);

        // Farming systems
        _gridManager = new GridManager(_map);
        _gridManager.LoadContent(device);
        _cropManager = new CropManager(_gridManager, CropRegistry.GetAllCrops());
        _toolController = new ToolController(_gridManager, _cropManager, _player);

        // Camera
        Services.Camera.Zoom = 3f;
        Services.Camera.Bounds = _map.GetWorldBounds();

        // HUD
        var font = content.Load<SpriteFont>("DefaultFont");
        _hud = new HUD(Services.Time, _player.Stats, _toolController);
        _hud.LoadContent(device, font);

        // Subscribe to day advance
        Services.Time.OnDayAdvanced += OnDayAdvanced;

        // Item registry
        ItemRegistry.Initialize();

        // Inventory and hotbar
        _inventory = new InventoryManager();
        Services.Inventory = _inventory;

        var itemSheet = LoadTexture(device, "Content/Sprites/Items/7_Pickup_Items_16x16.png");
        _spriteAtlas = SpriteAtlas.CreateDefault(itemSheet);

        _hotbar = new HotbarRenderer(_inventory, _spriteAtlas);
        _hotbar.LoadContent(device, font);

        // Test items for development (multiple rarities for visual testing)
        _inventory.TryAdd("Cabbage", 5);
        _inventory.TryAdd("Iron_Sword");
        _inventory.TryAdd("Cosmic_Carrot", 3);
        _inventory.TryAdd("Flame_Blade");
        _inventory.TryAdd("Leather_Armor");
        _inventory.TryAdd("Health_Potion", 10);

        // Load save data if available
        var save = SaveManager.Load();
        if (save != null)
        {
            Services.Time.SetDay(save.DayNumber);
            Services.Time.SetGameTime(save.GameTime);
            _player.Position = new Vector2(save.PlayerX, save.PlayerY);
            _player.Stats.SetStamina(save.StaminaCurrent);
            _gridManager.LoadFromSaveData(save.FarmCells, CropRegistry.All);
            _inventory.LoadFromState(save);
        }

        Console.WriteLine("[FarmScene] Loaded");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;

        // Sleep (advance day) with P
        if (input.IsKeyPressed(Keys.P))
        {
            Console.WriteLine("[FarmScene] Player sleeping...");
            Services.Time.ForceSleep();
        }

        // Test scene transition with T key (push so FarmScene stays alive on stack)
        if (input.IsKeyPressed(Keys.T))
        {
            Services.SceneManager.Push(new TestScene(Services));
            return;
        }

        // Hotbar number key selection (1-8)
        for (int i = 0; i < 8; i++)
        {
            if (input.IsKeyPressed(Keys.D1 + i))
                _inventory.SetActiveHotbar(i);
        }

        // Open inventory overlay (instant, no fade)
        if (input.IsKeyPressed(Keys.I))
        {
            Services.SceneManager.PushImmediate(new InventoryScene(Services, _inventory, _spriteAtlas));
            return;
        }

        // Time progression
        Services.Time.Update(deltaTime);

        // Tool switching & actions
        _toolController.Update(input);

        // Player movement
        _player.Update(deltaTime, input.Movement, _map);

        // Camera follow
        Services.Camera.Follow(_player.Position, deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var device = Services.GraphicsDevice;
        var viewport = device.Viewport;
        var transform = Services.Camera.GetTransformMatrix();

        // === World space (camera-transformed) ===
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null,
            transform
        );

        var topLeft = Services.Camera.ScreenToWorld(Vector2.Zero);
        var bottomRight = Services.Camera.ScreenToWorld(new Vector2(viewport.Width, viewport.Height));
        var viewArea = new Rectangle(
            (int)topLeft.X - 16, (int)topLeft.Y - 16,
            (int)(bottomRight.X - topLeft.X) + 32,
            (int)(bottomRight.Y - topLeft.Y) + 32
        );

        _map.Draw(spriteBatch, viewArea);
        _gridManager.DrawOverlays(spriteBatch, viewArea);
        _gridManager.DrawCrops(spriteBatch, viewArea);
        _player.Draw(spriteBatch);
        DrawFarmZoneHint(spriteBatch, viewArea);

        spriteBatch.End();

        // === Day/night overlay ===
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        float darkness = 1f - MathHelper.Clamp(Services.Time.GetLightIntensity(), 0f, 1f);
        if (darkness > 0.05f)
        {
            spriteBatch.Draw(_pixel,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * (darkness * 0.6f));
        }
        spriteBatch.End();

        // === Screen space (HUD) ===
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _hud.Draw(spriteBatch, viewport.Width, viewport.Height);
        _hotbar.Draw(spriteBatch, viewport.Width, viewport.Height);
        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        Services.Time.OnDayAdvanced -= OnDayAdvanced;
        _pixel?.Dispose();
        Console.WriteLine("[FarmScene] Unloaded");
    }

    /// <summary>
    /// Draw subtle green tint on tillable farm zone tiles.
    /// </summary>
    private void DrawFarmZoneHint(SpriteBatch sb, Rectangle viewArea)
    {
        int startX = Math.Max(0, viewArea.Left / TileMap.TileSize);
        int startY = Math.Max(0, viewArea.Top / TileMap.TileSize);
        int endX = Math.Min(_map.Width - 1, viewArea.Right / TileMap.TileSize);
        int endY = Math.Min(_map.Height - 1, viewArea.Bottom / TileMap.TileSize);

        for (int x = startX; x <= endX; x++)
        for (int y = startY; y <= endY; y++)
        {
            if (_map.IsFarmZone(x, y) && _gridManager.GetCell(new Point(x, y)) == null)
            {
                sb.Draw(_pixel,
                    new Rectangle(x * TileMap.TileSize, y * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize),
                    Color.Green * 0.08f);
            }
        }
    }

    /// <summary>
    /// Handle day advance: tick crops, reset watering, restore stamina, auto-save.
    /// </summary>
    private void OnDayAdvanced()
    {
        Console.WriteLine($"[FarmScene] === Day {Services.Time.DayNumber} ===");

        // Order matters: crops tick BEFORE watering resets
        _cropManager.OnDayAdvanced();
        _gridManager.OnDayAdvanced();
        _player.Stats.RestoreStamina();

        // Auto-save
        var state = new GameState
        {
            DayNumber = Services.Time.DayNumber,
            Season = Services.Time.Season,
            StaminaCurrent = _player.Stats.CurrentStamina,
            PlayerX = _player.Position.X,
            PlayerY = _player.Position.Y,
            GameTime = Services.Time.GameTime,
            FarmCells = _gridManager.GetSaveData(),
            CurrentScene = "Farm"
        };
        _inventory.SaveToState(state);
        SaveManager.Save(state);
    }

    /// <summary>
    /// Load a texture from a file path using stream.
    /// </summary>
    private Texture2D LoadTexture(GraphicsDevice device, string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }
}
