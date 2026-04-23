---
phase: quick-260423-shd
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Core/GameState.cs
  - src/Core/SaveManager.cs
  - src/Core/GameStateSnapshot.cs
  - src/Core/ServiceContainer.cs
  - src/Core/GameplayScene.cs
  - src/Scenes/FarmScene.cs
  - Game1.cs
autonomous: false
requirements:
  - QUICK-260423-shd-restore-scene-and-position
must_haves:
  truths:
    - "Player saves in Village, closes game, reopens -> spawns in Village at the exact last position"
    - "Player saves in Castle/Shop, closes game, reopens -> spawns in that interior at last position"
    - "Player saves in Farm, closes game, reopens -> spawns in Farm at last position (not tile 10,10)"
    - "Existing v9 saves still load after update (no data loss); migrated to v10 with Farm position preserved"
    - "Position in Farm does not stomp position in Village when you move between them (each scene remembers its own spot)"
    - "Dungeon scenes remain boot-exempt: if CurrentScene is a Dungeon:* variant at save time, boot falls back to Farm (no partially-restored dungeon runs)"
  artifacts:
    - path: "src/Core/GameState.cs"
      provides: "PositionByScene dictionary + SaveVersion=10"
      contains: "public Dictionary<string, ScenePosition> PositionByScene"
    - path: "src/Core/SaveManager.cs"
      provides: "v9 -> v10 migration seeds PositionByScene[\"Farm\"] from legacy PlayerX/Y"
      contains: "state.SaveVersion < 10"
    - path: "src/Core/GameStateSnapshot.cs"
      provides: "SaveNow writes current scene's live position into PositionByScene[currentScene]"
      contains: "PositionByScene"
    - path: "src/Core/ServiceContainer.cs"
      provides: "PendingRestoreScene one-shot + PendingRestorePosition one-shot"
      contains: "public string? PendingRestoreScene"
    - path: "src/Core/GameplayScene.cs"
      provides: "LoadContent consumes PendingRestorePosition for this SceneName before applying GetSpawn"
      contains: "PendingRestorePosition"
    - path: "src/Scenes/FarmScene.cs"
      provides: "After OnLoad, if PendingRestoreScene is a non-Farm non-Dungeon scene, issue TransitionTo on next frame"
      contains: "PendingRestoreScene"
    - path: "Game1.cs"
      provides: "Pre-reads save, seeds PendingRestoreScene/Position on ServiceContainer before pushing FarmScene"
      contains: "SaveManager.Load"
  key_links:
    - from: "Game1.LoadContent"
      to: "ServiceContainer.PendingRestoreScene"
      via: "read save, set one-shot flags before PushImmediate(FarmScene)"
      pattern: "PendingRestoreScene\\s*="
    - from: "FarmScene.OnLoad (end)"
      to: "SceneManager.TransitionTo"
      via: "consume PendingRestoreScene, build target scene with fromScene=\"RestoreSave\", enqueue transition"
      pattern: "TransitionTo\\("
    - from: "GameplayScene.LoadContent"
      to: "Player.Position"
      via: "override GetSpawn result with PositionByScene[SceneName] when present"
      pattern: "PendingRestorePosition|PositionByScene"
    - from: "GameStateSnapshot.SaveNow"
      to: "state.PositionByScene[currentScene]"
      via: "clone prior dict, overwrite entry for Services.GameState.CurrentScene"
      pattern: "PositionByScene\\["
---

<objective>
Fazer o jogo reabrir na última cena e posição onde o jogador estava, sem quebrar saves
antigos e sem stompar a posição de uma cena ao visitar outra.

Purpose: O save v9 já persiste `CurrentScene` mas o boot ignora — sempre empurra FarmScene
e stampa posição em `TileCenterWorld(10,10)`. Além disso, `GameState.PlayerX/Y` é um único
par de coordenadas compartilhado por todas as cenas (Farm/Village/Castle/Shop usam sistemas
de coordenadas independentes, então uma posição salva na Village não faz sentido na Farm).
Esta task corrige ambos.

Output: save bump v9→v10 com `PositionByScene` dict, migração segura para saves antigos,
Game1 consulta o save antes de empurrar cena, e GameplayScene aplica a posição salva da
própria cena na entrada de boot (não em transições normais por porta).
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@CLAUDE.md
@.planning/STATE.md

