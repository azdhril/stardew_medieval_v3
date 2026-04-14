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
        Position = new Vector2(
            MathHelper.Clamp(Position.X, b.Left + halfW, b.Right - halfW),
            MathHelper.Clamp(Position.Y, b.Top + halfH, b.Bottom - halfH)
        );
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
