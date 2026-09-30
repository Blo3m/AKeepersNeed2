using System;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;

namespace AKeepersNeed2.Shared.Movement;

/// <summary>
/// Teleports the player using the game's own teleport: within the current scene (the path
/// milestones use), optionally snapping to the nearest walkable ground first, or to a saved spot in
/// any scene (<see cref="TeleportToScene"/>).
/// Refusals are reported with a <see cref="Toast"/>. Scene rules (e.g. "outdoors only") are
/// up to the caller.
/// </summary>
internal static class PlayerTeleporter
{
    private const float SameSceneSnap = 2f;

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

    /// <summary>
    /// Teleports to <paramref name="position"/> in <paramref name="sceneId"/>, arriving with lighting
    /// <paramref name="preset"/> (empty keeps the current one). A spot the player once stood on, so
    /// it's used as-is; in the current scene it's snapped to walkable ground when that's within a
    /// step, in case the ground changed since. Another scene loads with the game's loading screen.
    /// </summary>
    public static bool TeleportToScene(string sceneId, Vector3 position, string preset, Action beforeTeleport = null)
    {
        if (!CanTeleport())
        {
            return false;
        }
        // A missing scene would leave the game's teleport half done (controls taken, screen faded).
        if (string.IsNullOrEmpty(sceneId) || MainGame.WorldData.GetGameSceneDataById(sceneId) == null)
        {
            Toast.Show("That place isn't in this save");
            return false;
        }
        bool sameScene = sceneId == MainGame.PlayerData.currentGameSceneId;
        Vector3 target = position;
        if (sameScene
            && WalkableResolver.TrySnap(position, out Vector3 snapped)
            && HorizontalDistance(position, snapped) <= SameSceneSnap)
        {
            target = snapped;
        }
        return Teleport(
            new PositionTeleportData(target, sceneId: sameScene ? null : sceneId, preset: preset),
            target,
            beforeTeleport
        );
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
        return Teleport(new PositionTeleportData(target, fade), target, beforeTeleport);
    }

    private static bool Teleport(TeleportDataBase data, Vector3 target, Action beforeTeleport)
    {
        Plugin.Logger.LogInfo(
            $"[Teleport] {MainGame.PlayerData.position.Value} -> {target} "
            + $"(scene '{MainGame.PlayerData.currentGameSceneId}' -> '{data.GetDestinationSceneData()?.id}')."
        );

        beforeTeleport?.Invoke();

        PlayerController.OnPlayerTeleported += NudgeOutOfOverlap;
        if (!PlayerController.Teleport(data))
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
