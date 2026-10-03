using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.FertilizerSlots;

/// <summary>
/// All 3 fertilizer slots on every garden and vineyard bed while on. The slot count is one player
/// stat, <c>g_garden_fertilizer_slots</c>, read in two places: the free-slot check before a fertilizer
/// is applied (<c>GardenInteractionHandler.HasFreeFertilizerPerkSlot</c>) and the bed window drawing its
/// slots (<c>UIGardenBedWindow.UpdatePerks</c>). Their <c>PlayerData.GetResInt</c> calls are swapped for
/// <see cref="SlotsOrMore"/>, which reports at least 3 for that stat; the stat itself is never changed,
/// so turning this off goes back to the slots you've unlocked. 3 is the game's ceiling: slot ids are
/// hard-coded 1–3 and the bed window has 3 slot widgets.
/// </summary>
internal sealed class FertilizerSlotsModule : HarmonyModule
{
    private const string SlotsStat = "g_garden_fertilizer_slots";
    private const int MaxSlots = 3;

    private static ConfigEntry<bool> _enabled;

    public override string Name => "FertilizerSlots";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "FertilizerSlots",
            "Enabled",
            false,
            "Every garden and vineyard bed has all 3 fertilizer slots (the game's maximum)."
        );
        settings.Toggle(MenuSection.Crops, 70, "All Fertilizer Slots", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        var swap = new HarmonyMethod(typeof(FertilizerSlotsModule), nameof(SwapSlotCount));
        harmony.Patch(
            AccessTools.Method(
                typeof(GardenInteractionHandler),
                nameof(GardenInteractionHandler.HasFreeFertilizerPerkSlot)
            ),
            transpiler: swap
        );
        harmony.Patch(AccessTools.Method(typeof(UIGardenBedWindow), "UpdatePerks"), transpiler: swap);
    }

    private static IEnumerable<CodeInstruction> SwapSlotCount(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.Method(typeof(PlayerData), nameof(PlayerData.GetResInt), new[] { typeof(string) }),
            AccessTools.Method(typeof(FertilizerSlotsModule), nameof(SlotsOrMore)),
            "FertilizerSlots"
        );
    }

    private static int SlotsOrMore(PlayerData player, string type)
    {
        int value = player.GetResInt(type);
        return _enabled.Value && type == SlotsStat
            ? Math.Max(value, MaxSlots)
            : value;
    }
}
