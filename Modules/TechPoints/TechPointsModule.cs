using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.TechPoints;

/// <summary>
/// Multiplies technology-point rewards (red/green/blue) by
/// <c>ModConfig.TechPointsMultiplier</c>. Every tech-point spawn funnels through
/// <c>DropSystem.DropTechPoints</c> — surveys, crafts, stored-point collection, etc. — so a
/// prefix that scales the r/g/b amounts covers them all. (Other <c>CreateSpawner</c> callers
/// only spawn happiness, not tech, and are untouched.)
/// </summary>
internal sealed class TechPointsModule : HarmonyModule
{
    public override string Name => "TechPoints";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.TechPointsEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(DropSystem), nameof(DropSystem.DropTechPoints)),
            prefix: new HarmonyMethod(typeof(TechPointsModule), nameof(ScaleTechPoints))
        );
    }

    private static void ScaleTechPoints(ref int r, ref int g, ref int b)
    {
        float multiplier = ModConfig.TechPointsMultiplier.Value;
        r = Mathf.RoundToInt(r * multiplier);
        g = Mathf.RoundToInt(g * multiplier);
        b = Mathf.RoundToInt(b * multiplier);
    }
}
