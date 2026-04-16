using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Stack-based scene manager with fade-to-black transitions.
/// Active scene (top of stack) receives Update calls.
/// All scenes in stack receive Draw calls (bottom to top).
/// </summary>
public class SceneManager
{
    private readonly Stack<Scene> _scenes = new();
    private TransitionState _state = TransitionState.None;
    private float _fadeAlpha;
    private const float FadeDuration = 0.4f; // seconds per fade direction
    private Action? _pendingAction;

    private Texture2D _pixel = null!;
    private GraphicsDevice _graphicsDevice = null!;

    public int SceneCount => _scenes.Count;
    public bool IsTransitioning => _state != TransitionState.None;

    /// <summary>Initialize with graphics device to create fade overlay texture.</summary>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>Clear stack and transition to a new scene (fade out -> swap -> fade in).</summary>
    public void TransitionTo(Scene newScene)
    {
        if (_state != TransitionState.None) return; // ignore during transition

        _pendingAction = () =>
        {
            // Unload all current scenes
            while (_scenes.Count > 0)
                _scenes.Pop().UnloadContent();
            _scenes.Push(newScene);
            newScene.LoadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
        Console.WriteLine("[SceneManager] Transition started");
    }

    /// <summary>Push overlay scene on top (e.g., pause menu). Fades in/out.</summary>
    public void Push(Scene overlay)
    {
        if (_state != TransitionState.None) return;

        _pendingAction = () =>
        {
            _scenes.Push(overlay);
            overlay.LoadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
    }

    /// <summary>Pop top scene (e.g., close pause menu). Fades out/in.</summary>
    public void Pop()
    {
        if (_state != TransitionState.None || _scenes.Count == 0) return;

        _pendingAction = () =>
        {
            if (_scenes.Count > 0)
                _scenes.Pop().UnloadContent();
        };
        _state = TransitionState.FadingOut;
        _fadeAlpha = 0f;
    }

    /// <summary>Push first scene without fade (used at game startup).</summary>
    public void PushImmediate(Scene scene)
    {
        _scenes.Push(scene);
        scene.LoadContent();
        Console.WriteLine("[SceneManager] Scene pushed (immediate)");
    }

    /// <summary>Pop top scene without fade (used for instant overlay close).</summary>
    public void PopImmediate()
    {
        if (_scenes.Count == 0) return;
        _scenes.Pop().UnloadContent();
        Console.WriteLine("[SceneManager] Scene popped (immediate)");
    }

    private Scene? _deferredPush;
    private float _deferredPushTimer;

    /// <summary>
    /// Push a scene after <paramref name="delaySeconds"/> real time. Used to
    /// re-open the pause menu a moment after a fullscreen toggle so the OS
    /// can commit the window resize (pop + toggle + push synchronously in
    /// one frame leaves black bars on first fullscreen entry).
    /// </summary>
    public void PushAfter(Scene scene, float delaySeconds)
    {
        _deferredPush = scene;
        _deferredPushTimer = delaySeconds;
    }

    /// <summary>
    /// Update the transition state machine and active scene.
    /// During transitions, no scene receives Update.
    /// During None state, only the top scene receives Update.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_deferredPush != null)
        {
            _deferredPushTimer -= deltaTime;
            if (_deferredPushTimer <= 0f)
            {
                var s = _deferredPush;
                _deferredPush = null;
                PushImmediate(s);
            }
        }

        switch (_state)
        {
            case TransitionState.FadingOut:
                _fadeAlpha += deltaTime / FadeDuration;
                if (_fadeAlpha >= 1f)
                {
                    _fadeAlpha = 1f;
                    _pendingAction?.Invoke();
                    _pendingAction = null;
                    _state = TransitionState.FadingIn;
                    Console.WriteLine("[SceneManager] Transition: scene swapped");
                }
                break;

            case TransitionState.FadingIn:
                _fadeAlpha -= deltaTime / FadeDuration;
                if (_fadeAlpha <= 0f)
                {
                    _fadeAlpha = 0f;
                    _state = TransitionState.None;
                    Console.WriteLine("[SceneManager] Transition complete");
                }
                break;

            case TransitionState.None:
                // Only top scene receives Update
                if (_scenes.Count > 0)
                    _scenes.Peek().Update(deltaTime);
                break;
        }
    }

    /// <summary>
    /// Draw all scenes bottom-to-top, then fade overlay on top of everything.
    /// Fade overlay uses its own SpriteBatch Begin/End in screen space.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw all scenes bottom-to-top (Stack enumerates top-first, so reverse)
        var scenesArray = _scenes.ToArray();
        for (int i = scenesArray.Length - 1; i >= 0; i--)
            scenesArray[i].Draw(spriteBatch);

        // Draw fade overlay on top of everything
        if (_fadeAlpha > 0f)
        {
            var viewport = _graphicsDevice.Viewport;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_pixel,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                Color.Black * _fadeAlpha);
            spriteBatch.End();
        }
    }
}

/// <summary>
/// Transition state for the fade-to-black state machine.
/// None -> FadingOut -> (execute action at black) -> FadingIn -> None
/// </summary>
public enum TransitionState
{
    None,
    FadingOut,
    FadingIn
}
