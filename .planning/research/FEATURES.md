# Feature Landscape

**Domain:** Medieval Fantasy RPG/Farming Hybrid (Stardew Valley + Tibia-style combat)
**Researched:** 2026-04-10
**Confidence:** MEDIUM (based on extensive genre knowledge; no web verification available)

## Context

This analysis targets a v1 "playable loop" milestone for a game that already has farming basics (crops, tilemap, day/night, stamina, seasons, save/load). The goal is adding combat, dungeons, inventory, NPCs, loot, XP, and map transitions to close the core loop: **farm -> village -> dungeon -> loot -> evolve -> farm**.

Reference games: Stardew Valley, Rune Factory 4/5, Moonlighter, Graveyard Keeper, Forager.

---

## Table Stakes

Features users expect from a farming+combat hybrid. Missing any of these = the v1 loop feels broken or incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Inventory system with grid slots** | Every RPG and farming game has one. Players need to carry tools, loot, crops. Without it there is no economy loop. | Medium | 20-30 slots, stack support for consumables, equipment slots separate from bag. Stardew uses 36-slot backpack + 12 hotbar. For v1, 20 bag + 8 hotbar is sufficient. |
| **Hotbar with quick-select** | Players need instant access to tools/weapons without opening inventory. Number keys 1-8 or scroll wheel. | Low | Already scoped in PROJECT.md. Must support both tools (hoe, watering can) and weapons (sword, staff). |
| **Melee combat with directional attack** | Core of the dungeon gameplay. Player swings in facing direction, hitbox appears briefly, damages enemies in range. | Medium | Stardew uses simple arc hitbox in front of player. Rune Factory adds combos. For v1: single swing with cooldown is enough. Add knockback on hit for game feel. |
| **At least 1 ranged attack (magic)** | Mixed combat is expected. Pure melee feels flat in dungeons. A fireball or similar projectile gives tactical variety. | Medium | Projectile entity with velocity, collision with enemies, mana/cooldown cost. One spell is enough for v1. |
| **Enemy AI (patrol, chase, attack)** | Dungeons need threats. Minimum viable AI: wander in area, detect player in range, chase, attack on contact/range, return to patrol if player escapes. | Medium | 3 enemy types for v1: melee rusher (skeleton), ranged caster (mage), slow tank (golem). State machine: Idle -> Patrol -> Chase -> Attack -> Return. |
| **HP system for player and enemies** | Combat needs health. Player and enemies have HP, damage reduces it, zero = death/defeat. | Low | Player HP shown on HUD. Enemy HP shown as small bar above sprite. Healing via potions (bought at shop). |
| **Death/defeat consequence** | Player needs stakes. Dying should cost something but not be devastating. | Low | Stardew: lose some items + gold + wake up next day. Rune Factory: wake up in clinic. For v1: lose 50% gold, respawn at farm, keep items. Punishing but not rage-quit. |
| **1 complete dungeon with rooms** | The "explore/fight" half of the loop. Multiple connected rooms with enemies, simple puzzles (kill all to open door), treasure chests. | High | 5-8 rooms is enough. Linear progression with optional side rooms for loot. Use Tiled maps with transition triggers between rooms. Final room has boss. |
| **Boss fight** | Dungeons need a climax. Boss is a stronger enemy with 1-2 unique attack patterns and a health bar. | Medium | For v1: bigger sprite, more HP, telegraphed attacks (wind-up animation before strike), drops unique loot. Don't need complex multi-phase. |
| **Loot drops from enemies** | Combat needs rewards. Enemies drop items (materials, gold, rare equipment) on death. | Medium | Drop table per enemy type. Items appear on ground, player walks over to collect. Gold + 2-3 material types + rare weapon/armor drops. |
| **Item rarity system (basic)** | Loot needs excitement. At minimum: common, uncommon, rare. Affects stats and visual distinction. | Low | Color-coded names (white, green, blue). Stat multipliers per rarity. Drop chance decreases with rarity. |
| **XP and leveling** | Progression keeps players engaged between dungeon runs. Kill enemies -> gain XP -> level up -> get stronger. | Low | Simple formula: XP per kill, increasing threshold per level. Level up grants +HP, +damage, +stamina. 10-15 levels for v1 content. |
| **Graphical HUD** | Text-only HUD breaks immersion. Need sprite-based HP bar, stamina bar, hotbar with item icons, clock/day display. | Medium | Already identified as needed in PROJECT.md. Priority: HP bar, stamina bar, hotbar, minimap or day/time indicator. |
| **Map transitions** | Player must move between farm, village, and dungeon. Screen transitions with loading or fade. | Medium | Trigger zones at map edges or doors. Fade to black -> load new map -> fade in. Preserve player state across transitions. |
| **Village with shop NPC** | Players need to buy potions, seeds, and basic equipment. Shop UI: list of items with prices, buy/sell. | Medium | One shop is enough for v1. Sells: seeds (by season), potions (HP restore), basic weapons/armor. Buy crops from player. |
| **King NPC with quest** | The narrative thread. King gives mission ("clear the dungeon"), player completes it, reports back for reward. | Low | Single quest chain for v1. Talk to King -> receive quest -> complete dungeon -> return -> get reward + "story continues in v2" ending. |
| **Basic dialogue system** | NPCs need to talk. Text box with portrait, advance with button press, branching not required for v1. | Low | Linear dialogue with NPC state tracking (has quest, quest active, quest complete). No complex dialogue trees. |
| **Save/load preserving all new systems** | Already exists for farming. Must extend to: inventory, equipment, XP/level, quest state, dungeon progress, gold. | Medium | Extend existing JSON save system. Add versioned fields for new data. |

