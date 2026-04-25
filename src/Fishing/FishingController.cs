using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Farming;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Fishing;

/// <summary>Player-visible phases of a fishing attempt.</summary>
public enum FishingState
{
    /// <summary>No active fishing — rod equipped, waiting for the player to click on water.</summary>
    Idle,
    /// <summary>LMB held — cast power bar fills under the player; release picks the throw distance.</summary>
    Charging,
    /// <summary>Bobber flying through the air toward the target tile.</summary>
    Casting,
    /// <summary>Bobber on water, waiting for a bite.</summary>
    Waiting,
    /// <summary>Bobber flashing — player has a short window to click and hook the fish.</summary>
    Bite,
    /// <summary>Ring-shaped minigame active: bobber-as-joystick versus fish-on-ring; ends in foge or 1/2/3⭐.</summary>
    Reeling,
    /// <summary>Fish arcing over the player from the bobber to a landing spot behind them.</summary>
    Pulling,
    /// <summary>Brief lock-out after a catch/escape so transitions read on screen.</summary>
    Done,
}

/// <summary>
/// Owns the fishing state machine and bobber entity. The minigame itself
/// (ring + joystick + charge) lands in a future iteration; for now a successful
/// hook auto-catches at 2-star quality so the rest of the system (fish drops,
/// rarity smoke aura, vendable items) can be tested end-to-end.
///
/// Input contract: <see cref="ToolController"/> short-circuits its DoAction
/// while a fishing rod is equipped, so this controller is the sole consumer of
/// LMB clicks during fishing. <see cref="Update"/> must run every frame the
/// rod is the active tool.
/// </summary>
public class FishingController
{
    public FishingState State { get; private set; } = FishingState.Idle;

    /// <summary>World position of the bobber sprite. Only valid when <see cref="State"/> != Idle.</summary>
    public Vector2 BobberPosition { get; private set; }

    /// <summary>The fish that took the bait — set when <see cref="State"/> reaches Bite or later.</summary>
    public FishData? HookedFish { get; private set; }

    /// <summary>True while the player should see the bite indicator (flash) on the bobber.</summary>
    public bool ShowBiteIndicator => State == FishingState.Bite;

    /// <summary>The minigame instance — exposed so the FarmScene renderer can read it.</summary>
    public FishingMinigame Minigame { get; } = new();

    /// <summary>
    /// True while a fishing round is monopolizing input (Bite, Reeling, or Pulling).
    /// FarmScene gates ESC/I/movement on this so the player can't pause or walk
    /// off mid-minigame.
    /// </summary>
    public bool IsCapturingInput => State == FishingState.Bite
                                 || State == FishingState.Reeling
                                 || State == FishingState.Pulling;

    /// <summary>True while the ring minigame is active and should be drawn on top of the world.</summary>
    public bool IsMinigameActive => State == FishingState.Reeling;

    /// <summary>0..1 cast power while <see cref="State"/> is Charging — drives the bar under the player.</summary>
    public float CastChargePct => State == FishingState.Charging ? _castCharge : 0f;

    /// <summary>
    /// Debug-only: when non-null, the next <see cref="BeginBite"/> ignores the biome /
    /// season / time / weather filters and uses this fish id instead. Wired to F4 in
    /// FarmScene so the user can test rare-fish behaviors (Phantom Eel Sprint pattern,
    /// etc.) on the farm pond without waiting for the right RNG roll.
    /// </summary>
    public string? DebugForcedFishId { get; set; }

    private readonly PlayerEntity _player;
    private readonly InventoryManager _inventory;
    private readonly TileMap _map;
    private readonly TimeManager _time;
    private readonly Action<string, int, Vector2, int> _spawnDrop;
    private readonly Random _rng = new();
    private int _caughtFishQuality;
    private bool _caughtFishShiny;

    private Vector2 _castStart;
    private Vector2 _castTarget;
    private float _stateTimer;
    private float _waitDuration;

    private const float CastDuration = 0.45f;
    private const float CastArcHeight = 22f;
    private const float WaitMinSeconds = 2.5f;
    private const float WaitMaxSeconds = 6.0f;
    private const float DoneDuration = 0.4f;
    private const int MaxCastTiles = 6;

