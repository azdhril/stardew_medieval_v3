namespace stardew_medieval_v3.Quest;

/// <summary>
/// State of the main quest. Values are explicitly assigned so they can be safely
/// round-tripped through <see cref="Core.GameState.QuestState"/> (int) in the save file.
/// </summary>
public enum MainQuestState
{
    /// <summary>Quest has not been offered/accepted yet.</summary>
    NotStarted = 0,

    /// <summary>Quest is active; objective not yet complete.</summary>
    Active = 1,

    /// <summary>Quest objective has been satisfied.</summary>
    Complete = 2
}
