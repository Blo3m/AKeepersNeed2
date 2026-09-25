using System.Collections.Generic;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.CraftDrops;

/// <summary>
/// Multiplies the quantity a craft station produces when a craft finishes, by
/// <c>ModConfig.CraftDropMultiplier</c>. On completion, <c>CraftComponent.HandleOutput</c>
/// builds the output by calling <c>CraftElementBase.MakeOutput()</c> and then delivers it
/// (to the station inventory or as drops). A postfix on <c>MakeOutput</c> scales the
/// returned items before delivery. The base method covers normal craft elements; the
/// <c>ConveyorCraftElement</c> override (which emits one item at a time) is patched too.
/// </summary>
internal sealed class CraftDropsModule : HarmonyModule
{
    public override string Name => "CraftDrops";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.CraftDropsEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(CraftElementBase), nameof(CraftElementBase.MakeOutput)),
            postfix: new HarmonyMethod(typeof(CraftDropsModule), nameof(ScaleCraftOutput))
        );
        harmony.Patch(
            AccessTools.Method(typeof(ConveyorCraftElement), nameof(ConveyorCraftElement.MakeOutput)),
            postfix: new HarmonyMethod(typeof(CraftDropsModule), nameof(ScaleCraftOutput))
        );
    }

    private static void ScaleCraftOutput(List<Item> __result)
    {
        if (__result == null)
        {
            return;
        }
        float multiplier = ModConfig.CraftDropMultiplier.Value;
        foreach (Item item in __result)
        {
            if (item != null && item.Count > 0)
            {
                item.Count = Mathf.Max(1, Mathf.RoundToInt(item.Count * multiplier));
            }
        }
    }
}
