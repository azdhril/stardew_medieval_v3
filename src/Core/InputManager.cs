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

    /// <summary>True while the left mouse button is held down (level signal, not edge).</summary>
    public bool IsLeftButtonDown =>
        _currentMouse.LeftButton == ButtonState.Pressed;

    /// <summary>True on the frame left mouse button is first released (falling edge).</summary>
    public bool IsLeftClickReleased =>
        _currentMouse.LeftButton == ButtonState.Released &&
        _previousMouse.LeftButton == ButtonState.Pressed;

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

    /// <summary>
    /// Refresh input state. <paramref name="windowFocused"/> gates the OS poll —
    /// when the game window is unfocused/minimized, MonoGame's <c>Mouse.GetState</c>
    /// returns the global desktop state (any click in another app reaches us as a
    /// real click), which made the fishing rod fire ghost casts in the background.
    /// Pass <c>Game.IsActive</c> from <c>Game1.Update</c>; when false we synthesise
    /// a "fully released" state so no edges (Pressed/Released) fire and held keys
    /// don't keep counting.
    /// </summary>
    public void Update(bool windowFocused = true)
    {
        _previousMouse = _currentMouse;
        _previousKeys = _currentKeys;

        if (windowFocused)
        {
            _currentMouse = Mouse.GetState();
            _currentKeys = Keyboard.GetState();
        }
        else
        {
            // Snapshot mouse position only (so cursor doesn't teleport to (0,0) on refocus)
            // but force every button into Released. Same for keyboard — empty state means
            // no key registers as held while we're in the background.
            _currentMouse = new MouseState(
                _currentMouse.X, _currentMouse.Y, _currentMouse.ScrollWheelValue,
                ButtonState.Released, ButtonState.Released, ButtonState.Released,
                ButtonState.Released, ButtonState.Released);
            // Mirror previous so no falling-edge events fire on the first inactive frame.
            _previousMouse = _currentMouse;
            _currentKeys = new KeyboardState();
            _previousKeys = _currentKeys;
        }

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
