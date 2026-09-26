using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Crafting;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.IgnoreCraftingMastery;

/// <summary>
/// Lets the player run crafts whose mastery lock is above their level (station crafts, star
/// crafts, autopsies), working as if they just met the requirement. Garden crafts are left to
/// IgnoreGardenMastery.
/// <list type="bullet">
/// <item><c>CraftParamsData.MasteryValue</c> (the player's level for the craft) is raised to at
/// least <c>MasteryLock</c>, so <c>PlayerController.CheckWorkerDependentValues</c> no longer
/// returns <c>NotEnoughMastery</c> and the craft UI shows the lock as met.</item>
/// <item><c>PlayerCraftActivity.IsEnoughMastery</c> (the tool gate) is forced true, and a 0 from
/// <c>GetActionDamage</c> (under the lock, or a failed star/autopsy chance roll) becomes 1 cell,
/// the at-the-lock progress.</item>
/// </list>
/// Zombie workers keep their own mastery.
/// </summary>
internal sealed class IgnoreCraftingMasteryModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "IgnoreCraftingMastery";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "IgnoreCraftingMastery",
            "Enabled",
            false,
            "Run crafts above your mastery level (incl. star crafts and autopsies), as if you just met it."
        );
        settings.Toggle(MenuSection.Crafting, 30, "Ignore Crafting Mastery", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.PropertyGetter(typeof(CraftParamsData), nameof(CraftParamsData.MasteryValue)),
            postfix: new HarmonyMethod(typeof(IgnoreCraftingMasteryModule), nameof(RaiseToLock))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(PlayerCraftActivity), nameof(PlayerCraftActivity.IsEnoughMastery)),
            postfix: new HarmonyMethod(typeof(IgnoreCraftingMasteryModule), nameof(ForceEnoughMastery))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(PlayerCraftActivity), nameof(PlayerCraftActivity.GetActionDamage)),
            postfix: new HarmonyMethod(typeof(IgnoreCraftingMasteryModule), nameof(DamageAsIfMet))
        );
    }

    private static void RaiseToLock(CraftParamsData __instance, ref int __result)
    {
        if (__instance.craftParamsType != CraftParamsData.CraftParamsType.Common
            || CraftWorkers.IsZombieCraft(__instance))
        {
            return;
        }
        int masteryLock = __instance.MasteryLock;
        if (__result < masteryLock)
        {
            __result = masteryLock;
        }
    }

    private static void ForceEnoughMastery(ref bool __result)
    {
        __result = true;
    }

    // At or above the lock the game gives ≥ 1 cell, so a 0 with a craft running only comes from
    // being under the lock or losing the star/autopsy chance roll.
    private static void DamageAsIfMet(PlayerCraftActivity __instance, ref int __result)
    {
        if (__result == 0 && __instance.CraftComponent?.CurrentCraftElement != null)
        {
            __result = 1;
        }
    }
}
