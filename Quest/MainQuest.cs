using System;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Quest;

/// <summary>
/// Container for the single main quest. Tracks <see cref="MainQuestState"/>,
/// exposes Activate/Complete transitions, and round-trips through
/// <see cref="GameState.QuestState"/> for save/load.
/// </summary>
public class MainQuest
{
    /// <summary>Current state of the quest. Starts as <see cref="MainQuestState.NotStarted"/>.</summary>
    public MainQuestState State { get; private set; } = MainQuestState.NotStarted;

    /// <summary>Fires after a state transition with the new state value.</summary>
    public event Action<MainQuestState>? OnQuestStateChanged;

    /// <summary>
    /// Transition NotStarted -> Active. No-op if the quest is already Active or Complete.
    /// </summary>
    public void Activate()
    {
        if (State != MainQuestState.NotStarted) return;
        State = MainQuestState.Active;
        Console.WriteLine("[MainQuest] Activated");
        OnQuestStateChanged?.Invoke(State);
    }

    /// <summary>
    /// Transition to Complete. No-op if already complete. Works from either NotStarted or Active
    /// so dev/debug hooks in Phase 4 can fast-forward the quest.
    /// </summary>
    public void Complete()
    {
        if (State == MainQuestState.Complete) return;
        State = MainQuestState.Complete;
        Console.WriteLine("[MainQuest] Completed");
        OnQuestStateChanged?.Invoke(State);
    }

    /// <summary>Hydrate quest state from a deserialized <see cref="GameState"/>.</summary>
    public void LoadFromState(GameState state)
    {
        int raw = state.QuestState;
        if (raw < 0 || raw > 2) raw = 0;
        State = (MainQuestState)raw;
    }

    /// <summary>Persist current quest state into a <see cref="GameState"/> prior to save.</summary>
    public void SaveToState(GameState state)
    {
        state.QuestState = (int)State;
    }
}
