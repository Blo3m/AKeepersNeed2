using Pathfinding;
using UnityEngine;

namespace AKeepersNeed2.Shared.Movement;

/// <summary>
/// Finds the nearest walkable ground via the game's A* graphs. The world graph is fully loaded
/// at startup, unlike colliders, which stream in by chunk and may be missing far away.
/// Interiors get their own graph scanned on entry.
/// </summary>
internal static class WalkableResolver
{
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