## Differentiators

Features that are NOT expected in a minimal v1 but would make the game stand out. Implement selectively.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **"False hero" narrative identity** | Project's unique premise: player is a fraud pretending to be a hero. This is a story differentiator that costs almost nothing mechanically -- it's in dialogue, quest framing, and NPC reactions. | Low | Lean into this in King's dialogue, shop NPC comments, death messages. "The hero returns... barely alive." This is FREE differentiation. |
| **Tibia-style loot feel** | Tibia's loot system creates dopamine -- items physically drop on ground, you see the pile, open it like a container. More visceral than auto-pickup. | Low-Medium | Instead of auto-loot, enemies drop a "loot bag" sprite. Player interacts to see contents and choose what to take. Small feature, big feel difference. |
| **Crop-to-combat pipeline** | Crops aren't just for selling -- specific crops can be crafted into combat consumables (healing herb -> potion, fire pepper -> damage buff). Links the two halves of the gameplay. | Medium | For v1, even a simplified version works: sell crops at shop, shop sells potions. The explicit "your farm fuels your adventure" feeling. |
| **Dungeon environment variety** | Rooms with different tileset themes (crypt, cave, flooded chamber). Visual variety makes the single dungeon feel larger. | Low-Medium | 2-3 tileset variations within the same dungeon. Purely visual differentiation using Tiled. |
| **Shadow/stealth mode for observation** | Before combat, let player peek into rooms to see enemy layout. Creates tactical feel without complex stealth mechanics. | Low | Just a wider camera view or "peek through door" mechanic before entering a room. Minimal code, adds planning feel. |
| **Weather affecting gameplay** | Rain auto-waters crops (Stardew does this). Could also affect dungeon (flooded rooms on rainy days). | Low | Rain already semi-expected in farming games. Auto-water is the key feature. Dungeon effects are bonus. |
| **Stamina as shared resource** | Stamina used for BOTH farming and combat creates meaningful daily planning. "Do I farm today or save stamina for the dungeon?" | Low | Already have stamina system. Just make combat actions consume it too. This is the farming+RPG link that Rune Factory nails. |
| **Night danger on farm** | Weak enemies spawn on farm at night, forcing player to either go home before dark or fight them. Links day/night cycle to combat. | Medium | 1-2 weak enemy types that spawn after 10pm on farm map. Despawn at dawn. Creates urgency to the day cycle. |

## Anti-Features

Features to explicitly NOT build for v1. Each would consume significant dev time with low return.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Crafting system** | Full crafting (recipes, stations, material combinations) is a massive system. Rune Factory's crafting alone is dozens of hours of content design. | Buy everything from shop. Sell crops for gold, buy potions/equipment with gold. Crafting is v2. |
| **Relationship/romance system** | Social simulation is an entire game pillar (Stardew has 12+ marriage candidates with events). Enormous content investment. | NPCs have static personalities via dialogue. No friendship meters, gifts, or events. v2+. |
| **Procedural dungeon generation** | Roguelike dungeon generation adds massive complexity (room connectivity, balance, dead ends, backtracking). | Hand-designed dungeon in Tiled. One dungeon, well-crafted. Procedural is v2+. |
| **Skill trees / class system** | Branching progression requires balancing dozens of abilities and making them all feel meaningful. | Flat stat growth on level up. One melee weapon, one spell. Classes and specialization in v2. |
| **Complex AI behaviors** | Flanking, group tactics, retreating, calling for help -- sophisticated AI is a rabbit hole. | Simple state machine: patrol, chase, attack. Enemies are independent. Coordination is v2. |
| **Multiplayer** | Networking a real-time game is an order of magnitude more complex. Already scoped out in PROJECT.md. | Single player only. Period. |
| **Multiple weapon types** | Sword, axe, spear, bow each need unique animations, hitboxes, timing. Content multiplier. | One melee weapon (sword) + one ranged (magic staff/spell). Two combat styles is enough variety for v1. |
| **Complex quest journal** | Multiple quests, tracking, map markers, quest log UI -- significant UI work for v1's single quest line. | One active quest from King, tracked via simple HUD text or dialogue. "Clear the dungeon." That's it. |
| **Farming automation** | Sprinklers, auto-harvesters, scarecrows with AOE -- quality of life that's nice but not essential for v1 loop. | Manual farming only. Automation rewards are v2 progression goals. |
| **Minimap** | Rendering a minimap requires a separate render pass or texture generation from tile data. | Use map transitions and consistent art direction so player knows where they are. Minimap in v2 if needed. |
| **Fishing / Mining** | Each is a complete minigame system with its own mechanics, tools, and progression. Already scoped out. | Not in v1. These are separate content pillars for v2. |
| **NPC schedules / daily routines** | Stardew NPCs walk around town on schedules. This requires pathfinding, time tables, and lots of content. | NPCs are stationary. King is in castle, shopkeeper is in shop. Always available. |

