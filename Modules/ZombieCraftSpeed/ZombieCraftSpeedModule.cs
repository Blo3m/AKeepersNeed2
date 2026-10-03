using System;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Zombies;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombieCraftSpeed;

/// <summary>
/// Zombies work crafting stations faster. <c>ZombieCraftActivity.Update</c> accumulates real time
/// and calls <c>UseTool(null, steps)</c> once a whole work step (~1s) has built up, but
/// <c>UseTool</c> ignores <c>steps</c> and does one. A prefix scales the time, and a second
/// prefix repeats <c>UseTool</c> once per step, stopping when the craft ends, so faster work
/// isn't capped by how often the game updates. Only zombies use this activity.
/// </summary>
internal sealed class ZombieCraftSpeedModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;
    private static bool _repeating;

    private static readonly Func<ZombieCraftActivity, CraftComponent> Station =
        AccessTools.MethodDelegate<Func<ZombieCraftActivity, CraftComponent>>(
            AccessTools.PropertyGetter(typeof(ZombieCraftActivity), "CraftComponent")
        );

    public override string Name => "ZombieCraftSpeed";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    // A zombie's own speed (Zombies tab editor) applies even while the global toggle is off.
    protected override bool AlwaysPatched => true;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("ZombieCraftSpeed", "Enabled", false, "Zombies work crafting stations faster.");
        _multiplier = settings.Profile(
            "ZombieCraftSpeed",
            "Multiplier",
            1f,
            "Zombie crafting speed multiplier.",
            new AcceptableValueRange<float>(1f, 50f)
        );
        settings.Toggle(MenuSection.ZombieWork, 10, "Zombie Craft Speed", _enabled);
        settings.Slider(MenuSection.ZombieWork, 20, _multiplier, 1f, 50f, "'x'0.#");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieCraftActivity), nameof(ZombieCraftActivity.Update)),
            prefix: new HarmonyMethod(typeof(ZombieCraftSpeedModule), nameof(ScaleTime))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieCraftActivity), nameof(ZombieCraftActivity.UseTool)),
            prefix: new HarmonyMethod(typeof(ZombieCraftSpeedModule), nameof(RepeatSteps))
        );
    }

    private static void ScaleTime(ZombieCraftActivity __instance, ref float deltaTime)
    {
        deltaTime *= Multiplier(__instance.Zombie);
    }

    /// <summary>The zombie's own speed if it has one, else the global one while it's on.</summary>
    private static float Multiplier(ZombieWgoData zombie)
    {
        return ZombieOverrides.CraftSpeed(zombie) ?? (_enabled.Value ? _multiplier.Value : 1f);
    }

    private static bool RepeatSteps(ZombieCraftActivity __instance, Item tool, int deltaTick)
    {
        if (_repeating || deltaTick <= 1 || Multiplier(__instance.Zombie) == 1f)
        {
            return true;
        }
        _repeating = true;
        try
        {
            for (int i = 0; i < deltaTick && IsCrafting(__instance); i++)
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

    private static bool IsCrafting(ZombieCraftActivity activity)
    {
        CraftComponent station = Station(activity);
        return station != null
            && station.CurrentCraftElement != null
            && station.Status != CraftComponentStatus.WaitingForOutputDrop;
    }
}