@src/Core/GameState.cs
@src/Core/SaveManager.cs
@src/Core/GameStateSnapshot.cs
@src/Core/GameplayScene.cs
@src/Core/ServiceContainer.cs
@src/Core/SceneManager.cs
@src/Scenes/FarmScene.cs
@src/Scenes/VillageScene.cs
@src/Scenes/CastleScene.cs
@src/Scenes/ShopScene.cs
@src/Scenes/DungeonScene.cs
@Game1.cs

<interfaces>
<!-- Key types/contracts the executor needs. Extracted from source. -->

From src/Core/GameState.cs:
```csharp
public class GameState
{
    public int SaveVersion { get; set; } = 9;   // bump to 10 in this task
    public int DayNumber { get; set; } = 1;
    public float PlayerX { get; set; }           // keep for backcompat (migrated, then unused)
    public float PlayerY { get; set; }
    public string CurrentScene { get; set; } = "Farm";
    // ... other fields unchanged
}
```

From src/Core/SaveManager.cs:
```csharp
public static class SaveManager
{
    private const int CURRENT_SAVE_VERSION = 9;   // bump to 10
    public static void Save(GameState state);
    public static GameState? Load();              // returns null if no save
    private static void MigrateIfNeeded(GameState state); // chain-of-ifs per prior version
}
```

From src/Core/ServiceContainer.cs — has `GameState? GameState` slot already, plus `Player`,
`Inventory`, `SceneManager`, etc. Add two new nullable one-shot flags.

From src/Core/GameplayScene.cs:
```csharp
public abstract class GameplayScene : Scene
{
    protected abstract string SceneName { get; }           // "Farm" | "Village" | "Castle" | "Shop" | "Dungeon:{roomId}"
    protected virtual Vector2 GetSpawn(string fromScene);   // scene-specific default spawn
    protected string FromScene { get; }                     // ctor param
    public override void LoadContent() {
        // ... Player.Position = GetSpawn(FromScene);
        // ... Services.GameState.CurrentScene = SceneName;   (line ~161)
    }
}
```

From src/Core/SceneManager.cs:
```csharp
public void PushImmediate(Scene scene);           // used at startup, no fade
public void TransitionTo(Scene newScene);          // fade out, swap stack, fade in. Ignored if transition in progress.
public bool IsTransitioning { get; }
```

From src/Scenes/FarmScene.cs:
```csharp
public FarmScene(ServiceContainer services, string fromScene = "Fresh") : base(services, fromScene) { }
protected override string SceneName => "Farm";
// FarmScene.OnLoad line 245: `_loadedState.CurrentScene = "Farm";`  ← REMOVE
// FarmScene.BuildCurrentStateSnapshot line 734: `state.CurrentScene = "Farm";`  ← REMOVE
```

From Game1.cs:
```csharp
protected override void LoadContent() {
    // ... services created ...
    _sceneManager.PushImmediate(new FarmScene(_services));   // hardcoded — needs save-aware routing
}
```
</interfaces>

# Decision notes (read before implementing)

