using System;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Fishing;

/// <summary>
/// Per-rod multipliers applied to "skill" minigame attributes when computing the
/// effective minigame for a round. A better rod widens the tolerance arc, fills
/// charge faster, etc., letting fish that are nearly impossible with the starter
/// rod become viable as the player upgrades. Multipliers ≥ 1 make the round
/// easier; the basic rod uses <see cref="None"/> (everything = 1.0).
///
/// IMPORTANT: only attributes that represent *player skill / equipment quality*
/// are modulated here. Fish-intrinsic attributes (RingSpeed, Behavior, drain
/// rate, charge thresholds) are NOT scaled — those are properties of the fish
/// itself and should stay constant regardless of gear.
/// </summary>
public readonly struct RodModifiers
{
    /// <summary>Multiplier on <see cref="FishData.ToleranceArc"/> — wider on-target wedge.</summary>
    public float ToleranceMult { get; init; }

    /// <summary>Multiplier on <see cref="FishData.ChargeFillRate"/> — faster charge while on-target.</summary>
    public float FillRateMult { get; init; }

    /// <summary>Multiplier on <see cref="FishData.BiteWindow"/> — more time to react to the "!".</summary>
    public float BiteWindowMult { get; init; }

    /// <summary>Identity modifiers (no rod bonuses). Used by the starter rod and as a fallback.</summary>
    public static RodModifiers None => new() { ToleranceMult = 1f, FillRateMult = 1f, BiteWindowMult = 1f };
}

/// <summary>Final result of a fishing minigame round.</summary>
public readonly struct FishingOutcome
{
    /// <summary>True when the fish escaped (charge below MinChargePct or patience timed out without enough charge).</summary>
    public bool Fled { get; init; }

    /// <summary>Quality grade of the catch: 1 = ⭐, 2 = ⭐⭐, 3 = ⭐⭐⭐. 0 when fled.</summary>
    public int Quality { get; init; }

    /// <summary>True when the player landed a 3-star catch while perfectly on-target (bonus tier).</summary>
    public bool Shiny { get; init; }
}

/// <summary>
/// Pure state machine for the ring-shaped fishing minigame.
/// <para>
/// The fish travels along a circular "ring" (rosca) at the center of the screen.
/// The player's bobber acts as an arcade joystick on the same ring — WASD pushes
/// it toward an angle, and the goal is to keep it overlapping the fish's
/// tolerance arc to fill the charge bar before the patience timer runs out.
/// LMB at any point pulls the line — the charge level at the moment of pull
/// decides the outcome (foge / 1⭐ / 2⭐ / 3⭐ / shiny).
/// </para>
/// <para>
/// Rendering lives in <see cref="FishingMinigameRenderer"/>; this class is
/// engine-agnostic state + math so it stays unit-test friendly.
/// </para>
/// </summary>
public class FishingMinigame
{
    /// <summary>Below this lean magnitude the joystick is considered neutral (no aim).</summary>
    private const float MinLeanForAim = 0.35f;

    // Charge fill/drain rates now live per-fish on FishData (ChargeFillRate /
    // ChargeDrainRate) so designers can make easy fish forgiving and rare fish
    // punishing without touching code. The defaults match the previous hardcoded
    // 35/18 numbers so existing fish behavior is unchanged.

    /// <summary>True while the round is in progress; false before <see cref="Begin"/> and after completion.</summary>
    public bool IsActive { get; private set; }

    /// <summary>True once the round has produced an <see cref="Outcome"/>.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Outcome produced by the last completed round. Undefined while active.</summary>
    public FishingOutcome Outcome { get; private set; }

    /// <summary>Fish currently being caught — drives ring speed, behavior, thresholds.</summary>
    public FishData? Fish { get; private set; }

    /// <summary>Fish position on the ring (degrees, 0..360, where 0 = right, 90 = down).</summary>
    public float FishAngleDeg { get; private set; }

    /// <summary>
    /// Joystick lean vector. Magnitude 0 = inert (centered, "no aim"); magnitude
    /// approaches 1 as the player holds WASD. Direction = aim vector. Renderer
    /// uses this to offset the bobber sprite from the ring's center; gameplay
    /// uses it to test on-target alignment versus <see cref="FishAngleDeg"/>.
    /// </summary>
    public Vector2 JoystickTilt { get; private set; }

