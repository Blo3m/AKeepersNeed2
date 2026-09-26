using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.FreeCrafting;

/// <summary>
/// Crafting ignores item costs. Every "can start" path funnels through the
/// <c>CanStartCraft</c> overrides, which are wrapped in an <see cref="ItemCostScope"/> so their
/// inventory checks pass. Items are taken in exactly one place, <c>CraftComponent.RemoveRequirements</c>,
/// which is skipped outright, so nothing is consumed, <c>craftInput</c> stays empty, and
/// cancelling refunds nothing. The alchemy window checks and removes ingredients itself, so its
/// methods get the same scope.
/// </summary>
internal sealed class FreeCraftingModule : HarmonyModule
{
    public override string Name => "FreeCrafting";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.FreeCraftingEnabled;

    protected override void Apply(Harmony harmony)
    {
        ItemCostScope.PatchInventoryChecks(harmony);

        ItemCostScope.Wrap(harmony, AccessTools.DeclaredMethod(typeof(CraftElementBase), "CanStartCraft"));
        ItemCostScope.Wrap(harmony, AccessTools.DeclaredMethod(typeof(CraftElement), "CanStartCraft"));
        ItemCostScope.Wrap(harmony, AccessTools.DeclaredMethod(typeof(ConveyorCraftElement), "CanStartCraft"));
        // Fuel crafts bypass CanStartCraft for their count check and use GetTotalCount directly.
        ItemCostScope.Wrap(
            harmony,
            AccessTools.DeclaredMethod(typeof(UIBaseCraftSelectionWindowData), "UpdateCanStartStatus")
        );
        ItemCostScope.Wrap(harmony, AccessTools.DeclaredMethod(typeof(UIAlchemyWindow), "RedrawAlchemyTabLite"));
        ItemCostScope.Wrap(harmony, AccessTools.DeclaredMethod(typeof(UIAlchemyWindow), "OnStartMix"));

        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(CraftComponent), "RemoveRequirements"),
            prefix: new HarmonyMethod(typeof(FreeCraftingModule), nameof(SkipRemoveRequirements))
        );
    }

    private static bool SkipRemoveRequirements()
    {
        return false;
    }
}
