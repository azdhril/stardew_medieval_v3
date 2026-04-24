# Quick Task 260423-tu6: Migrar SpriteFont → FontStashSharp - Research

**Researched:** 2026-04-23
**Domain:** MonoGame 3.8 DesktopGL text rendering
**Confidence:** HIGH

## Summary

FontStashSharp is the de facto runtime TTF rasterizer for MonoGame. It solves the exact issue driving this migration: `SpriteFont` bakes a bitmap at one size, so any non-integer scale (e.g. `DrawCenteredTitle` at 1.35x) bilinear-filters an already-compressed glyph atlas and looks blurry/smudged. FontStashSharp instead rasterizes each requested size on-demand from the TTF — `_fontSystem.GetFont(16)` and `_fontSystem.GetFont(18)` are both crisp native bitmaps.

The library integrates via an extension method `spriteBatch.DrawString(SpriteFontBase, ...)` so the call-site diff is minimal: replace `SpriteFont` type with `SpriteFontBase`, replace `Content.Load<SpriteFont>(...)` with `FontSystem.GetFont(size)`, and fonts load from disk via `File.ReadAllBytes` (no MGCB round-trip). `MeasureString` keeps the same `Vector2` return, same signature shape — existing pixel-perfect positioning math (`+ (RowHeight - sz.Y) / 2`) continues to work.

**Primary recommendation:** Install `FontStashSharp.MonoGame 1.5.5`, load `NotoSerif-Regular.ttf` + `NotoSerif-Bold.ttf` directly from `assets/Sprites/System/Font/` via `File.ReadAllBytes`, expose a `FontService` (or extend `ServiceContainer`) returning `SpriteFontBase` keyed by `(role, size)` so call sites get `service.GetFont(FontRole.Body, 12)` instead of wiring a specific `DynamicSpriteFont` per scene. Delete the 4 `.spritefont` assets and their `Content.mgcb` entries in the same commit.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FontStashSharp.MonoGame | 1.5.5 | Runtime TTF rasterizer for MonoGame | Only actively-maintained TTF runtime loader for MonoGame 3.8; integrates via `SpriteBatch.DrawString` extension method so migration is low-friction |

**Installation:**
```xml
<PackageReference Include="FontStashSharp.MonoGame" Version="1.5.5" />
```

**Version verification:** [VERIFIED: nuget.org] — `FontStashSharp.MonoGame 1.5.5` published 2026-04-14, targets `netstandard2.0` (compatible with `net8.0`), depends on `FontStashSharp.Base 1.2.3`, `FontStashSharp.Rasterizers.StbTrueTypeSharp 1.2.3`, `Cyotek.Drawing.BitmapFont 2.0.4`, `StbImageSharp 2.30.15`. [License: Zlib — permissive, compatible with commercial distribution.]

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| FontStashSharp | SpriteFontPlus (by same author rds1983) | Older library; author moved to FontStashSharp. Not recommended for new code. |
| FontStashSharp | Sign-Distance-Field (SDF) fonts | Heavier setup (shader required), overkill for 12pt UI at 960×540. |
| FontStashSharp | Keep `SpriteFont`, bake each size separately | Multiplies content pipeline work; every future "make text bigger" change means re-baking. Defeats the purpose of the migration. |

## Architecture Patterns

### Canonical migration pattern

**Before (current code):**
```csharp
// Scene / UI component
private SpriteFont _font = null!;

public void LoadContent()
{
    _font = Services.Content.Load<SpriteFont>("NotoSerif");
}

public void Draw(SpriteBatch sb)
{
    var size = _font.MeasureString("Hello");   // Vector2
    sb.DrawString(_font, "Hello", pos, Color.White);
}
```

**After (FontStashSharp):**
```csharp
using FontStashSharp;   // SpriteFontBase, FontSystem, extension method

private SpriteFontBase _font = null!;

public void LoadContent()
{
    _font = Services.Fonts.GetFont(FontRole.Body, 12);   // see FontService below
}

public void Draw(SpriteBatch sb)
{
    var size = _font.MeasureString("Hello");   // still Vector2 — identical signature
    sb.DrawString(_font, "Hello", pos, Color.White);   // extension method
}
```

