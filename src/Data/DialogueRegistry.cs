using System.Collections.Generic;
using stardew_medieval_v3.Quest;

namespace stardew_medieval_v3.Data;

/// <summary>
/// Static registry mapping (npcId, MainQuestState) to dialogue line arrays.
/// Keys are lowercase NPC identifiers. Missing keys return a single "..." line
/// (never null/empty). All dialogues kept to 1-3 lines, each ≤80 chars
/// (UI-SPEC §Copywriting).
/// </summary>
public static class DialogueRegistry
{
    private static readonly Dictionary<(string, MainQuestState), string[]> _lines = new()
    {
        // ── King ────────────────────────────────────────────────────────────
        [("king", MainQuestState.NotStarted)] = new[]
        {
            "Brave traveler, the realm is in peril.",
            "Dark creatures have overrun the old catacombs.",
            "I charge you: clear the dungeon and bring peace to my people.",
        },
        [("king", MainQuestState.Active)] = new[]
        {
            "The dungeon still festers with evil, hero.",
            "Return to me only when the beasts lie slain.",
        },
        [("king", MainQuestState.Complete)] = new[]
        {
            "You have done it! The catacombs fall silent once more.",
            "The kingdom owes you a debt beyond gold.",
        },

        // ── Shopkeeper ──────────────────────────────────────────────────────
        [("shopkeeper", MainQuestState.NotStarted)] = new[]
        {
            "Welcome, friend! Seeds, potions, or a sharp blade?",
            "Coin talks louder than my chatter. Have a look.",
        },
        [("shopkeeper", MainQuestState.Active)] = new[]
        {
            "Heading for the catacombs? You'll want a potion or three.",
            "I stock what the King's hero needs. Browse freely.",
        },
        [("shopkeeper", MainQuestState.Complete)] = new[]
        {
            "The hero returns! The whole village drinks to your name.",
            "Discount? Bah. Buy what you like, on me today.",
        },
    };

    /// <summary>
    /// Returns the dialogue lines for the given NPC and quest state.
    /// Returns a single-line fallback [ "..." ] if the key is not registered.
    /// </summary>
    /// <param name="npcId">NPC identifier (case-insensitive).</param>
    /// <param name="state">Current main quest state.</param>
    public static string[] Get(string npcId, MainQuestState state)
    {
        return _lines.TryGetValue((npcId.ToLowerInvariant(), state), out var lines)
            ? lines
            : new[] { "..." };
    }
}
