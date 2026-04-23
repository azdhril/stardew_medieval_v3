using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using stardew_medieval_v3.Data;

namespace stardew_medieval_v3.Core;

/// <summary>
/// JSON-based save/load system.
/// </summary>
public static class SaveManager
{
    private const int CURRENT_SAVE_VERSION = 10;

    private static string SavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StardewMedieval",
        "savegame.json"
    );

    public static void Save(GameState state)
    {
        state.SaveVersion = CURRENT_SAVE_VERSION;

        var dir = Path.GetDirectoryName(SavePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(SavePath, json);
        Console.WriteLine($"[SaveManager] Saved day {state.DayNumber}");
    }

    public static GameState? Load()
    {
        if (!File.Exists(SavePath))
            return null;

        try
        {
            var json = File.ReadAllText(SavePath);
            var state = JsonSerializer.Deserialize<GameState>(json);
            if (state != null)
            {
                MigrateIfNeeded(state);
                Console.WriteLine($"[SaveManager] Loaded day {state.DayNumber}");
            }
            return state;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaveManager] Load failed: {ex.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private static void MigrateIfNeeded(GameState state)
    {
        if (state.SaveVersion < 2)
        {
            // v1 -> v2: farm cells added
            state.SaveVersion = 2;
            Console.WriteLine("[SaveManager] Migrated save from v1 to v2");
        }

        if (state.SaveVersion < 3)
        {
            // v2 -> v3: inventory, gold, xp, level, scene, quest, equipment, hotbar
            state.Inventory ??= new();
            state.Gold = 0;
            state.XP = 0;
            state.Level = 1;
            state.CurrentScene = "Farm";
            state.QuestState = 0;
            state.WeaponId = null;
            state.ArmorId = null;
            state.HotbarSlots ??= new List<string?>(new string?[8]);
            state.SaveVersion = 3;
            Console.WriteLine("[SaveManager] Migrated save from v2 to v3");
        }

        if (state.SaveVersion < 4)
        {
            // v3 -> v4: boss tracking
            state.BossKilled = false;
            state.SaveVersion = 4;
            Console.WriteLine("[SaveManager] Migrated save from v3 to v4");
        }

        if (state.SaveVersion < 5)
        {
            // v4 -> v5: MainQuestState now semantically backed by int QuestState (already present since v3).
            // No data reshape; existing 0=NotStarted/1=Active/2=Complete mapping is preserved.
            // Clamp to valid enum range so future casts are safe (T-04-01 mitigation).
            if (state.QuestState < 0 || state.QuestState > 2) state.QuestState = 0;
            state.SaveVersion = 5;
            Console.WriteLine("[SaveManager] Migrated save from v4 to v5 (MainQuestState normalization)");
        }

        if (state.SaveVersion < 6)
        {
            state.Chests ??= new();
            state.SaveVersion = 6;
            Console.WriteLine("[SaveManager] Migrated save from v5 to v6 (dynamic chests)");
        }

        if (state.SaveVersion < 7)
        {
            state.Resources ??= new();
            state.SaveVersion = 7;
            Console.WriteLine("[SaveManager] Migrated save from v6 to v7 (dynamic resources)");
        }

        if (state.SaveVersion < 8)
        {
            // v7 -> v8: dungeon run state default
            state.Dungeon ??= new World.DungeonStateSnapshot();
            state.SaveVersion = 8;
            Console.WriteLine("[SaveManager] Migrated v7->v8 (Dungeon defaults)");
        }

        if (state.SaveVersion < 9)
        {
            // v8 -> v9: progression stats derived from Level
            int level = Math.Max(1, state.Level);
            state.MaxHP = 100 + (level - 1) * 10;
            state.MaxStamina = 100 + (level - 1) * 5;
            state.BaseDamageBonus = level - 1;
            state.SaveVersion = 9;
            Console.WriteLine($"[SaveManager] Migrated v8->v9 (MaxHP={state.MaxHP}, MaxSta={state.MaxStamina}, DmgBonus={state.BaseDamageBonus})");
        }

        if (state.SaveVersion < 10)
        {
            // v9 -> v10: per-scene position. Legacy PlayerX/Y is implicitly Farm (boot
            // always dumped the player on the farm in v9 and earlier), so seed Farm
            // from those fields. Other scenes get spawn defaults on next entry.
            state.PositionByScene ??= new();
            if (!state.PositionByScene.ContainsKey("Farm"))
            {
                state.PositionByScene["Farm"] = new ScenePosition
                {
                    X = state.PlayerX,
                    Y = state.PlayerY,
                };
            }
            // Clamp a historical oddity: some v9 saves may have CurrentScene="Dungeon:*"
            // from a crash mid-run. Those scenes aren't restorable — normalize to Farm.
            if (!string.IsNullOrEmpty(state.CurrentScene) && state.CurrentScene.StartsWith("Dungeon:"))
            {
                Console.WriteLine($"[SaveManager] v9->v10: normalizing CurrentScene '{state.CurrentScene}' -> Farm");
                state.CurrentScene = "Farm";
            }
            state.SaveVersion = 10;
            Console.WriteLine($"[SaveManager] Migrated v9->v10 (PositionByScene seeded: Farm=({state.PlayerX},{state.PlayerY}))");
        }
    }
}
