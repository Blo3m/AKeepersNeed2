using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Crafting;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.IgnoreGardenMastery;

/// <summary>
/// Lets the player plant and grow crops without the gardening (green) mastery a seed needs,
/// as if they just met it.
/// <list type="bullet">
/// <item>Planting refuses at green mastery 0 in two places, <c>GardenInteractionHandler.Interact</c>
/// (using a seed on a bed) and <c>UIGardenBedWindow.OnPlantButtonPress</c>. While either runs
/// (a <see cref="PatchScope"/>), <c>PlayerController.GetMasteryLevelForTalentBranch("talent_green")</c>
/// reports at least 1. That call is used everywhere, so it's only lifted inside those two methods.</item>
/// <item>Garden crafts' <c>CraftParamsData.MasteryValue</c> is raised to at least
/// <c>MasteryLock</c>, which clears the planting <c>NotEnoughMastery</c> status and makes
/// <c>CraftComponent.UpdateGardenGrowingCraft</c> grow 1 cell per tick instead of a
/// mastery-based chance (0 at mastery 0, so the crop would never grow).</item>
/// </list>
/// Zombie gardeners keep their own mastery.
/// </summary>
internal sealed class IgnoreGardenMasteryModule : HarmonyModule
{
    private const string GreenTalent = "talent_green";

    private static readonly PatchScope Planting = new PatchScope("IgnoreGardenMastery");

    private static ConfigEntry<bool> _enabled;

    public override string Name => "IgnoreGardenMastery";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "IgnoreGardenMastery",
            "Enabled",
            false,
            "Plant and grow crops without the gardening mastery they need, as if you just met it."
        );
        settings.Toggle(MenuSection.Crafting, 40, "Ignore Garden Mastery", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        Planting.Wrap(harmony, AccessTools.DeclaredMethod(typeof(GardenInteractionHandler), "Interact"));
        Planting.Wrap(harmony, AccessTools.DeclaredMethod(typeof(UIGardenBedWindow), "OnPlantButtonPress"));
        harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(PlayerController),
                nameof(PlayerController.GetMasteryLevelForTalentBranch)
            ),
            postfix: new HarmonyMethod(typeof(IgnoreGardenMasteryModule), nameof(LiftGreenWhilePlanting))
        );
        harmony.Patch(
            AccessTools.PropertyGetter(typeof(CraftParamsData), nameof(CraftParamsData.MasteryValue)),
            postfix: new HarmonyMethod(typeof(IgnoreGardenMasteryModule), nameof(RaiseToLock))
        );
    }

    private static void LiftGreenWhilePlanting(string talentId, ref int __result)
    {
        if (Planting.Active && talentId == GreenTalent && __result <= 0)
        {
            __result = 1;
        }
    }

    private static void RaiseToLock(CraftParamsData __instance, ref int __result)
    {
        if (__instance.craftParamsType == CraftParamsData.CraftParamsType.Common
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
}
