using Pathfinding;
using UnityEngine;

namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// Finds the nearest walkable ground via the game's A* graph, which is fully loaded at
/// startup — unlike colliders, which stream in by chunk and may be missing far away.
/// </summary>
internal static class WalkableResolver
{
    private const int HeightPasses = 3;
    private const float HeightTolerance = 0.1f;

    /// <summary>
    /// Resolves a map point to walkable ground. The map folds height into Z, so the height is
    /// refined over a few passes, starting from <paramref name="startHeight"/>.
    /// </summary>
    public static bool TryResolve(Vector2 mapPoint, Vector2 mapSize, float startHeight, out Vector3 world)
    {
        world = Vector3.zero;
        float height = startHeight;
        for (int i = 0; i < HeightPasses; i++)
        {
            if (!TrySnap(MapProjection.MapToWorld(mapPoint, mapSize, height), out world))
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

    /// <summary>The nearest walkable point to <paramref name="position"/>.</summary>
    public static bool TrySnap(Vector3 position, out Vector3 walkable)
    {
        walkable = Vector3.zero;
        if (AstarPath.active == null)
        {
            return false;
        }
        NNInfo nearest = AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
        if (nearest.node == null)
        {
            return false;
        }
        walkable = nearest.position;
        return true;
    }
}
