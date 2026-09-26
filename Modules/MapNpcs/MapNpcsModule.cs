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
    public override string Name => "MapNpcs";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.MapNpcsEnabled;

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
