using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// Converts between world positions and points on the world map, mirroring the projection
/// <c>MapPageWidget.UpdatePlayerPos</c> / <c>UpdateMilestones</c> use. Map points are relative
/// to the centre of the widget's <c>mapRect</c>, which is what a child anchored at the centre
/// uses as its <c>anchoredPosition</c>.
/// </summary>
internal static class MapProjection
{
    private static readonly AccessTools.FieldRef<MapPageWidget, RectTransform> MapRectRef =
        AccessTools.FieldRefAccess<MapPageWidget, RectTransform>("mapRect");

    private static readonly AccessTools.FieldRef<CharacterWindow, MapPageWidget> CharacterMapRef =
        AccessTools.FieldRefAccess<CharacterWindow, MapPageWidget>("mapPageWidget");

    public static RectTransform GetMapRect(MapPageWidget widget)
    {
        return MapRectRef(widget);
    }

    /// <summary>
    /// The map page, if a map is currently open. The map the player normally opens (M key) is
    /// the Map page of <c>CharacterWindow</c>; <c>UIMapWindow</c> is a separate standalone
    /// map window, so both are checked.
    /// </summary>
    public static bool TryGetOpenMap(out MapPageWidget widget)
    {
        widget = null;
        if (MainGame.Instance == null)
        {
            return false;
        }

        CharacterWindow character = LazyUI.GetWindow<CharacterWindow>();
        if (IsShowingMap(character))
        {
            widget = CharacterMapRef(character);
            return true;
        }

        UIMapWindow mapWindow = LazyUI.GetWindow<UIMapWindow>();
        if (mapWindow != null && mapWindow.IsShown && IsActive(mapWindow.MapPageWidget))
        {
            widget = mapWindow.MapPageWidget;
            return true;
        }
        return false;
    }

    /// <summary>Closes whichever window is showing the map.</summary>
    public static void CloseOpenMap()
    {
        CharacterWindow character = LazyUI.GetWindow<CharacterWindow>();
        if (IsShowingMap(character))
        {
            character.Close();
        }
        UIMapWindow mapWindow = LazyUI.GetWindow<UIMapWindow>();
        if (mapWindow != null && mapWindow.IsShown)
        {
            mapWindow.Close();
        }
    }

    private static bool IsShowingMap(CharacterWindow character)
    {
        return character != null
            && character.IsShown
            && character.LastOpenedPage == CharacterWindowData.CharPage.Map
            && IsActive(CharacterMapRef(character));
    }

    private static bool IsActive(MapPageWidget widget)
    {
        return widget != null && widget.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Projects a world position onto the map. False when it lies outside the mapped world
    /// (other scenes, e.g. building interiors, sit outside these bounds).
    /// </summary>
    public static bool TryWorldToMap(Vector3 world, Vector2 mapSize, out Vector2 mapPoint)
    {
        Transform min = GUIElements.Instance.WorldMin;
        Transform max = GUIElements.Instance.WorldMax;
        float adjustedZ = world.z + world.y * TiltTangent();

        float minX = Mathf.Min(min.position.x, max.position.x);
        float maxX = Mathf.Max(min.position.x, max.position.x);
        float minZ = Mathf.Min(min.position.z, max.position.z);
        float maxZ = Mathf.Max(min.position.z, max.position.z);
        if (world.x < minX || world.x > maxX || adjustedZ < minZ || adjustedZ > maxZ)
        {
            mapPoint = Vector2.zero;
            return false;
        }

        float u = Mathf.InverseLerp(min.position.x, max.position.x, world.x);
        float v = Mathf.InverseLerp(min.position.z, max.position.z, adjustedZ);
        mapPoint = new Vector2((u - 0.5f) * mapSize.x, (v - 0.5f) * mapSize.y);
        return true;
    }

    /// <summary>
    /// Inverse of <see cref="TryWorldToMap"/>. The map folds height into Z, so the caller
    /// supplies the height to unfold it with.
    /// </summary>
    public static Vector3 MapToWorld(Vector2 mapPoint, Vector2 mapSize, float height)
    {
        Transform min = GUIElements.Instance.WorldMin;
        Transform max = GUIElements.Instance.WorldMax;
        float u = mapPoint.x / mapSize.x + 0.5f;
        float v = mapPoint.y / mapSize.y + 0.5f;
        float x = Mathf.LerpUnclamped(min.position.x, max.position.x, u);
        float adjustedZ = Mathf.LerpUnclamped(min.position.z, max.position.z, v);
        return new Vector3(x, height, adjustedZ - height * TiltTangent());
    }

    /// <summary>Screen position to a map point, or false if it isn't over the map.</summary>
    public static bool TryScreenToMap(RectTransform mapRect, Vector2 screen, out Vector2 mapPoint)
    {
        mapPoint = Vector2.zero;
        Canvas canvas = mapRect.GetComponentInParent<Canvas>();
        Camera camera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.rootCanvas.worldCamera;
        if (!RectTransformUtility.RectangleContainsScreenPoint(mapRect, screen, camera))
        {
            return false;
        }
        bool hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect,
            screen,
            camera,
            out Vector2 local
        );
        if (!hit)
        {
            return false;
        }
        mapPoint = local - mapRect.rect.center;
        return true;
    }

    public static bool IsPlayerInWorld()
    {
        return MainGame.PlayerData != null
            && TryWorldToMap(MainGame.PlayerData.position.Value, Vector2.one, out _);
    }

    // The game feeds the quaternion's x component (not an Euler angle) into this; match it
    // exactly so our points line up with the player icon and milestones.
    private static float TiltTangent()
    {
        return Mathf.Tan(Mathf.Deg2Rad * GUIElements.Instance.WorldMin.rotation.x);
    }
}
