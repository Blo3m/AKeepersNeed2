using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Garden;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.GardenYield;

/// <summary>
/// More produce per garden or vineyard harvest. When a crop finishes growing, its harvest (crops,
/// seeds, leaves) is put into the ready bed; harvesting destroys the bed, and
/// <c>WgoData.RunLogicsAfterDeath</c> drops what it holds through <c>WgoData.MakeDrop</c>. Inside that
/// death, drops from a crop bed (<see cref="GardenBeds.HasCrop"/>) are multiplied. Scaling at the
/// drop leaves the bed's saved contents as the game made them. Resource Drops skips crop beds, so
/// the two don't stack.
/// </summary>
internal sealed class GardenYieldModule : HarmonyModule
{
    private const float MaxMultiplier = 20f;

    private static readonly PatchScope Death = new PatchScope("GardenYield");

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    public override string Name => "GardenYield";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "GardenYield",
            "Enabled",
            false,
            "Multiply what garden and vineyard harvests give (crops, seeds and leaves)."
        );
        _multiplier = settings.Profile(
            "GardenYield",
            "Multiplier",
            1f,
            "Harvest multiplier.",
            new AcceptableValueRange<float>(1f, MaxMultiplier)
        );
        settings.Toggle(MenuSection.Crops, 40, "Harvest Yield", _enabled);
        settings.Slider(MenuSection.Crops, 50, _multiplier, 1f, MaxMultiplier, "'x'0", () => _enabled.Value);
    }

    protected override void Apply(Harmony harmony)
    {
        Death.Wrap(harmony, AccessTools.Method(typeof(WgoData), "RunLogicsAfterDeath"));
        harmony.Patch(
            AccessTools.Method(typeof(WgoData), nameof(WgoData.MakeDrop), new[] { typeof(Item) }),
            prefix: new HarmonyMethod(typeof(GardenYieldModule), nameof(ScaleHarvestDrop))
        );
    }

    private static void ScaleHarvestDrop(WgoData __instance, Item item)
    {
        if (!Death.Active || item == null || item.Count <= 0 || !GardenBeds.HasCrop(__instance))
        {
            return;
        }
        item.Count = Mathf.Max(1, Mathf.RoundToInt(item.Count * Mathf.Round(_multiplier.Value)));
    }
}