1. **Scopes that we restore**: Farm, Village, Castle, Shop. Dungeons are **not** restored:
   their SceneName is `"Dungeon:{roomId}"`, a dungeon run is per-session state (DungeonState
   is rebuilt on every entry via `BeginRun`), and landing in a half-constructed dungeon scene
   after boot is worse UX than landing on the Farm. If `save.CurrentScene` starts with
   `"Dungeon:"`, boot treats it as Farm (but still restores Farm's saved position).

2. **Boot routing strategy**: keep FarmScene as the mandatory first scene (it owns Player,
   Inventory, Atlas, Hotbar, Progression, Quest construction). After FarmScene.OnLoad
   finishes, if the saved scene is non-Farm (Village/Castle/Shop), issue a `TransitionTo`
   on the **first Update frame** so the fade animation reads as intentional rather than
   the game flickering on boot.

3. **Position restore is one-shot, not per-scene-entry**: Saving populates
   `PositionByScene[SceneName]` for every scene the player visits. But on a normal
   door transition (e.g. Farm → Village via trigger), we want the spawn marker at the door,
   not the last-visited position on the far side of the map. So restore only fires on boot:
   Game1 seeds `ServiceContainer.PendingRestorePosition` once, the first matching scene
   consumes it (sets to null), subsequent entries use normal `GetSpawn`.

4. **Stop force-setting CurrentScene in FarmScene**: GameplayScene.LoadContent already
   writes `Services.GameState.CurrentScene = SceneName` on line 161. FarmScene.OnLoad:245
   and FarmScene.BuildCurrentStateSnapshot:734 re-stamping `"Farm"` is (a) redundant for Farm
   itself and (b) actively wrong during boot because it runs *before* Game1's routing can
   read the pre-load scene name. Removing them is safe — the base class is authoritative.

5. **Migration**: v9 saves have `PlayerX/Y` that implicitly refer to Farm coordinates
   (since boot always dumped you in Farm). v10 adds `PositionByScene` and the migration
   seeds `PositionByScene["Farm"] = (PlayerX, PlayerY)`. PlayerX/Y stay in the schema for
   backcompat but `GameStateSnapshot.SaveNow` stops writing them starting v10 (we still
   mirror the current scene's position into PlayerX/Y defensively so a downgrade doesn't
   corrupt completely — low cost, trivial fallback).

<tasks>

<task type="auto">
  <name>Task 1: Save schema + migration (v9 → v10 with PositionByScene)</name>
  <files>src/Core/GameState.cs, src/Core/SaveManager.cs, src/Core/GameStateSnapshot.cs, src/Core/ServiceContainer.cs</files>
  <action>
  Add the per-scene position data model, bump save version, and make SaveNow/Load
  preserve/load the new dict. No scene-routing logic yet — that comes in Task 2.

  **src/Core/GameState.cs:**

  Add a simple value carrier at the bottom of the file (outside `GameState`, alongside
  `FarmCellSaveData` etc.):

  ```csharp
  /// <summary>
  /// Serializable player position for a given scene. Stored in
  /// <see cref="GameState.PositionByScene"/> so each scene remembers its own spot.
  /// </summary>
  public class ScenePosition
  {
      public float X { get; set; }
      public float Y { get; set; }
  }
  ```

  Inside `GameState`:
  - Change default: `public int SaveVersion { get; set; } = 10;` (was 9).
  - Add a new field, grouped as `// === New (v10): per-scene position ===`:
    ```csharp
    public Dictionary<string, ScenePosition> PositionByScene { get; set; } = new();
    ```
  - Leave `PlayerX/PlayerY` fields alone (backcompat for downgrades; still written defensively).
  - Leave `CurrentScene` alone (already present, default `"Farm"`).

  **src/Core/SaveManager.cs:**

  - Change `private const int CURRENT_SAVE_VERSION = 9;` → `= 10`.
  - Append a new migration block after the `SaveVersion < 9` block:
    ```csharp
    if (state.SaveVersion < 10)
    {
        // v9 -> v10: per-scene position. Legacy PlayerX/Y is implicitly Farm (boot
        // always dumped the player on the farm in v9 and earlier), so seed Farm
        // from those fields. Other scenes get spawn defaults on next entry.
        state.PositionByScene ??= new();
        if (!state.PositionByScene.ContainsKey("Farm"))
        {
            state.PositionByScene["Farm"] = new ScenePosition
            {
                X = state.PlayerX,
                Y = state.PlayerY,
            };
        }
        // Clamp a historical oddity: some v9 saves may have CurrentScene="Dungeon:*"
        // from a crash mid-run. Those scenes aren't restorable — normalize to Farm.
        if (!string.IsNullOrEmpty(state.CurrentScene) && state.CurrentScene.StartsWith("Dungeon:"))
        {
            Console.WriteLine($"[SaveManager] v9->v10: normalizing CurrentScene '{state.CurrentScene}' -> Farm");
            state.CurrentScene = "Farm";
        }
        state.SaveVersion = 10;
        Console.WriteLine($"[SaveManager] Migrated v9->v10 (PositionByScene seeded: Farm=({state.PlayerX},{state.PlayerY}))");
    }
    ```
  - Do NOT touch any existing migration block.

  **src/Core/GameStateSnapshot.cs:**

  In `SaveNow`, after the `var prior = services.GameState;` line, build the per-scene
  dict by **cloning** prior's entries (so positions saved in other scenes aren't wiped)
  then overwriting the entry for the **currently active scene** with the live player
  position. Key source: prefer `prior?.CurrentScene` (authoritative, set by
  `GameplayScene.LoadContent`) and fall back to `"Farm"` for safety.

  Insert before the `var state = new GameState { ... }` block:

  ```csharp
  // v10: per-scene position. Clone prior dict, then overwrite the entry for the
  // scene we're currently inside so each scene keeps its own last-known spot.
  var positionByScene = new Dictionary<string, ScenePosition>(
      prior?.PositionByScene ?? new Dictionary<string, ScenePosition>());
  string currentScene = prior?.CurrentScene ?? "Farm";
  // Skip dungeon scenes — their positions are intra-run only and the SceneName
  // encodes a specific room (Dungeon:r1, Dungeon:boss) we don't want to land on at boot.
  if (services.Player != null && !string.IsNullOrEmpty(currentScene)
      && !currentScene.StartsWith("Dungeon:"))
  {
      positionByScene[currentScene] = new ScenePosition
      {
          X = services.Player.Position.X,
          Y = services.Player.Position.Y,
      };
  }
  ```

  Then in the `new GameState { ... }` initializer, add:
  ```csharp
  PositionByScene = positionByScene,
  ```
  Leave the existing `PlayerX = services.Player?.Position.X ?? ...` and `PlayerY = ...`
  lines alone (still written defensively for downgrade compat; they reflect whichever
  scene the player is currently in).

  **src/Core/ServiceContainer.cs:**

  Add two one-shot restore flags near the `GameState? GameState` slot (line ~56):

  ```csharp
  /// <summary>
  /// One-shot scene restore target set by Game1.LoadContent from the loaded save.
  /// FarmScene reads this at end of OnLoad and, if non-null and not "Farm", issues
  /// a TransitionTo on the next frame. Consumer MUST set to null after consumption.
  /// </summary>
  public string? PendingRestoreScene { get; set; }

  /// <summary>
  /// One-shot restore position for a specific scene. Set by Game1 from
  /// <see cref="GameState.PositionByScene"/> for whichever scene we're about to enter.
  /// Consumed by GameplayScene.LoadContent when SceneName matches
  /// <see cref="PendingRestoreSceneName"/>. Use <c>null</c> to indicate no restore.
  /// </summary>
  public Vector2? PendingRestorePosition { get; set; }

  /// <summary>Name of the scene whose position is queued in <see cref="PendingRestorePosition"/>.</summary>
  public string? PendingRestoreSceneName { get; set; }
  ```

  You'll need `using Microsoft.Xna.Framework;` at the top of ServiceContainer.cs if not
  already present — check the existing using list first.
  </action>
  <verify>
    <automated>dotnet build stardew_medieval_v3.csproj</automated>
  </verify>
  <done>
    - Project compiles with zero new warnings.
    - GameState has `PositionByScene` dict and `ScenePosition` class; default SaveVersion=10.
    - SaveManager CURRENT_SAVE_VERSION=10 and has a `SaveVersion < 10` migration block that
      seeds Farm from PlayerX/Y and normalizes stray Dungeon:* CurrentScene values.
    - GameStateSnapshot.SaveNow clones prior PositionByScene, overwrites the current scene's
      entry (but skips Dungeon:* scenes), and writes the dict into the new state.
    - ServiceContainer has `PendingRestoreScene`, `PendingRestorePosition`, `PendingRestoreSceneName` slots.
    - Loading a v9 save and saving again produces a v10 save with PositionByScene["Farm"] populated.
  </done>
