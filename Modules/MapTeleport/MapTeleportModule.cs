using System.Collections.Generic;
using AKeepersNeed2.Core;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Modules.MapTeleport;

/// <summary>
/// Shift-click on the open world map teleports the player to the nearest walkable spot under
/// the cursor. Polls input in <see cref="Tick"/>; no Harmony patch is needed.
/// </summary>
internal sealed class MapTeleportModule : IModule, IUpdatable
{
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    public string Name => "MapTeleport";

    public int Order => 0;

    public void Enable()
    {
    }

    public void Disable()
    {
    }

    public void Tick()
    {
        if (!ModConfig.MapTeleportEnabled.Value || !Input.GetMouseButtonDown(0))
        {
            return;
        }
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            return;
        }
        if (InputGate.IsBlocked)
        {
            return;
        }
        if (!MapProjection.TryGetOpenMap(out MapPageWidget widget))
        {
            Ignore("no open map");
            return;
        }
        if (!IsMapTopmostTarget(widget, out string reason))
        {
            Ignore(reason);
            return;
        }

        RectTransform mapRect = MapProjection.GetMapRect(widget);
        if (mapRect == null)
        {
            Ignore("map rect not found");
            return;
        }
        if (!MapProjection.TryScreenToMap(mapRect, Input.mousePosition, out Vector2 point))
        {
            Ignore("cursor outside the map image");
            return;
        }
        MapTeleporter.TeleportToMapPoint(point, mapRect.sizeDelta);
    }

    private static void Ignore(string reason)
    {
        Plugin.Logger.LogInfo($"[MapTeleport] shift-click ignored: {reason}.");
    }

    // Skips clicks that land on another window (e.g. the mod menu), a milestone (it teleports
    // on its own click) or a mod element that handles its own shift-click.
    private bool IsMapTopmostTarget(MapPageWidget widget, out string reason)
    {
        reason = null;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            reason = "no EventSystem";
            return false;
        }
        var pointer = new PointerEventData(eventSystem) { position = Input.mousePosition };
        _hits.Clear();
        eventSystem.RaycastAll(pointer, _hits);
        if (_hits.Count == 0)
        {
            reason = "nothing under the cursor";
            return false;
        }

        GameObject top = _hits[0].gameObject;
        if (!top.transform.IsChildOf(widget.transform))
        {
            reason = $"cursor over '{top.name}', not the map";
            return false;
        }
        if (top.GetComponentInParent<UIMapMilestone>() != null)
        {
            reason = "cursor over a milestone";
            return false;
        }
        IMapClickTarget target = top.GetComponentInParent<IMapClickTarget>();
        if (target != null && target.HandlesShiftClick)
        {
            reason = "cursor over an NPC face (it teleports itself)";
            return false;
        }
        return true;
    }
}