    /// <summary>Time (seconds) to fill the cast power bar from 0 to 100% while LMB is held.</summary>
    private const float CastChargeFullDuration = 1.4f;

    /// <summary>Minimum fraction of <see cref="MaxCastTiles"/> a cast travels even at zero charge.</summary>
    private const float MinCastFraction = 0.2f;

    /// <summary>
    /// Minimum charge (0..1) required to actually fire a cast on release. Single-frame
    /// fluke clicks (alt-tab edges, scene-transition clicks bleeding through) charge
    /// for ≈ dt/CastChargeFullDuration ≈ 1-2% — anything below this threshold is treated
    /// as "no cast" and snaps the controller back to Idle silently.
    /// </summary>
    private const float MinChargeToCast = 0.05f;

    /// <summary>Brief cooldown after a missed cast (bobber landed on land) before returning to Idle.</summary>
    private const float MissedCastDelay = 0.35f;

    private float _castCharge;
    private bool _castMissed;

    /// <summary>
    /// Rod modifiers locked in at cast time and used through Bite/Reel. Switching
    /// hotbar mid-fishing won't change the active mods — the cast committed the rod.
    /// </summary>
    private RodModifiers _activeMods = RodModifiers.None;

    /// <summary>Line snaps if the player wanders past this distance from the bobber.</summary>
    private const float MaxLineLength = 80f;

    /// <summary>Distance behind the player where the caught fish lands.</summary>
    private const float FishLandBehindDistance = 22f;

    /// <summary>How long the fish takes to arc from bobber over the player to the ground.</summary>
    private const float PullDuration = 0.55f;

    /// <summary>Peak height of the fish arc above the midpoint between bobber and landing spot.</summary>
    private const float PullArcHeight = 36f;

    private Vector2 _fishStart;
    private Vector2 _fishLanding;
    private string? _caughtFishId;
    private Vector2 _fishCurrentPos;

    public FishingController(PlayerEntity player, InventoryManager inventory, TileMap map,
        TimeManager time, Action<string, int, Vector2, int> spawnDrop)
    {
        _player = player;
        _inventory = inventory;
        _map = map;
        _time = time;
        _spawnDrop = spawnDrop;
    }

