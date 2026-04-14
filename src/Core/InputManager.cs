using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Centralized input handling. Tracks current and previous states for edge detection.
/// </summary>
public class InputManager
{
    private KeyboardState _currentKeys;
    private KeyboardState _previousKeys;
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    public Vector2 Movement { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool IsRunHeld { get; private set; }

    /// <summary>Current mouse position in screen coordinates.</summary>
    public Point MousePosition => _currentMouse.Position;

    /// <summary>True on the frame left mouse button is first pressed.</summary>
    public bool IsLeftClickPressed =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>True on the frame right mouse button is first pressed.</summary>
    public bool IsRightClickPressed =>
        _currentMouse.RightButton == ButtonState.Pressed &&
        _previousMouse.RightButton == ButtonState.Released;

    /// <summary>
    /// Mouse wheel delta for the current frame. Positive = wheel up, negative = wheel down.
    /// Native MonoGame delta is typically ±120 per tick; callers should use Math.Sign to normalize.
    /// </summary>
    public int ScrollWheelDelta =>
        _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

    public void Update()
    {
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();

        _previousKeys = _currentKeys;
        _currentKeys = Keyboard.GetState();

        // WASD / Arrow movement
        var move = Vector2.Zero;
        if (_currentKeys.IsKeyDown(Keys.W) || _currentKeys.IsKeyDown(Keys.Up)) move.Y -= 1;
        if (_currentKeys.IsKeyDown(Keys.S) || _currentKeys.IsKeyDown(Keys.Down)) move.Y += 1;
        if (_currentKeys.IsKeyDown(Keys.A) || _currentKeys.IsKeyDown(Keys.Left)) move.X -= 1;
        if (_currentKeys.IsKeyDown(Keys.D) || _currentKeys.IsKeyDown(Keys.Right)) move.X += 1;

        if (move != Vector2.Zero)
            move.Normalize();

        Movement = move;
        InteractPressed = IsKeyPressed(Keys.E);
        IsRunHeld = _currentKeys.IsKeyDown(Keys.LeftShift) || _currentKeys.IsKeyDown(Keys.RightShift);
    }

    public bool IsKeyPressed(Keys key) =>
        _currentKeys.IsKeyDown(key) && !_previousKeys.IsKeyDown(key);

    public bool IsKeyDown(Keys key) => _currentKeys.IsKeyDown(key);
}
