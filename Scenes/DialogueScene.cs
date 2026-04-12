using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Entities;
using stardew_medieval_v3.UI;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Overlay scene driving the dialogue typewriter state machine. Pushed by
/// gameplay scenes (e.g. CastleScene) via <see cref="SceneManager.PushImmediate"/>.
/// Consumes E or Space (edge-triggered) to advance or snap-to-full.
/// See UI-SPEC §Interaction Contracts §State machine: dialogue box.
/// </summary>
public class DialogueScene : Scene
{
    private enum DialogueState
    {
        Typing,
        WaitingAdvance,
    }

    /// <summary>40 chars/sec → 0.025s per char (UI-SPEC §Typography).</summary>
    private const float CharInterval = 0.025f;

    private readonly NpcEntity _npc;
    private readonly string[] _lines;
    private readonly Action? _onClose;

    private int _lineIndex;
    private int _charsRevealed;
    private float _charTimer;
    private float _advancePulse;
    private DialogueState _state = DialogueState.Typing;

    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private DialogueBox _box = null!;

    /// <summary>
    /// Creates a new dialogue overlay.
    /// </summary>
    /// <param name="services">Shared service container.</param>
    /// <param name="npc">NPC being spoken to (used for portrait).</param>
    /// <param name="lines">Dialogue lines to reveal sequentially.</param>
    /// <param name="onClose">Optional callback invoked after the final line advances (before scene pop).</param>
    public DialogueScene(ServiceContainer services, NpcEntity npc, string[] lines, Action? onClose = null)
        : base(services)
    {
        _npc = npc;
        _lines = lines != null && lines.Length > 0 ? lines : new[] { "..." };
        _onClose = onClose;
    }

    public override void LoadContent()
    {
        var device = Services.GraphicsDevice;
        _font = Services.Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _box = new DialogueBox();
        Console.WriteLine($"[DialogueScene] Opened for {_npc.NpcId}, {_lines.Length} lines");
    }

    public override void Update(float deltaTime)
    {
        var input = Services.Input;
        string currentLine = _lines[_lineIndex];

        _advancePulse += deltaTime;

        bool advancePressed = input.IsKeyPressed(Keys.E) || input.IsKeyPressed(Keys.Space);

        switch (_state)
        {
            case DialogueState.Typing:
                _charTimer += deltaTime;
                while (_charTimer >= CharInterval && _charsRevealed < currentLine.Length)
                {
                    _charsRevealed++;
                    _charTimer -= CharInterval;
                }

                // Snap on key press
                if (advancePressed)
                {
                    _charsRevealed = currentLine.Length;
                    _charTimer = 0f;
                    _state = DialogueState.WaitingAdvance;
                    break;
                }

                // Auto-transition when line finishes typing
                if (_charsRevealed >= currentLine.Length)
                {
                    _charsRevealed = currentLine.Length;
                    _state = DialogueState.WaitingAdvance;
                }
                break;

            case DialogueState.WaitingAdvance:
                if (advancePressed)
                {
                    if (_lineIndex + 1 < _lines.Length)
                    {
                        _lineIndex++;
                        _charsRevealed = 0;
                        _charTimer = 0f;
                        _state = DialogueState.Typing;
                    }
                    else
                    {
                        _onClose?.Invoke();
                        Services.SceneManager.PopImmediate();
                    }
                }
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        string currentLine = _lines[_lineIndex];
        string revealed = _charsRevealed >= currentLine.Length
            ? currentLine
            : currentLine.Substring(0, _charsRevealed);
        bool showAdvance = _state == DialogueState.WaitingAdvance;
        bool pulseOn = ((int)(_advancePulse * 4)) % 2 == 0; // 2 Hz pulse

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _box.Draw(spriteBatch, _font, _pixel, _npc.Portrait, revealed, showAdvance, pulseOn);
        spriteBatch.End();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        Console.WriteLine("[DialogueScene] Closed");
    }
}
