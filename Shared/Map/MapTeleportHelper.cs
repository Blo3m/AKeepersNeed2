using AKeepersNeed2.Shared.Movement;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;

namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// Map-driven teleports (shift-click on the map or on an NPC face). Adds the map rules on top
/// of <see cref="PlayerTeleporter"/>: the player must be in the outdoor world the map shows,
/// map points are resolved to walkable ground, and the map closes before the teleport.
/// </summary>
internal static class MapTeleportHelper
{
    private const int HeightPasses = 3;
    private const float HeightTolerance = 0.1f;

    public static void TeleportToMapPoint(Vector2 mapPoint, Vector2 mapSize)
    {
        if (!PlayerTeleporter.CanTeleport() || !CheckPlayerInWorld())
        {
            return;
        }
        float startHeight = MainGame.PlayerData.position.Value.y;
        if (!TryResolve(mapPoint, mapSize, startHeight, out Vector3 target))
        {
            Toast.Show("No walkable ground nearby");
            return;
        }
        PlayerTeleporter.TeleportTo(target, MapProjection.CloseOpenMap);
    }

    public static void TeleportNear(Vector3 world)
    {
        if (!PlayerTeleporter.CanTeleport() || !CheckPlayerInWorld())
        {
            return;
        }
        PlayerTeleporter.TeleportNear(world, beforeTeleport: MapProjection.CloseOpenMap);
    }

    private static bool CheckPlayerInWorld()
    {
        if (MapProjection.IsPlayerInWorld())
        {
            return true;
        }
        string scene = MainGame.PlayerData.currentGameSceneId;
        Plugin.Logger.LogInfo($"[MapTeleport] refused outside the world: scene '{scene}'.");
        Toast.Show("Can't teleport from here");
        return false;
    }

    /// <summary>
    /// Resolves a map point to walkable ground. The map folds height into Z, so the height is
    /// refined over a few passes, starting from <paramref name="startHeight"/>.
    /// </summary>
    private static bool TryResolve(Vector2 mapPoint, Vector2 mapSize, float startHeight, out Vector3 world)
    {
        world = Vector3.zero;
        float height = startHeight;
        for (int i = 0; i < HeightPasses; i++)
        {
            if (!WalkableResolver.TrySnap(MapProjection.MapToWorld(mapPoint, mapSize, height), out world))
            {
                return false;
            }
            if (Mathf.Abs(world.y - height) < HeightTolerance)
            {
                break;
            }
            height = world.y;
        }
        return true;
    }
}
