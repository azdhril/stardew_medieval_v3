---
phase: 05-dungeon
verified: 2026-04-14T23:00:00Z
status: human_needed
score: 4/4 must-haves verified
overrides_applied: 0
human_verification:
  - test: "Enter dungeon from village — walk to the cave entrance trigger at (880, 240) in the village and confirm the scene transitions with fade into dungeon room r1"
    expected: "Screen fades to black, then loads dungeon_r1.tmx with enemies visible and player spawned at the south threshold"
    why_human: "TriggerZone collision + SceneManager fade animation cannot be verified without running the game"
  - test: "Kill all enemies in r1, confirm the door opens and the r2 transition is unblocked"
    expected: "Colored-rect door (red closed) switches to green, player can walk through and the exit_r1_to_r2 trigger fires a transition to r2"
    why_human: "Visual sprite-swap / colored-rect feedback and collision-box toggle needs eyes"
  - test: "Navigate to optional room r3a, open the chest with E, drag items into inventory in the overlay, re-enter the room and confirm the chest shows as already opened"
    expected: "Chest overlay (ChestScene push) pauses gameplay, items are draggable, re-entry shows open-chest sprite, contents unchanged"
    why_human: "Drag-and-drop UX and overlay pause-state require human feel-check"
  - test: "Clear all 4 main rooms (r1–r4), attempt the boss door early (should block), then after r4 clear, enter boss room, defeat boss, confirm loot drops, exit to village near castle door"
    expected: "Boss door blocks until r1–r4 all cleared; boss spawns on entry; victory drops item entities, opens exit door; transition lands player near castle_door position (208, 128) in village"
    why_human: "End-to-end flow, boss fight balance, door-block messaging, and spawn position are all runtime visual behaviors"
  - test: "Talk to King NPC after boss defeat — confirm dialogue reflects quest-complete state"
    expected: "King NPC shows quest-complete branch (NPC-04) rather than the active-mission dialogue"
    why_human: "NPC dialogue branch selection requires running the game and interacting with the NPC"
  - test: "Die in the dungeon, confirm respawn at farm with dungeon reset (doors closed, chests sealed, enemies respawned, BossDefeated persists)"
    expected: "Player respawns at farm spawn; re-entering dungeon shows fresh enemies and closed doors; boss room entry still requires clearing main rooms; boss does NOT respawn after prior victory"
    why_human: "Death respawn flow, dungeon state reset, and BossDefeated persistence across sessions require a full play session"
---

# Phase 5: Dungeon Verification Report

**Phase Goal:** Players can enter and progress through a complete dungeon experience from entrance to boss room
**Verified:** 2026-04-14T23:00:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Player can enter the dungeon from the village and navigate through 5-8 connected rooms | VERIFIED | `village.tmx` has `enter_dungeon` trigger at (880,240). `VillageScene.HandleTrigger` case `enter_dungeon` calls `BeginDungeonRun` + `TransitionTo(new DungeonScene(Services, "r1", "village"))`. 7 rooms registered in `DungeonRegistry` (r1, r2, r3, r3a, r4, r4a, boss). All 7 TMXs confirmed on disk. |
| 2 | Clearing all enemies in a room opens the door to the next room (visible gate/barrier change) | VERIFIED | `DungeonScene.OnPreUpdate` fires room-clear when `_enemies.Count==0 && _room.HasGatedExit && !_clearedThisEntry`, then iterates `_doors` and calls `door.Open()`. `DungeonDoor.CollisionBox` returns `Rectangle.Empty` when open, removing the collision block. `DrawFallback` renders red (closed) / green (open) rect. |
| 3 | Optional side rooms contain treasure chests with randomized loot | VERIFIED | `DungeonChestSeeder.Seed(DungeonState)` rolls a `LootTable` using `Random(RunSeed)` and populates `DungeonState.ChestContents`. `DungeonScene.OnLoad` hydrates `ChestInstance` from `ChestContents` per chestId. r3a has `dungeon_r3a_chest`, r4a has `dungeon_r4a_chest`. `LootRollTests` proves determinism and full registry coverage. |
| 4 | The final room contains the boss; defeating it completes the dungeon objective | VERIFIED | `DungeonRegistry.Rooms["boss"].IsBossRoom==true`. `DungeonScene.OnLoad` calls `BossSpawnGate.ShouldSpawn` and `_spawner.SpawnBoss`. Victory handler in `OnPreUpdate` calls `Services.Quest?.Complete()`, sets `BossDefeated=true`, opens doors, calls `GameStateSnapshot.SaveNow`. Exit `exit_boss_to_village` routes to village at `castle_door` spawn (208, 128). |

**Score:** 4/4 truths verified (code level — manual UAT required per validation plan)

---

