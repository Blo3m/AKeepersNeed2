using System;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;

namespace AKeepersNeed2.Shared.Movement;

/// <summary>
/// Teleports the player within the current scene using the game's own same-scene teleport
/// (the one milestones use), optionally snapping to the nearest walkable ground first.
/// Refusals are reported with a <see cref="Toast"/>. Scene rules (e.g. "outdoors only") are
/// up to the caller.
/// </summary>
internal static class PlayerTeleporter
{
    /// <summary>
    /// Teleports to the walkable spot nearest <paramref name="world"/>. A snap further than
    /// <paramref name="maxSnapDistance"/> (horizontal) is refused, since the nearest node across
    /// all graphs can belong to a far-off area. Without <paramref name="fade"/> the move is
    /// instant; the fade hides object streaming, so keep it for long jumps.
    /// </summary>
    public static bool TeleportNear(
        Vector3 world,
        float maxSnapDistance = float.PositiveInfinity,
        Action beforeTeleport = null,
        bool fade = true
    )
    {
        if (!CanTeleport())
        {
            return false;
        }
        if (!WalkableResolver.TrySnap(world, out Vector3 target)
            || HorizontalDistance(world, target) > maxSnapDistance)
        {
            Toast.Show("No walkable ground nearby");
            return false;
        }
        return Teleport(target, beforeTeleport, fade);
    }

    /// <summary>Teleports to <paramref name="walkable"/>, which the caller has already resolved.</summary>
    public static bool TeleportTo(Vector3 walkable, Action beforeTeleport = null, bool fade = true)
    {
        if (!CanTeleport())
        {
            return false;
        }
        return Teleport(walkable, beforeTeleport, fade);
    }

    /// <summary>A game and player exist, and controls aren't taken (cutscene, dialogue, …).</summary>
    public static bool CanTeleport()
    {
        if (MainGame.Instance == null || MainGame.PlayerController == null)
        {
            return false;
        }
        if (MainGame.PlayerData == null)
        {
            return false;
        }
        // Open windows (e.g. the world map a map teleport starts from) take control ByUI, so
        // that flag is ignored.
        if (!MainGame.PlayerController.IsControlsEnabledExcept(TakenControlType.ByUI))
        {
            Toast.Show("Can't teleport right now");
            return false;
        }
        return true;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private static bool Teleport(Vector3 target, Action beforeTeleport, bool fade)
    {
        Plugin.Logger.LogInfo(
            $"[Teleport] {MainGame.PlayerData.position.Value} -> {target} "
            + $"(scene '{MainGame.PlayerData.currentGameSceneId}')."
        );

        beforeTeleport?.Invoke();

        PlayerController.OnPlayerTeleported += NudgeOutOfOverlap;
        if (!PlayerController.Teleport(new PositionTeleportData(target, fade)))
        {
            PlayerController.OnPlayerTeleported -= NudgeOutOfOverlap;
            return false;
        }
        return true;
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
