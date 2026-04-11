using System;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.Data;
using stardew_medieval_v3.Inventory;
using stardew_medieval_v3.Player;

namespace stardew_medieval_v3.Combat;

/// <summary>
/// Coordinates all player combat: melee attacks, fireball casting, damage calculation,
/// i-frames, and input handling. Owns MeleeAttack instance and fireball cooldown state.
/// </summary>
public class CombatManager
{
    private readonly InventoryManager _inventory;
    private readonly MeleeAttack _melee = new();

    // I-frame fields (per D-15: 1s immunity after hit)
    private float _iFrameTimer;
    private const float IFrameDuration = 1.0f;

    // Fireball cooldown (per D-09: 2s cooldown)
    private float _fireballCooldownTimer;
    private const float FireballCooldown = 2.0f;

    // Flag set when fireball should be spawned this frame
    private bool _fireballRequested;

    /// <summary>True while the player is invulnerable from i-frames.</summary>
    public bool IsPlayerInvulnerable => _iFrameTimer > 0;

    /// <summary>Current i-frame timer value.</summary>
    public float IFrameTimer => _iFrameTimer;

    /// <summary>Remaining fireball cooldown in seconds.</summary>
    public float FireballCooldownRemaining => _fireballCooldownTimer;

    /// <summary>Maximum fireball cooldown duration.</summary>
    public float FireballCooldownMax => FireballCooldown;

    /// <summary>Access the melee attack component.</summary>
    public MeleeAttack Melee => _melee;

    /// <summary>True if a fireball was requested this frame (consumed after reading).</summary>
    public bool ConsumeFireballRequest()
    {
        if (_fireballRequested)
        {
            _fireballRequested = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Create a new CombatManager.
    /// </summary>
    /// <param name="inventory">Player inventory for weapon/equipment lookups.</param>
    public CombatManager(InventoryManager inventory)
    {
        _inventory = inventory;
    }

    /// <summary>
    /// Process combat input: LMB for melee, RMB for fireball.
    /// Per D-01/D-08: melee requires weapon in active hotbar slot.
    /// Per D-08/D-09: fireball has fixed 2s cooldown.
    /// </summary>
    /// <param name="input">Input manager for mouse click detection.</param>
    /// <param name="player">Player entity (skipped if not alive).</param>
    public void HandleInput(InputManager input, PlayerEntity player)
    {
        if (!player.IsAlive) return;

        // LMB: Melee attack
        if (input.IsLeftClickPressed)
        {
            var activeItem = _inventory.GetActiveHotbarItem();
            if (activeItem != null)
            {
                var itemDef = ItemRegistry.Get(activeItem.ItemId);
                if (itemDef?.Type == ItemType.Weapon)
                {
                    // Get weapon cooldown from stats, fallback 0.5s
                    float cooldown = 0.5f;
                    if (itemDef.Stats.TryGetValue("cooldown", out float cd))
                        cooldown = cd;

                    if (_melee.TrySwing(cooldown))
                        Console.WriteLine($"[CombatManager] Melee swing with {itemDef.Id}");
                }
            }
        }

        // RMB: Fireball
        if (input.IsRightClickPressed)
        {
            if (_fireballCooldownTimer <= 0)
            {
                _fireballCooldownTimer = FireballCooldown;
                _fireballRequested = true;
                Console.WriteLine("[CombatManager] Fireball cast!");
            }
        }
    }

    /// <summary>
    /// Calculate melee damage from active weapon + equipment attack bonus.
    /// Per D-06: weapon base damage + equipment attack stat.
    /// </summary>
    /// <returns>Total melee damage.</returns>
    public float CalculateMeleeDamage()
    {
        float weaponDamage = 0f;

        var activeItem = _inventory.GetActiveHotbarItem();
        if (activeItem != null)
        {
            var itemDef = ItemRegistry.Get(activeItem.ItemId);
            if (itemDef?.Stats != null && itemDef.Stats.TryGetValue("damage", out float dmg))
                weaponDamage = dmg;
        }

        var (attack, _) = EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment());
        return weaponDamage + attack;
    }

    /// <summary>
    /// Try to apply damage to the player, respecting i-frames and defense.
    /// Per D-15: if i-frames active, damage is ignored. Otherwise applies defense reduction.
    /// </summary>
    /// <param name="player">Player entity to damage.</param>
    /// <param name="rawDamage">Raw incoming damage before defense.</param>
    /// <returns>True if damage was applied.</returns>
    public bool TryPlayerTakeDamage(PlayerEntity player, float rawDamage)
    {
        if (_iFrameTimer > 0) return false;

        var (_, defense) = EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment());
        float damage = Math.Max(1f, rawDamage - defense);

        if (player.TakeDamage(damage))
        {
            _iFrameTimer = IFrameDuration;
            player.IFrameTimer = IFrameDuration;
            Console.WriteLine($"[CombatManager] Player took {damage:F0} damage (raw={rawDamage:F0}, def={defense:F0}). HP: {player.HP:F0}/{player.MaxHP:F0}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update combat timers (melee, i-frames, fireball cooldown).
    /// </summary>
    /// <param name="deltaTime">Frame time in seconds.</param>
    public void Update(float deltaTime)
    {
        _melee.Update(deltaTime);

        if (_iFrameTimer > 0)
            _iFrameTimer -= deltaTime;

        if (_fireballCooldownTimer > 0)
            _fireballCooldownTimer -= deltaTime;
    }
}
