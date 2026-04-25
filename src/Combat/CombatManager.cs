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
    private const float MeleeHitStaminaCost = 4f;
    private const float SpellHitStaminaCost = 6f;
    private const float PunchCooldown = 0.7f;
    private const float PunchDamage = 3f;

    private readonly InventoryManager _inventory;
    private readonly MeleeAttack _melee = new();

    // I-frame fields (per D-15: 1s immunity after hit)
    private float _iFrameTimer;
    private const float IFrameDuration = 1.0f;

    // Fireball cooldown. Duration is read from the equipped staff's "cooldown" stat
    // (fallback 2s if unset). Tracks both the current timer and the max used for the
    // last cast, so the HUD can draw a progress arc relative to the actual duration.
    private float _fireballCooldownTimer;
    private float _fireballCooldownMax = 2.0f;
    private const float FireballCooldownDefault = 2.0f;

    // Flag set when fireball should be spawned this frame
    private bool _fireballRequested;

    /// <summary>True while the player is invulnerable from i-frames.</summary>
    public bool IsPlayerInvulnerable => _iFrameTimer > 0;

    /// <summary>Current i-frame timer value.</summary>
    public float IFrameTimer => _iFrameTimer;

    /// <summary>Remaining fireball cooldown in seconds.</summary>
    public float FireballCooldownRemaining => _fireballCooldownTimer;

    /// <summary>Maximum fireball cooldown duration for the last cast (depends on equipped staff).</summary>
    public float FireballCooldownMax => _fireballCooldownMax;

    /// <summary>Flat damage bonus from progression (leveling). Added to melee damage.</summary>
    public int DamageBonus { get; set; } = 0;

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
    /// Process combat input. Stardew-style: the active hotbar item defines the action.
    /// A weapon tagged "melee" (sword) swings on LMB; a weapon tagged "spell" (staff)
    /// casts a fireball on LMB using its own "cooldown" stat. Only one weapon is active
    /// at a time — there is no separate melee/spell keybind split.
    /// </summary>
    /// <param name="input">Input manager for mouse click detection.</param>
    /// <param name="player">Player entity (skipped if not alive).</param>
    public void HandleInput(InputManager input, PlayerEntity player)
    {
        if (!player.IsAlive) return;
        if (!input.IsLeftClickPressed) return;

        var activeItem = _inventory.GetActiveHotbarItem();
        var itemDef = activeItem != null ? ItemRegistry.Get(activeItem.ItemId) : null;
        bool isWeapon = itemDef?.Type == ItemType.Weapon;

        if (!isWeapon)
        {
            // Only swing for an empty hotbar slot (a punch) or for tools that
            // double as bludgeons (hoe/axe/pickaxe/scythe/hammer). Fishing rod,
            // watering can, seeds, consumables, etc. should be silent on LMB so
            // their dedicated action (cast, water, plant) keeps the click.
            if (!CanSwingUnarmed(activeItem, itemDef)) return;

            if (_melee.TrySwing(PunchCooldown))
                Console.WriteLine($"[CombatManager] Bludgeon ({(activeItem == null ? "fist" : itemDef!.Id)})");
            return;
        }

        bool isSpell = itemDef!.Stats.TryGetValue("spell", out float s) && s > 0;

        if (isSpell)
        {
            if (_fireballCooldownTimer <= 0)
            {
                float cd = itemDef.Stats.TryGetValue("cooldown", out float cdv) ? cdv : FireballCooldownDefault;
                _fireballCooldownMax = cd;
                _fireballCooldownTimer = cd;
                _fireballRequested = true;
                Console.WriteLine($"[CombatManager] Fireball cast with {itemDef.Id} (cd={cd:F1}s)");
            }
        }
        else
        {
            // Melee (default for any Weapon without spell flag)
            float swingCd = itemDef.Stats.TryGetValue("cooldown", out float scd) ? scd : 0.5f;
            if (_melee.TrySwing(swingCd))
                Console.WriteLine($"[CombatManager] Melee swing with {itemDef.Id}");
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
            if (itemDef?.Type == ItemType.Weapon && itemDef.Stats.TryGetValue("damage", out float dmg))
                weaponDamage = dmg;
        }

        if (weaponDamage <= 0f)
            weaponDamage = PunchDamage;

        var (attack, _) = EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment());
        return weaponDamage + attack + DamageBonus;
    }

    /// <summary>Consumes stamina when a melee hit actually connects.</summary>
    public void OnPlayerMeleeHit(PlayerEntity player)
    {
        player.Stats.SpendStamina(MeleeHitStaminaCost);
    }

    /// <summary>Consumes stamina when a player spell projectile connects.</summary>
    public void OnPlayerSpellHit(PlayerEntity player)
    {
        player.Stats.SpendStamina(SpellHitStaminaCost);
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
    /// True when the active hotbar slot can attack on LMB without holding a Weapon:
    /// either the slot is empty (a punch) or the held item is a tool that reads as
    /// a blunt instrument in-fiction (hoe/axe/pickaxe/scythe/hammer). Fishing rod,
    /// watering can, seeds, consumables and other "use" items return false so their
    /// dedicated LMB action (cast/water/plant/use) is the sole click handler.
    /// </summary>
    private static bool CanSwingUnarmed(ItemStack? activeItem, ItemDefinition? itemDef)
    {
        if (activeItem == null) return true; // empty slot → fist
        if (itemDef == null || itemDef.Type != ItemType.Tool) return false;
        return itemDef.Id is "Hoe" or "Axe" or "Pickaxe" or "Scythe" or "Hammer";
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
