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

    public Vector2 Movement { get; private set; }
    public bool InteractPressed { get; private set; }

    public void Update()
    {
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
    }

    public bool IsKeyPressed(Keys key) =>
        _currentKeys.IsKeyDown(key) && !_previousKeys.IsKeyDown(key);

    public bool IsKeyDown(Keys key) => _currentKeys.IsKeyDown(key);
}
