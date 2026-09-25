using AKeepersNeed2.Core;
using UnityEngine;

namespace AKeepersNeed2.Modules.EnergyRegen;

/// <summary>
/// Passively regenerates the player's energy while awake. Each frame it adds energy at
/// <c>ModConfig.EnergyRegenRate</c> (energy per 5 seconds) — vanilla only restores energy
/// during sleep, so this fills the bar during normal play. Skipped while sleeping (the
/// game already restores then), while paused, and outside of an active game.
/// </summary>
internal sealed class EnergyRegenModule : IModule, IUpdatable
{
    private const float RatePeriodSeconds = 5f;

    public string Name => "EnergyRegen";

    public int Order => 100;

    public void Enable()
    {
    }

    public void Disable()
    {
    }

    public void Tick()
    {
        if (!ModConfig.EnergyRegenEnabled.Value)
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

        float perSecond = ModConfig.EnergyRegenRate.Value / RatePeriodSeconds;
        system.Add(perSecond * Time.deltaTime);
    }
}