### Required Artifacts

| Artifact | Status | Evidence |
|----------|--------|----------|
| `tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj` | VERIFIED | File exists; 22 tests pass (`Aprovado! Falha: 0, Aprovado: 22`) |
| `src/World/DungeonState.cs` | VERIFIED | Exists; `BeginRun`, `ClearedRooms`, `OpenedChestIds`, `ChestContents`, `BossDefeated`, `RunSeed`, `IsRunActive`, `ToSnapshot`/`LoadFromSnapshot` all present |
| `src/Data/DungeonRegistry.cs` | VERIFIED | Exists; 7 rooms (r1, r2, r3, r3a, r4, r4a, boss) in `BuildRooms()` |
| `src/Data/DungeonRoomData.cs` | VERIFIED | Exists; `DungeonRoomData` record with Spawns, Chests, Exits, HasGatedExit, IsBossRoom, IsOptional |
| `src/Scenes/DungeonScene.cs` | VERIFIED | Exists; contains boss-room branch, victory handler, `OnRoomCleared` event, `GetSolids` with doors, `SpawnItemDrop` |
| `src/Combat/CombatLoop.cs` | VERIFIED | Exists; `FarmScene` delegates at line 362 (`CombatLoop.Update(deltaTime, combatCtx)`) |
| `src/Combat/EnemySpawner.cs` | VERIFIED | `SpawnAll(IEnumerable<(string id, Vector2 pos)>, List<EnemyEntity>)` — no `static readonly SpawnPoints` |
| `src/Core/GameStateSnapshot.cs` | VERIFIED | `SaveNow` populates `Chests`, `Resources`, `Dungeon` from services with prior-fallback |
| `src/World/DungeonDoor.cs` | VERIFIED | Exists; `IsOpen` toggles `CollisionBox` to `Rectangle.Empty`; `Open()`/`Close()` idempotent |
| `src/World/DungeonChestSeeder.cs` | VERIFIED | Exists; `Seed(DungeonState)` overload for testability; uses `new Random(dungeon.RunSeed)` |
| `src/Combat/BossSpawnGate.cs` | VERIFIED | Exists; `ShouldSpawn(DungeonState?) => state == null \|\| !state.BossDefeated` |
| `assets/Maps/dungeon_r1.tmx` | VERIFIED | File exists; contains `exit_r1_to_r2` and `exit_r1_to_village` trigger objects |
| `assets/Maps/dungeon_r2.tmx` | VERIFIED | File exists |
| `assets/Maps/dungeon_r3.tmx` | VERIFIED | File exists |
| `assets/Maps/dungeon_r3a.tmx` | VERIFIED | File exists (optional chest room) |
| `assets/Maps/dungeon_r4.tmx` | VERIFIED | File exists |
| `assets/Maps/dungeon_r4a.tmx` | VERIFIED | File exists (optional chest room) |
| `assets/Maps/dungeon_boss.tmx` | VERIFIED | File exists; contains `exit_boss_to_village` trigger at (208, 272) |
| `assets/Maps/dungeon_tileset.tsx` | VERIFIED | File exists |
| `tests/.../BossVictoryTests.cs` | VERIFIED | Exists; 4 tests covering quest-complete, save roundtrip, re-entry guard, BeginRun milestone preservation |

---

### Key Link Verification

| From | To | Via | Status | Evidence |
|------|----|-----|--------|----------|
| `village.tmx` | `DungeonScene` | `enter_dungeon` TriggerZone → `VillageScene.HandleTrigger` → `BeginDungeonRun` + `TransitionTo` | WIRED | `VillageScene.cs:53-55`; `village.tmx:51` has `enter_dungeon` object |
| `DungeonScene` | `DungeonDoor` | `GetSolids()` includes `_doors` | WIRED | `DungeonScene.cs:321-335` iterates `_doors` in `GetSolids`; door `CollisionBox` toggle verified |
| `DungeonScene` | `DungeonState` | `ChestContents` seeded once on `BeginRun`, hydrated on room entry | WIRED | `DungeonChestSeeder.Seed` called from `BeginDungeonRun`; `OnLoad` reads `Services.Dungeon.ChestContents[chestId]` |
| `DungeonScene (boss)` | `MainQuest` | `Services.Quest?.Complete()` on boss death | WIRED | `DungeonScene.cs:286` confirmed |
| `DungeonScene` | `VillageScene` | `exit_boss_to_village` → `TransitionTo(VillageScene)` with `TargetTrigger="castle_door"` | WIRED | Registry has `LeaveDungeon:true, TargetScene:"village", TargetTrigger:"castle_door"`; `VillageScene.Spawns["castle_door"] = (208,128)` confirmed |
| `FarmScene` | `CombatLoop` | `CombatLoop.Update(deltaTime, combatCtx)` replaces inline body | WIRED | `FarmScene.cs:362` |
| `GameStateSnapshot.SaveNow` | `DungeonState` | `services.Dungeon?.ToSnapshot()` with prior-fallback | WIRED | `GameStateSnapshot.cs:40` |

