using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.GardenGrowth;

/// <summary>
/// Crops grow faster, or finish at once. A garden bed's growing is a craft advanced by
/// <c>CraftComponent.Update(deltaTime)</c>: it adds real time and runs as many growth ticks as fit
/// (clamped to what's left), each a success/failure roll against the grower's mastery. A prefix
/// scales that time for <c>GardenGrowing</c> crafts only; Instant Grow passes enough time to run
/// every remaining tick. Applies to every bed, including ones tended by zombie gardeners.
/// </summary>
internal sealed class GardenGrowthModule : HarmonyModule
{
    // Far longer than any crop; Update clamps the tick count to what's left.
    private const float InstantTime = 1_000_000f;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;
    private static ConfigEntry<bool> _instant;

    public override string Name => "GardenGrowth";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags => new[] { _enabled, _instant };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("GardenGrowth", "Enabled", false, "Crops grow faster.");
        _multiplier = settings.Profile(
            "GardenGrowth",
            "Multiplier",
            5f,
            "Crop growth speed multiplier.",
            new AcceptableValueRange<float>(1f, 100f)
        );
        _instant = settings.Profile(
            "GardenGrowth",
            "Instant",
            false,
            "Crops finish growing right after planting. Overrides the growth speed multiplier."
        );
        settings.Toggle(MenuSection.Growth, 10, "Growth Speed", _enabled);
        settings.Slider(MenuSection.Growth, 20, _multiplier, 1f, 100f, "0");
        settings.Toggle(MenuSection.Growth, 30, "Instant Grow", _instant);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(CraftComponent), nameof(CraftComponent.Update)),
            prefix: new HarmonyMethod(typeof(GardenGrowthModule), nameof(ScaleGrowth))
        );
    }

    private static void ScaleGrowth(CraftComponent __instance, ref float deltaTime)
    {
        CraftElementBase craft = __instance.CurrentCraftElement;
        if (craft?.ParamsData?.craftParamsType != CraftParamsData.CraftParamsType.GardenGrowing)
        {
            return;
        }
        if (_instant.Value)
        {
            deltaTime = InstantTime;
        }
        else if (_enabled.Value)
        {
            deltaTime *= _multiplier.Value;
        }
    }
}
