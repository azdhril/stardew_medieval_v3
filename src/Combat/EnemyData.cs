using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Static data definition for an enemy type. Immutable after construction.
/// Contains all stats, visual info, AI parameters, and loot for one enemy archetype.
/// </summary>
/// <param name="Id">Unique identifier for this enemy type.</param>
/// <param name="Name">Display name.</param>
/// <param name="PlaceholderColor">Color used for placeholder rectangle rendering.</param>
/// <param name="Width">Sprite/collision width in pixels.</param>
/// <param name="Height">Sprite/collision height in pixels.</param>
/// <param name="MaxHP">Maximum hit points.</param>
/// <param name="MoveSpeed">Movement speed in pixels per second.</param>
/// <param name="DetectionRange">Distance in pixels at which enemy detects player.</param>
/// <param name="AttackRange">Distance in pixels at which enemy can attack.</param>
/// <param name="AttackDamage">Raw damage per attack.</param>
/// <param name="AttackCooldown">Seconds between attacks.</param>
/// <param name="KnockbackResistance">0 = full knockback, 1 = immune. Scales the 32px base distance.</param>
/// <param name="IsRanged">True if enemy attacks with projectiles instead of melee.</param>
/// <param name="ProjectileSpeed">Speed of projectiles in pixels/second (0 if melee).</param>
/// <param name="Loot">Loot table defining possible drops on death.</param>
public record EnemyData(
    string Id,
    string Name,
    Color PlaceholderColor,
    int Width,
    int Height,
    float MaxHP,
    float MoveSpeed,
    float DetectionRange,
    float AttackRange,
    float AttackDamage,
    float AttackCooldown,
    float KnockbackResistance,
    bool IsRanged,
    float ProjectileSpeed,
    LootTable Loot
);

/// <summary>
/// Static registry of all enemy type definitions.
/// Per D-16/D-17/D-18: Skeleton (melee rusher), DarkMage (ranged keeper), Golem (slow tank).
/// </summary>
public static class EnemyRegistry
{
    /// <summary>
    /// Get all enemy type definitions.
    /// </summary>
    /// <returns>List of all enemy data definitions.</returns>
    public static List<EnemyData> GetAll()
    {
        return new List<EnemyData>
        {
            // D-16: Skeleton - fast melee rusher, low HP
            new EnemyData(
                Id: "Skeleton",
                Name: "Skeleton",
                PlaceholderColor: Color.White,
                Width: 16,
                Height: 16,
                MaxHP: 40f,
                MoveSpeed: 60f,
                DetectionRange: 120f,
                AttackRange: 24f,
                AttackDamage: 10f,
                AttackCooldown: 1.0f,
                KnockbackResistance: 0f,
                IsRanged: false,
                ProjectileSpeed: 0f,
                Loot: new LootTable(new List<LootDrop>
                {
                    new LootDrop("Bones", 0.8f)
                })
            ),

            // D-17: Dark Mage - ranged attacker, keeps distance, fires every 3s
            new EnemyData(
                Id: "DarkMage",
                Name: "Dark Mage",
                PlaceholderColor: new Color(128, 0, 128),
                Width: 16,
                Height: 16,
                MaxHP: 30f,
                MoveSpeed: 30f,
                DetectionRange: 160f,
                AttackRange: 100f,
                AttackDamage: 12f,
                AttackCooldown: 3.0f,
                KnockbackResistance: 0f,
                IsRanged: true,
                ProjectileSpeed: 150f,
                Loot: new LootTable(new List<LootDrop>
                {
                    new LootDrop("Mana_Crystal", 0.7f)
                })
            ),

            // D-18: Golem - slow tank, high HP/damage, resists knockback
            new EnemyData(
                Id: "Golem",
                Name: "Golem",
                PlaceholderColor: Color.SaddleBrown,
                Width: 20,
                Height: 20,
                MaxHP: 120f,
                MoveSpeed: 20f,
                DetectionRange: 80f,
                AttackRange: 24f,
                AttackDamage: 20f,
                AttackCooldown: 2.0f,
                KnockbackResistance: 0.75f,
                IsRanged: false,
                ProjectileSpeed: 0f,
                Loot: new LootTable(new List<LootDrop>
                {
                    new LootDrop("Stone_Chunk", 0.9f),
                    new LootDrop("Steel_Sword", 0.1f)
                })
            )
        };
    }
}