### Recommended FontService wrapper

Centralises font loading and caching so 18 call-site files don't each know about `FontSystem`. Owned by `ServiceContainer` like the existing `Atlas` slot.

```csharp
// src/Core/FontService.cs
using FontStashSharp;

public enum FontRole { Body, Bold, Small }   // Small = old NotoSerifSmall 10pt

public sealed class FontService : IDisposable
{
    private readonly FontSystem _body;
    private readonly FontSystem _bold;

    public FontService(string fontDir)
    {
        _body = new FontSystem();
        _body.AddFont(File.ReadAllBytes(Path.Combine(fontDir, "NotoSerif-Regular.ttf")));

        _bold = new FontSystem();
        _bold.AddFont(File.ReadAllBytes(Path.Combine(fontDir, "NotoSerif-Bold.ttf")));
    }

    /// <summary>Returns a SpriteFontBase cached internally by FontSystem per size.</summary>
    public SpriteFontBase GetFont(FontRole role, int size) => role switch
    {
        FontRole.Bold  => _bold.GetFont(size),
        FontRole.Small => _body.GetFont(size),
        _              => _body.GetFont(size),
    };

    public void Dispose()
    {
        _body.Dispose();
        _bold.Dispose();
    }
}
```

Wire once in `Game1.LoadContent` (or `GameplayScene.LoadContent` — composition root) and store on `ServiceContainer.Fonts`.

### Size mapping from current SpriteFont names

| Old .spritefont | Font file | Size | New call |
|---|---|---|---|
| `NotoSerif` | NotoSerif-Regular.ttf | 12 | `Fonts.GetFont(FontRole.Body, 12)` |
| `NotoSerifSmall` | NotoSerif-Regular.ttf | 10 | `Fonts.GetFont(FontRole.Body, 10)` |
| `NotoSerifBold` | NotoSerif-Bold.ttf | 12 | `Fonts.GetFont(FontRole.Bold, 12)` |
| `DefaultFont` (Arial 12) | — | — | Remove or redirect to `FontRole.Body`,12 — only 1 fallback call site in `GameplayScene.cs:140` |

### DrawCenteredTitle scale fix — the whole reason we're doing this

Current code at `ShopPanel.cs:548` uses `scale=1.35f` with a 12pt baked SpriteFont — blurry. With FontStashSharp:

```csharp
// Before: scale parameter on bitmap glyphs → bilinear smudge
sb.DrawString(font, c, pos, color, 0f, Vector2.Zero, 1.35f, SpriteEffects.None, 0f);

// After: request the font at the target native size → crisp rasterisation
var titleFont = Services.Fonts.GetFont(FontRole.Bold, 16);   // 12 * 1.35 ≈ 16
sb.DrawString(titleFont, c, pos, color);
```

The `DrawCenteredTitle` helper can drop the `scale` parameter entirely — callers pass the already-sized font.

### Anti-Patterns to Avoid
- **Loading the same TTF twice:** Create one `FontSystem` per typeface family (Regular, Bold). All sizes share a single `FontSystem` — `GetFont(12)`, `GetFont(16)`, `GetFont(24)` reuse the same atlas.
- **Creating FontSystem inside hot paths:** Never `new FontSystem()` inside `Draw` or `LoadContent` of a short-lived overlay scene. Own it at `ServiceContainer` level. [CITED: github.com/FontStashSharp/FontStashSharp wiki]
- **Scaling `DrawString`:** Defeats the purpose. Ask for the right size instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---|---|---|---|
| TTF rasterisation at runtime | Custom StbTrueType wrapper | FontStashSharp (which wraps StbTrueTypeSharp internally) | Edge cases: kerning, hinting, ligatures, Unicode fallback, atlas packing |
| Multi-size font cache | `Dictionary<int,Texture2D>` baked fonts | `FontSystem.GetFont(size)` | FontSystem already does this with a single shared atlas |
| UTF-8 / accent coverage | Custom glyph fallback chain | `FontSystem.AddFont()` multiple times (primary + fallback) | Wiki pattern supports Japanese/Emoji fallback out of the box [CITED: fontstashsharp wiki] |

