using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Small testable helper encapsulating the rule "should the boss entity be
/// spawned on this scene load?". Extracted from <see cref="stardew_medieval_v3.Scenes.DungeonScene"/>
/// so unit tests can verify re-entry after victory does not respawn the boss.
///
/// Rule: spawn the boss once per lifetime of a <see cref="DungeonState.BossDefeated"/>
/// flag set to false. Once defeated, the milestone persists across runs (by design,
/// see Phase 05 Plan 03 Task 2 decision: D-14 "derrotar o boss = dungeon completa"
/// is a terminal state and <see cref="DungeonState.BeginRun"/> must not clear it).
/// </summary>
public static class BossSpawnGate
{
    /// <summary>True if the boss should spawn on entry given current run state.</summary>
    /// <param name="state">Live dungeon state; a null state is treated as "never defeated".</param>
    public static bool ShouldSpawn(DungeonState? state) =>
        state == null || !state.BossDefeated;
}
