using System;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Zombies;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombieGatherSpeed;

/// <summary>
/// Zombies chop, mine and harvest faster. <c>ZombieHPActivity.Update</c> accumulates real time and
/// calls <c>UseTool(hand, hits)</c> once a whole hit (~1s) has built up, but <c>UseTool</c> ignores
/// <c>hits</c> and swings once. A prefix scales the time, and a second prefix repeats
/// <c>UseTool</c> once per hit, stopping when the object is used up. Each hit is a normal swing,
/// so HP-threshold drops still fire. Only zombies use this activity.
/// </summary>
internal sealed class ZombieGatherSpeedModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;
    private static bool _repeating;

    private static readonly Func<ZombieHPActivity, WgoData> Target =
        AccessTools.MethodDelegate<Func<ZombieHPActivity, WgoData>>(
            AccessTools.PropertyGetter(typeof(ZombieHPActivity), "WgoData")
        );

    public override string Name => "ZombieGatherSpeed";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    // A zombie's own speed (Zombies tab editor) applies even while the global toggle is off.
    protected override bool AlwaysPatched => true;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "ZombieGatherSpeed",
            "Enabled",
            false,
            "Zombies chop, mine and harvest faster."
        );
        _multiplier = settings.Profile(
            "ZombieGatherSpeed",
            "Multiplier",
            1f,
            "Zombie gathering speed multiplier.",
            new AcceptableValueRange<float>(1f, 50f)
        );
        settings.Toggle(MenuSection.ZombieWork, 30, "Zombie Gather Speed", _enabled);
        settings.Slider(MenuSection.ZombieWork, 40, _multiplier, 1f, 50f, "'x'0.#");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieHPActivity), nameof(ZombieHPActivity.Update)),
            prefix: new HarmonyMethod(typeof(ZombieGatherSpeedModule), nameof(ScaleTime))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieHPActivity), nameof(ZombieHPActivity.UseTool)),
            prefix: new HarmonyMethod(typeof(ZombieGatherSpeedModule), nameof(RepeatHits))
        );
    }

    private static void ScaleTime(ZombieHPActivity __instance, ref float deltaTime)
    {
        deltaTime *= Multiplier(__instance.Zombie);
    }

    /// <summary>The zombie's own speed if it has one, else the global one while it's on.</summary>
    private static float Multiplier(ZombieWgoData zombie)
    {
        return ZombieOverrides.GatherSpeed(zombie) ?? (_enabled.Value ? _multiplier.Value : 1f);
    }

    private static bool RepeatHits(ZombieHPActivity __instance, Item tool, int deltaTick)
    {
        if (_repeating || deltaTick <= 1 || Multiplier(__instance.Zombie) == 1f)
        {
            return true;
        }
        _repeating = true;
        try
        {
            for (int i = 0; i < deltaTick && HasHpLeft(__instance); i++)
            {
                __instance.UseTool(tool, 1);
            }
        }
        finally
        {
            _repeating = false;
        }
        return false;
    }

    private static bool HasHpLeft(ZombieHPActivity activity)
    {
        WgoData target = Target(activity);
        return target?.HpComponent != null && target.HpComponent.Hp > 0;
    }
}
