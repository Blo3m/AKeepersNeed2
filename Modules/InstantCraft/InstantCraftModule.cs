using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InstantCraft;

/// <summary>
/// Crafts finish on their first progress tick. <c>CraftElementBase.Start</c> is where a craft's
/// length is fixed (<c>totalProgressTicks</c> from the def's duration plus perks), so a postfix
/// shrinks it to 1. It isn't 0 because the progress getters treat 0 as "no progress". Garden growing
/// runs on the same timer and is left alone, as are star and autopsy crafts, whose quality
/// is graded on ticks worked.
/// </summary>
internal sealed class InstantCraftModule : HarmonyModule
{
    private static readonly AccessTools.FieldRef<CraftElementBase, int> TotalProgressTicks =
        AccessTools.FieldRefAccess<CraftElementBase, int>("totalProgressTicks");

    public override string Name => "InstantCraft";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.InstantCraftEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(CraftElementBase), nameof(CraftElementBase.Start), new[] { typeof(ICraftable) }),
            postfix: new HarmonyMethod(typeof(InstantCraftModule), nameof(ShortenCraft))
        );
    }

    private static void ShortenCraft(CraftElementBase __instance)
    {
        if (__instance.ParamsData?.craftParamsType == CraftParamsData.CraftParamsType.GardenGrowing)
        {
            return;
        }
        if (__instance.Def.isStarCraft || __instance.Def.isAutopsyCraft)
        {
            return;
        }
        TotalProgressTicks(__instance) = 1;
    }
}