    /// <summary>Current aim angle (degrees) derived from the joystick tilt; only meaningful when <see cref="IsAiming"/>.</summary>
    public float AimAngleDeg { get; private set; }

    /// <summary>True when the joystick is leaned past <see cref="MinLeanForAim"/> — otherwise the player is "neutral".</summary>
    public bool IsAiming => JoystickTilt.LengthSquared() >= MinLeanForAim * MinLeanForAim;

    /// <summary>Charge bar fill, 0..100. Fills on-target, drains off-target.</summary>
    public float ChargePct { get; private set; }

    /// <summary>Patience bar fill, 0..1 (1 = fresh, 0 = timed out).</summary>
    public float PatiencePct => Fish == null ? 0f : MathHelper.Clamp(_patience / Fish.MinigameDuration, 0f, 1f);

    /// <summary>Active rod modifiers for this round (set by <see cref="Begin"/>; identity if none).</summary>
    public RodModifiers Mods { get; private set; } = RodModifiers.None;

    /// <summary>
    /// Effective tolerance arc (degrees) — fish base width × rod tolerance multiplier.
    /// Wider with better rods, lets the player "lock on" from a broader joystick angle.
    /// </summary>
    public float ToleranceArc => (Fish?.ToleranceArc ?? 35f) * Mods.ToleranceMult;

    /// <summary>True when the bobber is currently inside the tolerance arc (renderer uses this for highlight).</summary>
    public bool BobberOnTarget { get; private set; }

    private float _patience;
    private float _behaviorTimer;
    private float _fishVelocityDeg;       // signed deg/s for Smooth/Mixed
    private float _stopTimer;             // remaining stop time for Mixed
    private float _oscillateBaseDeg;      // center for Oscillate
    private float _oscillatePhase;        // 0..2π animation phase
    private bool _sprintActive;           // Sprint: true during the high-speed dash
    private float _sprintTimer;           // Sprint: remaining duration of the current dash
    private float _sprintVelocityDeg;     // Sprint: signed deg/s during the dash
    private Vector2 _tiltVelocity;        // running velocity for the smooth-damp spring on the joystick
    private readonly Random _rng = new();

    /// <summary>
    /// Smoothing time (seconds) for the joystick spring. Lower = snappier, higher
    /// = floppier. Tuned so direction changes (D → W diagonal swap) read as a
    /// curved sweep rather than a hard 90° flip — matches the "ease cúbica" feel.
    /// </summary>
    private const float TiltSmoothTime = 0.18f;

    /// <summary>
    /// Reset and start a new round for the given fish. <paramref name="mods"/>
    /// scales the skill-related attributes (tolerance, fill rate) by the active
    /// fishing rod's multipliers — pass <see cref="RodModifiers.None"/> for the
    /// starter rod or when modifiers don't apply.
    /// </summary>
    public void Begin(FishData fish, RodModifiers mods)
    {
        Fish = fish;
        Mods = mods;
        _patience = fish.MinigameDuration;
        ChargePct = 0f;
        FishAngleDeg = (float)(_rng.NextDouble() * 360.0);
        JoystickTilt = Vector2.Zero;
        _tiltVelocity = Vector2.Zero;
        AimAngleDeg = 0f;
        BobberOnTarget = false;
        _behaviorTimer = 0f;
        _stopTimer = 0f;
        _oscillateBaseDeg = FishAngleDeg;
        _oscillatePhase = 0f;
        _sprintActive = false;
        _sprintTimer = 0f;
        _sprintVelocityDeg = 0f;
        // Initial fish velocity: random direction at the configured ring speed.
        _fishVelocityDeg = (_rng.NextDouble() < 0.5 ? -1f : 1f) * fish.RingSpeed;
        IsActive = true;
        IsComplete = false;
        Outcome = default;
    }

