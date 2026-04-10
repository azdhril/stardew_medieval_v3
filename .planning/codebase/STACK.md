# Technology Stack

**Analysis Date:** 2026-04-10

## Languages

**Primary:**
- C# 12 - Entire application codebase, all game systems and logic
- MGCB (MonoGame Content Builder) - Content pipeline for sprites, fonts, and assets

**Secondary:**
- XML - Tiled map format (.tmx, .tsx files)
- JSON - Game state serialization and save files

## Runtime

**Environment:**
- .NET 8.0 - Target framework with forward compatibility enabled via RollForward=Major
- Windows (DesktopGL) - DesktopGL profile for MonoGame

**Package Manager:**
- NuGet - .NET package management
- Lockfile: Implicit (managed by .NET project file)

## Frameworks

**Core:**
- MonoGame.Framework.DesktopGL 3.8.* - Game engine for 2D graphics, input handling, and game loop
  - Provides `Game` base class (entry point in `Game1.cs`)
  - Provides `SpriteBatch` for rendering
  - Provides `GraphicsDevice` for texture management
  - Provides `Keyboard`, `Mouse` input APIs

**Build/Dev:**
- MonoGame.Content.Builder.Task 3.8.* - Content pipeline build integration
- dotnet-mgcb 3.8.4.1 - Command-line tool for building game content (mgcb command)

## Key Dependencies

**Critical:**
- TiledCS 3.3.3 - Tiled map format parsing and loading
  - Used in `World/TileMap.cs` to load `.tmx` files
  - Provides `TiledMap`, `TiledTileset`, `TiledLayer` classes
  - Handles tileset parsing from `.tsx` files
  - Loads collision objects from Tiled object layers

## Configuration

**Environment:**
- Manifest: `app.manifest` - Windows application manifest for DPI awareness and OS compatibility
  - Supports Windows Vista through Windows 10
  - Configured for per-monitor DPI awareness v2

**Build:**
- `stardew_medieval_v3.csproj` - MSBuild project file
  - OutputType: WinExe (Windows executable)
  - TargetFramework: net8.0
  - RollForward: Major (allows running on newer .NET versions)
  - PublishReadyToRun: false
  - TieredCompilation: false
  - Nullable: enable (strict null safety)
  - Embeds Icon.ico and Icon.bmp as resources
  - Content files marked for `PreserveNewest` copy to output

**Content Pipeline:**
- `Content/Content.mgcb` - MonoGame content project file
  - OutputDir: `bin/$(Platform)` - Platform-specific output
  - Platform: DesktopGL
  - Profile: Reach (compatibility profile)
  - Processes sprite fonts and assets

## Platform Requirements

**Development:**
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- MonoGame 3.8 templates and tools

**Production:**
- .NET 8.0 Runtime
- Windows 7+ (with DPI awareness support)
- GPU capable of OpenGL support (via DesktopGL)
- 960x540 minimum window resolution

**Asset Requirements:**
- MonoGame content must be pre-built via MGCB
- Tiled map files (.tmx) with referenced tilesets (.tsx)
- Sprite PNG files for crop growth stages and player animation
- SpriteFont files (.spritefont XML) for text rendering

---

*Stack analysis: 2026-04-10*
