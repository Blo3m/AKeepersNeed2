using System.Collections.Generic;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapReveal;

/// <summary>
/// Reveals the whole map. <c>MapPageWidget.UpdateZones</c> clouds every zone whose id isn't
/// in <c>knowledgeSystem.knownMapZones</c>, and <c>UIMapZone.Draw</c> hides area labels not in
/// <c>visitedWorldZones</c>. A replacement prefix draws every zone as known and shows every
/// label, without touching either save list — turning it off restores the real state on the
/// next map redraw.
/// </summary>
internal sealed class MapRevealModule : HarmonyModule
{
    private static readonly AccessTools.FieldRef<MapPageWidget, Dictionary<string, UIMapZone>> ZonesRef =
        AccessTools.FieldRefAccess<MapPageWidget, Dictionary<string, UIMapZone>>("zones");

    private static readonly AccessTools.FieldRef<MapPageWidget, List<UISortComponent>> OtherMapRef =
        AccessTools.FieldRefAccess<MapPageWidget, List<UISortComponent>>("sortComponentsOtherMap");

    public override string Name => "MapReveal";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.RevealMapEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(MapPageWidget), "UpdateZones"),
            prefix: new HarmonyMethod(typeof(MapRevealModule), nameof(RevealAllZones))
        );
    }

    private static bool RevealAllZones(MapPageWidget __instance)
    {
        foreach (UIMapZone zone in ZonesRef(__instance).Values)
        {
            zone.Draw(false);
            foreach (UIMapWorldZone worldZone in zone.GetComponentsInChildren<UIMapWorldZone>(true))
            {
                worldZone.gameObject.SetActive(true);
            }
        }

        // The original hides the "other map" overlay once every zone is known.
        foreach (UISortComponent item in OtherMapRef(__instance))
        {
            ((Component)item).gameObject.SetActive(false);
        }
        return false;
    }
}
