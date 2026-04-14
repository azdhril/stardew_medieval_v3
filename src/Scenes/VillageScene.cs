using System.Collections.Generic;
using Microsoft.Xna.Framework;
using stardew_medieval_v3.Core;

namespace stardew_medieval_v3.Scenes;

/// <summary>
/// Village hub: west exit back to Farm, plus Castle and Shop doors.
/// All cross-cutting behavior (HUD, input, pause) lives in <see cref="GameplayScene"/>.
/// </summary>
public class VillageScene : GameplayScene
{
    private static readonly Dictionary<string, Vector2> Spawns = new()
    {
        ["Farm"]   = new Vector2(96, 270),
        ["Castle"] = new Vector2(208, 128),
        ["Shop"]   = new Vector2(736, 128),
    };

    public VillageScene(ServiceContainer services, string fromScene) : base(services, fromScene) { }

    protected override string MapPath => "assets/Maps/village.tmx";
    protected override string SceneName => "Village";

    protected override Vector2 GetSpawn(string fromScene) =>
        Spawns.TryGetValue(fromScene, out var p) ? p : new Vector2(48, 270);

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
        }
        return false;
    }
}