    public void Update(float deltaTime, InputManager input)
    {
        switch (State)
        {
            case FishingState.Idle:
                if (input.IsLeftClickPressed)
                {
                    _castCharge = 0f;
                    _activeMods = ResolveRodModifiers();
                    State = FishingState.Charging;
                }
                break;

            case FishingState.Charging:
                // Bar fills while LMB is held; release picks the throw distance.
                if (input.IsLeftButtonDown)
                {
                    _castCharge = Math.Min(1f, _castCharge + deltaTime / CastChargeFullDuration);
                }
                else
                {
                    // Tiny fluke clicks (held for ≈1 frame, charge below the threshold)
                    // get swallowed instead of producing a 1-tile cast. Stops alt-tab
                    // ghost clicks and accidental UI taps from spamming the fishing log.
                    if (_castCharge < MinChargeToCast)
                    {
                        _castCharge = 0f;
                        State = FishingState.Idle;
                    }
                    else
                    {
                        ReleaseCast();
                    }
                }
                break;

            case FishingState.Casting:
                _stateTimer += deltaTime;
                float t = MathHelper.Clamp(_stateTimer / CastDuration, 0f, 1f);
                Vector2 lerp = Vector2.Lerp(_castStart, _castTarget, t);
                float arc = -CastArcHeight * MathF.Sin(MathF.PI * t);
                BobberPosition = lerp + new Vector2(0, arc);
                if (t >= 1f)
                {
                    BobberPosition = _castTarget;
                    _stateTimer = 0;
                    if (_castMissed)
                    {
                        // Bobber landed on dirt/grass — Done state cools the cast back to Idle.
                        State = FishingState.Done;
                        Console.WriteLine("[Fishing] Cast overshot the water — line cancelled.");
                    }
                    else
                    {
                        _waitDuration = (float)(WaitMinSeconds + _rng.NextDouble() * (WaitMaxSeconds - WaitMinSeconds));
                        State = FishingState.Waiting;
                    }
                }
                break;

            case FishingState.Waiting:
                _stateTimer += deltaTime;
                // Idle bob — small vertical oscillation so the bobber looks alive on the water
                BobberPosition = _castTarget + new Vector2(0, MathF.Sin(_stateTimer * 4f) * 1.2f);
                if (LineBroke()) { EndAttempt(escaped: true, reason: "line broke — too far"); break; }
                // Cancel cast on click — pulls the line back without a catch
                if (input.IsLeftClickPressed)
                {
                    EndAttempt(escaped: true);
                    break;
                }
                if (_stateTimer >= _waitDuration)
                    BeginBite();
                break;

            case FishingState.Bite:
                _stateTimer += deltaTime;
                BobberPosition = _castTarget + new Vector2(0, MathF.Sin(_stateTimer * 18f) * 2.5f);
                if (LineBroke()) { EndAttempt(escaped: true, reason: "line broke — too far"); break; }
                if (input.IsLeftClickPressed)
                {
                    StartMinigame();
                    break;
                }
                // Reaction window: fish base × rod modifier (better rod = more time to react).
                float window = (HookedFish?.BiteWindow ?? 1.0f) * _activeMods.BiteWindowMult;
                if (_stateTimer >= window)
                    EndAttempt(escaped: true);
                break;

            case FishingState.Reeling:
                // Bobber idles on the water during the minigame; the action is on the ring overlay.
                _stateTimer += deltaTime;
                BobberPosition = _castTarget + new Vector2(0, MathF.Sin(_stateTimer * 8f) * 1.5f);

                bool justFinished = Minigame.Update(deltaTime, input.Movement, input.IsLeftClickPressed);
                if (justFinished)
                {
                    var outcome = Minigame.Outcome;
                    if (outcome.Fled)
                    {
                        EndAttempt(escaped: true, reason: $"charge {Minigame.ChargePct:0}% < min");
                    }
                    else
                    {
                        _caughtFishQuality = outcome.Quality;
                        _caughtFishShiny = outcome.Shiny;
                        Catch();
                    }
                }
                break;

            case FishingState.Pulling:
                _stateTimer += deltaTime;
                float pt = MathHelper.Clamp(_stateTimer / PullDuration, 0f, 1f);
                Vector2 pulled = Vector2.Lerp(_fishStart, _fishLanding, pt);
                _fishCurrentPos = pulled + new Vector2(0, -PullArcHeight * MathF.Sin(MathF.PI * pt));
                if (pt >= 1f)
                {
                    if (_caughtFishId != null)
                        _spawnDrop(_caughtFishId, 1, _fishLanding, _caughtFishQuality);
                    string stars = _caughtFishQuality switch { 1 => "⭐", 2 => "⭐⭐", 3 => _caughtFishShiny ? "⭐⭐⭐ shiny" : "⭐⭐⭐", _ => "" };
                    Console.WriteLine($"[Fishing] Landed {_caughtFishId} {stars} at ({_fishLanding.X:0},{_fishLanding.Y:0})");
                    _caughtFishId = null;
                    _caughtFishQuality = 0;
                    _caughtFishShiny = false;
                    _stateTimer = 0;
                    State = FishingState.Done;
                }
                break;

            case FishingState.Done:
                _stateTimer += deltaTime;
                if (_stateTimer >= DoneDuration)
                {
                    State = FishingState.Idle;
                    HookedFish = null;
                }
                break;
        }
    }

