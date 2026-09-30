using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Markers on the world map for resource nodes (by what they drop), chests, stockpiles, NPCs with
/// something to say, fishing spots and doors, filtered from a collapsible legend on the map page.
/// A postfix on <c>MapPageWidget.Redraw</c> attaches a <see cref="MarkerLayer"/> (and its
/// <see cref="MarkerLegend"/>) and rescans. Display-only; nothing is written to the save.
/// </summary>
internal sealed class MapMarkersModule : HarmonyModule
{
    private const float MinSize = 8f;
    private const float MaxSize = 32f;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _size;

    public override string Name => "MapMarkers";

    internal static bool Enabled => _enabled.Value;

    internal static float MarkerSize => Mathf.Clamp(Mathf.Round(_size.Value), MinSize, MaxSize);

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "MapMarkers",
            "Enabled",
            false,
            "Show markers for resources, chests, NPCs and more on the world map, picked in its legend."
        );
        _size = settings.Profile(
            "MapMarkers",
            "Size",
            16f,
            "Size of the map markers.",
            new AcceptableValueRange<float>(MinSize, MaxSize)
        );
        MarkerFilters.Bind(settings.Profile(
            "MapMarkers",
            "Shown",
            string.Empty,
            "Marker types ticked in the map's legend (set from the legend)."
        ));
        settings.Toggle(MenuSection.Map, 70, "Map Markers", _enabled);
        settings.Slider(MenuSection.Map, 80, _size, MinSize, MaxSize, "0 px", () => _enabled.Value);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(MapPageWidget), nameof(MapPageWidget.Redraw)),
            postfix: new HarmonyMethod(typeof(MapMarkersModule), nameof(AfterRedraw))
        );
    }

    private static void AfterRedraw(MapPageWidget __instance)
    {
        RectTransform mapRect = MapProjection.GetMapRect(__instance);
        if (mapRect != null)
        {
            MarkerLayer.Ensure(__instance, mapRect).Rescan();
        }
    }
}
