using System;
using System.Collections.Generic;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using GK2.FlowCanvasNodes;
using HarmonyLib;

namespace AKeepersNeed2.Modules.FreeBuilding;

/// <summary>
/// Building ignores item costs. Every build path receives the player's chosen costs as a
/// <c>selectedNeedItems</c> argument to a local function, then both checks and removes exactly
/// that list, sometimes later in a fade callback or a build pointer's cached delegate. Swapping
/// in an empty list at the entry point makes <c>HasItemsById</c> pass and <c>RemoveItems</c> a
/// no-op on every downstream path, while the build-limit checks still run.
/// </summary>
internal sealed class FreeBuildingModule : HarmonyModule
{
    public override string Name => "FreeBuilding";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.FreeBuildingEnabled;

    protected override void Apply(Harmony harmony)
    {
        PatchEntry(harmony, typeof(BuildManager), "OpenBuildingWindow", "CanBuild");
        PatchEntry(harmony, typeof(BuildManager), "OpenBuildingWindow", "OnBuildPressed");
        PatchEntry(harmony, typeof(TownBuildingPlaceInteractionHandler), "Interact", "OnBuildPressed");
        PatchEntry(harmony, typeof(Flow_OpenTownBuildingWindow), "Show", "OnBuildPressed");
    }

    private static void PatchEntry(Harmony harmony, Type type, string outerMethod, string localFunction)
    {
        var method = LocalFunctions.Find(type, outerMethod, localFunction);
        if (method == null)
        {
            Plugin.Logger.LogWarning(
                $"[FreeBuilding] {type.Name}.{outerMethod} local function {localFunction} not found; "
                + "a game update may have renamed it."
            );
            return;
        }
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(FreeBuildingModule), nameof(ClearNeeds)));
    }

    private static void ClearNeeds(ref List<NeedItemData> selectedNeedItems)
    {
        selectedNeedItems = new List<NeedItemData>();
    }
}
