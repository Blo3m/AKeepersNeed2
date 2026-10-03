using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.TechPoints;

/// <summary>
/// Multiplies technology-point rewards (red/green/blue) by
/// the module's multiplier setting. Every tech-point spawn funnels through
/// <c>DropSystem.DropTechPoints</c> — surveys, crafts, stored-point collection, etc. — so a
/// prefix that scales the r/g/b amounts covers them all. (Other <c>CreateSpawner</c> callers
/// only spawn happiness, not tech, and are untouched.)
/// </summary>
internal sealed class TechPointsModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    public override string Name => "TechPoints";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "TechPoints",
            "Enabled",
            false,
            "Multiply technology-point rewards (red/green/blue)."
        );
        _multiplier = settings.Profile(
            "TechPoints",
            "Multiplier",
            1f,
            "Technology point multiplier.",
            new AcceptableValueRange<float>(1f, 10f)
        );
        settings.Toggle(MenuSection.Drops, 30, "Tech Points", _enabled);
        settings.Slider(MenuSection.Drops, 40, _multiplier, 1f, 10f, "'x'0.#");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(DropSystem), nameof(DropSystem.DropTechPoints)),
            prefix: new HarmonyMethod(typeof(TechPointsModule), nameof(ScaleTechPoints))
        );
    }

    private static void ScaleTechPoints(ref int r, ref int g, ref int b)
    {
        float multiplier = _multiplier.Value;
        r = Mathf.RoundToInt(r * multiplier);
        g = Mathf.RoundToInt(g * multiplier);
        b = Mathf.RoundToInt(b * multiplier);
    }
}