</task>

<task type="auto">
  <name>Task 2: Boot routing + position restore + remove force-stamp of CurrentScene</name>
  <files>Game1.cs, src/Core/GameplayScene.cs, src/Scenes/FarmScene.cs</files>
  <action>
  Wire Game1 to read the save and seed the restore flags, have GameplayScene consume
  the position flag, and have FarmScene route to the saved scene after its OnLoad. Also
  delete the two force-stamps of `CurrentScene="Farm"` in FarmScene.

  **Game1.cs — LoadContent (around line 111, right before `_sceneManager.PushImmediate`):**

  Replace the single hardcoded push with save-aware routing. Read the save, seed
  ServiceContainer one-shot flags, then always push FarmScene (it's the bootstrap
  scene that owns Player/Inventory/Atlas/Hotbar creation). FarmScene's OnLoad will
  handle the hop to the saved scene.

  ```csharp
  // Pre-read save so boot routing knows which scene to land on.
  // Always push FarmScene first (bootstrap): it constructs Player, Inventory, Atlas,
  // Hotbar, Progression, Quest. If the saved scene is non-Farm (Village/Castle/Shop),
  // FarmScene will TransitionTo it on the next frame after its OnLoad completes.
  // Dungeon:* saved scenes are intentionally normalized to Farm during v9->v10 migration.
  var bootSave = SaveManager.Load();
  if (bootSave != null)
  {
      string target = bootSave.CurrentScene ?? "Farm";
      if (string.IsNullOrEmpty(target) || target.StartsWith("Dungeon:"))
          target = "Farm";

      _services.PendingRestoreScene = target;
      _services.PendingRestoreSceneName = target;
      if (bootSave.PositionByScene != null
          && bootSave.PositionByScene.TryGetValue(target, out var scenePos))
      {
          _services.PendingRestorePosition = new Vector2(scenePos.X, scenePos.Y);
          Console.WriteLine($"[Game1] Boot restore queued: scene={target} pos=({scenePos.X},{scenePos.Y})");
      }
      else
      {
          Console.WriteLine($"[Game1] Boot restore queued: scene={target} (no position entry — will use spawn default)");
      }
  }

  // Push initial scene
  _sceneManager.PushImmediate(new FarmScene(_services));
  ```

  Add `using Microsoft.Xna.Framework;` and `using stardew_medieval_v3.Core;` at the top
  of Game1.cs if not already present (Vector2 lives in the first; SaveManager in the second).

  **src/Core/GameplayScene.cs — LoadContent (around line 154, right after `Player.Position = GetSpawn(FromScene);`):**

  Insert a position-restore override that fires only when the current scene matches the
  queued restore. Consume (null out) the flags so the next scene entry uses normal spawn.

  ```csharp
  Player.Position = GetSpawn(FromScene);

  // v10: one-shot position restore on boot. If Game1 queued a restore for THIS scene,
  // override the spawn. Subsequent entries (e.g. door transitions) skip this and use
  // the normal GetSpawn marker.
  if (Services.PendingRestorePosition.HasValue
      && !string.IsNullOrEmpty(Services.PendingRestoreSceneName)
      && string.Equals(Services.PendingRestoreSceneName, SceneName, StringComparison.Ordinal))
  {
      var restored = Services.PendingRestorePosition.Value;
      Player.Position = restored;
      Services.PendingRestorePosition = null;
      Services.PendingRestoreSceneName = null;
      Console.WriteLine($"[{SceneName}Scene] Boot position restored to ({restored.X},{restored.Y})");
  }
  ```

  Add `using System;` at the top if not already present (StringComparison).

  **src/Scenes/FarmScene.cs — delete two force-stamps:**

  - Delete line 245: `_loadedState.CurrentScene = "Farm";` — GameplayScene.LoadContent
    line 161 already writes `Services.GameState.CurrentScene = SceneName` which equals
    `"Farm"` for FarmScene. The line is redundant and actively wrong during boot because
    it runs before routing decides where to go. Leave the surrounding
    `Services.GameState = _loadedState;` and the chest/resource init calls intact.

  - Delete line 734: `state.CurrentScene = "Farm";` inside `BuildCurrentStateSnapshot`.
    That snapshot is only built from `SaveCurrentState` (OnDayAdvanced, chest close,
    resource reset) — all of which happen while we're actually in FarmScene, so
    `Services.GameState.CurrentScene` is already `"Farm"` (set by GameplayScene.LoadContent
    line 161 on entry). Line 734 is pure redundancy now; removing it keeps the single
    source of truth.

  **src/Scenes/FarmScene.cs — OnLoad (at the very end, after `Console.WriteLine("[FarmScene] Loaded");`):**

  Queue the hop to the saved scene if it isn't Farm. Must fire AFTER all
  Player/Inventory/Atlas/Hotbar/Progression/Quest are constructed (they are by end of OnLoad).
  Use `SceneManager.TransitionTo` not `PushImmediate` — we want a visible fade and we want
  FarmScene unloaded from the stack (Village/Castle/Shop replace it; a Farm transition trigger
  from the target scene later creates a fresh FarmScene instance, which is safe because
  `Services.Player != null` guards against re-constructing it).

  ```csharp
  Console.WriteLine("[FarmScene] Loaded");

  // v10: boot-time scene restore. If save.CurrentScene was non-Farm, hop there now that
  // Player/Inventory/Atlas/Hotbar are fully constructed. PushImmediate can't be used —
  // we're still inside LoadContent; TransitionTo defers the swap to the fade-out point.
  var restoreTarget = Services.PendingRestoreScene;
  if (!string.IsNullOrEmpty(restoreTarget) && restoreTarget != "Farm")
  {
      Services.PendingRestoreScene = null; // one-shot
      Scene? next = restoreTarget switch
      {
          "Village" => new VillageScene(Services, "RestoreSave"),
          "Castle"  => new CastleScene(Services, "RestoreSave"),
          "Shop"    => new ShopScene(Services, "RestoreSave"),
          _         => null,
      };
      if (next != null)
      {
          Console.WriteLine($"[FarmScene] Boot restore: transitioning to {restoreTarget}");
          Services.SceneManager.TransitionTo(next);
      }
      else
      {
          Console.WriteLine($"[FarmScene] Boot restore: unknown scene '{restoreTarget}', staying on Farm");
          // PendingRestorePosition is still queued for Farm — if the player's last Farm
          // position was saved, GameplayScene.LoadContent already consumed it above.
          // Nothing else to do here.
      }
  }
  else
  {
      Services.PendingRestoreScene = null; // nothing to restore; clear so no stale state
  }
  ```

  Note: when the hop fires, the Village/Castle/Shop `LoadContent` will run and its base
  `GameplayScene.LoadContent` consumes `PendingRestorePosition` if `PendingRestoreSceneName`
  matches. The "Farm" case (no hop) would have already consumed it inside FarmScene's own
  base-class LoadContent path above.

  **Scene fallback fromScene value**: using `"RestoreSave"` as `fromScene` for boot
  restores means `GetSpawn("RestoreSave")` will hit the fallback branch in VillageScene/
  CastleScene/ShopScene (none of them have a `RestoreSave` key in `Spawns` nor a TMX
  `from_RestoreSave` marker). That fallback spawn is then immediately overwritten by
  `PendingRestorePosition` consumption in the base class — so the specific fromScene
  value only matters if the position dict is empty, in which case landing on the scene's
  default fallback spawn is the correct behavior (better than crashing or landing on a
  random door).
  </action>
  <verify>
    <automated>dotnet build stardew_medieval_v3.csproj</automated>
  </verify>
  <done>
    - Project compiles with zero new warnings.
    - Game1.LoadContent reads save before push, seeds ServiceContainer restore flags.
    - GameplayScene.LoadContent overrides Player.Position with PendingRestorePosition when
      SceneName matches PendingRestoreSceneName, then nulls both flags.
    - FarmScene.cs:245 line `_loadedState.CurrentScene = "Farm";` deleted.
    - FarmScene.cs:734 line `state.CurrentScene = "Farm";` deleted.
    - FarmScene.OnLoad ends with a switch that calls SceneManager.TransitionTo when
      PendingRestoreScene is Village/Castle/Shop.
    - grep `state.CurrentScene = "Farm"` returns only the SaveManager v2->v3 migration line.
  </done>
