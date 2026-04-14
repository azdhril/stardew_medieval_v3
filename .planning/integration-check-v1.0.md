# Integration Check - Milestone v1.0 (Phases 1-3)

**Date:** 2026-04-12
**Scope:** Phases 1 (Architecture), 2 (Items/Inventory), 3 (Combat)
**Verdict:** **OK**

## Executive Summary

All cross-phase wiring between Phase 1, 2, and 3 is connected and functional. The E2E loop (farm -> harvest -> inventory -> equip -> fight -> loot -> pickup -> save) is intact end-to-end. No orphaned exports, no missing consumers, no broken flows identified within the scope.

## Wiring Summary

- **Connected:** 6/6 key cross-phase exports used by downstream consumers
- **Orphaned:** 0
- **Missing:** 0
- **E2E Flows Complete:** 1/1 (core gameplay loop)

## Integration Findings

### 1. Entity base class consumed by subclasses - WIRED
**REQ-ID:** ARCH-02

| Consumer | File | Evidence |
|---|---|---|
| PlayerEntity | src/Player/PlayerEntity.cs:14 | class PlayerEntity : Entity |
| DummyNpc | src/Entities/DummyNpc.cs:13 | class DummyNpc : Entity |
| EnemyEntity | src/Combat/EnemyEntity.cs:13 | class EnemyEntity : Entity |
| BossEntity | src/Combat/BossEntity.cs | extends EnemyEntity transitively |
| ItemDropEntity | src/Entities/ItemDropEntity.cs:15 | class ItemDropEntity : Entity |

Phase 3 combat methods (TakeDamage, ApplyKnockback, IFrameTimer, HP, MaxHP) live on the Phase 1 Entity base and are reused by enemies, boss, and player. Confirmed via CombatManager.TryPlayerTakeDamage (line 144) calling player.TakeDamage(...).

### 2. ItemRegistry + items.json feeds InventoryManager + LootTable - WIRED
**REQ-IDs:** ARCH-03, INV-01, INV-04, CMB-04

- Init: FarmScene.LoadContent:79 calls ItemRegistry.Initialize() loading src/Data/items.json (45 items per SUMMARY).
- InventoryManager consumption: 7 call sites in src/Inventory/InventoryManager.cs (lines 50, 134, 240, 280, 325, 347, 369) look up ItemRegistry.Get(itemId) for StackLimit, Rarity, Type, sprite, EquipSlot.
- LootTable consumption: EnemyData.cs:40,73,95,117 defines loot drops by ItemId string. LootTable.Roll returns (itemId, quantity) pairs resolved against ItemRegistry when ItemDropEntity renders (ItemDropEntity.cs:69).
- Combat consumption: CombatManager.cs:84,121 resolves active hotbar item via ItemRegistry.Get(...) to read Stats[damage], Stats[cooldown], Stats[spell].

### 3. InventoryManager persists via GameState/SaveManager - WIRED
**REQ-IDs:** ARCH-04, INV-01, INV-02, INV-03

- InventoryManager.LoadFromState(GameState) (InventoryManager.cs:399) reads state.Inventory, state.Equipment, state.HotbarSlots.
- InventoryManager.SaveToState(GameState) (InventoryManager.cs:445) writes all three back.
- FarmScene.LoadContent:113-121 calls SaveManager.Load() then _inventory.LoadFromState(save).
- FarmScene.OnDayAdvanced:648-649 calls _inventory.SaveToState(state) then SaveManager.Save(state).
- Migration ladder v1->v2->v3->v4 covered in SaveManager.MigrateIfNeeded (lines 67-99). v3 adds inventory/equipment/hotbar; v4 adds BossKilled.

### 4. EquipmentData stat bonuses read by CombatManager - WIRED
**REQ-IDs:** INV-03, CMB-01, CMB-02

