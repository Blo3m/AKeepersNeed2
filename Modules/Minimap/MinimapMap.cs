using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Map;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Minimap;

/// <summary>
/// A picture-only copy of the game's world map for the minimap. The map is a hierarchy of zone images
/// and fog, not one texture, so the content of the map window's scroll view is copied. The widget
/// is loaded through <c>LazyUI.GetWindow</c> (it loads windows that aren't open yet), initialised,
/// and its zones drawn for the current knowledge first (<c>UpdateZones</c>, which Map Reveal also
/// patches). Zone art is moved under the widget's hierarchy sorter target on init; when that target
/// isn't inside the content it's copied alongside. The copy is built under an inactive holder so none
/// of the game's scripts wake up in it, and every script but Unity's own UI and TextMesh Pro is
/// stripped: it's only pictures. The player icon and the mod's own layers are left out.
/// </summary>
internal static class MinimapMap
{
    private static readonly AccessTools.FieldRef<MapPageWidget, ScrollRect> ScrollRef =
        AccessTools.FieldRefAccess<MapPageWidget, ScrollRect>("scrollRect");
    private static readonly AccessTools.FieldRef<MapPageWidget, RectTransform> PlayerIconRef =
        AccessTools.FieldRefAccess<MapPageWidget, RectTransform>("playerIcon");
    private static readonly AccessTools.FieldRef<MapPageWidget, UIHierarchySorter> SorterRef =
        AccessTools.FieldRefAccess<MapPageWidget, UIHierarchySorter>("hierarchySorter");

    /// <summary>
    /// Copies the map under <paramref name="parent"/>. Returns the copy's root, sized like the map,
    /// whose centre is the map's centre (where <see cref="MapProjection"/> points are measured from),
    /// or null when the map can't be loaded.
    /// </summary>
    public static RectTransform Build(RectTransform parent, out Vector2 mapSize)
    {
        mapSize = Vector2.zero;
        MapPageWidget widget = LazyUI.GetWindow<UIMapWindow>()?.MapPageWidget;
        RectTransform mapRect = widget != null
            ? MapProjection.GetMapRect(widget)
            : null;
        ScrollRect scroll = widget != null
            ? ScrollRef(widget)
            : null;
        if (mapRect == null || scroll == null || scroll.content == null)
        {
            Plugin.Logger.LogWarning("[Minimap] the world map couldn't be loaded.");
            return null;
        }
        widget.Init();
        AccessTools.Method(typeof(MapPageWidget), "UpdateZones").Invoke(widget, null);
        mapSize = mapRect.sizeDelta;
        RectTransform content = scroll.content;

        var holder = new GameObject("AKN_MinimapCopy", typeof(RectTransform));
        holder.SetActive(false);
        holder.transform.SetParent(parent, false);
        var root = (RectTransform)holder.transform;
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = mapSize;
        root.anchoredPosition = Vector2.zero;

        // Everything is placed relative to the map's centre, which becomes the root's centre.
        Vector3 mapCentre = mapRect.TransformPoint(mapRect.rect.center);
        RectTransform contentCopy = Copy(content, root, mapCentre, mapRect);
        // The map window only switches the map on when it draws (UpdatePlayerPos); unopened, the copy
        // would show the zone fog (kept elsewhere in the content) over nothing.
        RectTransform mapCopy = null;
        if (mapRect == content)
        {
            mapCopy = contentCopy;
        }
        else if (mapRect.IsChildOf(content))
        {
            mapCopy = contentCopy.Find(PathFrom(content, mapRect)) as RectTransform;
            for (Transform t = mapCopy; t != null && t != contentCopy; t = t.parent)
            {
                t.gameObject.SetActive(true);
            }
        }
        RectTransform target = SorterRef(widget)?.HierarchyTarget;
        bool targetOutside = target != null && !target.IsChildOf(content);
        if (targetOutside)
        {
            Copy(target, root, mapCentre, mapRect);
        }
        RemovePlayerIcon(root, PlayerIconRef(widget));
        // Lay the copy out the way the map window does when it draws (UpdatePlayerPos sizes the
        // content to the map); the zone art is placed against the content, so before that the source's
        // layout can be off. Then centre the copy's own map rect, which is what map points are
        // measured from.
        contentCopy.sizeDelta = mapSize;
        holder.SetActive(true);
        Vector2 shift = Vector2.zero;
        if (mapCopy != null)
        {
            Vector3 centre = root.InverseTransformPoint(mapCopy.TransformPoint(mapCopy.rect.center));
            shift = new Vector2(centre.x, centre.y);
            contentCopy.anchoredPosition -= shift;
        }
        Plugin.Logger.LogInfo(
            $"[Minimap] map copied: size {mapSize}, content '{content.name}', "
                + $"sorter target {(target == null ? "none" : targetOutside ? "outside (copied too)" : "inside")}, "
                + $"map re-centred by {shift}."
        );
        return root;
    }

