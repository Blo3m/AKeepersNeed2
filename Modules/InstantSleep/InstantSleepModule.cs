using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InstantSleep;

/// <summary>
/// Sleeping fills energy at once. While asleep, <c>EnergySystem.RestoreEnergyWhileSleeping</c>
/// adds energy per tick until it's full, then wakes the player once at least 2 real seconds
/// have passed. A prefix fills energy to max first, so the game's own wake-up path (lack-of-sleep
/// cleanup, the autosave, after-sleep events) still runs, about 2 seconds after falling asleep.
/// </summary>
internal sealed class InstantSleepModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "InstantSleep";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "InstantSleep",
            "Enabled",
            false,
            "Energy is full as soon as you fall asleep; you wake up about 2 seconds later."
        );
        settings.Toggle(MenuSection.Energy, 40, "Instant Sleep", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(EnergySystem), "RestoreEnergyWhileSleeping"),
            prefix: new HarmonyMethod(typeof(InstantSleepModule), nameof(FillEnergy))
        );
    }

    private static void FillEnergy()
    {
        PlayerEnergyGameResSystem energy = PlayerEnergyGameResSystem.GetSystem();
        if (energy != null && !energy.HasMax())
        {
            energy.Set(energy.Max);
        }
    }
}
