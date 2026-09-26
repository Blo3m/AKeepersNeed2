using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.CraftDrops;

/// <summary>
/// Multiplies the quantity a craft station produces when a craft finishes, by
/// the module's multiplier setting. On completion, <c>CraftComponent.HandleOutput</c>
/// builds the output by calling <c>CraftElementBase.MakeOutput()</c> and then delivers it
/// (to the station inventory or as drops). A postfix on <c>MakeOutput</c> scales the
/// returned items before delivery. The base method covers normal craft elements; the
/// <c>ConveyorCraftElement</c> override (which emits one item at a time) is patched too.
/// </summary>
internal sealed class CraftDropsModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    public override string Name => "CraftDrops";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "CraftDrops",
            "Enabled",
            false,
            "Multiply the output quantity of craft stations."
        );
        _multiplier = settings.Profile(
            "CraftDrops",
            "Multiplier",
            1f,
            "Craft output multiplier.",
            new AcceptableValueRange<float>(1f, 10f)
        );
        settings.Toggle(MenuSection.CraftOutput, 10, "Craft Output", _enabled);
        settings.Slider(MenuSection.CraftOutput, 20, _multiplier, 1f, 10f, "0.0");
    }

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
        float multiplier = _multiplier.Value;
        foreach (Item item in __result)
        {
            if (item != null && item.Count > 0)
            {
                item.Count = Mathf.Max(1, Mathf.RoundToInt(item.Count * multiplier));
            }
        }
    }
}
