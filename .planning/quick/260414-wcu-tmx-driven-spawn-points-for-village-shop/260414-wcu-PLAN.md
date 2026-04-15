---
phase: quick-260414-wcu
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Core/GameplayScene.cs
  - src/Scenes/VillageScene.cs
  - src/Scenes/ShopScene.cs
  - src/Scenes/CastleScene.cs
autonomous: true
requirements:
  - QUICK-260414-wcu
must_haves:
  truths:
    - "VillageScene, ShopScene, and CastleScene resolve their player spawn from a TMX 'Spawn' object group when a matching 'from_<PrevScene>' object exists."
    - "When no 'Spawn' group or no matching 'from_<X>' object exists, the three scenes fall back to their existing hardcoded Spawns dictionary (unchanged)."
    - "Resolution source (TMX vs dict) is logged to the console so designers can verify which path fired."
    - "DungeonScene and FarmScene behavior is unchanged."
    - "Project builds cleanly (`dotnet build`)."
  artifacts:
    - path: "src/Core/GameplayScene.cs"
      provides: "Shared TryReadTmxSpawn(fromScene, out pos) helper reading object group 'Spawn' by convention 'from_<prev>' (case-insensitive)."
    - path: "src/Scenes/VillageScene.cs"
      provides: "GetSpawn uses TryReadTmxSpawn first, then Spawns dict, then the existing (48, 270) final fallback."
    - path: "src/Scenes/ShopScene.cs"
      provides: "GetSpawn uses TryReadTmxSpawn first, then Spawns dict, then existing (208, 416) final fallback."
    - path: "src/Scenes/CastleScene.cs"
      provides: "GetSpawn uses TryReadTmxSpawn first, then Spawns dict, then existing (208, 416) final fallback."
  key_links:
    - from: "GameplayScene.TryReadTmxSpawn"
      to: "TileMap.GetObjectGroup(\"Spawn\")"
      via: "Map field (already populated by the time GetSpawn runs)"
      pattern: "GetObjectGroup\\(\"Spawn\"\\)"
    - from: "VillageScene/ShopScene/CastleScene.GetSpawn"
      to: "GameplayScene.TryReadTmxSpawn"
      via: "protected helper call"
      pattern: "TryReadTmxSpawn\\("
---

<objective>
Drive hub-scene player spawn positions from TMX authoring instead of hardcoded
dictionaries, mirroring the existing DungeonScene pattern. Keeps the current
hardcoded `Spawns` dict in each scene as a safety net so nothing breaks if the
TMX hasn't been annotated yet.

Purpose: Allow level designers to reposition spawn points per-scene by editing
TMX files in Tiled (drop a point object in a "Spawn" group and name it
`from_<PrevScene>`) without touching C#. Reduces coupling between map layout
and code, matches the pattern DungeonScene already uses.

Output:
- `GameplayScene.TryReadTmxSpawn` helper (protected, ~12 lines).
- VillageScene / ShopScene / CastleScene `GetSpawn` updated to TMX-first,
  dict-fallback resolution.
- Console log line documenting which path resolved (TMX vs dict).
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@CLAUDE.md
@src/Core/GameplayScene.cs
@src/Scenes/VillageScene.cs
@src/Scenes/ShopScene.cs
@src/Scenes/CastleScene.cs
@src/Scenes/DungeonScene.cs
@src/World/TileMap.cs

<interfaces>
<!-- Existing helpers the plan relies on -->

From src/World/TileMap.cs:
```csharp
public record TmxObject(
    string Name,
    Rectangle Bounds,
    Vector2 Point,
    Dictionary<string, string> Properties);

// Case-insensitive group lookup, empty list if missing.
public List<TmxObject> GetObjectGroup(string groupName);
```

From src/Core/GameplayScene.cs:
```csharp
protected TileMap Map = null!;                 // Loaded before GetSpawn fires
protected string FromScene { get; }
protected virtual Vector2 GetSpawn(string fromScene); // override per scene
```