---

### Locked Decisions Honored

| Decision | Status | Evidence |
|----------|--------|----------|
| **D-01** One TMX per room, `TriggerZone` transitions | HONORED | 7 TMX files; `DungeonScene.HandleTrigger` dispatches from `_room.Exits` dict matching trigger names |
| **D-02** Linear layout: 4 main + 2 optional + 1 boss | HONORED | r1→r2→r3→r4 (main); r3a, r4a (optional side); boss (final) — 7 rooms total |
| **D-03** Hand-authored maps | HONORED | All TMXs are hand-authored XML |
| **D-04/D-05** Door states: closed blocks collision, opens on all-enemies-cleared | HONORED | `DungeonDoor.CollisionBox` toggle; `OnPreUpdate` room-clear → `door.Open()` |
| **D-06** Optional rooms not gated | HONORED | r3a/r4a exits have no `RequiresCleared` flag; `RoomClearedTests` asserts these are non-gated |
| **D-07** 2 chests total (optional rooms only) | HONORED | `dungeon_r3a_chest` in r3a, `dungeon_r4a_chest` in r4a; boss loot is `ItemDropEntity` not a chest |
| **D-08/D-09** Chest overlay E-key with drag-and-drop | HONORED | `DungeonScene.OnPreUpdate` mirrors FarmScene chest-open flow → pushes `ChestScene` overlay |
| **D-10** Chest contents sealed on `BeginRun`, idempotent on re-entry | HONORED | `DungeonChestSeeder.Seed` called once in `BeginDungeonRun`; `LootRollTests.Seed_IsDeterministic_ForSameSeed` locks this |
| **D-11** Dungeon entry from village with zero dialogue | HONORED | `enter_dungeon` trigger fires direct transition, no dialogue |
| **D-12** Boss door requires all 4 main rooms cleared | HONORED | `DungeonScene.HandleTrigger:386-394` iterates `{"r1","r2","r3","r4"}` and blocks if any uncleared |
| **D-13** Death resets run (enemies, chests, doors, loot); penalty deferred to Phase 6 | HONORED | `DungeonScene.OnPreUpdate` death path calls `Services.Dungeon.BeginRun()` + heal + `TransitionTo(FarmScene)`; `BeginRun` clears `ClearedRooms`/`OpenedChestIds`/`ChestContents` |
| **D-14** Boss defeat = dungeon complete; loot as `ItemDropEntity`; exit to village at castle door | HONORED | `BossDefeated` is persistent (not reset by `BeginRun`); boss drops via `SpawnItemDrop`; exits to `VillageScene` with `castle_door` spawn (208, 128) |

---

### Requirements Coverage

| Requirement | Description | Status | Evidence |
|-------------|-------------|--------|----------|
| **DNG-01** | 1 dungeon completa com 5-8 salas conectadas (linear com salas opcionais) | SATISFIED | 7 rooms (r1-r4 + r3a + r4a + boss); `DungeonRegistryTests.AllRooms_LoadWithoutThrow` (22 tests passing) |
| **DNG-02** | Progressao de sala: matar todos inimigos para abrir porta para proxima sala | SATISFIED | Room-clear event + `door.Open()` + `CollisionBox` toggle; `RoomClearedTests` (4 passing) |
| **DNG-03** | Baus de tesouro em salas opcionais com loot aleatorio | SATISFIED | `DungeonChestSeeder` + `DungeonState.ChestContents`; `LootRollTests` (3 passing) |
| **DNG-04** | Boss room como sala final da dungeon | SATISFIED | `dungeon_boss.tmx` + `BossSpawnGate` + victory handler → `Quest.Complete()`; `BossVictoryTests` (4 passing) |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| All 22 tests pass | `dotnet test tests/stardew_medieval_v3.Tests/stardew_medieval_v3.Tests.csproj --nologo` | `Aprovado! Falha: 0, Aprovado: 22, Ignorado: 0` | PASS |
| Build clean | `dotnet build stardew_medieval_v3.csproj --nologo` | 0 new errors; 1 pre-existing warning in `GameplayScene.cs:177` (out of scope) | PASS |
| No static SpawnPoints in EnemySpawner | `grep "static readonly.*SpawnPoints" src/Combat/EnemySpawner.cs` | No match | PASS |
| SaveNow references ChestManager and Dungeon | `grep "ChestManager\|Dungeon" src/Core/GameStateSnapshot.cs` | Both present on lines 38 and 40 | PASS |
| BossDefeated NOT cleared in BeginRun | `grep "BossDefeated" src/World/DungeonState.cs` | Comment confirms intentional omission; `BossVictoryTests.BeginRun_DoesNotClear_BossDefeatedMilestone` locks it | PASS |
| Village.tmx has enter_dungeon trigger | `grep "enter_dungeon" assets/Maps/village.tmx` | Object id=20 at (880,240) | PASS |
| Boss TMX has exit trigger | `grep "exit_boss_to_village" assets/Maps/dungeon_boss.tmx` | Object id=5 at (208,272) | PASS |

