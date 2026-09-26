using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InfiniteStamina;

/// <summary>
/// Stops combat stamina (sword/bow attacks) from draining. Attacks spend it through
/// <c>StaminaSystem.ConsumeStamina</c> → <c>PlayerStaminaGameResSystem.Add(-cost)</c>, so a
/// prefix that skips the original for negative values blocks all spending while regen and the
/// fight-start refill still work. Separate from energy, which work actions spend.
/// </summary>
internal sealed class InfiniteStaminaModule : HarmonyModule
{
    public override string Name => "InfiniteStamina";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.InfiniteStaminaEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(
                typeof(PlayerStaminaGameResSystem),
                nameof(PlayerStaminaGameResSystem.Add),
                new[] { typeof(float), typeof(bool) }
            ),
            prefix: new HarmonyMethod(typeof(InfiniteStaminaModule), nameof(BlockStaminaDrain))
        );
    }

    private static bool BlockStaminaDrain(float value)
    {
        return value >= 0f;
    }
}
