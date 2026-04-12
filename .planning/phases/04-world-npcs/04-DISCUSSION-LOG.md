# Phase 4: World & NPCs - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in 04-CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-12
**Phase:** 4-world-npcs
**Areas discussed:** Village layout & scope, Dialogue UX, Shop UX, Quest state & tracker

---

## Village Layout & Scope

### Q: How big should the village map be?
| Option | Description | Selected |
|--------|-------------|----------|
| Single screen (960×540) | One view, no scrolling, Castle + Shop + path | ✓ |
| Small scrollable (~2 screens) | Camera scrolls, room for decorations | |
| Medium scrollable (~4 screens) | Bigger town feel with streets | |

### Q: Which NPCs should live in the village for this phase?
| Option | Description | Selected |
|--------|-------------|----------|
| Just King + Shopkeeper | Minimum for success criteria | ✓ |
| King + Shopkeeper + 1 flavor NPC | Adds ambience | |
| King + Shopkeeper + Innkeeper | Third NPC with a function | |

### Q: Castle/Shop interior — separate scenes or same village scene?
| Option | Description | Selected |
|--------|-------------|----------|
| Separate scenes via door trigger | Door = transition to CastleScene / ShopScene | ✓ |
| Same scene, NPC stands outside | No interiors, simpler, loses door trigger | |
| Same scene, fade into interior overlay | Overlay instead of scene swap | |

---

## Dialogue UX

### Q: Dialogue advance — which input and pacing?
| Option | Description | Selected |
|--------|-------------|----------|
| E/Space with typewriter | Char-by-char reveal, E/Space skip+advance | ✓ |
| E/Space instant | Full text shown immediately | |
| Mouse click + typewriter | Click to advance | |

### Q: Portrait style — where and what kind?
| Option | Description | Selected |
|--------|-------------|----------|
| Left side box, static portrait | Portrait on left inside bottom dialogue box | ✓ |
| Above box, static portrait | Portrait floats above text box | |
| No portrait, name label only | Text-only | |

### Q: Dialogue structure — linear or with choices?
| Option | Description | Selected |
|--------|-------------|----------|
| Linear only for this phase | Sequential lines, no choices | ✓ |
| Linear + simple yes/no on King | Accept/Decline quest prompt | |
| Full branching choice support | Any NPC branching | |

---

## Shop UX

### Q: Shop screen layout — buy and sell together or separate?
| Option | Description | Selected |
|--------|-------------|----------|
| Tabs: Buy \| Sell | Two tabs in one shop UI | ✓ |
| Single list with mode toggle | Toggle between buy/sell context | |
| Separate dialogue options | Dialogue picks screen to open | |

### Q: Shopkeeper stock — what does he sell in Phase 4?
| Option | Description | Selected |
|--------|-------------|----------|
| Seeds + basic consumables + starter weapon/armor | Matches NPC-03, ~6–10 items | ✓ |
| Seeds + potions only | Minimal stock | |
| Full catalog | Breaks progression | |

### Q: Transaction confirmation — how much friction?
| Option | Description | Selected |
|--------|-------------|----------|
| Click/select + buy, no popup | Instant with a small toast | ✓ |
| Confirmation dialog on every purchase | Yes/No popup | |
| Hold-to-buy on expensive items only | Threshold-gated hold | |

### Q: What happens if player can't afford / inventory full?
| Option | Description | Selected |
|--------|-------------|----------|
| Button disabled + reason text | Greyed out with label | ✓ |
| Allow click, show error toast | Flash error on attempt | |
| Hide unaffordable items | Filter out what they can't buy | |

---

## Quest State & Tracker

### Q: Quest data model — how is quest state represented?
| Option | Description | Selected |
|--------|-------------|----------|
| Enum states on a single MainQuest | `QuestState` enum on GameState | ✓ |
| Generic Quest list with flags dictionary | Dictionary-based, future-proof | |
| Hardcoded booleans | Two bools on GameState | |

### Q: What triggers the quest to flip to Complete?
| Option | Description | Selected |
|--------|-------------|----------|
| Placeholder flag for now; wired in Phase 5 | `SetQuestComplete()` hook + debug key | ✓ |
| Return to King after obtaining a 'proof' item | Requires missing item-drop system | |
| Time-based / automatic | Doesn't match roadmap intent | |

### Q: Quest tracker — where does it show on HUD?
| Option | Description | Selected |
|--------|-------------|----------|
| Top-right corner, text only | Always visible plain text | ✓ |
| Top-right, toggleable with Q key | Hidden by default | |
| Only in pause/inventory screen | No always-on tracker | |

### Q: Does quest state affect which NPCs' dialogue changes (NPC-04)?
| Option | Description | Selected |
|--------|-------------|----------|
| Both King and Shopkeeper react | Three variants each on quest state | ✓ |
| Only King reacts | Minimal NPC-04 proof | |
| All future NPCs will, but Phase 4 only wires King | Defers shopkeeper reaction | |

---

## Claude's Discretion

- Portrait art (placeholders acceptable).
- Trigger-zone implementation approach (planner picks based on TileMap patterns).
- Item prices and exact shop stock.
- Dialogue text wording.
- Scene-transition spawn points.

## Deferred Ideas

- Flavor/idle villagers.
- Innkeeper / sleep-save NPC.
- Branching dialogue choices.
- Hold-to-buy for expensive items.
- Full quest list data structure (multi-quest).
- Graphical quest tracker (HUD-04 / Phase 6).
- Shop stock rotation / daily refresh / limited quantities.
