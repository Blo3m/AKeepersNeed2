using System.Collections.Generic;
using AKeepersNeed2.Core.MapPins;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapNpcs;

/// <summary>
/// Shows every NPC that has a portrait on the world map as their face, with the name on hover
/// and (optionally) shift-click to teleport to them. A postfix on <c>MapPageWidget.Redraw</c>
/// attaches an <see cref="NpcMarkerLayer"/> to the map, which keeps the markers live while the
/// map is open. Display-only; nothing is written to the save.
/// </summary>
internal sealed class MapNpcsModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<bool> _teleport;

    private const float PinRescanSeconds = 1f;
    private const float PinSize = 20f;
    private static readonly List<WgoData> Npcs = new List<WgoData>();
    private static float _nextScan;
    private const float MaterialSearchSeconds = 30f;
    private static Material _portraitMaterial;
    private static float _nextMaterialSearch;

    public override string Name => "MapNpcs";

    /// <summary>Show NPCs on the map (the patch flag).</summary>
    internal static bool ShowEnabled => _enabled.Value;

    /// <summary>Shift-click a face to teleport; only offered while <see cref="ShowEnabled"/>.</summary>
    internal static bool TeleportEnabled => _teleport.Value;

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "MapNpcs",
            "Enabled",
            false,
            "Show NPCs that have a portrait on the world map, with their face and name on hover."
        );
        _teleport = settings.Profile(
            "MapNpcs",
            "ShiftClickTeleport",
            false,
            "Shift-click an NPC's face on the map to teleport next to them. Needs MapNpcs on."
        );
        settings.Toggle(MenuSection.Map, 30, "Show NPCs", _enabled);
        settings.Toggle(MenuSection.Map, 40, "    Shift-Click NPC to Teleport", _teleport, () => _enabled.Value);
    }

    protected override void OnEnabled()
    {
        MapPinRegistry.Register(Pins);
    }

    protected override void OnDisabled()
    {
        MapPinRegistry.Unregister(Pins);
    }

    /// <summary>
    /// Named NPCs' faces for the minimap. The NPC list is rescanned every second; positions are read
    /// live, so faces follow their NPC.
    /// </summary>
    private static IEnumerable<MapPin> Pins()
    {
        if (!_enabled.Value || MainGame.PlayerData == null)
        {
            yield break;
        }
        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + PinRescanSeconds;
            ScanNpcs();
            // Searched rarely: it looks through every loaded NPC widget, and logs when none is found.
            if (_portraitMaterial == null && Time.unscaledTime >= _nextMaterialSearch)
            {
                _nextMaterialSearch = Time.unscaledTime + MaterialSearchSeconds;
                _portraitMaterial = NpcMarkerLayer.CreatePortraitMaterial();
            }
        }
        foreach (WgoData npc in Npcs)
        {
            if (npc == null || npc.IsHidden)
            {
                continue;
            }
            yield return new MapPin
            {
                World = npc.Position,
                Icon = npc.Definition.Portrait,
                Material = _portraitMaterial,
                Size = PinSize,
                Layer = 20,
            };
        }
    }

    private static void ScanNpcs()
    {
        Npcs.Clear();
        string worldScene = MapProjection.WorldSceneId;
        GameSceneData scene = worldScene != null
            ? MainGame.WorldData.GetGameSceneDataById(worldScene)
            : null;
        if (scene == null)
        {
            return;
        }
        foreach (WgoData wgo in scene.wgoDataList)
        {
            if (NpcMarkerLayer.IsMappedNpc(wgo, Vector2.one))
            {
                Npcs.Add(wgo);
            }
        }
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(MapPageWidget), nameof(MapPageWidget.Redraw)),
            postfix: new HarmonyMethod(typeof(MapNpcsModule), nameof(AfterRedraw))
        );
    }

    private static void AfterRedraw(MapPageWidget __instance)
    {
        RectTransform mapRect = MapProjection.GetMapRect(__instance);
        if (mapRect == null)
        {
            return;
        }
        NpcMarkerLayer.Ensure(mapRect).Rescan();
    }
}