    /// <summary>Copies <paramref name="source"/> into <paramref name="root"/>, placed as it is around the map
    /// centre.</summary>
    private static RectTransform Copy(
        RectTransform source,
        RectTransform root,
        Vector3 mapCentre,
        RectTransform mapRect
    )
    {
        GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, root, true);
        Strip(copy);
        var rect = (RectTransform)copy.transform;
        Vector2 size = source.rect.size;
        // Positions in map units: undo the map's own scale (the window may be zoomed).
        Vector3 offset = mapRect.InverseTransformPoint(source.position) - mapRect.InverseTransformPoint(mapCentre);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(offset.x, offset.y);
        copy.SetActive(true);
        return rect;
    }

    private static string PathFrom(Transform ancestor, Transform descendant)
    {
        string path = descendant.name;
        for (Transform t = descendant.parent; t != null && t != ancestor; t = t.parent)
        {
            path = t.name + "/" + path;
        }
        return path;
    }

    private static void Strip(GameObject copy)
    {
        var stripped = new Dictionary<string, int>();
        foreach (Transform child in copy.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.StartsWith("AKN_", StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || IsPicture(behaviour))
            {
                continue;
            }
            string type = behaviour.GetType().Name;
            stripped.TryGetValue(type, out int count);
            stripped[type] = count + 1;
            UnityEngine.Object.DestroyImmediate(behaviour);
        }
        if (stripped.Count > 0)
        {
            Plugin.Logger.LogInfo(
                "[Minimap] stripped from the map copy: "
                    + string.Join(", ", stripped.Select(pair => pair.Key + " x" + pair.Value).ToArray())
            );
        }
        foreach (Graphic graphic in copy.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }

    // What draws the map: any graphic (the game's own image classes included), mesh effects, masks,
    // Unity's layout components and TextMesh Pro text.
    private static bool IsPicture(MonoBehaviour behaviour)
    {
        if (behaviour is Selectable || behaviour is ScrollRect)
        {
            return false;
        }
        if (behaviour is Graphic || behaviour is BaseMeshEffect || behaviour is Mask || behaviour is RectMask2D)
        {
            return true;
        }
        // The map texture isn't in the prefab: this loads it from Addressables when its object is
        // switched on and releases it when switched off. The copy keeps its own, so it holds its own
        // reference and the world map closing can't unload the minimap's picture.
        if (behaviour is AssetReferenceImage)
        {
            return true;
        }
        string space = behaviour.GetType().Namespace ?? string.Empty;
        return space.StartsWith("UnityEngine", StringComparison.Ordinal) || behaviour is TMP_Text;
    }

    private static void RemovePlayerIcon(RectTransform root, RectTransform playerIcon)
    {
        if (playerIcon == null)
        {
            return;
        }
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == playerIcon.name && child != root)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                return;
            }
        }
    }
}
