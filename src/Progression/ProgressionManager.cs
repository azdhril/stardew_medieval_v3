using System;
using System.Collections.Generic;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Progression;

/// <summary>
/// Tracks player XP, handles level-up logic with stat increases,
/// and provides gold rolling for enemy kills. Central progression hub.
/// </summary>
public class ProgressionManager
{
    private readonly PlayerEntity _player;
    private readonly PlayerStats _stats;

    /// <summary>Current XP toward next level (resets on level-up).</summary>
    public int XP { get; private set; }

    /// <summary>Current player level (starts at 1).</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Flat bonus damage added to melee attacks per level gained.</summary>
    public int BaseDamageBonus { get; private set; } = 0;

    /// <summary>XP required for the current level to advance.</summary>
    public int XPToNext => XPTable.XPToNextLevel(Level);

    /// <summary>Fired with the new level number when the player levels up.</summary>
    public event Action<int>? OnLevelUp;

    /// <summary>Fired with the XP amount each time XP is awarded.</summary>
    public event Action<int>? OnXPGained;

    /// <summary>Gold base amounts per enemy type. Keys match EnemyData.Id.</summary>
    private static readonly Dictionary<string, int> GoldPerEnemy = new()
    {
        { "Skeleton", 5 },
        { "DarkMage", 8 },
        { "Golem", 15 },
        { "SkeletonKing", 100 }
    };

    public ProgressionManager(PlayerEntity player, PlayerStats stats)
    {
        _player = player;
        _stats = stats;
    }

    /// <summary>
    /// Award XP for killing an enemy. Triggers level-up(s) if threshold met.
    /// Unknown enemies default to 5 XP.
    /// </summary>
    /// <param name="enemyId">EnemyData.Id of the killed enemy.</param>
    public void AwardXP(string enemyId)
    {
        int amount = XPTable.XPPerEnemy.TryGetValue(enemyId, out int xp) ? xp : 5;
        XP += amount;
        OnXPGained?.Invoke(amount);
        Console.WriteLine($"[Progression] +{amount} XP from {enemyId} (total: {XP}/{XPToNext})");

        while (XP >= XPToNext && Level < 100)
        {
            XP -= XPToNext;
            Level++;

            // Push stat increases per D-04
            _player.MaxHP += 10;
            _player.HP = _player.MaxHP;
            _stats.MaxStamina += 5;
            _stats.RestoreStamina();
            BaseDamageBonus++;

            Console.WriteLine(
                $"[Progression] Level up! Now level {Level} " +
                $"(MaxHP={_player.MaxHP}, MaxSta={_stats.MaxStamina}, DmgBonus={BaseDamageBonus})");
            OnLevelUp?.Invoke(Level);
        }
    }

    /// <summary>
    /// Restore progression state from a saved GameState (v9+).
    /// Also pushes MaxHP/MaxStamina to the live player/stats objects.
    /// </summary>
    public void LoadFromState(GameState state)
    {
        XP = state.XP;
        Level = Math.Max(1, state.Level);
        BaseDamageBonus = state.BaseDamageBonus;

        _player.MaxHP = state.MaxHP;
        _player.HP = _player.MaxHP;
        _stats.MaxStamina = state.MaxStamina;

        Console.WriteLine(
            $"[Progression] Loaded: Level={Level}, XP={XP}, " +
            $"MaxHP={_player.MaxHP}, MaxSta={_stats.MaxStamina}, DmgBonus={BaseDamageBonus}");
    }

    /// <summary>
    /// Persist progression state into a GameState for serialization.
    /// </summary>
    public void SaveToState(GameState state)
    {
        state.XP = XP;
        state.Level = Level;
        state.BaseDamageBonus = BaseDamageBonus;
        state.MaxHP = (int)_player.MaxHP;
        state.MaxStamina = (int)_stats.MaxStamina;
    }

    /// <summary>
    /// Roll a gold coin drop amount for the given enemy type.
    /// Applies +/-30% variance, minimum 1 gold.
    /// </summary>
    /// <param name="enemyId">EnemyData.Id of the killed enemy.</param>
    /// <param name="rng">Random instance for variance.</param>
    /// <returns>Gold coin quantity to drop.</returns>
    public static int RollGold(string enemyId, Random rng)
    {
        int baseGold = GoldPerEnemy.TryGetValue(enemyId, out int g) ? g : 3;
        int variance = (int)(baseGold * 0.3f);
        int result = baseGold + rng.Next(-variance, variance + 1);
        return Math.Max(1, result);
    }
}