## Runtime State Inventory

This is a code migration — no stored data / live service config / OS-registered state touches any of the affected systems.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — savegame.json contains no font references | None |
| Live service config | None | None |
| OS-registered state | None | None |
| Secrets/env vars | None | None |
| Build artifacts | `assets/bin/DesktopGL/*.xnb` compiled from the 4 `.spritefont` files | Delete after migration (auto-regenerated on next `dotnet build`; stale .xnb files will be ignored since MGCB no longer references them) |

**Nothing else to migrate** — this is a pure code+content-pipeline refactor. No runtime data.

## Common Pitfalls

### Pitfall 1: Forgetting the `using FontStashSharp;` directive
**What goes wrong:** `spriteBatch.DrawString(font, ...)` compiles against the MonoGame `SpriteFont` overload and fails because `SpriteFontBase` isn't `SpriteFont`. Compiler error like `cannot convert SpriteFontBase to SpriteFont`.
**Why it happens:** `DrawString(SpriteFontBase, ...)` is an extension method defined in the FontStashSharp namespace.
**How to avoid:** Add `using FontStashSharp;` to every file that calls `DrawString` or `MeasureString` on an FSS font. Consider `global using FontStashSharp;` in a single file (C# 10+) so every UI file picks it up.

### Pitfall 2: TTF file path at runtime
**What goes wrong:** `File.ReadAllBytes("NotoSerif-Regular.ttf")` throws `FileNotFoundException` because the working directory at runtime is `assets/bin/DesktopGL` (where `stardew_medieval_v3.exe` lives), not the project root.
**Why it happens:** TTFs currently live at `assets/Sprites/System/Font/`. The `.csproj` already copies `assets\Sprites\**\*` to output via `CopyToOutputDirectory="PreserveNewest"` (line 33), so at runtime they end up at `{output}/assets/Sprites/System/Font/NotoSerif-Bold.ttf`. Verify path resolution in `FontService` constructor — either use `Path.Combine(AppContext.BaseDirectory, "assets", "Sprites", "System", "Font", ...)` or rely on relative path matching current working directory.
**How to avoid:** Use `AppContext.BaseDirectory` as anchor; add a smoke test that logs the resolved absolute path in the `FontService` constructor on first run.

### Pitfall 3: SamplerState mismatch (crisp vs smooth text)
**What goes wrong:** FontStashSharp glyphs are rasterized with antialiasing by default. If you render inside a `spriteBatch.Begin(..., SamplerState.PointClamp)` block (which most UI scenes use for pixel-art look), the glyphs still look correct — PointClamp just means nearest-neighbour when the texture is sampled, but the texture itself is already anti-aliased pixel-for-pixel at the requested size. **Result: crisp native glyphs, no bilinear smudge from scaling.**
**Why it happens:** This is actually the desired behavior. Unlike SpriteFont+scale (which produces a bilinear-smudged sample), FSS at native size produces 1:1 pixels that PointClamp reproduces exactly.
**How to avoid:** Keep existing `SamplerState.PointClamp` in all UI scenes. If you want *perfectly hard-edged* bitmap-font look (no antialiasing at all), swap to `FontSystemSettings.GlyphRenderer` with the no-antialias variant [CITED: fontstashsharp.github.io/docs/glyph-rendering.html] — but default antialiased is the recommended starting point and matches typical medieval UI polish.

### Pitfall 4: Line height / baseline differs from SpriteFont
**What goes wrong:** Existing code uses `(rowHeight - MeasureString("A").Y) / 2` to vertically centre. `MeasureString` on FSS reflects the actual glyph ascender, so descender-less strings ("A", "1") return a shorter Y than on SpriteFont, potentially shifting text 1-2px.
**Why it happens:** SpriteFont's `MeasureString` returns a fixed line-height per font; FSS measures per-string. Use `font.LineHeight` property (int) when you want a consistent baseline. [CITED: community.monogame.net/t/fontstashsharp/17728/7]
**How to avoid:** Replace `MeasureString("A").Y` patterns with `font.LineHeight` for vertical layout anchors. Only use `MeasureString(actualText).Y` when you genuinely need the per-string height (e.g. multi-line wrapping).

### Pitfall 5: Portuguese accent coverage
**What goes wrong:** Existing `.spritefont` files declare `<CharacterRegion>` as `32..126` (ASCII only) — any accented char like `á`, `ç`, `ã` currently renders as a blank box. Existing UI uses "Sleep", "Inventory" (English/ASCII) so it works by accident.
**Why it happens:** Noto Serif TTFs include full Latin-1 + extended Latin; FSS rasterizes any glyph present in the TTF on demand. **Migration will silently fix this** — accented Portuguese characters will start rendering for free.
**How to avoid:** No action required; flag as a positive side-effect to the planner. Verify with test string `"Estação: árvore coração"` during smoke test.

### Pitfall 6: First-draw glyph atlas stutter
**What goes wrong:** FSS rasterizes glyphs on first use. First frame showing a new character triggers `stbtt_MakeGlyphBitmap` + texture upload. For typical UI with a small glyph set (~100 chars), this is <1ms total and imperceptible. For a long debug text dumping random Unicode, could cause a hitch.
**Why it happens:** Lazy atlas generation.
**How to avoid:** Pre-warm in `LoadContent` by calling `font.MeasureString(" !\"#$%&'()...")` with every ASCII+accent glyph you expect to use. Optional; probably unnecessary for 18 UI files.

## Code Examples

### Loading at startup (GameplayScene.LoadContent)
```csharp
// Source: FontStashSharp wiki — https://github.com/FontStashSharp/FontStashSharp/wiki/Using-FontStashSharp-in-MonoGame-or-FNA
protected override void LoadContent()
{
    var fontDir = Path.Combine(AppContext.BaseDirectory, "assets", "Sprites", "System", "Font");
    Services.Fonts = new FontService(fontDir);
    // ...
}
```

### Drawing with an extension method (unchanged call shape)
```csharp
// Source: FontStashSharp wiki
using FontStashSharp;

var font = Services.Fonts.GetFont(FontRole.Body, 12);
var sz = font.MeasureString(text);   // Vector2 — same type as MonoGame
spriteBatch.DrawString(font, text, new Vector2(x, y), Color.White);
```

### MeasureString signature (for reference)
```csharp
// Source: github.com/FontStashSharp/FontStashSharp/blob/main/src/FontStashSharp/SpriteFontBase.cs
public Vector2 MeasureString(string text,
    Vector2? scale = null,
    float characterSpacing = 0.0f,
    float lineSpacing = 0.0f,
    FontSystemEffect effect = FontSystemEffect.None,
    int effectAmount = 0);
```

The first two optional parameters mean existing call `font.MeasureString(text)` is a drop-in replacement — positional and named. No call-site edits required for `MeasureString`.

### Lazy atlas note
> "Glyphs are rendered on-demand on the texture atlas" — [CITED: fontstashsharp.github.io]. A single `FontSystem` keeps one texture atlas that grows; all sizes from `GetFont(N)` share it.

## State of the Art

| Old Approach | Current Approach | Why |
|---|---|---|
| `SpriteFont` baked at fixed size via MGCB | `FontStashSharp.FontSystem + GetFont(size)` at runtime | Any size renders crisp without re-baking; atlas shared across sizes |
| SpriteFontPlus (older runtime baker) | FontStashSharp | Same author (rds1983), superseded |
| Custom character-region declarations in `.spritefont` XML | FSS rasterizes any glyph in the TTF on demand | Accents (Portuguese) and extended glyphs work automatically |

**Deprecated:** SpriteFontPlus package — use FontStashSharp instead. [VERIFIED: github.com/rds1983]

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|---|---|---|---|---|
| NotoSerif-Regular.ttf | FontSystem body | ✓ | present at `assets/Sprites/System/Font/` | — |
| NotoSerif-Bold.ttf | FontSystem bold | ✓ | present at `assets/Sprites/System/Font/` | — |
| NotoSerif-Medium.ttf | (unused by migration) | ✓ | present | Ignore, no SpriteFont references it |
| NuGet feed access | Package install | ✓ | assumed (dotnet restore works) | — |

No blockers. All TTFs already on disk, already copied to output by existing csproj glob.

## Call Sites Inventory (for planner)

**18 files** use SpriteFont (verified via grep):

| File | What it needs |
|---|---|
| `src/Core/GameplayScene.cs` | 2× `Content.Load<SpriteFont>` → `FontService.GetFont` |
| `src/UI/HUD.cs` | `SpriteFont` field + `LoadContent(font)` param + static `DrawQuestTracker(font)` + small-font fallback load |
| `src/UI/BossHealthBar.cs` | Static `Draw(font)` signature |
| `src/UI/ContainerGridRenderer.cs` | Field + `LoadContent(font)` param |
| `src/UI/DeathBanner.cs` | `Draw(font)` param |
| `src/UI/DialogueBox.cs` | `Draw(font)` param |
| `src/UI/HotbarRenderer.cs` | Field + `LoadContent(font)` param |
| `src/UI/InteractionPrompt.cs` | `Draw(font)` param |
| `src/UI/InventoryGridRenderer.cs` | Field + `LoadContent(font)` param |
| `src/UI/LevelUpBanner.cs` | `Draw(font)` param |
| `src/UI/ShopPanel.cs` | 2× `Draw(font, titleFont)` + private helpers + drop `scale` in `DrawCenteredTitle` |
| `src/UI/Toast.cs` | `Draw(font)` param |
| `src/Scenes/ChestScene.cs` | 3× `Load<SpriteFont>` + field + forwarded helper param |
| `src/Scenes/DialogueScene.cs` | 1× `Load<SpriteFont>` |
| `src/Scenes/InventoryScene.cs` | 1× `Load<SpriteFont>` |
| `src/Scenes/PauseScene.cs` | 1× `Load<SpriteFont>` |
| `src/Scenes/ShopOverlayScene.cs` | 2× `Load<SpriteFont>` + fields |
| `src/Scenes/TestScene.cs` | 1× `Load<SpriteFont>` + field |

**Strategy:** The signature change (`SpriteFont` → `SpriteFontBase`) is mechanical. Grep-replace `SpriteFont` → `SpriteFontBase` in the 18 files (only those 18; don't touch `spritefont` XML files), add `using FontStashSharp;` per file. Replace each `Content.Load<SpriteFontBase>(...)` with `Services.Fonts.GetFont(role, size)`. `.spritefont` file deletion + `Content.mgcb` edit is a separate atomic step at end.

## Content Pipeline Cleanup

After migration:
1. Delete `assets/DefaultFont.spritefont`, `assets/NotoSerif.spritefont`, `assets/NotoSerifSmall.spritefont`, `assets/NotoSerifBold.spritefont`.
2. Remove the 4 `#begin ...spritefont` blocks (lines 16-43) from `assets/Content.mgcb`.
3. Next `dotnet build` will stop producing the corresponding `.xnb` files; stale .xnb in `assets/bin/DesktopGL/` can be deleted via `git clean` or ignored (they're not referenced by any `Content.Load`).
4. TTF files at `assets/Sprites/System/Font/*.ttf` stay where they are — already copied to output via existing csproj glob.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|---|---|---|
| A1 | `AppContext.BaseDirectory` resolves to `assets/bin/DesktopGL` at runtime so TTFs copied via `CopyToOutputDirectory` are reachable | Pitfall 2 | Low — verified pattern in existing codebase (saves use `LocalApplicationData`, not `BaseDirectory`, but other content-pipeline-adjacent files already resolve from output dir). Planner should add a `Console.WriteLine("[FontService] loading from {absPath}")` sanity log in first commit. |
| A2 | Size 16 is a good native-size target for the old `scale=1.35 × 12pt` title | DrawCenteredTitle fix | Low — cosmetic; dev can tune to 15 or 18 during visual verification. |
| A3 | No other code-generation or automated refactor tool in the repo references the `.spritefont` file names | Cleanup step | Low — grep for the strings during migration confirms. |

## Open Questions

1. **Should we keep `DefaultFont.spritefont` (Arial) as a fallback?**
   - What we know: Only 1 call site (`GameplayScene.cs:140`) uses it as a fallback when `NotoSerif` fails to load. With FSS loading directly from TTF, a missing TTF throws `FileNotFoundException` which the planner can catch — but there's no "Arial on Windows" equivalent without going through GDI+/DirectWrite.
   - Recommendation: Remove the fallback. If `NotoSerif-Regular.ttf` is missing, the game is broken anyway (all UI renders with no text); fail-fast is more diagnosable than a silent Arial fallback.

2. **Should we expose sizes beyond the current 10/12, or lock it to the existing set during migration?**
   - Recommendation: Migration should be behaviour-preserving. Use 10pt and 12pt and 16pt (16pt = new native replacement for the `scale=1.35` title). Adding more sizes is a follow-up polish task.

## Validation Architecture

Project uses `xunit` (~50 tests). No existing tests cover rendering — as expected for UI/graphics. Migration is visually-verified, not unit-tested.

### Smoke test plan (manual, via `dotnet run`)
| Req | Behavior | How to verify |
|---|---|---|
| Text renders crisp | All 18 UI surfaces display readable text | Run game, open each scene: farm HUD, inventory (I), chest, shop (via merchant), pause (Esc), dialogue (King), death banner, level-up banner |
| No regression in positioning | Pixel-art text stays aligned in HUD bars, hotbar slot labels, quest tracker | Compare side-by-side screenshots to pre-migration build |
| Shop title renders without bilinear smudge | "Buy/Sell" tab headers sharp at new native size | Open shop, confirm title glyphs have hard pixel edges |
| Accents render | Portuguese text with `á`, `ç` renders correctly | Add a test string to pause scene or temporary diagnostic |

### Build validation
- `dotnet build` must succeed after all 18 files updated.
- No `.spritefont` should appear in `assets/bin/DesktopGL/` after clean build.

## Security Domain

Not applicable — local game, no user input reaches the font loader, TTFs are bundled assets.

## Sources

### Primary (HIGH confidence)
- [VERIFIED: nuget.org] https://www.nuget.org/packages/FontStashSharp.MonoGame/ — version 1.5.5, net8.0 compatible, published 2026-04-14
- [CITED: github.com] https://github.com/FontStashSharp/FontStashSharp — library overview, Zlib license
- [CITED: github.com/FontStashSharp/FontStashSharp/wiki] https://github.com/FontStashSharp/FontStashSharp/wiki/Using-FontStashSharp-in-MonoGame-or-FNA — canonical MonoGame usage
- [CITED: github source] https://github.com/FontStashSharp/FontStashSharp/blob/main/src/FontStashSharp/SpriteFontBase.cs — MeasureString signature verified
- [CITED: github source] https://github.com/FontStashSharp/FontStashSharp/blob/main/src/FontStashSharp/DynamicSpriteFont.cs — class hierarchy
- [CITED: fontstashsharp.github.io] https://fontstashsharp.github.io/FontStashSharp/docs/glyph-rendering.html — glyph renderer customization (no-antialias mode)

### Secondary (MEDIUM confidence)
- [CITED: community.monogame.net] https://community.monogame.net/t/fontstashsharp/17728 — line-height / baseline guidance, community usage patterns

## Metadata

**Confidence breakdown:**
- NuGet package + version: HIGH — verified on nuget.org 2026-04-23, supports net8.0
- Migration pattern (DrawString extension, MeasureString signature): HIGH — verified in source
- Call-site count (18 files): HIGH — verified via grep
- Pitfalls (SamplerState, path resolution, line height): MEDIUM-HIGH — verified via docs + community thread
- Portuguese accent coverage: MEDIUM — reasoned from TTF Unicode support + FSS lazy rasterisation, not explicitly tested in this session

**Research date:** 2026-04-23
**Valid until:** 2026-05-23 (30 days — FontStashSharp is stable, MonoGame 3.8 is LTS)