    /// <summary>
    /// Snapshot the cast power and throw the bobber. Distance scales linearly from
    /// <see cref="MinCastFraction"/> × <see cref="MaxCastTiles"/> at zero charge to
    /// <see cref="MaxCastTiles"/> at full charge. The target tile is determined by
    /// the charge alone, not by water-tile scanning — if the target lands on dirt
    /// or grass the cast still flies but transitions straight to Done (cancelled),
    /// matching the fliperama "throw and miss" feedback the player asked for.
    /// </summary>
    private void ReleaseCast()
    {
        Point origin = _player.GetTilePosition();
        Point step = _player.FacingDirection switch
        {
            Direction.Up    => new Point(0, -1),
            Direction.Down  => new Point(0,  1),
            Direction.Left  => new Point(-1, 0),
            Direction.Right => new Point( 1, 0),
            _ => new Point(0, 1),
        };

        // Charge maps to a tile distance in 1..MaxCastTiles. Floor of charge*range
        // plus a guaranteed minimum so a tap at 0% still throws a short cast.
        float minTiles = MinCastFraction * MaxCastTiles;
        int tiles = (int)MathF.Round(MathHelper.Lerp(minTiles, MaxCastTiles, _castCharge));
        tiles = Math.Max(1, Math.Min(MaxCastTiles, tiles));

        int tx = origin.X + step.X * tiles;
        int ty = origin.Y + step.Y * tiles;

        _castTarget = new Vector2(tx * TileMap.TileSize + TileMap.TileSize / 2f,
                                   ty * TileMap.TileSize + TileMap.TileSize / 2f);
        _castStart = _player.GetFootPosition();
        BobberPosition = _castStart;
        _castMissed = !_map.IsWater(tx, ty);
        _stateTimer = 0;
        State = FishingState.Casting;
        Console.WriteLine($"[Fishing] Released cast: {(_castCharge * 100f):0}% charge → {tiles} tiles ({(_castMissed ? "MISS — not water" : $"water tile ({tx},{ty})")})");
    }

    /// <summary>
    /// Roll a fish from the registry filtered by biome/season/time/weather and
    /// transition to <see cref="FishingState.Bite"/>. The hooked fish lives on
    /// <see cref="HookedFish"/> until catch or escape.
    /// </summary>
    private void BeginBite()
    {
        // Debug override (F4 in FarmScene): bypass all filters and use the chosen fish.
        if (DebugForcedFishId != null)
        {
            var forced = FishRegistry.Get(DebugForcedFishId);
            if (forced != null)
            {
                HookedFish = forced;
                _stateTimer = 0;
                State = FishingState.Bite;
                Console.WriteLine($"[Fishing] Bite! ({HookedFish.Id}) [DEBUG forced]");
                return;
            }
            Console.WriteLine($"[Fishing] DEBUG forced fish '{DebugForcedFishId}' not registered — falling back to roll.");
        }

        FishBiome biome = FishBiome.River; // FarmScene-only for now; future: detect from cast tile
        string season = SeasonName(_time.Season);
        string timeOfDay = TimeOfDay(_time.CurrentHour);
        string weather = "Sun"; // weather system TBD

        var candidates = FishRegistry.GetCandidates(biome, season, timeOfDay, weather);
        if (candidates.Count == 0)
        {
            // Fallback: pick any fish so the loop still closes during testing.
            foreach (var f in FishRegistry.All.Values) { candidates.Add(f); break; }
        }
        if (candidates.Count == 0)
        {
            Console.WriteLine("[Fishing] No fish registered — escaping.");
            EndAttempt(escaped: true);
            return;
        }

        HookedFish = WeightedPick(candidates);
        _stateTimer = 0;
        State = FishingState.Bite;
        Console.WriteLine($"[Fishing] Bite! ({HookedFish.Id})");
    }

    /// <summary>
    /// Hand off to the ring minigame: bobber-as-joystick versus fish-on-ring,
    /// charge bar above, patience timer below. Outcome (foge / 1⭐ / 2⭐ / 3⭐)
    /// is read back in the Reeling state and routed into <see cref="Catch"/> or
    /// <see cref="EndAttempt"/>.
    /// </summary>
    private void StartMinigame()
    {
        if (HookedFish == null)
        {
            EndAttempt(escaped: true);
            return;
        }
        Minigame.Begin(HookedFish, _activeMods);
        _stateTimer = 0;
        State = FishingState.Reeling;
        Console.WriteLine(
            $"[Fishing] Minigame start ({HookedFish.Id}, dur={HookedFish.MinigameDuration}s, " +
            $"tolMult={_activeMods.ToleranceMult:F2}, fillMult={_activeMods.FillRateMult:F2})");
    }

    private FishData WeightedPick(List<FishData> options)
    {
        int total = 0;
        foreach (var f in options) total += Math.Max(1, f.Weight);
        int roll = _rng.Next(total);
        int acc = 0;
        foreach (var f in options)
        {
            acc += Math.Max(1, f.Weight);
            if (roll < acc) return f;
        }
        return options[0];
    }

