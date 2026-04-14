---
phase: 04-world-npcs
plan: 01
subsystem: quest-foundation
tags: [quest, npc, save-migration, trigger-zones, gold-api]
requirements: [WLD-03, WLD-04, NPC-04]
dependency_graph:
  requires:
    - "src/Core/Entity.cs (base class)"
    - "src/Core/GameState.cs (QuestState int field pre-existed since v3)"
    - "src/Inventory/InventoryManager.cs (existing slot/equip API)"
    - "src/World/TileMap.cs (TMX object layer parsing pattern)"
  provides:
    - "Quest.MainQuestState enum"
    - "Quest.MainQuest container + OnQuestStateChanged event"
    - "Entities.NpcEntity base class"
    - "World.TriggerZone record + TileMap.Triggers list"
    - "InventoryManager.Gold + TrySpendGold + OnGoldChanged"
    - "ServiceContainer.Quest slot"
    - "Save v5 with migration from v3/v4"
  affects:
    - "Downstream plans 04-02 (scenes), 04-03 (dialogue), 04-04 (shop)"
tech-stack:
  added: []
  patterns:
    - "Event-pub model (mirrors TimeManager.OnDayAdvanced)"
    - "Records for immutable data (TriggerZone)"
    - "Static migration chain in SaveManager"
key-files:
  created:
    - "src/Quest/MainQuestState.cs"
    - "src/Quest/MainQuest.cs"
    - "src/Entities/NpcEntity.cs"
    - "src/World/TriggerZone.cs"
  modified:
    - "src/World/TileMap.cs"
    - "src/Inventory/InventoryManager.cs"
    - "src/Core/ServiceContainer.cs"
    - "src/Core/SaveManager.cs"
    - "src/Scenes/FarmScene.cs"
decisions:
  - "Wired MainQuest in FarmScene (actual composition root) instead of Game1.cs (plan-specified root is a thin shell)"
  - "MainQuest.Complete() allowed from any non-Complete state so dev/debug hook can fast-forward"
  - "LoadFromState clamps incoming QuestState to [0,2] as defense-in-depth (belt+suspenders with migration clamp)"
metrics:
  duration: "~15 min"
  completed: "2026-04-12"
  tasks: 3
  files_touched: 9
---

# Phase 04 Plan 01: Foundation Contracts Summary

Lay down the foundation contracts every other Phase 4 plan consumes: quest state model, NPC base class, trigger-zone plumbing in TileMap, save migration to v5, and Gold API on InventoryManager -- all additive, zero runtime behavior change to existing Phase 1-3 gameplay.

## Contracts Exposed

```csharp
// src/Quest/MainQuestState.cs
public enum MainQuestState { NotStarted = 0, Active = 1, Complete = 2 }

// src/Quest/MainQuest.cs
public class MainQuest {
    public MainQuestState State { get; private set; }
    public event Action<MainQuestState>? OnQuestStateChanged;
    public void Activate();   // NotStarted -> Active (no-op otherwise)
    public void Complete();   // any -> Complete (no-op if already complete)
    public void LoadFromState(GameState state);
    public void SaveToState(GameState state);
}

// src/Entities/NpcEntity.cs : Core.Entity
public class NpcEntity : Entity {
    public const float InteractRange = 28f;
    public string NpcId { get; }
    public Texture2D? Portrait { get; }
    public NpcEntity(string npcId, Texture2D sprite, Texture2D? portrait, Vector2 position);
    public bool IsInInteractRange(Vector2 playerPos);
    public override void Draw(SpriteBatch spriteBatch);
}

// src/World/TriggerZone.cs
public record TriggerZone(string Name, Rectangle Bounds) {
    public bool ContainsPoint(Vector2 p);
}

// src/World/TileMap.cs (additions)
public IReadOnlyList<TriggerZone> Triggers { get; }
// LoadTriggerObjects() parses ObjectLayer name=="Triggers" (case-insensitive), tolerant of missing/empty groups.

// src/Inventory/InventoryManager.cs (additions)
public int Gold { get; private set; }
public event Action? OnGoldChanged;
public void SetGold(int value);         // clamps >= 0
public void AddGold(int amount);         // rejects negative + logs
public bool TrySpendGold(int amount);    // false if negative or > Gold
// Load/SaveToState round-trip Gold

// src/Core/ServiceContainer.cs (additions)
public MainQuest? Quest { get; set; }
```