</task>

<task type="checkpoint:human-verify" gate="blocking">
  <name>Task 3: Manual save-and-reopen verification (all four scenes + old save migration)</name>
  <what-built>
    Per-scene position persistence: `GameState.PositionByScene` dict saved/loaded,
    `SaveVersion` bumped to 10 with a migration that seeds Farm from legacy PlayerX/Y,
    `Game1.cs` consults the save before pushing the initial scene, `GameplayScene` restores
    the per-scene position on boot, and `FarmScene` hops to Village/Castle/Shop on boot when
    the saved scene is non-Farm. Dungeon saves normalize to Farm (as designed).
  </what-built>
  <how-to-verify>
    Backup the existing save first so you can test the migration path:
    1. Copy `%LOCALAPPDATA%\StardewMedieval\savegame.json` to a safe location (e.g.
       `savegame.v9.bak.json`) — this is your pre-update v9 save for the migration test.

    **Migration test (v9 → v10 backcompat):**
    2. `dotnet run`. Confirm console shows `[SaveManager] Migrated v9->v10 (PositionByScene
       seeded: Farm=(X,Y))` with the X/Y matching where you were on the farm before.
    3. Confirm the game loads on the Farm at the same position as before the update.
       No crash, no reset to (160,160).
    4. Press F5 (manual save). Open `%LOCALAPPDATA%\StardewMedieval\savegame.json` in a
       text editor — confirm `"SaveVersion": 10` and a `"PositionByScene": { "Farm": {...} }`
       block with your current position. `"PlayerX"/"PlayerY"` should still exist.

    **Farm restore test:**
    5. Walk the farm character to a specific corner (e.g. top-right). Press F5.
    6. Close the game (not just pause — quit the window).
    7. `dotnet run` again. Player should spawn in the Farm at that exact corner, NOT at
       tile (10,10).

    **Village restore test:**
    8. Walk east from the farm through `enter_village` trigger → you're in Village.
    9. Move to a specific spot in Village (e.g. near the shop door). Press F5.
    10. Close the game, `dotnet run`. Console should show
        `[Game1] Boot restore queued: scene=Village pos=(X,Y)`,
        `[FarmScene] Boot restore: transitioning to Village`,
        `[VillageScene] Boot position restored to (X,Y)`.
    11. After the fade, you should be in Village at the same spot — NOT at the Farm entrance.

    **Castle restore test:**
    12. From Village, enter Castle via `door_castle`. Walk to a specific spot.
    13. F5, close, relaunch. Confirm you're in Castle at that spot after the boot fade.

    **Shop restore test:**
    14. From Village, enter Shop via `door_shop`. Walk to a specific spot (not in front
        of the shopkeeper — that auto-opens dialogue on proximity).
    15. F5, close, relaunch. Confirm you're in Shop at that spot after the boot fade.

    **Per-scene isolation test:**
    16. Starting from Farm: note Farm position A.
    17. Go to Village, move to position B, F5 (saves Village:B and updates Farm's dict
        entry to whatever the Farm snapshot was when you left).
    18. Close, relaunch — should boot in Village at B.
    19. Transition back to Farm via `exit_to_farm`. You should land at the `from_Village`
        spawn marker (NOT at position A — normal door spawn behavior is preserved,
        restore is boot-only).
    20. Move around the Farm, F5. Close, relaunch — should boot in Farm at the new position.

    **Dungeon normalization test (regression guard):**
    21. From Village, enter the dungeon. Walk into a room. F5 (save writes CurrentScene as
        `Dungeon:r1` but SaveNow skips position for Dungeon:* scenes — check console log).
    22. Close, relaunch. Console should show `[SaveManager]` normalization log OR
        `[Game1] Boot restore queued: scene=Farm` (the boot normalizes Dungeon:*).
    23. Player should spawn on the Farm (not crash, not try to enter a dungeon room).

    **Old FarmScene stomp is gone:**
    24. Grep the project for `CurrentScene = "Farm"`. Only `SaveManager.cs` (in the v2->v3
        migration block) should match. FarmScene should no longer contain that assignment.
  </how-to-verify>
  <resume-signal>Type "approved" or describe issues</resume-signal>