    /// <summary>
    /// Etapa 2 auto-catch: kick off the Pulling animation that arcs the fish
    /// from the bobber over the player to a landing point behind them. The
    /// actual <see cref="ItemDropEntity"/> spawn happens once the arc finishes,
    /// so the magnet + rarity smoke aura kicks in only after the fish lands —
    /// matches the visual intent (the "yank" in Stardew).
    /// </summary>
    private void Catch()
    {
        if (HookedFish == null)
        {
            EndAttempt(escaped: true);
            return;
        }

        // Landing spot = behind the player (opposite of facing direction).
        Vector2 backDir = _player.FacingDirection switch
        {
            Direction.Up    => new Vector2(0,  1),
            Direction.Down  => new Vector2(0, -1),
            Direction.Left  => new Vector2(1,  0),
            Direction.Right => new Vector2(-1, 0),
            _ => new Vector2(0, -1),
        };

        _fishStart = BobberPosition;
        _fishLanding = _player.GetFootPosition() + backDir * FishLandBehindDistance;
        _fishCurrentPos = _fishStart;
        _caughtFishId = HookedFish.Id;
        _stateTimer = 0;
        State = FishingState.Pulling;
        Console.WriteLine($"[Fishing] Pulling {HookedFish.Id} over the player...");
    }

    private void EndAttempt(bool escaped, string? reason = null)
    {
        if (escaped && HookedFish != null)
            Console.WriteLine($"[Fishing] {HookedFish.Id} escaped" + (reason != null ? $" ({reason})!" : "!"));
        else if (escaped)
            Console.WriteLine("[Fishing] Cast cancelled" + (reason != null ? $" ({reason})." : "."));

        _stateTimer = 0;
        State = FishingState.Done;
    }

    /// <summary>
    /// Returns true when the player has wandered past <see cref="MaxLineLength"/>
    /// from the bobber. Called every tick of Waiting/Bite so the pesca aborta
    /// quando o player se afasta — não é só timeout.
    /// </summary>
    private bool LineBroke()
    {
        return Vector2.Distance(_player.GetFootPosition(), BobberPosition) > MaxLineLength;
    }

    /// <summary>
    /// Read the active hotbar rod's Stats dict and pack the relevant multipliers
    /// into a <see cref="RodModifiers"/>. Recognized stat keys (defaults = 1.0):
    ///   - <c>tolerance_mult</c>   → <see cref="RodModifiers.ToleranceMult"/>
    ///   - <c>fill_rate_mult</c>   → <see cref="RodModifiers.FillRateMult"/>
    ///   - <c>bite_window_mult</c> → <see cref="RodModifiers.BiteWindowMult"/>
    /// Non-rod active items (or starter rod with empty Stats) yield <see cref="RodModifiers.None"/>.
    /// </summary>
    private RodModifiers ResolveRodModifiers()
    {
        var active = _inventory.GetActiveHotbarItem();
        if (active == null) return RodModifiers.None;
        var def = ItemRegistry.Get(active.ItemId);
        if (def == null) return RodModifiers.None;

        float tol  = def.Stats.TryGetValue("tolerance_mult",   out float t) ? t : 1f;
        float fill = def.Stats.TryGetValue("fill_rate_mult",   out float f) ? f : 1f;
        float bite = def.Stats.TryGetValue("bite_window_mult", out float b) ? b : 1f;

        return new RodModifiers
        {
            ToleranceMult  = tol,
            FillRateMult   = fill,
            BiteWindowMult = bite,
        };
    }

    private static string SeasonName(int s) => s switch
    {
        0 => "Spring",
        1 => "Summer",
        2 => "Fall",
        3 => "Winter",
        _ => "Spring",
    };

    private static string TimeOfDay(int hour)
    {
        // 6AM..18 = Day; everything else = Night. Hours past midnight are 24..26.
        int h = hour >= 24 ? hour - 24 : hour;
        return (h >= 6 && h < 19) ? "Day" : "Night";
    }

