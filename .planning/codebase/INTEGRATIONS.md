# External Integrations

**Analysis Date:** 2026-04-10

## APIs & External Services

**Not applicable** - This is a single-player offline game with no external API integrations.

## Data Storage

**Databases:**
- Not used - Game uses local JSON file storage only

**File Storage:**
- Local filesystem only
  - Save location: `%LOCALAPPDATA%\StardewMedieval\savegame.json`
  - Managed by `Core/SaveManager.cs`
  - Single JSON file per game instance containing all persistent state

**Asset Storage:**
- Local embedded resources
  - Sprites directory: `Content/Sprites/**/*.png`
  - Maps directory: `Content/Maps/**/*.tmx` (Tiled map files)
  - Fonts directory: `Content/DefaultFont.spritefont`
  - All assets copied to output via `PreserveNewest` in project file

**Caching:**
- In-memory only (not persistent)
  - Crop registry cached in `Data/CropRegistry.cs` as static dictionary
  - Tileset textures cached in `TileMap._tilesetTextures` dictionary
  - Player spritesheets loaded once at startup

## Authentication & Identity

**Auth Provider:**
- Not applicable - Single-player offline game with no user accounts or authentication

## Monitoring & Observability

**Error Tracking:**
- Console logging only (development-grade)
  - No external error reporting service
  - Errors logged to standard output via `Console.WriteLine()`

**Logs:**
- Console output only
  - SaveManager logs save/load operations: `[SaveManager] Saved day X`
  - TileMap logs map loading: `[TileMap] Loaded {Width}x{Height} map...`
  - CropRegistry logs texture load failures: `[CropRegistry] Failed to load {path}: {ex.Message}`
  - Game logs day advancement: `[Game] === Day {dayNumber} ===`
  - No persistent log files

## CI/CD & Deployment

**Hosting:**
- Not applicable - Desktop application (standalone executable)
- Distribution: Local file system deployment
- Execution: Direct Windows executable (WinExe)

**CI Pipeline:**
- Not detected - No CI configuration files found
- Manual build via Visual Studio or `dotnet build`

## Environment Configuration

**Required env vars:**
- None detected - Application has no external environment dependencies

**Hardcoded paths:**
- Game content root: `"Content"` - Relative to application directory
- Tiled map path: `"Content/Maps/test_farm.tmx"` - Hardcoded in `Game1.cs` LoadContent()
- Save directory: Uses Windows standard `%LOCALAPPDATA%` special folder

**Secrets location:**
- Not applicable - No API keys, tokens, or credentials required

## Webhooks & Callbacks

**Incoming:**
- Not applicable - Offline game with no network connectivity

**Outgoing:**
- Not applicable - No external service callbacks

## Platform-Specific Integrations

**Windows Integration:**
- DPI awareness via app.manifest (per-monitor DPI awareness v2)
- Application manifest specifies Windows 7-10 compatibility
- IsInvoker execution level (no admin privileges required)

**Graphics Integration:**
- MonoGame DesktopGL profile - OpenGL-based graphics rendering
- Direct GPU texture loading from PNG files
- SpriteBatch for batched 2D rendering

## File Format Support

**Tiled Map Format (TMX/TSX):**
- External integration point: `TiledCS 3.3.3` library
- Reads `.tmx` files (tile maps with layers, objects, properties)
- Reads `.tsx` files (tileset definitions)
- Supports multiple tile layers, object layers for collision detection
- Parses collision polygons from Tiled objects

**MonoGame Content Format:**
- `.spritefont` XML for font definitions
- `.mgcb` content project for asset compilation
- Compiled to platform-specific binary format during build

---

*Integration audit: 2026-04-10*