## Save Migration Notes

- **CURRENT_SAVE_VERSION: 4 -> 5**.
- `GameState.QuestState` (int) already existed since v3 migration, so v5 is a pure semantic normalization -- no new fields added to the serialization surface.
- v4 -> v5 branch clamps `QuestState` to `[0, 2]` so any tampered or corrupted save can't cast to an OOB `MainQuestState` enum value (T-04-01 / T-04-02 mitigations).
- `MainQuest.LoadFromState` also clamps defensively (belt+suspenders).
- Existing v3 and v4 saves load without intervention; new saves write v5.
- No `savegame.json` existed in working copy at execution time, so the migration is code-verified only. Downstream plans that exercise a save roundtrip will smoke-test it.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `19f7733` | MainQuestState, MainQuest, NpcEntity, TriggerZone (new files) |
| 2 | `dff8c4d` | TileMap triggers, Gold API, ServiceContainer.Quest slot |
| 3 | `5d57ee2` | SaveManager v5 migration + FarmScene MainQuest wiring |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Composition root is FarmScene, not Game1**
- **Found during:** Task 3
- **Issue:** Plan instructed wiring `_services.Quest = mainQuest` in `Game1.cs`, but `Game1.cs` is a 79-line thin shell that only creates camera/time/input; the actual composition-root where `InventoryManager` is instantiated, `SaveManager.Load` is called, and `SaveManager.Save` is issued is `src/Scenes/FarmScene.cs` (lines 80, 113, 649 in the pre-change file).
- **Fix:** Wired MainQuest alongside InventoryManager in FarmScene: instantiation + `Services.Quest = _mainQuest` after inventory (line ~83), `_mainQuest.LoadFromState(save)` after inventory load (line ~122), `_mainQuest.SaveToState(state)` before `SaveManager.Save(state)` (line ~651). Also added debug log `[FarmScene] MainQuest state loaded: {state}` per plan's traceability requirement.
- **Files modified:** `src/Scenes/FarmScene.cs`
- **Commit:** `5d57ee2`

No other deviations. No auth gates encountered. No architectural questions required.

## Interfaces Block Deltas

All interfaces in the plan's `<interfaces>` block were implemented as specified, with two minor behavior refinements that are strictly additive:

1. `MainQuest.Complete()` transitions from **any** non-Complete state (spec said `Active -> Complete`). This enables the Phase 4 `SetQuestComplete()` dev/debug hook (D-12) to fast-forward directly from NotStarted without first calling Activate.
2. `MainQuest.LoadFromState` clamps the raw int to `[0,2]` before casting to enum — defense-in-depth beyond the migration-layer clamp.

## Verification

- `dotnet build stardew_medieval_v3.csproj -c Debug --nologo -v q` -> **0 warnings, 0 errors** on every task boundary.
- Grep checks passed: all literal strings named in each task's `acceptance_criteria` are present.
- Existing farm map (`assets/Maps/test_farm.tmx`) has no `Triggers` object group; `TileMap.Triggers` returns empty list -- baseline unchanged.

## Self-Check: PASSED

- src/Quest/MainQuestState.cs: FOUND (contains `NotStarted = 0` and `Complete = 2`)
- src/Quest/MainQuest.cs: FOUND (contains `public event Action<MainQuestState>? OnQuestStateChanged`)
- src/Entities/NpcEntity.cs: FOUND (contains `public const float InteractRange = 28f`)
- src/World/TriggerZone.cs: FOUND (contains `public record TriggerZone(string Name, Rectangle Bounds)`)
- src/World/TileMap.cs: modified (contains `LoadTriggerObjects` + `public IReadOnlyList<TriggerZone> Triggers`)
- src/Inventory/InventoryManager.cs: modified (contains `TrySpendGold`, `OnGoldChanged`, Gold round-trip)
- src/Core/ServiceContainer.cs: modified (contains `public MainQuest? Quest`)
- src/Core/SaveManager.cs: modified (contains `CURRENT_SAVE_VERSION = 5` and `state.SaveVersion < 5` branch)
- src/Scenes/FarmScene.cs: modified (contains `Services.Quest = _mainQuest`)
- Commits 19f7733, dff8c4d, 5d57ee2: all FOUND in git log
