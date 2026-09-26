using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.IgnoreGatheringMastery;

/// <summary>
/// Lets the player chop, mine and dig world objects whose mastery lock is above their level,
/// working as if they just met the requirement. Removing an object is a
/// <c>PlayerHPActivity</c>: <c>ToolComponent</c> refuses the tool when <c>IsEnoughMastery</c>
/// is false, and <c>GetActionDamage</c> returns 0 below the lock. The first is forced true;
/// a 0 damage roll becomes the at-the-lock damage (<c>playerHpActivityMod</c> × 1 cell).
/// </summary>
internal sealed class IgnoreGatheringMasteryModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "IgnoreGatheringMastery";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "IgnoreGatheringMastery",
            "Enabled",
            false,
            "Chop, mine and dig objects above your mastery level, as if you just met the requirement."
        );
        settings.Toggle(MenuSection.Gathering, 10, "Ignore Gathering Mastery", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(PlayerHPActivity), nameof(PlayerHPActivity.IsEnoughMastery)),
            postfix: new HarmonyMethod(typeof(IgnoreGatheringMasteryModule), nameof(ForceEnoughMastery))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(PlayerHPActivity), nameof(PlayerHPActivity.GetActionDamage)),
            postfix: new HarmonyMethod(typeof(IgnoreGatheringMasteryModule), nameof(DamageAsIfMet))
        );
    }

    private static void ForceEnoughMastery(ref bool __result)
    {
        __result = true;
    }

    // At or above the lock the game deals playerHpActivityMod × (level / lock) ≥ 1 cells, so a 0
    // only comes from being under the lock (or an object that takes no damage, mod 0, which stays 0).
    private static void DamageAsIfMet(PlayerHPActivity __instance, ref int __result)
    {
        if (__result == 0 && __instance.WgoData != null)
        {
            __result = __instance.WgoData.Definition.playerHpActivityMod;
        }
    }
}