- CombatManager.CalculateMeleeDamage:126 calls EquipmentData.GetEquipmentStats(_inventory.GetAllEquipment()) and adds attack to weapon damage.
- CombatManager.TryPlayerTakeDamage:141 uses defense to reduce incoming damage (Math.Max(1f, rawDamage - defense)).
- EquipmentData.cs:21 resolves each equipped itemId via ItemRegistry.Get to aggregate Stats[attack]/Stats[defense].

### 5. Enemy loot drops -> ItemDropEntity -> InventoryManager pickup - WIRED
**REQ-IDs:** INV-05, CMB-03, CMB-04

Full flow verified in src/Scenes/FarmScene.cs:
- Death detection: FarmScene.cs:299 checks !enemy.IsAlive.
- Loot roll: FarmScene.cs:301 calls enemy.Data.Loot.Roll(_lootRng).
- Drop spawn: FarmScene.cs:304,580-583 calls SpawnItemDrop(itemId, quantity, enemy.Position) creating new ItemDropEntity added to _itemDrops.
- Magnetism + pickup: FarmScene.cs:387 calls _itemDrops[i].UpdateWithPlayer(deltaTime, _player.GetFootPosition(), _inventory). Inside ItemDropEntity the drop calls _inventory.TryAdd(itemId, quantity) and sets IsCollected.
- Boss path: Same flow for boss (FarmScene.cs:356-362), plus BossKilled = true flag set for save.

### 6. E2E Flow: farm -> harvest -> inventory -> equip -> fight -> loot -> pickup - WIRED
**REQ-IDs:** FARM-01, FARM-02, FARM-03, INV-01, INV-02, INV-03, INV-05, CMB-01, CMB-02, CMB-03, CMB-04

| Step | File:Line | Status |
|---|---|---|
| 1. Tile targeting (Scythe/Hoe/Can) | src/Farming/ToolController.cs:107 | WIRED |
| 2. Harvest spawns drop | src/Farming/ToolController.cs:181-186 -> FarmScene.SpawnItemDrop | WIRED |
| 3. Drop magnetism picks up into inv | ItemDropEntity.UpdateWithPlayer -> InventoryManager.TryAdd | WIRED |
| 4. Open InventoryScene (I key) | FarmScene.cs:220 new InventoryScene(Services, _inventory, _spriteAtlas, _hotbar) | WIRED |
| 5. Equip weapon (drag to slot) | InventoryGridRenderer.cs:176-205 routes slot -> equipment | WIRED |
| 6. Equipment modifies damage | CombatManager.CalculateMeleeDamage via EquipmentData.GetEquipmentStats | WIRED |
| 7. Swing / cast via hotbar | CombatManager.HandleInput:81-107 (sword vs staff via Stats[spell]) | WIRED |
| 8. Fireball spawned | FarmScene.cs:243-245 _combat.ConsumeFireballRequest then _projectiles.SpawnFireball | WIRED |
| 9. Enemy HP / knockback | EnemyEntity.TakeDamage (inherits Entity) + ApplyKnockback | WIRED |
| 10. Loot drop on death | FarmScene.cs:298-308 | WIRED |
| 11. Auto-pickup to inventory | Step 3 same path | WIRED |
| 12. Save on day advance | FarmScene.OnDayAdvanced:636-649 | WIRED |

## Requirements Integration Map