</task>

</tasks>

<verification>
- `dotnet build stardew_medieval_v3.csproj` succeeds with no new warnings after each task.
- grep `state.CurrentScene = "Farm"` in `src/` returns only the SaveManager v2->v3 migration line.
- grep `_loadedState.CurrentScene = "Farm"` in `src/` returns zero matches.
- A v9 save file loads, migrates to v10, and the migrated JSON contains `PositionByScene["Farm"]`
  with the legacy PlayerX/Y values.
- After the migration the player boots on the Farm at the saved position (not tile 10,10).
- Save in Village/Castle/Shop, restart, boot lands in the same scene at the same position.
- Door transitions (Farm ↔ Village ↔ Castle/Shop) still use spawn-marker positions, NOT
  the last-visited coordinates.
- Dungeon CurrentScene values (`Dungeon:*`) normalize to Farm on boot; no crash.
</verification>

<success_criteria>
- Single save round-trip preserves scene AND position for Farm, Village, Castle, Shop.
- Per-scene positions are independent: moving in Village does not change Farm's saved position.
- Existing v9 saves auto-migrate on first load without user action; no data loss.
- Door-based scene transitions still use spawn markers (regression guard on normal travel).
- Dungeon saves normalize safely to Farm at boot (no partial-run restoration).
- No new compilation warnings.
</success_criteria>

<output>
After completion, create `.planning/quick/260423-shd-restaurar-ltima-cena-e-posi-o-ao-reabrir/260423-shd-SUMMARY.md`
</output>
