using System.Collections.Generic;
using AKeepersNeed2.Core.MapPins;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
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
    private const float PinRescanSeconds = 2f;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _size;
    private static float _scannedAt = float.MinValue;

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

    /// <summary>
    /// The scanned world objects, shared by the world map's layer and the minimap pins so the world
    /// is scanned once whichever shows it.
    /// </summary>
    internal static MarkerCatalog Catalog { get; } = new MarkerCatalog();

    /// <summary>Rescans when the last scan is older than <paramref name="maxAge"/> seconds.</summary>
    internal static void ScanIfStale(float maxAge)
    {
        if (Time.unscaledTime - _scannedAt < maxAge)
        {
            return;
        }
        _scannedAt = Time.unscaledTime;
        Catalog.Scan();
    }

    protected override void OnEnabled()
    {
        MapPinRegistry.Register(Pins);
    }

    protected override void OnDisabled()
    {
        MapPinRegistry.Unregister(Pins);
    }

    /// <summary>The ticked markers for the minimap, rescanning at most every couple of seconds.</summary>
    private static IEnumerable<MapPin> Pins()
    {
        if (!_enabled.Value || MainGame.PlayerData == null)
        {
            yield break;
        }
        ScanIfStale(PinRescanSeconds);
        float size = MarkerSize;
        foreach (MarkerEntry entry in Catalog.Entries)
        {
            if (!MarkerFilters.IsShown(entry.Type.Key))
            {
                continue;
            }
            Color color = entry.Highlight
                ? MarkerLayer.ReadyTint
                : entry.Type.Icon != null ? Color.white : MarkerLayer.DotColor;
            yield return new MapPin
            {
                World = entry.Wgo.Position,
                Icon = entry.Type.Icon ?? GeneratedSprites.Dot,
                Color = color,
                Size = size,
                Layer = 10,
            };
        }
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
