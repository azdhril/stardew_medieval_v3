using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Core;

/// <summary>
/// 2D camera with smooth follow and pixel-perfect rendering.
/// </summary>
public class Camera
{
    public Vector2 Position { get; private set; }
    public float Zoom { get; set; } = 3f;
    public Rectangle? Bounds { get; set; }
    public GraphicsDevice GraphicsDevice => _graphics;

    private readonly GraphicsDevice _graphics;
    private readonly float _smoothSpeed = 8f;

    public Camera(GraphicsDevice graphics)
    {
        _graphics = graphics;
    }

    /// <summary>Instant camera jump — use on scene enter to avoid a slide from the previous scene's position.</summary>
    public void SnapTo(Vector2 target)
    {
        Position = target;
        ClampToBounds();
    }

    public void Follow(Vector2 target, float deltaTime)
    {
        Position = Vector2.Lerp(Position, target, _smoothSpeed * deltaTime);
        ClampToBounds();
    }

    private void ClampToBounds()
    {
        if (!Bounds.HasValue) return;
        var viewport = _graphics.Viewport;
        float halfW = viewport.Width / (2f * Zoom);
        float halfH = viewport.Height / (2f * Zoom);
        var b = Bounds.Value;

        // When the visible area exceeds the map on an axis, center on that axis
        // instead of inverting the clamp (which would snap the camera off-map).
        float x = (halfW * 2f >= b.Width)
            ? b.Center.X
            : MathHelper.Clamp(Position.X, b.Left + halfW, b.Right - halfW);
        float y = (halfH * 2f >= b.Height)
            ? b.Center.Y
            : MathHelper.Clamp(Position.Y, b.Top + halfH, b.Bottom - halfH);

        Position = new Vector2(x, y);
    }

    /// <summary>
    /// Ensure Zoom is at least enough to fill the viewport with the current Bounds
    /// (no black bars). Keeps <paramref name="preferred"/> when it already covers.
    /// </summary>
    public void FitZoomToViewport(float preferred)
    {
        if (!Bounds.HasValue) { Zoom = preferred; return; }
        var vp = _graphics.Viewport;
        var b = Bounds.Value;
        if (b.Width <= 0 || b.Height <= 0) { Zoom = preferred; return; }
        float minX = vp.Width / (float)b.Width;
        float minY = vp.Height / (float)b.Height;
        Zoom = MathHelper.Max(preferred, MathHelper.Max(minX, minY));
    }

    public Matrix GetTransformMatrix()
    {
        var viewport = _graphics.Viewport;
        return Matrix.CreateTranslation(-Position.X, -Position.Y, 0)
             * Matrix.CreateScale(Zoom)
             * Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0);
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return Vector2.Transform(screenPos, Matrix.Invert(GetTransformMatrix()));
    }
}