## Feature Dependencies

```
Inventory System ──────────┬──> Hotbar (subset of inventory)
                           ├──> Loot Drops (need somewhere to put items)
                           ├──> Shop System (buy/sell requires inventory)
                           └──> Equipment (equip from inventory)

HP System ─────────────────┬──> Melee Combat (damage dealing)
                           ├──> Ranged Magic (damage dealing)
                           ├──> Enemy AI (enemies need HP too)
                           └──> Death/Defeat (HP reaches 0)

Map Transitions ───────────┬──> Village Access (farm -> village)
                           ├──> Dungeon Access (village -> dungeon)
                           └──> Dungeon Room Transitions (room -> room)

Enemy AI ──────────────────┬──> Loot Drops (enemies drop on death)
                           ├──> XP System (kill grants XP)
                           └──> Boss Fight (boss is special enemy)

Dialogue System ───────────┬──> King Quest (quest given via dialogue)
                           └──> Shop NPC (shop accessed via dialogue)

XP/Leveling ───────────────┬──> Stat Growth (HP, damage, stamina increase)
                           └──> Progression Feel (numbers go up)

Graphical HUD ─────────────┬──> HP Bar Display
                           ├──> Stamina Bar Display
                           ├──> Hotbar Display
                           ├──> Gold Display
                           └──> Quest Status Display
```

## Critical Path (Build Order)

Based on dependencies, the implementation order should be:

```
Phase 1: Foundation
  Inventory + Hotbar + HUD (everything else needs these)

Phase 2: Combat Core
  HP System -> Melee Combat -> Enemy AI (basic) -> Ranged Magic

Phase 3: World
  Map Transitions -> Village + Shop + King NPC + Dialogue

Phase 4: Dungeon
  Dungeon Rooms -> Enemy Placement -> Loot Drops -> Boss Fight

Phase 5: Progression
  XP/Leveling -> Equipment Stats -> Death/Defeat -> Quest Completion

Phase 6: Polish
  Balance pass, visual feedback (hit flash, damage numbers), sound
```

## MVP Recommendation

**Absolute minimum for a satisfying v1 loop:**

1. **Inventory + Hotbar** -- Without this, nothing else works. Player cannot hold weapons, loot, or potions.
2. **HP + Melee Combat** -- The core new mechanic. Must feel responsive: attack input -> hitbox -> damage -> enemy reaction.
3. **3 Enemy types with basic AI** -- Dungeon needs threats. Skeleton (melee), Mage (ranged), Golem (tank boss).
4. **Map transitions (farm/village/dungeon)** -- Connects the three gameplay spaces.
5. **1 hand-crafted dungeon (5-8 rooms)** -- The exploration content.
6. **Loot + Gold + Shop** -- The reward loop. Kill -> loot -> sell/buy -> get stronger.
7. **XP/Leveling** -- Numbers going up. Simple but essential for "one more run" feeling.
8. **King quest as narrative wrapper** -- Gives purpose to the dungeon. "Clear the dungeon for the King."
9. **Graphical HUD** -- Replace text with sprites. Visual polish that makes everything else feel real.

**Defer to post-v1 even though tempting:**
- **Item rarity**: Nice for loot excitement but adds balancing complexity. Can ship v1 without it and add in a fast follow.
- **Night enemies on farm**: Cool differentiator but not needed for the core loop. Add after v1 is stable.
- **Weather effects**: Auto-watering is nice QoL but farming already works without it.

## Complexity Budget

Rough estimate of relative effort for the v1 scope:

| Feature Group | Relative Effort | Risk |
|---------------|----------------|------|
| Inventory + Hotbar + Equipment | 20% | Medium -- UI-heavy, lots of edge cases (full inventory, stacking, drag) |
| Combat (melee + magic + HP) | 20% | Medium -- hitbox tuning, game feel requires iteration |
| Enemy AI (3 types + boss) | 15% | Medium -- state machines are straightforward but balancing takes time |
| Dungeon (rooms + transitions) | 15% | Low -- Tiled maps, trigger zones, mostly content creation |
| HUD (graphical) | 10% | Low -- well-understood problem, sprite rendering |
| Village + Shop + Dialogue | 10% | Low -- mostly UI and data, little new engine code |
| XP + Leveling + Death | 5% | Low -- simple math, save/load extension |
| Quest system (King) | 5% | Low -- state flag tracking, dialogue variation |

## Sources

- Genre knowledge from Stardew Valley (ConcernedApe, 2016), Rune Factory 4 Special / 5 (Marvelous, 2019/2021), Moonlighter (Digital Sun, 2018), Graveyard Keeper (Lazy Bear, 2018), Forager (HopFrog, 2019)
- Confidence: MEDIUM -- based on training data genre analysis, no live web verification available
- All games cited are well-established references in the farming+RPG hybrid genre