Reference pattern (DungeonScene.GetSpawn, lines 64-82):
```csharp
if (Map != null)
{
    var spawns = Map.GetObjectGroup("Spawn");
    string key = $"from_{fromScene}";
    foreach (var s in spawns)
    {
        if (string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase))
            return s.Point;
    }
}
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add TryReadTmxSpawn helper on GameplayScene</name>
  <files>src/Core/GameplayScene.cs</files>
  <action>
Add a protected helper method on `GameplayScene` (place it near `GetSpawn`, around line 40, keeping related concerns together):

```csharp
/// <summary>
/// Look up a TMX "Spawn" object-group entry named <c>from_&lt;prev&gt;</c>
/// (case-insensitive on the prev portion). Returns true and the object's
/// center Point on hit. Callers typically fall back to a hardcoded dict.
/// Mirrors the pattern used by <see cref="DungeonScene.GetSpawn"/>.
/// </summary>
protected bool TryReadTmxSpawn(string fromScene, out Vector2 pos)
{
    pos = Vector2.Zero;
    if (Map == null || string.IsNullOrEmpty(fromScene)) return false;

    var spawns = Map.GetObjectGroup("Spawn");
    string key = $"from_{fromScene}";
    foreach (var s in spawns)
    {
        if (string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase))
        {
            pos = s.Point;
            return true;
        }
    }
    return false;
}
```

Notes:
- `Map` is already populated before `GetSpawn` is called (see LoadContent: `Map.Load(...)` runs before `Player.Position = GetSpawn(FromScene)`).
- Do NOT modify DungeonScene (it has its own richer implementation that also falls back to preserved player position — leave it alone per scope clarifications).
- Do NOT change the existing `GetSpawn` default implementation. Subclasses opt in.
- No new `using` needed — `Vector2`, `StringComparison`, and `string` are already in scope via existing usings.
  </action>
  <verify>
    <automated>dotnet build</automated>
  </verify>
  <done>
`GameplayScene` exposes `protected bool TryReadTmxSpawn(string, out Vector2)`. `dotnet build` succeeds. DungeonScene/FarmScene untouched.
  </done>
</task>

<task type="auto">
  <name>Task 2: Wire Village/Shop/Castle GetSpawn to TMX-first with dict fallback</name>
  <files>src/Scenes/VillageScene.cs, src/Scenes/ShopScene.cs, src/Scenes/CastleScene.cs</files>
  <action>
For each of the three scenes, replace the current single-line `GetSpawn` with a
TMX-first lookup that falls back to the existing `Spawns` dictionary, with a
Console log indicating which path resolved. Keep the existing `Spawns`
dictionary entries intact — they are the safety net.

**VillageScene.cs** — replace the current `GetSpawn` (around lines 37-38) with:

```csharp
protected override Vector2 GetSpawn(string fromScene)
{
    if (TryReadTmxSpawn(fromScene, out var tmxPos))
    {
        Console.WriteLine($"[VillageScene] Spawn from {fromScene} resolved via TMX at ({tmxPos.X},{tmxPos.Y})");
        return tmxPos;
    }
    if (Spawns.TryGetValue(fromScene, out var p))
    {
        Console.WriteLine($"[VillageScene] Spawn from {fromScene} resolved via dict at ({p.X},{p.Y})");
        return p;
    }
    var fallback = new Vector2(48, 270);
    Console.WriteLine($"[VillageScene] Spawn from {fromScene} no match — using default ({fallback.X},{fallback.Y})");
    return fallback;
}
```

**ShopScene.cs** — replace the current `GetSpawn` (around lines 37-38). Note the
dict fallback and the final literal fallback are currently the same value
(208, 416) — keep that behavior (same default in both branches):

```csharp
protected override Vector2 GetSpawn(string fromScene)
{
    if (TryReadTmxSpawn(fromScene, out var tmxPos))
    {
        Console.WriteLine($"[ShopScene] Spawn from {fromScene} resolved via TMX at ({tmxPos.X},{tmxPos.Y})");
        return tmxPos;
    }
    if (Spawns.TryGetValue(fromScene, out var p))
    {
        Console.WriteLine($"[ShopScene] Spawn from {fromScene} resolved via dict at ({p.X},{p.Y})");
        return p;
    }
    var fallback = new Vector2(208, 416);
    Console.WriteLine($"[ShopScene] Spawn from {fromScene} no match — using default ({fallback.X},{fallback.Y})");
    return fallback;
}
```

**CastleScene.cs** — replace the current `GetSpawn` (around lines 37-38) with
the same shape; fallback stays (208, 416):

```csharp
protected override Vector2 GetSpawn(string fromScene)
{
    if (TryReadTmxSpawn(fromScene, out var tmxPos))
    {
        Console.WriteLine($"[CastleScene] Spawn from {fromScene} resolved via TMX at ({tmxPos.X},{tmxPos.Y})");
        return tmxPos;
    }
    if (Spawns.TryGetValue(fromScene, out var p))
    {
        Console.WriteLine($"[CastleScene] Spawn from {fromScene} resolved via dict at ({p.X},{p.Y})");
        return p;
    }
    var fallback = new Vector2(208, 416);
    Console.WriteLine($"[CastleScene] Spawn from {fromScene} no match — using default ({fallback.X},{fallback.Y})");
    return fallback;
}
```

Important constraints:
- Do NOT remove or edit the existing `private static readonly Dictionary<string, Vector2> Spawns` in any of the three files. They are the authoring safety net.
- Do NOT touch FarmScene.cs or DungeonScene.cs.
- Do NOT edit any .tmx files — this plan is pure C#. Map authoring comes later when designers want to override a spawn.
- `System` namespace is already imported in all three files (for `Console.WriteLine`). No new usings required.
- `fromScene` matching is case-insensitive against the TMX object name (handled in `TryReadTmxSpawn`). The dict lookup remains case-sensitive since that matches what callers pass today (`"Village"`, `"Farm"`, `"Shop"`, `"Castle"`, `"Dungeon"`, `"dungeon_entrance"`, `"castle_door"`).
  </action>
  <verify>
    <automated>dotnet build</automated>
  </verify>
  <done>
All three scenes compile. Each `GetSpawn` is TMX-first, dict-second, literal-fallback-third, with a Console log per path. Existing Spawns dicts are unchanged. Visual smoke test by user: transition Farm→Village→Shop→Village→Castle→Village and confirm player lands at the expected spots (behavior unchanged because no TMX `Spawn` groups have been authored yet; all logs should read "via dict").
  </done>
</task>

</tasks>

<verification>
- `dotnet build` succeeds with no new warnings in the four modified files.
- Manual smoke test (user): launch the game, transition Farm → Village → Shop → Village → Castle → Village → Farm. Player lands at the same positions as before the change. Console shows `[VillageScene] Spawn from Farm resolved via dict at (96,270)` etc.
- No edits to FarmScene.cs, DungeonScene.cs, or any .tmx file.
</verification>

<success_criteria>
- Helper `TryReadTmxSpawn` exists on `GameplayScene` and is reachable by subclasses.
- VillageScene, ShopScene, CastleScene each prefer a TMX `Spawn` group object named `from_<PrevScene>` (case-insensitive on name), falling back to the existing hardcoded dict, then to the original literal default.
- Designers can now add a TMX `Spawn` object group in village.tmx / shop.tmx / castle.tmx and an object named `from_Farm` (etc.) to override spawn position without touching C#. No TMX edits are required as part of this plan.
- Build is green, no regressions in existing scene transitions.
</success_criteria>

<output>
After completion, create `.planning/quick/260414-wcu-tmx-driven-spawn-points-for-village-shop/260414-wcu-SUMMARY.md` describing:
- The helper added on GameplayScene.
- Per-scene GetSpawn changes.
- How a designer uses it (TMX authoring recipe: add an object group named `Spawn`, drop a point object, name it `from_<PrevSceneName>`).
- Confirmation that fallback behavior is preserved and no existing spawn positions changed for users who don't edit TMX.
</output>
