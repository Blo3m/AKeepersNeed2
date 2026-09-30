using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.Weather;

/// <summary>
/// Pick the weather and optionally lock it. The game rolls a random next state every quarter of a
/// game day (<c>WeatherSystem.RollNextWeatherState</c>, skipped while a story script forces the
/// weather). Set enters the chosen state the same way the game does, without the "forced" flag,
/// so nothing lasting is saved. Lock blocks the roll; when a story script ends its forced weather
/// (<c>ResetWeatherState</c>, which goes back to clean weather), the locked weather is put back.
/// </summary>
internal sealed class WeatherModule : HarmonyModule, IUpdatable
{
    private static ConfigEntry<string> _chosen;
    private static ConfigEntry<bool> _lock;
    private static string _lockedState;
    private static bool _restoreLocked;

    public override string Name => "Weather";

    protected override ConfigEntry<bool> EnabledFlag => _lock;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _chosen = settings.Profile(
            "Weather",
            "State",
            string.Empty,
            "Weather state the Set button switches to (a state name from the game)."
        );
        _lock = settings.Profile(
            "Weather",
            "Lock",
            false,
            "Stop the weather from changing on its own. Story weather still plays, then the locked "
                + "weather comes back."
        );
        _lock.SettingChanged += (sender, args) => RememberCurrent();
        settings.Choice(MenuSection.Weather, 10, "Weather", _chosen, WeatherStates.Names);
        settings.Button(MenuSection.Weather, 20, "Set Weather", "Set", SetNow);
        settings.Toggle(MenuSection.Weather, 30, "Lock Weather", _lock);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(WeatherSystem), "RollNextWeatherState"),
            prefix: new HarmonyMethod(typeof(WeatherModule), nameof(BlockRoll))
        );
        harmony.Patch(
            AccessTools.Method(typeof(WeatherSystem), nameof(WeatherSystem.ResetWeatherState)),
            postfix: new HarmonyMethod(typeof(WeatherModule), nameof(AfterStoryReset))
        );
        harmony.Patch(
            AccessTools.Method(typeof(WeatherSystem), nameof(WeatherSystem.RestoreFSMStateFromData)),
            postfix: new HarmonyMethod(typeof(WeatherModule), nameof(AfterLoad))
        );
    }

    public void Tick()
    {
        if (!_restoreLocked)
        {
            return;
        }
        _restoreLocked = false;
        if (!_lock.Value || !WeatherStates.HasGame || WeatherStates.Data.hasForceState)
        {
            return;
        }
        string state = _lockedState ?? Selected();
        if (!string.IsNullOrEmpty(state) && state != WeatherStates.Data.stateName)
        {
            WeatherSystem.Instance.SetWeatherState(state);
            Plugin.Logger.LogInfo($"[Weather] story weather ended, locked weather {state} restored.");
        }
    }

    private static bool BlockRoll()
    {
        return !_lock.Value;
    }

    // Deferred to the next tick: the reset runs inside a story script, which may still be using the
    // state machine.
    private static void AfterStoryReset()
    {
        _restoreLocked = _lock.Value;
    }

    private static void AfterLoad()
    {
        _lockedState = null;
        RememberCurrent();
    }

    private static void RememberCurrent()
    {
        if (_lock.Value && WeatherStates.HasGame && !WeatherStates.Data.hasForceState)
        {
            _lockedState = WeatherStates.Data.stateName;
        }
    }

    private static string Selected()
    {
        return SettingsBuilder.Resolve(_chosen.Value, WeatherStates.Names());
    }

    private static void SetNow()
    {
        if (!WeatherStates.HasGame || WeatherSystem.Instance == null)
        {
            Toast.Show("No active game");
            return;
        }
        WeatherData data = WeatherStates.Data;
        if (data.hasForceState)
        {
            Toast.Show("The story controls the weather right now");
            return;
        }
        // Indoors and in cutscenes the weather object is switched off, and entering a state may not
        // take effect until it's back.
        if (data.isWeatherPausedByTimeOfDay || data.isWeatherPausedByCinematics)
        {
            Toast.Show("Weather is paused indoors and in cutscenes");
            return;
        }
        string state = Selected();
        if (string.IsNullOrEmpty(state))
        {
            Toast.Show("No weather types found");
            return;
        }
        string label = SettingsBuilder.SpaceWords(state);
        _lockedState = state;
        // Entering the active state again would fade it out and in and re-run its script.
        if (state == data.stateName)
        {
            Toast.Show($"Already {label}");
            return;
        }
        WeatherSystem.Instance.SetWeatherState(state);
        Plugin.Logger.LogInfo($"[Weather] set to {state}.");
        // The mod menu pauses the game, and the weather only fades while it runs.
        Toast.Show($"Weather: {label} (after the menu closes)");
    }
}
