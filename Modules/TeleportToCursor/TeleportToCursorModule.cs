using AKeepersNeed2.Core;
using AKeepersNeed2.Shared.Movement;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Modules.TeleportToCursor;

/// <summary>
/// Pressing the teleport key teleports the player to the nearest walkable spot under the
/// cursor, in any scene. The cursor is projected onto a horizontal plane at the player's
/// height, the same way the game's own mouse aiming does (<c>MouseAimHelper</c>). Polls input
/// in <see cref="Tick"/>; no Harmony patch is needed.
/// </summary>
internal sealed class TeleportToCursorModule : IModule, IUpdatable
{
    // Guards against snapping to a node in some other area when the cursor is over a spot
    // with no walkable graph (e.g. a wall or off the edge of an interior).
    private const float MaxSnapDistance = 3f;

    public string Name => "TeleportToCursor";

    public int Order => 0;

    public void Enable()
    {
    }

    public void Disable()
    {
    }

    public void Tick()
    {
        KeyCode key = ModConfig.TeleportToCursorKey.Value;
        if (!ModConfig.TeleportToCursorEnabled.Value || key == KeyCode.None || !Input.GetKeyDown(key))
        {
            return;
        }
        if (InputGate.IsBlocked)
        {
            return;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Plugin.Logger.LogInfo("[TeleportToCursor] ignored: cursor over UI.");
            return;
        }
        if (MainGame.PlayerData == null)
        {
            return;
        }
        if (!TryCursorToWorld(MainGame.PlayerData.position.Value.y, out Vector3 world))
        {
            Plugin.Logger.LogInfo("[TeleportToCursor] ignored: cursor doesn't hit the ground plane.");
            return;
        }
        PlayerTeleporter.TeleportNear(world, MaxSnapDistance, fade: false);
    }

    private static bool TryCursorToWorld(float height, out Vector3 world)
    {
        world = Vector3.zero;
        CameraSystem cameras = CameraSystem.Instance;
        if (cameras == null || cameras.MainCamera == null)
        {
            return false;
        }
        // CameraSystem.ScreenPointToRay divides by the render texture size in this mode.
        if (cameras.MainCamera.GetRenderType() == MainCamera.RenderMode.Lightweight
            && cameras.MainCamera.RenderTexture == null)
        {
            return false;
        }
        Ray ray = CameraSystem.ScreenPointToRay(Input.mousePosition);
        var ground = new Plane(Vector3.up, new Vector3(0f, height, 0f));
        if (!ground.Raycast(ray, out float distance) || distance <= 0f)
        {
            return false;
        }
        world = ray.GetPoint(distance);
        return true;
    }
}
