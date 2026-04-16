using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Core;
using stardew_medieval_v3.World;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Village hub: west exit back to Farm, plus Castle and Shop doors, plus the
/// dungeon entrance (D-11). All cross-cutting behavior (HUD, input, pause)
/// lives in <see cref="GameplayScene"/>.
/// </summary>
public class VillageScene : GameplayScene
{
    private static readonly Dictionary<string, Vector2> Spawns = new()
    {
        ["Farm"]              = new Vector2(96, 270),
        ["Castle"]            = new Vector2(214, 192),
        ["Shop"]              = new Vector2(736, 128),
        // Returning from dungeon lands just outside the cave entrance so the
        // player can see where they came from. Matches `enter_dungeon` AABB
        // in village.tmx minus a few px so they don't immediately re-trigger.
        ["Dungeon"]           = new Vector2(864, 280),
        ["dungeon_entrance"]  = new Vector2(864, 280),
        // Plan 05-03 D-14: returning from the boss room drops the player at
        // the castle door so the King quest-complete dialogue (NPC-04) is the
        // natural next beat. Reuses the existing Castle-return position.
        ["castle_door"]       = new Vector2(214, 192),
    };

    public VillageScene(ServiceContainer services, string fromScene) : base(services, fromScene) { }

    protected override string MapPath => "assets/Maps/village.tmx";
    protected override string SceneName => "Village";

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
        Console.WriteLine($"[VillageScene] Spawn from {fromScene} no match - using default ({fallback.X},{fallback.Y})");
        return fallback;
    }

    protected override bool HandleTrigger(string triggerName)
    {
        switch (triggerName)
        {
            case "exit_to_farm":
                Services.SceneManager.TransitionTo(new FarmScene(Services, "Village"));
                return true;
            case "door_castle":
                Services.SceneManager.TransitionTo(new CastleScene(Services, "Village"));
                return true;
            case "door_shop":
                Services.SceneManager.TransitionTo(new ShopScene(Services, "Village"));
                return true;
            case "enter_dungeon":
                BeginDungeonRun(Services);
                Services.SceneManager.TransitionTo(new DungeonScene(Services, "r1", "village"));
                return true;
        }
        return false;
    }

    /// <summary>
    /// Start a fresh dungeon run: ensure a <see cref="DungeonState"/> exists,
    /// reset per-run flags, and seed chest contents deterministically from the
    /// new <see cref="DungeonState.RunSeed"/>.
    /// </summary>
    private static void BeginDungeonRun(ServiceContainer svc)
    {
        svc.Dungeon ??= new DungeonState();
        svc.Dungeon.BeginRun();
        DungeonChestSeeder.Seed(svc);
        Console.WriteLine($"[VillageScene] Entering dungeon run seed={svc.Dungeon.RunSeed}");
    }
}