| Requirement | Integration Path | Status | Issue |
|---|---|---|---|
| ARCH-01 SceneManager | src/Core/SceneManager.cs pushed by FarmScene (InventoryScene/PauseScene/TestScene) | WIRED | - |
| ARCH-02 Entity base | src/Core/Entity.cs consumed by PlayerEntity, DummyNpc, EnemyEntity, BossEntity, ItemDropEntity | WIRED | - |
| ARCH-03 ItemDefinition/Registry | src/Data/ItemRegistry consumed by InventoryManager, CombatManager, ItemDropEntity, EquipmentData, ToolController, renderers | WIRED | - |
| ARCH-04 GameState v3 | src/Core/GameState consumed by InventoryManager.LoadFromState/SaveToState, SaveManager.Migrate | WIRED | - |
| ARCH-05 Game1 refactor | Game1.cs -> SceneManager -> FarmScene | WIRED | - |
| FARM-01 Tile targeting | ToolController consumes PlayerEntity facing + position | WIRED | - |
| FARM-02 Harvest sprites | SpriteAtlas (Phase 2) consumed by ItemDropEntity/renderers | WIRED | - |
| FARM-03 Farming -> inventory | ToolController.TryHarvest -> SpawnItemDrop -> InventoryManager.TryAdd | WIRED | - |
| INV-01 20-slot grid | InventoryManager.SlotCount/_slots consumed by UI + save/load | WIRED | - |
| INV-02 Hotbar 1-8 | _hotbarRefs[8] consumed by HotbarRenderer + save (state.HotbarSlots) | WIRED | - |
| INV-03 Equipment slots | 7 named EquipSlots in InventoryGridRenderer via _inventory.GetEquipped + save (state.Equipment) | WIRED | - |
| INV-04 Rarity | ItemDefinition.Rarity rendered by HotbarRenderer/InventoryGridRenderer tint | WIRED | - |
| INV-05 Magnetic drops | ItemDropEntity.UpdateWithPlayer(..., inventory) -> TryAdd | WIRED | - |
| HUD-02 Inventory UI | InventoryScene overlay pushed from FarmScene key handler | WIRED | - |
| CMB-01 Melee | MeleeAttack via CombatManager.HandleInput + FarmScene hitbox checks vs enemies/boss | WIRED | - |
| CMB-02 Projectile magic | ProjectileManager.SpawnFireball via CombatManager.ConsumeFireballRequest, damage from weapon stats | WIRED | - |
| CMB-03 HP bars | Entity.HP/MaxHP base props; EnemyHealthBar/BossHealthBar renderers | WIRED | - |
| CMB-04 3 enemy types | EnemyData (Skeleton/DarkMage/Golem) instantiated by EnemySpawner into FarmScene._enemies | WIRED | - |
| CMB-05 FSM AI | EnemyAI driven per-enemy in EnemyEntity.Update (called from FarmScene) | WIRED | - |
| CMB-06 Boss telegraphs | BossEntity.CheckSummonPhase, IsBossSlashReady, GetBossSlashHitbox consumed by FarmScene loop + GetBossLoot(BossKilled) integrated with save | WIRED | - |

Requirements with no cross-phase wiring: None - every REQ has at least one inter-phase touchpoint verified.

## Orphaned Exports
None.

## Missing Connections
None.

## Broken Flows
None.

## Notes / Observations (non-blocking)

1. EquipmentRenderer consolidation: Phase 2 SUMMARY lists a separate EquipmentRenderer, but in shipped code the equipment-slot rendering is merged into src/UI/InventoryGridRenderer.cs (DrawEquipment method, line 288). Behavior equivalent - single renderer handles 20-slot grid + 7 equipment slots + drag/drop between them. Doc accuracy note, not an integration defect.

2. BossKilled lifecycle: _loadedState is retained on the FarmScene instance (not refreshed from disk) so in-session updates to BossKilled after first kill are persisted only on next OnDayAdvanced. Acceptable - save cadence is day-based by design.

3. Service locator pattern: ServiceContainer.Inventory is set (FarmScene.cs:81) to allow future scenes to access the inventory without constructor injection. Currently only InventoryScene uses the inventory and receives it explicitly - the Services.Inventory slot is wired but unused by consumers. Not an orphan (it is assigned and readable), just under-utilized.

## Verdict

ok - Phases 1-3 integrate cleanly. No blocking issues for milestone v1.0 scope. Phases 4-6 (src/World/NPCs, Dungeon, Progression) remain out of scope and were not audited.
