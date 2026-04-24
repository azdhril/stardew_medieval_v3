using System;
using System.IO;
using FontStashSharp;

namespace stardew_medieval_v3.Core;

/// <summary>Typeface role used to select which FontSystem to pull a glyph size from.</summary>
public enum FontRole { Body, Bold, Small }

/// <summary>
/// Runtime TTF font cache. Owns one FontSystem per typeface family; each family
/// lazily rasterizes any requested size and shares a single atlas across sizes.
/// Replaces the SpriteFont/MGCB-baked font pipeline (see quick task 260423-tu6).
/// </summary>
public sealed class FontService : IDisposable
{
    private readonly FontSystem _body;
    private readonly FontSystem _bold;

    /// <summary>
    /// Loads NotoSerif-Regular.ttf and NotoSerif-Bold.ttf from the given directory.
    /// Logs the resolved absolute path once on construction for diagnostic sanity
    /// (Pitfall 2 guard: TTF path resolution at runtime).
    /// </summary>
    public FontService(string fontDir)
    {
        var regularPath = Path.Combine(fontDir, "NotoSerif-Regular.ttf");
        var boldPath = Path.Combine(fontDir, "NotoSerif-Bold.ttf");
        Console.WriteLine($"[FontService] Loading fonts from: {Path.GetFullPath(fontDir)}");

        _body = new FontSystem();
        _body.AddFont(File.ReadAllBytes(regularPath));

        _bold = new FontSystem();
        _bold.AddFont(File.ReadAllBytes(boldPath));
    }

    /// <summary>Returns a SpriteFontBase cached internally by the underlying FontSystem.</summary>
    /// <param name="role">Typeface family. FontRole.Small aliases to Body (no separate "small" TTF).</param>
    /// <param name="size">Glyph size in points.</param>
    public SpriteFontBase GetFont(FontRole role, int size) => role switch
    {
        FontRole.Bold => _bold.GetFont(size),
        FontRole.Small => _body.GetFont(size),
        _ => _body.GetFont(size),
    };

    public void Dispose()
    {
        _body.Dispose();
        _bold.Dispose();
    }
}