    /// <summary>
    /// Render the bobber sprite + the fishing line. No-op when idle. Pass a 1×1
    /// white pixel texture for the line, and the shared atlas for the bobber sprite
    /// (registered via <c>SpriteAtlas.RegisterFishingTools</c> as
    /// <c>fishing_bobber_4</c>).
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteAtlas atlas, Texture2D pixel)
    {
        if (State == FishingState.Idle || State == FishingState.Done) return;

        const int sprite = 16;

        // Charging phase: only the power bar under the player, no bobber/line yet.
        if (State == FishingState.Charging)
        {
            DrawCastChargeBar(spriteBatch, pixel);
            return;
        }

        // Pulling phase: fish leaping over the player. No line, no bobber — the
        // line "snapped" with the yank. Just the fish sprite arcing through the air.
        if (State == FishingState.Pulling)
        {
            if (_caughtFishId != null)
            {
                string fishSpriteId = _caughtFishId.ToLowerInvariant();
                var fishRect = atlas.GetRect(fishSpriteId);
                var fishTex = atlas.GetTexture(fishSpriteId);
                var fishDest = new Rectangle(
                    (int)(_fishCurrentPos.X - sprite / 2f),
                    (int)(_fishCurrentPos.Y - sprite / 2f),
                    sprite, sprite);
                spriteBatch.Draw(fishTex, fishDest, fishRect, Color.White);
            }
            return;
        }

        // Fishing line — thin segment from the player's hand area to the bobber.
        // Alpha kept low (~70/255) so the line reads as a hair-thin filament instead
        // of a rope at 16px tile scale; 1 world-pixel is already the floor for
        // pixel-art crispness, so transparency is what carries the "thinner" feel.
        Vector2 hand = _player.GetFootPosition() + new Vector2(0, -10);
        DrawLine(spriteBatch, pixel, hand, BobberPosition, new Color(220, 220, 220, 70));

        // Bobber sprite (16×16). During a bite we flash yellow + scale up slightly to
        // grab the player's eye — quality preview of the future minigame's "!"
        // indicator without yet pulling in a font.
        var srcRect = atlas.GetRect("fishing_bobber_4");
        var tex = atlas.GetTexture("fishing_bobber_4");

        bool flash = ShowBiteIndicator && (((int)(_stateTimer * 10)) % 2 == 0);
        Color tint = flash ? Color.Yellow : Color.White;
        int size = flash ? sprite + 4 : sprite;

        var dest = new Rectangle(
            (int)(BobberPosition.X - size / 2f),
            (int)(BobberPosition.Y - size / 2f),
            size, size);
        spriteBatch.Draw(tex, dest, srcRect, tint);
    }

    /// <summary>
    /// Draw the cast power bar under the player while LMB is held. World-space, so
    /// the camera transform applies — the bar tracks the player as they shift their
    /// stance. Color shifts green → yellow → red as charge climbs, hinting at how
    /// much harder the throw will be (a fully-charged cast easily overshoots a small
    /// pond and cancels back to Idle).
    /// </summary>
    private void DrawCastChargeBar(SpriteBatch sb, Texture2D pixel)
    {
        const int width = 22;
        const int height = 3;
        Vector2 foot = _player.GetFootPosition();
        int x = (int)(foot.X - width / 2f);
        int y = (int)(foot.Y + 4);

        // Frame
        sb.Draw(pixel, new Rectangle(x - 1, y - 1, width + 2, height + 2), new Color(0, 0, 0, 220));
        sb.Draw(pixel, new Rectangle(x, y, width, height), new Color(40, 40, 50, 230));

        int fillW = (int)(width * _castCharge);
        Color fill = _castCharge < 0.5f
            ? Color.Lerp(new Color(110, 200, 120), new Color(220, 200, 90), _castCharge * 2f)
            : Color.Lerp(new Color(220, 200, 90), new Color(220, 90, 80), (_castCharge - 0.5f) * 2f);
        sb.Draw(pixel, new Rectangle(x, y, fillW, height), fill);
    }

    private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.01f) return;
        float angle = MathF.Atan2(d.Y, d.X);
        sb.Draw(pixel, a, null, color, angle, Vector2.Zero,
            new Vector2(len, 1), SpriteEffects.None, 0f);
    }
}
