using System;
using System.Collections.Generic;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Builds a <see cref="GameState"/> from live <see cref="ServiceContainer"/> services
/// and persists it via <see cref="SaveManager"/>. Single source of truth for auto-save --
/// replaces ad-hoc GameState construction in FarmScene.OnDayAdvanced and enables save
/// from non-Farm scenes (shop overlay close, manual F5, future scene exits).
/// </summary>
public static class GameStateSnapshot
{
    /// <summary>
    /// Flush current live state to disk. Safe to call from any scene; missing services
    /// are tolerated (skipped) so a partially-initialized boot does not crash.
    /// </summary>
    /// <param name="services">Active ServiceContainer.</param>
    /// <param name="farmCells">Farm grid data. When null, reuses services.GameState.FarmCells
    /// to avoid wiping farm state on a non-Farm save.</param>
    public static void SaveNow(ServiceContainer services, List<FarmCellSaveData>? farmCells = null)
    {
        var prior = services.GameState;
        var state = new GameState
        {
            DayNumber = services.Time.DayNumber,
            Season = services.Time.Season,
            GameTime = services.Time.GameTime,
            StaminaCurrent = services.Player?.Stats.CurrentStamina ?? (prior?.StaminaCurrent ?? 100f),
            PlayerX = services.Player?.Position.X ?? (prior?.PlayerX ?? 0f),
            PlayerY = services.Player?.Position.Y ?? (prior?.PlayerY ?? 0f),
            FarmCells = farmCells ?? prior?.FarmCells ?? new List<FarmCellSaveData>(),
            CurrentScene = prior?.CurrentScene ?? "Farm",
            BossKilled = prior?.BossKilled ?? false,
            // Pitfall 1 fix: previously SaveNow silently dropped Chests/Resources
            // (and never wrote DungeonState at all). Pull live snapshots from the
            // active scene's managers, falling back to prior persisted lists.
            Chests = services.ChestManager?.GetSaveData() ?? prior?.Chests ?? new List<ChestSaveData>(),
            Resources = services.ResourceManager?.GetSaveData() ?? prior?.Resources ?? new List<ResourceSaveData>(),
            Dungeon = services.Dungeon?.ToSnapshot() ?? prior?.Dungeon ?? new World.DungeonStateSnapshot(),
        };

        services.Inventory?.SaveToState(state);
        services.Quest?.SaveToState(state);

        Console.WriteLine(
            $"[GameStateSnapshot] SaveNow: day={state.DayNumber} gold={state.Gold} " +
            $"quest={state.QuestState} items={state.Inventory.Count} scene={state.CurrentScene} " +
            $"chests={state.Chests.Count} resources={state.Resources.Count} " +
            $"dungeonRooms={state.Dungeon.ClearedRooms.Count}");

        SaveManager.Save(state);
    }
}
