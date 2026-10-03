using System.Collections.Generic;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.MovementSpeed;

/// <summary>
/// Scales the player's walking speed, always or only while a sprint key is held/toggled on.
/// <c>PlayerPhysicalBody.MoveByDirection</c> computes its force from
/// <c>physicsConfig.speed * SpeedMultiplier</c>. <c>SpeedMultiplier</c> is an auto-property the
/// game drops to 0.5 during sword/bow attacks, so rather than overwrite it (the game would fight
/// back) or patch its getter (Mono may inline it), a transpiler swaps that one read for
/// <see cref="EffectiveMultiplier"/>. Only the player has a <c>PlayerPhysicalBody</c>.
/// </summary>
internal sealed class MovementSpeedModule : HarmonyModule, IUpdatable
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;
    private static ConfigEntry<SpeedMode> _mode;
    private static ConfigEntry<KeyCode> _key;
    private static ConfigEntry<bool> _keepAttackSlowdown;
    private static bool _toggledOn;

    public override string Name => "MovementSpeed";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("MovementSpeed", "Enabled", false, "Change the player's walking speed.");
        _multiplier = settings.Profile(
            "MovementSpeed",
            "Multiplier",
            1f,
            "Walking speed multiplier while the speed change is active.",
            new AcceptableValueRange<float>(0.5f, 5f)
        );
        _mode = settings.Profile(
            "MovementSpeed",
            "Mode",
            SpeedMode.AlwaysOn,
            "AlwaysOn: always active. HoldKey: only while the speed key is held. "
                + "ToggleKey: the speed key turns it on/off."
        );
        _key = settings.Profile(
            "MovementSpeed",
            "Key",
            KeyCode.None,
            "Key for the HoldKey and ToggleKey modes (None = unbound)."
        );
        _keepAttackSlowdown = settings.Profile(
            "MovementSpeed",
            "KeepAttackSlowdown",
            true,
            "Keep the game's slowdown while attacking; off moves at full speed while attacking."
        );
        _mode.SettingChanged += (_, _) => _toggledOn = false;

        settings.Toggle(MenuSection.Movement, 30, "Movement Speed", _enabled);
        settings.Slider(MenuSection.Movement, 40, _multiplier, 0.5f, 5f, "'x'0.#");
        settings.Choice(MenuSection.Movement, 50, "Speed Mode", _mode);
        settings.Key(MenuSection.Movement, 60, "Speed Key", _key, enabledWhen: () => _mode.Value != SpeedMode.AlwaysOn);
        settings.Toggle(MenuSection.Movement, 70, "Keep Attack Slowdown", _keepAttackSlowdown);
    }

    public void Tick()
    {
        KeyCode key = _key.Value;
        if (!_enabled.Value || _mode.Value != SpeedMode.ToggleKey || key == KeyCode.None)
        {
            return;
        }
        if (Input.GetKeyDown(key) && !InputGate.IsBlocked)
        {
            _toggledOn = !_toggledOn;
        }
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(PlayerPhysicalBody), nameof(PlayerPhysicalBody.MoveByDirection)),
            transpiler: new HarmonyMethod(typeof(MovementSpeedModule), nameof(SwapSpeedMultiplier))
        );
    }

    private static IEnumerable<CodeInstruction> SwapSpeedMultiplier(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.PropertyGetter(typeof(PlayerPhysicalBody), nameof(PlayerPhysicalBody.SpeedMultiplier)),
            AccessTools.Method(typeof(MovementSpeedModule), nameof(EffectiveMultiplier)),
            "MovementSpeed"
        );
    }

    private static float EffectiveMultiplier(PlayerPhysicalBody body)
    {
        float multiplier = body.SpeedMultiplier;
        if (!_enabled.Value || !IsActive())
        {
            return multiplier;
        }
        if (!_keepAttackSlowdown.Value && multiplier < 1f)
        {
            multiplier = 1f;
        }
        return multiplier * _multiplier.Value;
    }

    private static bool IsActive()
    {
        switch (_mode.Value)
        {
            case SpeedMode.HoldKey:
                return _key.Value != KeyCode.None && Input.GetKey(_key.Value) && !InputGate.IsBlocked;
            case SpeedMode.ToggleKey:
                return _toggledOn;
            default:
                return true;
        }
    }
}