    /// <summary>
    /// Tick the minigame. <paramref name="wasd"/> is a normalized direction vector
    /// (e.g. <see cref="InputManager.Movement"/>) — magnitude 0 means "joystick neutral",
    /// magnitude 1 means "pushed fully". <paramref name="clickPressed"/> = pull the line
    /// (one-shot; only check the rising edge). Returns true when the round just finished.
    /// </summary>
    public bool Update(float dt, Vector2 wasd, bool clickPressed)
    {
        if (!IsActive || Fish == null) return false;

        UpdateFish(dt);
        UpdateBobber(dt, wasd);
        UpdateCharge(dt);

        _patience -= dt;
        if (_patience <= 0f)
        {
            FinishWithCurrentCharge(forced: true);
            return true;
        }

        if (clickPressed)
        {
            FinishWithCurrentCharge(forced: false);
            return true;
        }

        return false;
    }

    private void UpdateFish(float dt)
    {
        var f = Fish!;
        _behaviorTimer += dt;

        switch (f.Behavior)
        {
            case FishBehavior.Smooth:
                // Constant angular velocity with the occasional reversal (~3-5s cadence).
                if (_behaviorTimer >= 3f + (float)_rng.NextDouble() * 2f)
                {
                    _fishVelocityDeg = -_fishVelocityDeg;
                    _behaviorTimer = 0f;
                }
                FishAngleDeg = WrapDeg(FishAngleDeg + _fishVelocityDeg * dt);
                break;

            case FishBehavior.Mixed:
                // Smooth motion punctuated by sudden stops + direction flips.
                if (_stopTimer > 0f)
                {
                    _stopTimer -= dt;
                    if (_stopTimer <= 0f)
                    {
                        // Resume: flip direction and reset cadence.
                        _fishVelocityDeg = -_fishVelocityDeg;
                        _behaviorTimer = 0f;
                    }
                }
                else
                {
                    FishAngleDeg = WrapDeg(FishAngleDeg + _fishVelocityDeg * dt);
                    if (_behaviorTimer >= 1.2f + (float)_rng.NextDouble() * 1.5f)
                    {
                        _stopTimer = 0.4f + (float)_rng.NextDouble() * 0.4f;
                        _behaviorTimer = 0f;
                    }
                }
                break;

            case FishBehavior.Dart:
                // Teleports to a random new angle every 1.5-2.5s.
                if (_behaviorTimer >= 1.5f + (float)_rng.NextDouble())
                {
                    FishAngleDeg = WrapDeg(FishAngleDeg + 90f + (float)_rng.NextDouble() * 180f);
                    _behaviorTimer = 0f;
                }
                break;

            case FishBehavior.Oscillate:
                // Sinusoidal sweep ±60° around the base angle.
                _oscillatePhase += dt * (f.RingSpeed * MathHelper.Pi / 180f);
                FishAngleDeg = WrapDeg(_oscillateBaseDeg + 60f * MathF.Sin(_oscillatePhase));
                break;

            case FishBehavior.Sprint:
                // "Lurker" pattern: stay almost still for 1.8–3s, then burst around
                // the ring (150–210°, in 0.35–0.55s) and stop on the opposite side.
                // The high angular speed (≈350–600 deg/s) is way past what the joystick's
                // smooth-damp spring can chase, so the player must commit a pre-emptive
                // sweep — making this the hardest tracked pattern in the rotation.
                if (_sprintActive)
                {
                    FishAngleDeg = WrapDeg(FishAngleDeg + _sprintVelocityDeg * dt);
                    _sprintTimer -= dt;
                    if (_sprintTimer <= 0f)
                    {
                        _sprintActive = false;
                        _behaviorTimer = 0f;
                    }
                }
                else
                {
                    // Tiny idle drift during the lull so the fish feels alive (uses RingSpeed
                    // as the dormant drift, scaled way down — fish.json sets RingSpeed low).
                    FishAngleDeg = WrapDeg(FishAngleDeg + _fishVelocityDeg * 0.15f * dt);
                    if (_behaviorTimer >= 1.8f + (float)_rng.NextDouble() * 1.2f)
                    {
                        float arcDeg = 150f + (float)_rng.NextDouble() * 60f;     // 150–210°
                        float duration = 0.35f + (float)_rng.NextDouble() * 0.20f; // 0.35–0.55s
                        float dir = _rng.NextDouble() < 0.5 ? -1f : 1f;
                        _sprintVelocityDeg = (arcDeg / duration) * dir;
                        _sprintTimer = duration;
                        _sprintActive = true;
                        // Flip the dormant drift sign so post-sprint idle drifts the
                        // opposite way — small touch but reads as the fish "settling".
                        _fishVelocityDeg = -_fishVelocityDeg;
                    }
                }
                break;
        }
    }

