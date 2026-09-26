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
