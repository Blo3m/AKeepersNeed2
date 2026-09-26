using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InfiniteEnergy;

/// <summary>
/// Stops energy from ever draining. Every energy change funnels through
/// <c>PlayerEnergyGameResSystem.Add</c> (both direct calls and
/// <c>PlayerData.SubRes/AddRes("energy", …)</c> via <c>GameRes.Add</c>), so a prefix that
/// skips the original for negative values blocks all consumption while leaving gains
/// (sleep restore, passive regen) untouched.
/// </summary>
internal sealed class InfiniteEnergyModule : HarmonyModule
{
    public override string Name => "InfiniteEnergy";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.InfiniteEnergyEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                typeof(PlayerEnergyGameResSystem),
                nameof(PlayerEnergyGameResSystem.Add),
                new[] { typeof(float), typeof(bool) }
            ),
            prefix: new HarmonyMethod(typeof(InfiniteEnergyModule), nameof(BlockEnergyDrain))
        );
    }

    private static bool BlockEnergyDrain(float value)
    {
        return value >= 0f;
    }
}