    private void UpdateBobber(float dt, Vector2 wasd)
    {
        // Critically-damped spring (a.k.a. Unity's SmoothDamp) toward the WASD
        // direction (or Vector2.Zero on release). The damped spring naturally
        // produces a soft acceleration/deceleration curve — visually close to
        // an ease-in-out cubic — so flipping from D to W (or to a diagonal)
        // sweeps the joystick through a curved arc instead of snapping. Same
        // smoothing handles the "spring back to center" when WASD is released.
        Vector2 target = wasd; // zero vector when no key held
        JoystickTilt = SmoothDamp(JoystickTilt, target, ref _tiltVelocity, TiltSmoothTime, dt);

        // Cap the magnitude at 1 so visuals and on-target math have a stable upper bound.
        if (JoystickTilt.LengthSquared() > 1f)
        {
            JoystickTilt = Vector2.Normalize(JoystickTilt);
            // Project the velocity onto the surface so it doesn't keep pushing past 1.
            float dot = Vector2.Dot(_tiltVelocity, JoystickTilt);
            if (dot > 0f) _tiltVelocity -= JoystickTilt * dot;
        }

        if (IsAiming)
            AimAngleDeg = WrapDeg(MathHelper.ToDegrees(MathF.Atan2(JoystickTilt.Y, JoystickTilt.X)));
    }

    /// <summary>
    /// Critically-damped spring smoothing — same shape as Unity's
    /// <c>Vector2.SmoothDamp</c>. <paramref name="smoothTime"/> is the approximate
    /// time the value takes to reach the target; smaller = snappier.
    /// </summary>
    private static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothTime, float dt)
    {
        smoothTime = MathF.Max(0.0001f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * dt;
        // Padé approximation of exp(-x) — keeps numerical behavior stable across dt sizes.
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        Vector2 change = current - target;
        Vector2 temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exp;
        return target + (change + temp) * exp;
    }

    private void UpdateCharge(float dt)
    {
        if (IsAiming)
        {
            float delta = ShortestAngularDelta(AimAngleDeg, FishAngleDeg);
            BobberOnTarget = Math.Abs(delta) <= ToleranceArc * 0.5f;
        }
        else
        {
            // Joystick at rest = no aim → can't lock on; charge always drains.
            BobberOnTarget = false;
        }

        // Per-fish fill/drain rates: easy fish drain slowly so the player can recover
        // after losing the track; rare fish drain fast and punish off-target frames.
        // Rod tier multiplies fill rate (better gear = faster charge); drain stays
        // constant — that's an intrinsic property of the fish, not affected by gear.
        float fillRate  = (Fish?.ChargeFillRate  ?? 35f) * Mods.FillRateMult;
        float drainRate =  Fish?.ChargeDrainRate ?? 18f;

        if (BobberOnTarget)
            ChargePct = Math.Min(100f, ChargePct + fillRate * dt);
        else
            ChargePct = Math.Max(0f, ChargePct - drainRate * dt);
    }

    private void FinishWithCurrentCharge(bool forced)
    {
        var f = Fish!;
        IsActive = false;
        IsComplete = true;

        if (ChargePct < f.MinChargePct)
        {
            Outcome = new FishingOutcome { Fled = true, Quality = 0, Shiny = false };
            return;
        }

        int quality;
        if (ChargePct < f.GoodChargePct) quality = 1;
        else if (ChargePct < f.PerfectChargePct) quality = 2;
        else quality = 3;

        bool shiny = quality == 3 && BobberOnTarget && !forced;
        Outcome = new FishingOutcome { Fled = false, Quality = quality, Shiny = shiny };
    }

    /// <summary>Wrap an angle to 0..360.</summary>
    public static float WrapDeg(float deg)
    {
        float r = deg % 360f;
        if (r < 0f) r += 360f;
        return r;
    }

    /// <summary>Signed shortest delta from <paramref name="from"/> to <paramref name="to"/> in degrees, range -180..180.</summary>
    public static float ShortestAngularDelta(float from, float to)
    {
        float d = WrapDeg(to - from);
        if (d > 180f) d -= 360f;
        return d;
    }
}
