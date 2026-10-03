using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Garden;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.FreePlanting;

/// <summary>
/// Plant and fertilize without using up seeds or fertilizer, on garden and vineyard beds, for the
/// player and zombie gardeners alike. Every bed craft (planting a seed, applying a fertilizer) takes
/// its items in <c>CraftComponent.RemoveRequirements</c>; a prefix skips it when the craft belongs to a
/// bed, empty or planted (<see cref="GardenBeds.IsBed"/>). You still pick a seed from your inventory, so you need
/// one of each kind. Free Crafting leaves bed crafts to this toggle.
/// </summary>
internal sealed class FreePlantingModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "FreePlanting";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "FreePlanting",
            "Enabled",
            false,
            "Planting and fertilizing garden and vineyard beds uses up no seeds or fertilizer (zombie gardeners too)."
        );
        settings.Toggle(MenuSection.Crops, 60, "Free Planting", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(CraftComponent), "RemoveRequirements"),
            prefix: new HarmonyMethod(typeof(FreePlantingModule), nameof(KeepSeeds))
        );
    }

    private static bool KeepSeeds(CraftComponent __instance)
    {
        return !GardenBeds.IsBed(__instance.CraftableObject as WgoData);
    }
}
