using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.GatheringSpeed;

/// <summary>
/// Makes chopping, mining, digging and harvesting take fewer swings. The game models each swing
/// as damage to the object's HP (<c>PlayerHPActivity.GetActionDamage</c>, applied by
/// <c>UseTool</c>), so a postfix scales that damage, or with One Hit raises it to the object's
/// remaining HP. Negative results are "restore HP" objects and 0 means the swing does nothing,
/// so both are left alone. <see cref="Priority.Low"/> runs it after IgnoreGatheringMastery,
/// which turns an under-the-lock 0 into a normal swing. Zombies use <c>ZombieHPActivity</c>,
/// so only the player is affected.
/// </summary>
internal sealed class GatheringSpeedModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;
    private static ConfigEntry<bool> _oneHit;

    public override string Name => "GatheringSpeed";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags => new[] { _enabled, _oneHit };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "GatheringSpeed",
            "Enabled",
            false,
            "Chop, mine, dig and harvest with fewer swings."
        );
        _multiplier = settings.Profile(
            "GatheringSpeed",
            "Multiplier",
            2f,
            "Gathering speed multiplier (how much each swing does).",
            new AcceptableValueRange<float>(1f, 20f)
        );
        _oneHit = settings.Profile(
            "GatheringSpeed",
            "OneHit",
            false,
            "Every swing finishes the object. Overrides the gathering speed multiplier."
        );
        settings.Toggle(MenuSection.Gathering, 20, "Gathering Speed", _enabled);
        settings.Slider(MenuSection.Gathering, 30, _multiplier, 1f, 20f, "0.0");
        settings.Toggle(MenuSection.Gathering, 40, "One Hit", _oneHit);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(PlayerHPActivity), nameof(PlayerHPActivity.GetActionDamage)),
            postfix: new HarmonyMethod(typeof(GatheringSpeedModule), nameof(ScaleSwing)) { priority = Priority.Low }
        );
    }

    private static void ScaleSwing(PlayerHPActivity __instance, ref int __result)
    {
        if (__result <= 0)
        {
            return;
        }
        if (_oneHit.Value)
        {
            HPComponent hp = __instance.WgoData?.HpComponent;
            if (hp != null && hp.Hp > __result)
            {
                __result = hp.Hp;
            }
            return;
        }
        if (_enabled.Value)
        {
            __result = Mathf.Max(__result, Mathf.RoundToInt(__result * _multiplier.Value));
        }
    }
}
