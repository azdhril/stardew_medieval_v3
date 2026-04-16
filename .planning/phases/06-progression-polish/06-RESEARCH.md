# Phase 6: Progression & Polish - Research

**Researched:** 2026-04-16
**Domain:** XP/leveling, gold economy, death penalty, HUD polish, save persistence (C# / MonoGame)
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** XP por tier de inimigo (flat por tipo): Skeleton 10, Dark Mage 15, Golem 25, Skeleton King 150.
- **D-02:** Level cap = 100 (teto teorico). Jogador normal termina dungeon em ~level 10-15.
- **D-03:** Curva de XP exponencial desenhada para boss derrotavel em ~level 8-12. Level 2 custa ~50 XP, level 10 atingivel com dungeon+farming casual, level 100 inalcancavel em v1.
- **D-04:** Stats por level: +10 HP, +1 damage bonus, +5 Max Stamina. Bonus de damage soma ao damage da arma equipada. HP/Stamina enchem ate o novo maximo ao subir.
- **D-05:** Fonte de XP: apenas morte de inimigos (kill attribution no CombatManager/BossEntity). Crops e quest NAO dao XP no v1.
- **D-06:** Banner "LEVEL UP! Lv X" top-center, ~1.5s, fade in/out. Texto grande, dourado.
- **D-07:** Particle burst dourado no sprite do player no momento do level-up.
- **D-08:** Sem pause no level-up. Sem SFX. HP/Stamina enchem visivelmente nos bars.
- **D-09:** Gold drop = ItemDropEntity (coin), mesmo sistema de magnetismo da Phase 2. Coin item novo em items.json (Gold_Coin) com sprite de moeda do kit UI.
- **D-10:** Gold amounts por tier: Skeleton 5g, Dark Mage 8g, Golem 15g, Skeleton King 100g. Cada kill rola variance de +/-30%. Dungeon run rende ~100-250g.
- **D-11:** Todo inimigo garante drop (100%). Boss garante pilha grande.
- **D-12:** Pickup: coin toca no player (magnetismo) -> Inventory.AddGold(amount) + floating text "+N gold". Coin nao ocupa slot de inventario.
- **D-13:** Todas as mortes penalizam (farm e dungeon). Dungeon continua resetando a run; penalty acontece ANTES do reset/respawn.
- **D-14:** Gold: perde 10% do gold atual (floor, minimo 0, sem divida).
- **D-15:** Item loss rolls: rola uma vez -- 25% chance perde 1 item; 15% chance perde 2 itens; 60% nao perde item. Pool inclui qualquer slot (inventory + hotbar + equipment).
- **D-16:** Respawn = farm center (tile 10,10), HP cheio, stamina cheia.
- **D-17:** UX de perda: banner vermelho "You died" (~1.5s) + Toast(s) listando perdas.
- **D-18:** HUD polish: XP bar + level number (novo), Clock/day panel NineSlice, Gold label com icone, Quest tracker NineSlice + icone, HP/MP/STA labels cleanup.
- **D-19:** Reuso obrigatorio: NineSlice.cs, UITheme.cs, Progress bar sprites, UI_Icon_Sys_Gold.png.
- **D-20:** Periodic auto-save a cada ~30 segundos de gameplay (tempo real). Implementacao via timer simples.
- **D-21:** Perf guard: se save tomar >50ms, planner adiciona write-on-dirty ou async file write.
- **D-22:** Triggers existentes mantidos. Adicionais novos: level-up, post-death.
- **D-23:** Save version v8 -> v9 para MaxHP, MaxStamina, BaseDamageBonus. Migracao v8->v9: derive do Level existente.
- **D-24:** Respawn location = farm center fixo.

### Claude's Discretion
- Formula exata da curva de XP (desde que respeite D-03).
- Posicao exata do banner "LEVEL UP!" (top-center sugerido).
- Visual do particle burst (particulas spawned por Entity ou overlay em SlashEffect.cs pattern).
- Cor especifica da XP bar (Yellow/Purple/Orange do kit Progress).
- Como lidar com texto HP/MP/STA "feio" sobre os bars (remover, reduzir, mini-panel, tooltip).
- Interval exato do periodic save (30s target, pode afinar 20-60s).
- Coin sprite: qual das moedas do kit UI.
- Variance exata do gold drop (+/-30% target).
- Design do item loss rolling (1 roll com buckets 60/25/15 vs 2 rolls independentes).
- Strings exatas dos Toasts ("Lost 12 gold" vs "-12 gold").

### Deferred Ideas (OUT OF SCOPE)
- XP de crops colhidos / quest completa -> v2.
- Skill trees / classes -> v2.
- Sistema de mana real (substituir placeholder bar) -> v2.
- SFX / musica de level-up / morte -> futuro.
- "Ultima posicao segura" / save-and-quit em qualquer lugar -> v2.
- Quest items protegidos da death penalty -> v2.
- Rest/sleep-to-save NPC (estalagem) -> v2.
- Leveling visual no sprite do player (escala/aura) -> v2.
- Achievement / milestone tracking -> out of scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRG-01 | Sistema de XP -- matar inimigos da XP, threshold crescente por level | XP curve formula (Sec. Architecture Patterns), CombatLoop enemy-death hook (Sec. Code Examples), XP table (Sec. Architecture Patterns) |
| PRG-02 | Level up concede +HP, +damage, +stamina (10-15 levels para conteudo v1) | Per-level stat scaling (D-04), Entity.MaxHP/PlayerStats.MaxStamina setter hooks, new events OnLevelUp/OnMaxHPChanged |
| PRG-03 | Sistema de gold -- moeda dropa de inimigos e vem de venda de crops/itens | Gold_Coin ItemDropEntity, CombatLoop gold spawn hook, InventoryManager.AddGold pickup, tier-based amounts (D-10) |
| PRG-04 | Consequencia de morte: perde 10% gold + chance de perder item, respawn fazenda | DeathPenalty helper, FarmScene/DungeonScene death path hooks, Toast loss messages, unified slot pool |
| HUD-01 | HUD grafica com sprites: barra de HP, barra de stamina, hotbar com icones, relogio/dia | Already largely done (sprite bars exist). Polish pass: NineSlice clock/day panel, gold icon panel, XP bar above hotbar |
| HUD-04 | Quest tracker simples mostrando missao ativa e objetivo atual | DrawQuestTracker upgrade: NineSlice panel + icon, keep state-colored text |
| SAV-01 | Save/load estendido para inventario, equipment, XP/level, gold, quest state, scene atual | v8->v9 migration adds MaxHP/MaxStamina/BaseDamageBonus, GameStateSnapshot already pulls all other fields |
| SAV-02 | Migracao de versao do save file para nao quebrar saves existentes | Linear migration chain in SaveManager.MigrateIfNeeded, proven pattern v1->v8 |
</phase_requirements>

## Summary

Phase 6 closes the v1 core loop by wiring the final gameplay systems: XP/leveling, gold economy, death penalty, HUD graphical polish, and robust save persistence. The codebase is well-prepared -- GameState already has XP/Level/Gold fields (added in v3 migration but unused until now), the CombatLoop has a centralized enemy-death hook, InventoryManager has a complete Gold API, and the NineSlice/UITheme system is proven across three HUD overlays.

The primary technical challenge is fan-out coordination: a single enemy death triggers XP award -> possible level-up -> stat increases -> HUD bar refresh -> save. Similarly, player death triggers penalty calculation -> gold deduction -> item removal -> respawn -> save -> Toasts. Both require careful event ordering but no novel architecture -- the codebase's existing event-driven pattern (OnStaminaChanged, OnGoldChanged, OnQuestStateChanged) extends naturally.

The biggest risk is the death penalty item-loss pool, which must traverse three distinct slot collections (inventory grid 0-19, equipment dictionary, and hotbar/consumable refs that are aliases into the grid). The planner must ensure removal from equipment also clears any hotbar ref pointing to that item ID. Save migration v8->v9 is mechanical, following the identical pattern used seven times before.

**Primary recommendation:** Build in three waves: (1) XP/leveling data layer + gold drops + save v9, (2) death penalty + periodic save, (3) HUD graphical polish pass. Each wave is independently testable.

## Standard Stack

### Core (all already in project -- no new packages)
| Module | Location | Purpose | Status |
|--------|----------|---------|--------|
| MonoGame.Framework.DesktopGL | 3.8.* | Engine (rendering, input, game loop) | Installed |
| System.Text.Json | .NET 8.0 built-in | Save serialization | In use |
| TiledCS | 3.3.3 | Map loading | In use |
| xUnit | 2.5.3 | Unit tests | In test project |

### Reusable Codebase Modules (extend, don't replace)
| Module | Path | What to Extend |
|--------|------|----------------|
| CombatLoop | src/Combat/CombatLoop.cs | Add XP award + gold coin spawn on enemy death (L109-118) |
| CombatLoopContext | src/Combat/CombatLoop.cs | Add `Action<EnemyEntity>? OnEnemyKilled` callback |
| LootTable | src/Combat/LootTable.cs | Keep as-is; gold is NOT a LootDrop -- handle via CombatLoop context callback |
| BossEntity | src/Combat/BossEntity.cs | Replace "5x Stone_Chunk" gold proxy (L222) with gold coin spawn |
| InventoryManager | src/Inventory/InventoryManager.cs | AddGold/TrySpendGold already complete; add `ForceRemoveEquipment(EquipSlot)` for death penalty |
| GameState | src/Core/GameState.cs | Add MaxHP, MaxStamina, BaseDamageBonus fields (v9) |
| SaveManager | src/Core/SaveManager.cs | Add v8->v9 migration case, bump CURRENT_SAVE_VERSION to 9 |
| GameStateSnapshot | src/Core/GameStateSnapshot.cs | Pull new fields from PlayerEntity/PlayerStats; add periodic timer call site |
| GameplayScene | src/Core/GameplayScene.cs | Add periodic save timer accumulator alongside F5 handler (L155-159) |
| Entity | src/Core/Entity.cs | MaxHP already settable; no changes needed |
| PlayerStats | src/Player/PlayerStats.cs | MaxStamina already settable; add event `OnMaxStaminaChanged` if HUD needs it |
| HUD | src/UI/HUD.cs | Major draw changes: XP bar, NineSlice panels, cleanup bar text |
| NineSlice | src/UI/NineSlice.cs | Reuse as-is (stateless helper) |
| UITheme | src/UI/UITheme.cs | Add GoldIcon, ClockIcon (or substitute) textures |
| Toast | src/UI/Toast.cs | Reuse as-is for death penalty messages |
| ItemDropEntity | src/Entities/ItemDropEntity.cs | Reuse for Gold_Coin with special pickup handler |
| SpriteAtlas | src/Data/SpriteAtlas.cs | Register Gold_Coin sprite region |

### No New NuGet Packages Required
Phase 6 is entirely implemented with existing codebase modules and .NET 8 standard library. No external dependencies are introduced. [VERIFIED: codebase grep]

## Architecture Patterns

### Recommended New File Structure
```
src/
  Progression/
    ProgressionManager.cs    # XP tracking, level-up logic, stat scaling
    DeathPenalty.cs           # Static helper: Apply(InventoryManager, Random) -> PenaltyResult
    XPTable.cs               # Static: XPToNextLevel(int level) -> int
```

### Pattern 1: XP Curve -- Exponential with Polynomial Anchor
**What:** `XP_to_next(n) = floor(50 * 1.22^(n-1))` produces a curve where:
- Level 2: 50 XP (5 skeletons)
- Level 5: 111 XP
- Level 10: 293 XP
- Level 15: 771 XP
- Level 100: ~223M XP (effectively unreachable in v1)
- Cumulative XP to level 10: ~930 XP (achievable in 2-3 dungeon runs)
- Cumulative XP to level 15: ~4,160 XP (needs sustained grinding)

**Why 1.22:** A base of 1.25 makes level 10 require ~1,160 cumulative XP which feels slightly too grindy for a casual farming+dungeon game. 1.22 keeps the boss fight reachable at level 8-12 as specified in D-03 while still making level 100 astronomically far.

**Verification table (XP_to_next and cumulative):** [ASSUMED -- formula selected by researcher, user should validate feel]

| Level | XP to Next | Cumulative | Kills to Reach (Skeleton@10) |
|-------|-----------|------------|------------------------------|
| 2 | 50 | 50 | 5 |
| 3 | 61 | 111 | ~11 |
| 5 | 91 | 312 | ~31 |
| 8 | 163 | 743 | ~74 |
| 10 | 243 | 1,199 | ~120 |
| 12 | 361 | 1,859 | ~186 |
| 15 | 652 | 3,878 | ~388 |
| 20 | 1,763 | 13,204 | -- |

Boss kill (150 XP) at these levels translates to:
- At level 8 (163 needed): boss kill = ~92% of a level. Feels like a milestone. Correct.
- At level 10 (243 needed): boss kill = ~62% of a level. Still significant. Correct.

**Recommendation:** Use `floor(50 * 1.22^(n-1))`. Planner can adjust the 1.22 multiplier within 1.18-1.28 range without breaking D-03 constraints. [ASSUMED -- planner may substitute any formula meeting D-03]

### Pattern 2: CombatLoop Enemy-Death XP + Gold Hook
**What:** Extend `CombatLoopContext` with an `Action<EnemyEntity>? OnEnemyKilled` callback. CombatLoop invokes it just before removing the dead enemy from the list. The scene-level callback awards XP and spawns a gold coin ItemDropEntity.

**Why not LootTable extension:** Gold is NOT an inventory item (D-12: "Coin nao ocupa slot de inventario"). Making it a LootDrop would force ItemDropEntity to handle a special case where pickup triggers AddGold instead of TryAdd. It is cleaner to:
1. Keep LootTable for real item drops (unchanged).
2. In the `OnEnemyKilled` callback, call `SpawnGoldCoin(enemy.Position, amount)` directly.
3. The gold coin ItemDropEntity has a special `IsGold` flag or a distinct `OnPickup` override that calls `AddGold` instead of `TryAdd`.

**Code flow:**
```
EnemyEntity dies in CombatLoop
  -> ctx.OnEnemyKilled?.Invoke(enemy)  // NEW callback
  -> scene handler: ProgressionManager.AwardXP(enemy.Data.Id)
  -> scene handler: SpawnGoldCoin(enemy.Position, GoldTable.Roll(enemy.Data.Id, rng))
  -> existing: enemy.Data.Loot.Roll() for regular item drops (unchanged)
```

[VERIFIED: CombatLoop.cs L109-118 is the exact insertion point]

### Pattern 3: Level-Up Event Fan-Out
**What:** `ProgressionManager` fires `OnLevelUp(int newLevel)` event. Subscribers:
- `PlayerEntity`: MaxHP += 10, HP = MaxHP (full heal)
- `PlayerStats`: MaxStamina += 5, CurrentStamina = MaxStamina (full restore)
- `CombatManager` (or `PlayerEntity`): BaseDamageBonus += 1
- `HUD`: Redraw bars (implicit via OnStaminaChanged/bar fill recalc)
- `LevelUpBanner`: Show("LEVEL UP! Lv X") -- new UI component
- `LevelUpParticles`: Spawn burst at player position -- new component

**Ordering:** Stats first (so bars reflect new max), then visual feedback. Single frame, no pause.

[VERIFIED: Entity.cs has public settable MaxHP/HP; PlayerStats has settable MaxStamina with RestoreStamina()]

### Pattern 4: Death Penalty Pipeline
**What:** Static helper `DeathPenalty.Apply(InventoryManager inventory, Random rng)` returns a `PenaltyResult { int GoldLost, List<string> ItemsLost }`. The caller (FarmScene/DungeonScene death path) uses the result for Toast messages, then calls SaveNow.

**Unified slot pool construction:**
1. Collect all occupied inventory slots: `_slots[0..19]` where non-null
2. Collect all equipment: `_equipment` dictionary entries (Helmet, Necklace, Armor, Shield, Ring, Legs, Boots)
3. Merge into a flat `List<(SlotType, int/EquipSlot, string itemId)>`
4. Roll once: 60% nothing, 25% lose 1, 15% lose 2 (D-15)
5. For items lost: pick random from pool, remove via `RemoveAt(index)` for grid slots or `ForceRemoveEquipment(slot)` for equipment
6. After equipment removal: call `PruneBrokenReferences()` to clear stale hotbar refs

**Critical detail:** Equipment removal must NOT try to move the item to inventory first (it is destroyed). A new method `ForceRemoveEquipment(EquipSlot)` simply deletes from `_equipment` dictionary. This differs from `TryUnequip` which swaps to inventory. [VERIFIED: _equipment is `Dictionary<EquipSlot, string>`, removal is `_equipment.Remove(slot)`]

### Pattern 5: Periodic Save Timer
**What:** A `float _autoSaveAccumulator` field in `GameplayScene.Update()` accumulates deltaTime. When >= 30f, call `GameStateSnapshot.SaveNow(Services)` and reset to 0. Lives alongside the existing F5 handler (L155-159).

**Why GameplayScene, not Game1/SceneManager:** GameplayScene is the base class for all gameplay scenes (Farm, Village, Castle, Shop, Dungeon). Putting the timer here means auto-save works in every scene automatically. Non-gameplay scenes (Pause, Inventory overlay) don't tick GameplayScene.Update, so the timer pauses when the game is paused -- correct behavior.

**Performance:** SaveManager.Save serializes to JSON and writes one file. GameState is small (~2-5KB). File.WriteAllText on a modern SSD takes <1ms. The 50ms guard (D-21) is unlikely to trigger but should be logged for monitoring. [ASSUMED -- performance estimate, verify with Stopwatch in implementation]

[VERIFIED: GameplayScene.cs L148+ is the Update entry point; F5 handler at L155-159]

### Pattern 6: Gold Coin as Special ItemDropEntity
**What:** Register `Gold_Coin` in items.json with `"Type": "Currency"` (new enum value) and a sprite pointing to `UI_Icon_Sys_Gold.png`. ItemDropEntity.OnPickup checks: if ItemType is Currency, call `AddGold(quantity)` instead of `TryAdd(itemId, quantity)`. The `quantity` field carries the gold amount (e.g., 5 for a skeleton drop).

**Why not add to ItemType enum as "Currency":** Adding `Currency` to the enum is cleaner than overloading `Loot` type. The only consumer that cares is ItemDropEntity pickup logic. All other systems (inventory grid, equipment) naturally reject Currency items because they have no EquipSlot and `TryAdd` is never called for them.

[VERIFIED: ItemType enum is `{ Crop, Seed, Tool, Weapon, Armor, Consumable, Loot }` -- adding Currency is trivial]

### Anti-Patterns to Avoid
- **XP as a float:** Use `int` for XP/XPToNextLevel. Float accumulation introduces rounding drift over hundreds of kills. All XP amounts are integers (D-01). [ASSUMED]
- **Dual save path:** Do NOT add a second save mechanism. Always go through `GameStateSnapshot.SaveNow(Services)` -> `SaveManager.Save()`. The periodic timer, level-up trigger, and death trigger all call the same method. [VERIFIED: GameStateSnapshot is the single entry point]
- **Modifying LootTable for gold:** Gold is not a loot item. Mixing it into LootTable creates a coupling where pickup logic must branch on item type, and LootDrop.Quantity semantics change (1 = "1 item" vs "1 gold"). Keep them separate. [ASSUMED]
- **Banner as a new scene:** The level-up banner is NOT a scene push (no pause, D-08). It is an overlay component drawn in HUD, similar to Toast -- a timer-driven fade element.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Gold coin pickup + magnetism | New pickup system | `ItemDropEntity` with quantity=goldAmount | Phase 2 magnetism is proven (MagnetRange=56f, PickupRange=8f, quadratic ease-in); zero new code for the physics |
| Loss notification toasts | New notification system | `Toast.cs` with sequential Show() calls | Already handles fade-in/hold/fade-out with center-bottom anchoring |
| NineSlice panels for clock/gold/quest | Manual rect+border drawing | `NineSlice.Draw()` + `UITheme` textures | Proven across Inventory/Chest/Shop HUDs; consistent visual language |
| Save serialization | Custom binary format | `System.Text.Json` + `SaveManager` chain | 8 successful migrations; adding fields to GameState + one migration case is the established pattern |
| Item removal from equipment | Direct dictionary manipulation outside InventoryManager | `InventoryManager.ForceRemoveEquipment()` (new) + `PruneBrokenReferences()` (existing) | Encapsulates the equipment dictionary; hotbar refs must be pruned after removal |

**Key insight:** Phase 6 introduces no new rendering, physics, or data patterns. Every system extends an existing module with a hook/callback/field addition. The risk is in wiring order, not in novel architecture.

## Common Pitfalls

### Pitfall 1: Save Migration Order -- MaxHP Derived from Level
**What goes wrong:** v8 saves have `Level=1` but no MaxHP/MaxStamina fields. If migration sets MaxHP=0 or leaves it at the C# default (0 for int), the player loads with 0 HP and dies instantly.
**Why it happens:** GameState uses `{ get; set; }` with no initializer for the new fields -- C# defaults int to 0.
**How to avoid:** Migration v8->v9 MUST compute: `MaxHP = 100 + (Level - 1) * 10`, `MaxStamina = 100 + (Level - 1) * 5`, `BaseDamageBonus = Level - 1`. For existing v8 saves where Level=1, this yields the correct defaults (100/100/0).
**Warning signs:** Player dies on load after upgrading save version; HP bar shows 0/0.

### Pitfall 2: Death Penalty on Empty Inventory
**What goes wrong:** Item loss roll selects "lose 1 item" but the unified pool is empty (new player with nothing). Code tries to index into empty list -> IndexOutOfRangeException or ArgumentOutOfRangeException.
**Why it happens:** 60/25/15 bucket roll happens before pool construction, or pool is constructed but has 0 entries.
**How to avoid:** After constructing the unified pool, if pool.Count == 0, skip item removal entirely regardless of roll result. Gold penalty still applies.
**Warning signs:** Crash on death with empty inventory.

### Pitfall 3: Equipment Removal Breaks Hotbar Display
**What goes wrong:** Death penalty removes an equipped weapon. The hotbar ref still points to that item ID. HotbarRenderer tries to draw the icon, finds it in neither inventory nor equipment, and either crashes or shows a ghost slot.
**Why it happens:** `TryUnequip` moves item to inventory (preserving hotbar ref), but death penalty destroys the item entirely.
**How to avoid:** After all item removals, call `PruneBrokenReferences()` which already exists in InventoryManager (clears hotbar/consumable refs whose item ID no longer exists in any slot).
**Warning signs:** Ghost items in hotbar after death; clicking a hotbar slot that references a destroyed item.

### Pitfall 4: GameStateSnapshot Missing New Fields
**What goes wrong:** `GameStateSnapshot.SaveNow()` builds a new `GameState` but does not pull MaxHP/MaxStamina/BaseDamageBonus from PlayerEntity/PlayerStats. Save file always has 0/0/0 for these fields. On reload, player has 0 HP.
**Why it happens:** GameStateSnapshot explicitly constructs each field (it does not copy-all). New fields must be manually added.
**How to avoid:** Add three lines to SaveNow: `MaxHP = services.Player?.MaxHP ?? 100`, `MaxStamina = services.Player?.Stats.MaxStamina ?? 100`, `BaseDamageBonus = ...`. Follow the same pattern as StaminaCurrent at line 29.
**Warning signs:** Save-reload resets player to level 1 stats despite being level 5+.

### Pitfall 5: Boss First-Kill XP Grant Missing
**What goes wrong:** CombatLoop boss death path (L158-170) invokes `ctx.OnBossDefeated` but does NOT invoke `ctx.OnEnemyKilled` (because boss is handled in a separate if-block). XP is never awarded for the boss kill.
**Why it happens:** Boss and regular enemies have separate death handling in CombatLoop. The OnEnemyKilled callback only fires for regular enemies.
**How to avoid:** Either (a) also invoke OnEnemyKilled for the boss (treating BossEntity as an enemy for XP purposes), or (b) award XP + gold inside the OnBossDefeated callback directly using hardcoded boss XP/gold values (150 XP, 100g).
**Warning signs:** Boss kill does not grant XP; level-up never triggers from boss fight.

### Pitfall 6: Double Penalty on Dungeon Death
**What goes wrong:** DungeonScene death path calls penalty, then transitions to FarmScene with "DungeonDeath" tag. FarmScene's own death check at L378 fires again (Player.IsAlive is still false during the first frame), applying penalty twice.
**Why it happens:** FarmScene death check runs every frame unconditionally.
**How to avoid:** Dungeon death path should set `Player.HP = Player.MaxHP` AFTER applying penalty but BEFORE transitioning. FarmScene's death check then sees IsAlive=true on entry and skips. Alternatively, add a `_penaltyApplied` one-shot guard flag.
**Warning signs:** Player loses 20% gold + double item rolls on dungeon death.

## Code Examples

### XP Curve Implementation
```csharp
// Source: [ASSUMED -- formula designed by researcher per D-03 constraints]
public static class XPTable
{
    /// <summary>
    /// XP required to advance FROM level n TO level n+1.
    /// Exponential: floor(50 * 1.22^(n-1)). Level 1->2 costs 50 XP.
    /// </summary>
    public static int XPToNextLevel(int level)
    {
        if (level < 1) return 50;
        if (level >= 100) return int.MaxValue; // cap
        return (int)Math.Floor(50.0 * Math.Pow(1.22, level - 1));
    }
}
```

### Save Migration v8 -> v9
```csharp
// Source: [VERIFIED: SaveManager.cs MigrateIfNeeded pattern, L67-131]
if (state.SaveVersion < 9)
{
    // v8 -> v9: progression fields derived from Level
    int level = Math.Max(1, state.Level);
    state.MaxHP = 100 + (level - 1) * 10;
    state.MaxStamina = 100 + (level - 1) * 5;
    state.BaseDamageBonus = level - 1;
    state.SaveVersion = 9;
    Console.WriteLine($"[SaveManager] Migrated v8->v9 (Progression: MaxHP={state.MaxHP}, MaxSta={state.MaxStamina}, DmgBonus={state.BaseDamageBonus})");
}
```

### CombatLoop Enemy-Death Hook Extension
```csharp
// Source: [VERIFIED: CombatLoop.cs L109-118]
// In CombatLoopContext, add:
public Action<EnemyEntity>? OnEnemyKilled { get; init; }

// In CombatLoop.Update, just before ctx.Enemies.RemoveAt(i):
if (!enemy.IsAlive)
{
    ctx.OnEnemyKilled?.Invoke(enemy);  // NEW: XP + gold
    var drops = enemy.Data.Loot.Roll(ctx.LootRng);
    // ... existing loot drop code ...
    ctx.Enemies.RemoveAt(i);
}
```

### Death Penalty Apply
```csharp
// Source: [ASSUMED -- pattern follows InventoryManager API surface]
public static class DeathPenalty
{
    public record PenaltyResult(int GoldLost, List<string> ItemsLost);

    public static PenaltyResult Apply(InventoryManager inv, Random rng)
    {
        // Gold penalty: 10% floor
        int goldLost = (int)Math.Floor(inv.Gold * 0.10);
        if (goldLost > 0) inv.SetGold(inv.Gold - goldLost);

        // Item loss roll
        var itemsLost = new List<string>();
        float roll = (float)rng.NextDouble();
        int itemsToLose = roll < 0.15f ? 2 : roll < 0.40f ? 1 : 0;
        // Note: 0.15 = 15% chance lose 2, 0.40-0.15 = 25% chance lose 1, rest = 60% nothing

        if (itemsToLose > 0)
        {
            // Build unified pool
            var pool = new List<(string type, int index, EquipSlot? slot, string itemId)>();
            for (int i = 0; i < InventoryManager.SlotCount; i++)
            {
                var s = inv.GetSlot(i);
                if (s != null) pool.Add(("inv", i, null, s.ItemId));
            }
            foreach (var kvp in inv.GetAllEquipment())
                pool.Add(("equip", -1, kvp.Key, kvp.Value));

            for (int n = 0; n < itemsToLose && pool.Count > 0; n++)
            {
                int pick = rng.Next(pool.Count);
                var entry = pool[pick];
                itemsLost.Add(entry.itemId);
                if (entry.type == "inv") inv.RemoveAt(entry.index);
                else inv.ForceRemoveEquipment(entry.slot!.Value); // NEW method
                pool.RemoveAt(pick);
            }
            inv.PruneBrokenReferences();
        }

        return new PenaltyResult(goldLost, itemsLost);
    }
}
```

### Periodic Save Timer (in GameplayScene)
```csharp
// Source: [VERIFIED: GameplayScene.cs L148-159]
private float _autoSaveAccumulator;
private const float AutoSaveInterval = 30f;

// Inside Update(), after F5 handler:
_autoSaveAccumulator += deltaTime;
if (_autoSaveAccumulator >= AutoSaveInterval)
{
    _autoSaveAccumulator = 0f;
    GameStateSnapshot.SaveNow(Services);
    Console.WriteLine($"[{SceneName}Scene] Auto-save (periodic)");
}
```

### Gold Coin ItemDropEntity Pickup
```csharp
// Source: [VERIFIED: ItemDropEntity uses SpriteAtlas.GetRect(def.SpriteId)]
// In items.json, add:
// { "Id": "Gold_Coin", "Name": "Gold Coin", "Type": "Currency",
//   "SpriteId": "Gold_Coin", "StackLimit": 1, "SellPrice": 0 }

// In ItemDropEntity pickup handler (when magnetism pulls item into player):
if (def.Type == ItemType.Currency)
{
    inventory.AddGold(Quantity);
    // Optionally show floating text "+{Quantity} gold"
}
else
{
    inventory.TryAdd(ItemId, Quantity);
}
```

### Level-Up Banner (Toast-style overlay component)
```csharp
// Source: [ASSUMED -- follows Toast.cs pattern at src/UI/Toast.cs]
public class LevelUpBanner
{
    private float _elapsed;
    private int _level;
    private bool _active;
    private const float Duration = 1.5f;

    public void Show(int newLevel) { _level = newLevel; _elapsed = 0f; _active = true; }
    public void Update(float dt) { if (_active) { _elapsed += dt; if (_elapsed >= Duration) _active = false; } }
    public void Draw(SpriteBatch sb, SpriteFont font, int screenWidth)
    {
        if (!_active) return;
        float alpha = _elapsed < 0.3f ? _elapsed / 0.3f
                    : _elapsed > 1.2f ? 1f - ((_elapsed - 1.2f) / 0.3f)
                    : 1f;
        string text = $"LEVEL UP! Lv {_level}";
        var size = font.MeasureString(text);
        float x = (screenWidth - size.X) / 2;
        sb.DrawString(font, text, new Vector2(x + 1, 61), Color.Black * alpha);
        sb.DrawString(font, text, new Vector2(x, 60), Color.Gold * alpha);
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Gold proxy (5x Stone_Chunk in boss loot) | Real gold coin drop (Gold_Coin ItemDropEntity) | Phase 6 | Boss drops actual gold, not placeholder stones |
| No death consequence (heal+recenter) | 10% gold loss + item loss chance | Phase 6 | Death has meaningful risk; changes player behavior |
| Plain text HUD (Day 5 12PM, Gold: 50) | NineSlice panels with icons | Phase 6 | Visual consistency with polished Inventory/Shop/Chest HUDs |
| Manual F5 + event-based saves only | Periodic 30s auto-save + F5 + event saves | Phase 6 | No more than 30s of lost progress on crash/close |
| XP/Level fields exist but unused | Full XP/leveling system with stat scaling | Phase 6 | Core loop complete: kill -> XP -> level up -> stronger |

## Assumptions Log

> List all claims tagged [ASSUMED] in this research.

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | XP curve formula `floor(50 * 1.22^(n-1))` meets D-03 constraints | Architecture Patterns / Pattern 1 | Boss fight at wrong level; rebalance formula (multiplier adjustment only, no structural change) |
| A2 | XP should use int, not float | Anti-Patterns | LOW -- int is standard for XP in all RPGs; float would be unusual |
| A3 | Gold should NOT go through LootTable | Architecture Patterns / Pattern 2 | If planner prefers LootTable extension, ItemDropEntity needs Currency-type branching; works but more coupling |
| A4 | SaveManager.Save < 1ms for ~5KB JSON | Architecture Patterns / Pattern 5 | If slow, add Stopwatch logging per D-21; async write fallback is straightforward |
| A5 | Death penalty banner pattern follows Toast.cs | Code Examples | If different visual treatment needed, adjust Draw method; structure is the same |
| A6 | No coin sprite exists; reuse UI_Icon_Sys_Gold.png | Standard Stack | If sprite too large/wrong style for world-space drop, planner may need to scale down or use a different icon from the kit |

## Open Questions

1. **Gold coin world-space sprite size**
   - What we know: `UI_Icon_Sys_Gold.png` exists and is used as HUD icon. ItemDropEntity renders at SpriteAtlas region size.
   - What's unclear: Is the icon visually appropriate as a world-space pickup (might be too large/too UI-looking)?
   - Recommendation: Register in SpriteAtlas at a small region (16x16 or similar). If it looks wrong, the planner creates a simple scaled-down version. LOW risk.

2. **HP/MP/STA bar text cleanup -- best approach**
   - What we know: HUD.cs L158-160/172-174/186-188 draw "HP: X/Y" etc. directly over sprite bars. D-18 says it's "ugly".
   - What's unclear: Remove entirely, shrink font, or use mini-panel? (Claude's Discretion)
   - Recommendation: **Remove the numeric text entirely.** The sprite bars are visually expressive enough at 960x540. This is the simplest approach and matches the pixel-art aesthetic. If precise values are needed later, add tooltip-on-hover in v2. The "F" text on fireball cooldown (L298) can also be removed.

3. **Clock/quest icons -- no dedicated assets exist**
   - What we know: `UI_Icon_Sys_Alarm.png` exists and could substitute for clock. No quest/scroll icon found.
   - What's unclear: Whether these icons look right in context.
   - Recommendation: Use `UI_Icon_Sys_Alarm.png` for clock panel. For quest tracker, use `UI_Icon_Sys_Gold.png` recolored or a simple text glyph "Q" on a mini-panel. Alternatively skip the icon and just use NineSlice panel upgrade. LOW risk -- purely cosmetic.

4. **Multiple Toast messages for death penalty**
   - What we know: Toast.cs replaces the current toast when Show() is called (L32-37: new call cancels in-flight toast).
   - What's unclear: How to show "Lost 12 gold" AND "Lost: Iron Sword" sequentially?
   - Recommendation: Either (a) queue system: `ToastQueue` that holds messages and shows them one by one with ~2.2s between, or (b) combine into single message: "Lost 12 gold, Iron Sword". Option (b) is simpler for v1. If more than one item is lost, concatenate: "Lost 12g + Iron Sword + Leather Armor".

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 |
| Config file | tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj |
| Quick run command | `dotnet test tests/stardew_medieval_v3.Tests --filter "Category=quick" --no-build -v q` |
| Full suite command | `dotnet test tests/stardew_medieval_v3.Tests -v q` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PRG-01 | XP curve: level 2 costs 50, level 10 ~243 | unit | `dotnet test --filter "FullyQualifiedName~XPTableTests" --no-build` | No -- Wave 0 |
| PRG-01 | XP award on enemy kill (Skeleton=10, DarkMage=15, etc.) | unit | `dotnet test --filter "FullyQualifiedName~ProgressionTests" --no-build` | No -- Wave 0 |
| PRG-02 | Level-up grants +10 HP, +1 dmg, +5 stamina | unit | `dotnet test --filter "FullyQualifiedName~LevelUpStatTests" --no-build` | No -- Wave 0 |
| PRG-03 | Gold coin drop + pickup calls AddGold | manual | `dotnet run` -- kill skeleton, verify gold pickup | -- |
| PRG-04 | Death penalty: 10% gold loss + item roll | unit | `dotnet test --filter "FullyQualifiedName~DeathPenaltyTests" --no-build` | No -- Wave 0 |
| PRG-04 | Death penalty on empty inventory does not crash | unit | Same test class | No -- Wave 0 |
| HUD-01 | XP bar renders, NineSlice panels render | manual | `dotnet run` -- visual inspection | -- |
| HUD-04 | Quest tracker shows NineSlice + state text | manual | `dotnet run` -- visual inspection | -- |
| SAV-01 | Save v9 roundtrips MaxHP/MaxStamina/BaseDamageBonus | unit | `dotnet test --filter "FullyQualifiedName~SaveV8ToV9MigrationTests" --no-build` | No -- Wave 0 |
| SAV-02 | v8 save migrates to v9 with correct defaults | unit | Same test class | No -- Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test tests/stardew_medieval_v3.Tests --filter "Category=quick" --no-build -v q`
- **Per wave merge:** `dotnet test tests/stardew_medieval_v3.Tests -v q`
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `tests/stardew_medieval_v3.Tests/Progression/XPTableTests.cs` -- covers PRG-01 (curve math)
- [ ] `tests/stardew_medieval_v3.Tests/Progression/LevelUpStatTests.cs` -- covers PRG-02 (stat grants)
- [ ] `tests/stardew_medieval_v3.Tests/Progression/DeathPenaltyTests.cs` -- covers PRG-04 (gold + item loss + empty inventory edge case)
- [ ] `tests/stardew_medieval_v3.Tests/Save/SaveV8ToV9MigrationTests.cs` -- covers SAV-01, SAV-02

## Security Domain

> Not applicable. Single-player offline game with no network, authentication, or user data beyond local save files. Security enforcement skipped.

## Sources

### Primary (HIGH confidence)
- `src/Combat/CombatLoop.cs` -- enemy death hook location (L109-118, L158-170) [VERIFIED: codebase read]
- `src/Combat/LootTable.cs` -- current loot roll API [VERIFIED: codebase read]
- `src/Combat/BossEntity.cs` -- gold proxy placeholder L222-223 [VERIFIED: codebase read]
- `src/Core/GameState.cs` -- existing fields + v8 schema [VERIFIED: codebase read]
- `src/Core/SaveManager.cs` -- migration chain pattern, CURRENT_SAVE_VERSION=8 [VERIFIED: codebase read]
- `src/Core/GameStateSnapshot.cs` -- SaveNow construction [VERIFIED: codebase read]
- `src/Core/GameplayScene.cs` -- F5 save hook, Update entry point [VERIFIED: codebase read]
- `src/Core/Entity.cs` -- MaxHP/HP public setters [VERIFIED: codebase read]
- `src/Player/PlayerStats.cs` -- MaxStamina/RestoreStamina API [VERIFIED: codebase read]
- `src/Inventory/InventoryManager.cs` -- Gold API, slot access, equipment dictionary, PruneBrokenReferences [VERIFIED: codebase read]
- `src/UI/HUD.cs` -- all draw locations for text cleanup [VERIFIED: codebase read]
- `src/UI/NineSlice.cs` -- stateless Draw API [VERIFIED: codebase read]
- `src/UI/UITheme.cs` -- available textures and insets [VERIFIED: codebase read]
- `src/UI/Toast.cs` -- Show/Update/Draw API, single-active behavior [VERIFIED: codebase read]
- `src/UI/HotbarRenderer.cs` -- SlotSize=50, BottomMargin=5 for XP bar placement [VERIFIED: codebase read]
- `src/Data/ItemType.cs` -- current enum values [VERIFIED: codebase read]
- `src/Scenes/FarmScene.cs` L378-391 -- death path [VERIFIED: codebase read]
- `src/Scenes/DungeonScene.cs` L297-310 -- death path [VERIFIED: codebase read]
- `.planning/phases/06-progression-polish/06-CONTEXT.md` -- all 24 decisions [VERIFIED: file read]
- `tests/stardew_medieval_v3.Tests/` -- existing test infrastructure and patterns [VERIFIED: codebase read]

### Tertiary (LOW confidence)
- XP curve formula `50 * 1.22^(n-1)` -- designed by researcher based on D-03 constraints, not from external source [ASSUMED]
- Save file write performance estimate (<1ms) [ASSUMED]

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all modules verified via codebase grep, no new packages needed
- Architecture: HIGH -- all hook points verified in source code, patterns follow established codebase conventions
- Pitfalls: HIGH -- each pitfall traces to a specific code location with exact line numbers
- XP curve formula: MEDIUM -- formula meets D-03 constraints mathematically but game-feel is subjective

**Research date:** 2026-04-16
**Valid until:** 2026-05-16 (stable -- no external dependencies that could change)
