using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Modules.EnergyRegen;

/// <summary>
/// Passively regenerates the player's energy while awake. Each frame it adds energy at
/// the module's rate setting (energy per 5 seconds) — vanilla only restores energy
/// during sleep, so this fills the bar during normal play. Skipped while sleeping (the
/// game already restores then), while paused, and outside of an active game.
/// </summary>
internal sealed class EnergyRegenModule : IModule, IUpdatable, ISettingsDeclarer
{
    private const float RatePeriodSeconds = 5f;

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<float> _rate;

    public string Name => "EnergyRegen";

    public int Order => 100;

    public void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "EnergyRegen",
            "Enabled",
            false,
            "Passively regenerate energy while not sleeping."
        );
        _rate = settings.Profile(
            "EnergyRegen",
            "RatePer5s",
            1f,
            "Energy regenerated every 5 seconds while not sleeping.",
            new AcceptableValueRange<float>(0.1f, 10f)
        );
        settings.Toggle(MenuSection.Energy, 10, "Energy Regen", _enabled);
        settings.Slider(MenuSection.Energy, 20, _rate, 0.1f, 10f, "0.0");
    }

    public void Enable()
    {
    }

    public void Disable()
    {
    }

    public void Tick()
    {
        if (!_enabled.Value)
        {
            return;
        }
        if (MainGame.Instance == null || MainGame.IsGamePaused || MainGame.PlayerData == null)
        {
            return;
        }

        EnergySystem energy = MainGame.PlayerData.energySystem;
        if (energy == null || energy.IsSleeping || energy.IsInTransitionBetweenSleep)
        {
            return;
        }

        PlayerEnergyGameResSystem system = PlayerEnergyGameResSystem.GetSystem();
        if (system == null || system.HasMax())
        {
            return;
        }

        float perSecond = _rate.Value / RatePeriodSeconds;
        system.Add(perSecond * Time.deltaTime);
    }
}
