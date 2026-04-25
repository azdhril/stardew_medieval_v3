using System.Collections.Generic;

namespace stardew_medieval_v3.Data;

/// <summary>
/// How the fish moves around the ring during the minigame. Drives the
/// pattern that the player has to track with the joystick (WASD).
/// </summary>
public enum FishBehavior
{
    /// <summary>Constant angular speed, occasional direction reversal — easy to predict.</summary>
    Smooth,
    /// <summary>Smooth motion punctuated by sudden stops + direction flips.</summary>
    Mixed,
    /// <summary>Teleports to a new angle on the ring without warning. High-tier fish.</summary>
    Dart,
    /// <summary>Vai-e-vem entre dois angulos proximos (small arc oscillation).</summary>
    Oscillate,
    /// <summary>
    /// Mostly dormant — drifts barely or stays still — then suddenly bursts
    /// around the ring at very high angular speed (≈150–210°) and stops on the
    /// opposite side. The lap is visible (not a teleport) but too fast for the
    /// joystick to follow without a pre-emptive sweep, making it the hardest
    /// pattern in the rotation.
    /// </summary>
    Sprint,
}

/// <summary>
/// Biome where the fish lives. Drives which water tiles can roll the fish.
/// River = flowing water, Lake = still freshwater body, Ocean = saltwater,
/// Pond = small enclosed water, Swamp = murky reserved for future content.
/// </summary>
public enum FishBiome { River, Lake, Ocean, Pond, Swamp }

/// <summary>
/// Static fishing-specific data for a fish item. The vendable item entry
/// (Id/Name/Rarity/BasePrice/SpriteId) lives in items.json — this DTO only
/// holds the parameters used by the fishing roll + minigame so the two
/// concerns stay separated.
/// </summary>
public class FishData
{
    /// <summary>Item id — matches an entry in items.json.</summary>
    public string Id { get; set; } = "";

    /// <summary>Column index in fish_all.png (10 cols × 10 rows of 16×16 cells).</summary>
    public int SpriteCol { get; set; }

    /// <summary>Row index in fish_all.png.</summary>
    public int SpriteRow { get; set; }

    /// <summary>Where this fish can be caught.</summary>
    public FishBiome Biome { get; set; } = FishBiome.River;

    /// <summary>Seasons the fish appears in. Empty/null = any season.</summary>
    public List<string> Seasons { get; set; } = new();

    /// <summary>"Day", "Night" or "Any". Anything other value treated as Any.</summary>
    public string TimeOfDay { get; set; } = "Any";

    /// <summary>"Sun", "Rain" or "Any". Anything else treated as Any.</summary>
    public string Weather { get; set; } = "Any";

    /// <summary>Minimum water tiles surrounding the cast tile (anti pesca em poça).</summary>
    public int MinDepth { get; set; } = 1;

    /// <summary>Loot-table weight when rolling against the candidate set.</summary>
    public int Weight { get; set; } = 100;

    /// <summary>Movement pattern the fish uses on the ring.</summary>
    public FishBehavior Behavior { get; set; } = FishBehavior.Smooth;

    /// <summary>Angular speed the fish moves on the ring, in degrees/second.</summary>
    public float RingSpeed { get; set; } = 60f;

    /// <summary>Width (degrees) of the on-target arc — narrower = harder to track.</summary>
    public float ToleranceArc { get; set; } = 35f;

    /// <summary>Charge % required to land at all (anything less = fish escapes).</summary>
    public float MinChargePct { get; set; } = 30f;

    /// <summary>Charge % threshold for a 2-star catch.</summary>
    public float GoodChargePct { get; set; } = 65f;

    /// <summary>Charge % threshold for a 3-star catch.</summary>
    public float PerfectChargePct { get; set; } = 95f;

    /// <summary>Window in seconds during the bite "!" prompt to react before fleeing.</summary>
    public float BiteWindow { get; set; } = 1.2f;

    /// <summary>Total minigame duration in seconds — patience timer at the bottom.</summary>
    public float MinigameDuration { get; set; } = 16f;

    /// <summary>
    /// Charge fill rate (%/sec) while the joystick is leaning into the tolerance arc.
    /// Higher = easier to charge, useful for beginner fish. Default 35 matches the
    /// historical hard-coded rate so unspecified fish behave like before.
    /// </summary>
    public float ChargeFillRate { get; set; } = 35f;

    /// <summary>
    /// Charge drain rate (%/sec) while the joystick is neutral or off-target.
    /// Higher = punishes losing the fish more harshly (rare/hard fish); lower =
    /// forgiving (beginner fish that let the player recover). Default 18 matches
    /// the historical hard-coded rate.
    /// </summary>
    public float ChargeDrainRate { get; set; } = 18f;
}
