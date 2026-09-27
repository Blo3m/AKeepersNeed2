using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.WorldTime;

/// <summary>
/// The day/night clock: day length, freezing it, and skipping ahead. The clock is
/// <c>EnvironmentEngine.timeOfDay</c> (0–1, daytime 0.25–0.75), advanced by
/// <c>CustomUpdate(deltaTime)</c> at <c>deltaTime / (gameplayDayInMinutes * 60)</c> per call. A
/// prefix scales that time (Day Length) or skips the call (Freeze Time). Only the clock is
/// affected: crafts and crops run on real time. The clock keeps running while the player sleeps,
/// because sleep refills energy by game time and would never end. Skip runs the clock forward
/// through <c>CustomUpdate</c> itself, so the new day and time-of-day events fire as usual.
/// </summary>
internal sealed class WorldTimeModule : HarmonyModule
{
    private const float VanillaDayMinutes = 5f;

    private static ConfigEntry<bool> _dayLengthOn;
    private static ConfigEntry<float> _dayMinutes;
    private static ConfigEntry<bool> _freeze;
    private static ConfigEntry<SkipTarget> _skipTarget;
    private static bool _skipping;

    public override string Name => "WorldTime";

    protected override ConfigEntry<bool> EnabledFlag => _dayLengthOn;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags => new[] { _dayLengthOn, _freeze };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _dayLengthOn = settings.Profile(
            "WorldTime",
            "DayLengthEnabled",
            false,
            "Change how many real minutes one game day lasts."
        );
        _dayMinutes = settings.Profile(
            "WorldTime",
            "DayMinutes",
            10f,
            "Real minutes per game day (vanilla 5).",
            new AcceptableValueRange<float>(1f, 60f)
        );
        _freeze = settings.Profile(
            "WorldTime",
            "Freeze",
            false,
            "Stop the clock. It still runs while you sleep."
        );
        _skipTarget = settings.Profile(
            "WorldTime",
            "SkipTarget",
            SkipTarget.Sunrise,
            "Time the Skip button moves the clock forward to."
        );
        settings.Toggle(MenuSection.Time, 10, "Day Length", _dayLengthOn);
        settings.Slider(MenuSection.Time, 20, _dayMinutes, 1f, 60f, "0 min");
        settings.Toggle(MenuSection.Time, 30, "Freeze Time", _freeze);
        settings.Choice(MenuSection.Time, 40, "Skip To", _skipTarget);
        settings.Button(
            MenuSection.Time,
            50,
            "Skip Time",
            "Skip",
            "Skip time?",
            () => $"The clock moves forward to the next {_skipTarget.Value.ToString().ToLower()}. "
                + "Time-based events fire as if the time had passed. This changes your save.",
            Skip
        );
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(EnvironmentEngine), nameof(EnvironmentEngine.CustomUpdate)),
            prefix: new HarmonyMethod(typeof(WorldTimeModule), nameof(ScaleClock))
        );
    }

    private static bool ScaleClock(EnvironmentEngine __instance, ref float deltaTime)
    {
        if (_skipping || IsSleeping())
        {
            return true;
        }
        if (_freeze.Value)
        {
            return false;
        }
        if (_dayLengthOn.Value && _dayMinutes.Value > 0f)
        {
            float vanilla = __instance.gameplayDayInMinutes > 0f
                ? __instance.gameplayDayInMinutes
                : VanillaDayMinutes;
            deltaTime *= vanilla / _dayMinutes.Value;
        }
        return true;
    }

    private static bool IsSleeping()
    {
        EnergySystem energy = MainGame.PlayerData?.energySystem;
        return energy != null && (energy.IsSleeping || energy.IsInTransitionBetweenSleep);
    }

    private static void Skip()
    {
        EnvironmentEngine clock = EnvironmentEngine.Instance;
        if (clock == null || MainGame.PlayerData == null)
        {
            Toast.Show("No active game");
            return;
        }
        // Cutscenes and fights pause the clock; skipping then would do nothing.
        if (clock.IsPaused)
        {
            Toast.Show("Can't skip time right now");
            return;
        }
        float target = TargetTime(_skipTarget.Value);
        float ahead = Mathf.Repeat(target - clock.timeOfDay, 1f);
        if (ahead < 0.001f)
        {
            ahead = 1f;
        }
        float seconds = ahead * clock.gameplayDayInMinutes * 60f;
        _skipping = true;
        try
        {
            clock.CustomUpdate(seconds);
        }
        finally
        {
            _skipping = false;
        }
        Plugin.Logger.LogInfo($"[WorldTime] skipped {ahead:0.###} of a day to {_skipTarget.Value}.");
        Toast.Show($"Skipped to {_skipTarget.Value.ToString().ToLower()}");
    }

    private static float TargetTime(SkipTarget target)
    {
        switch (target)
        {
            case SkipTarget.Noon:
                return 0.5f;
            case SkipTarget.Sunset:
                return 0.75f;
            case SkipTarget.Midnight:
                return 0f;
            default:
                return 0.25f;
        }
    }
}
