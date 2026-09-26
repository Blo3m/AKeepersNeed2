using AKeepersNeed2.Shared.Ui;
using UnityEngine;

namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// Teleports the player to a map point or near a world position, snapped to the nearest
/// walkable ground, using the game's own same-scene teleport (the one milestones use).
/// Refusals are reported with a <see cref="Toast"/>.
/// </summary>
internal static class MapTeleporter
{
    public static void TeleportToMapPoint(Vector2 mapPoint, Vector2 mapSize)
    {
        if (!CanTeleport())
        {
            return;
        }
        float startHeight = MainGame.PlayerData.position.Value.y;
        if (!WalkableResolver.TryResolve(mapPoint, mapSize, startHeight, out Vector3 target))
        {
            Toast.Show("No walkable ground nearby");
            return;
        }
        Teleport(target);
    }

    public static void TeleportNear(Vector3 world)
    {
        if (!CanTeleport())
        {
            return;
        }
        if (!WalkableResolver.TrySnap(world, out Vector3 target))
        {
            Toast.Show("No walkable ground nearby");
            return;
        }
        Teleport(target);
    }

    private static bool CanTeleport()
    {
        if (MainGame.Instance == null || MainGame.PlayerController == null)
        {
            return false;
        }
        if (MainGame.PlayerData == null)
        {
            return false;
        }
        if (!MapProjection.IsPlayerInWorld())
        {
            string scene = MainGame.PlayerData.currentGameSceneId;
            Plugin.Logger.LogInfo($"[MapTeleport] refused outside the world: scene '{scene}'.");
            Toast.Show("Can't teleport from here");
            return false;
        }
        // The open map window itself takes control ByUI, so that flag is ignored.
        if (!MainGame.PlayerController.IsControlsEnabledExcept(TakenControlType.ByUI))
        {
            Toast.Show("Can't teleport right now");
            return false;
        }
        return true;
    }

    private static void Teleport(Vector3 target)
    {
        Plugin.Logger.LogInfo(
            $"[MapTeleport] {MainGame.PlayerData.position.Value} -> {target} "
            + $"(scene '{MainGame.PlayerData.currentGameSceneId}')."
        );

        MapProjection.CloseOpenMap();

        PlayerController.OnPlayerTeleported += NudgeOutOfOverlap;
        if (!PlayerController.Teleport(new PositionTeleportData(target)))
        {
            PlayerController.OnPlayerTeleported -= NudgeOutOfOverlap;
        }
    }

    // A navmesh node can sit right against a fence or prop; the game's own helper shifts the
    // player off anything they overlap.
    private static void NudgeOutOfOverlap()
    {
        PlayerController.OnPlayerTeleported -= NudgeOutOfOverlap;
        if (MainGame.PlayerController != null)
        {
            MainGame.PlayerController.TryTeleportPlayerToAnyFreePlace();
        }
    }
}