---

### Anti-Patterns Scan

No stub patterns found in production code that are load-bearing:

- `DungeonScene.SpawnItemDrop` is a real method (Plan 03 fixed the log-only stub from Plan 02)
- `LootRollTests` stub from Plan 01 was filled in by Plan 02
- No `return null` / `return {}` / `return []` in dungeon paths that render to player
- `DungeonDoor.DrawFallback` colored-rect is an intentional fallback (no art asset), not a stub — it is functional and visible

One deferred cosmetic item:
- **Door sprite art**: `DungeonDoor` uses `DrawFallback` (red/green rect) because no dungeon-door sprite sheet has been authored. This is a visual polish gap, not a functional one. The door is fully functional (blocks collision, opens on clear, color changes). Classified as **Info** — does not affect playability.

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `src/World/DungeonDoor.cs` | Colored-rect fallback instead of sprite sheet | Info | Visual only; door functions correctly |

---

### Human Verification Required

#### 1. Village → Dungeon Entry

**Test:** Walk player to the cave entrance at the east side of the village map (approx tile 55, 15) until the `enter_dungeon` trigger fires
**Expected:** Fade-to-black → dungeon_r1.tmx loads with 2 Skeleton enemies visible, player spawned near south wall
**Why human:** TriggerZone AABB collision and SceneManager fade animation require runtime observation

#### 2. Room-Clear Door Opening

**Test:** Enter r1, kill both Skeleton enemies, observe the north door
**Expected:** Door changes from red (blocked) to green (open); player can now walk through and `exit_r1_to_r2` trigger fires transition to r2
**Why human:** Visual sprite-swap (colored-rect feedback) and collision-box removal need eyes; sequential room navigation r1→r2→r3→r4 must be confirmed

#### 3. Optional Room Chest Overlay UX

**Test:** Navigate to r3a (east detour from r3), press E near the chest, drag items into inventory, close overlay, re-enter the room
**Expected:** ChestScene overlay pauses gameplay background; drag-and-drop works; chest shows open sprite on re-entry; contents not re-rolled
**Why human:** Drag-and-drop UX and overlay pause-state require human feel-check (per VALIDATION.md)

#### 4. Boss Gate Enforcement + Full Victory Flow

**Test:** After clearing r1–r3 only, attempt to walk through the boss door in r4 (should block). Then clear r4, re-attempt. Enter boss room, fight skeleton_king, defeat it.
**Expected:** Pre-clear: log message visible, player blocked. Post-clear: door opens. Boss spawns at center. On defeat: item drops appear on floor, exit door opens, transition goes to village near castle door (208, 128 — in front of castle).
**Why human:** Boss fight balance, door-block enforcement in-game, item drop spawning, and spawn position all require runtime confirmation (per VALIDATION.md)

#### 5. King Quest-Complete Dialogue (NPC-04)

**Test:** After boss defeat, walk to castle and talk to King NPC
**Expected:** King's dialogue shows quest-complete branch, not active-mission dialogue
**Why human:** NPC-04 branch selection exercises Phase 4 dialogue state machine; requires full session (per VALIDATION.md)

#### 6. Death Reset Semantics

**Test:** Enter dungeon, progress to r3, die to an enemy
**Expected:** Respawn at farm; re-enter dungeon: doors are closed, enemies present, chests sealed (if previously opened). If boss was previously defeated this session, boss room re-entry should NOT spawn boss again.
**Why human:** Death transition flow, dungeon state reset visibility, and BossDefeated persistence across death all need a live session (per VALIDATION.md)

---

### Gaps Summary

No automated gaps found. All 4 ROADMAP success criteria are code-verified. All locked context decisions (D-01 through D-14) are honored in the codebase. All 22 unit tests pass. The dungeon is complete at the code level.

The `human_needed` status reflects the 6 manual UAT items defined in `05-VALIDATION.md` — none of which can be verified programmatically (visual transitions, drag-and-drop UX, boss fight balance, NPC dialogue branch). These are expected and pre-planned, not newly discovered gaps.

---

_Verified: 2026-04-14T23:00:00Z_
_Verifier: Claude (gsd-verifier)_
