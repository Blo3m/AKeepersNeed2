using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using LazyBearTechnology;

namespace AKeepersNeed2.Modules.GardenQuality;

/// <summary>
/// Crops that never fail and come out gold. A bed's growing craft ticks in
/// <c>CraftComponent.UpdateGardenGrowingCraft</c>: below the mastery lock each tick is a roll, and a
/// failed tick still uses up a cell (no crop for it). Every successful cell adds the bed resources
/// the craft lists for it (<c>crop</c>, <c>crop_b</c>, <c>crop_s</c>, <c>crop_g</c>: common to gold),
/// which the harvest pays out. Planting is a craft worked by hand whose successful cells are the
/// crop's head start; for star crafts each hit is a roll too. Gardens and vineyards alike, beds
/// tended by zombies included. Only crops grown while on are affected; the save holds them as usual.
/// </summary>
internal sealed class GardenQualityModule : HarmonyModule
{
    private const string Gold = "crop_g";
    private static readonly string[] LowerQualities = { "crop", "crop_b", "crop_s" };

    private static readonly AccessTools.FieldRef<ZombieCraftActivity, WgoData> ZombieBedRef =
        AccessTools.FieldRefAccess<ZombieCraftActivity, WgoData>("wgoData");

    private static ConfigEntry<bool> _perfectGrowth;
    private static ConfigEntry<bool> _goldOnly;
    private static ConfigEntry<bool> _perfectPlanting;

    public override string Name => "GardenQuality";

    protected override ConfigEntry<bool> EnabledFlag => _perfectGrowth;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags =>
        new[] { _perfectGrowth, _goldOnly, _perfectPlanting };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _perfectGrowth = settings.Profile(
            "GardenQuality",
            "PerfectGrowth",
            false,
            "Every growth tick succeeds on every bed, so no crop cell is lost."
        );
        _goldOnly = settings.Profile("GardenQuality", "GoldOnly", false, "Every crop grows as gold quality.");
        _perfectPlanting = settings.Profile(
            "GardenQuality",
            "PerfectPlanting",
            false,
            "Planting work never fails a cell, so crops start with the full head start."
        );
        settings.Toggle(MenuSection.Crops, 10, "Perfect Growth", _perfectGrowth);
        settings.Toggle(MenuSection.Crops, 20, "Gold Crops Only", _goldOnly);
        settings.Toggle(MenuSection.Crops, 30, "Perfect Planting", _perfectPlanting);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(CraftComponent), nameof(CraftComponent.UpdateGardenGrowingCraft)),
            prefix: new HarmonyMethod(typeof(GardenQualityModule), nameof(GrowWithoutFailing))
        );
        harmony.Patch(
            AccessTools.Method(typeof(WgoData), nameof(WgoData.OnSuccessfulTicksChange)),
            prefix: new HarmonyMethod(typeof(GardenQualityModule), nameof(AddCropsAsGold))
        );
        var plantingHit = new HarmonyMethod(typeof(GardenQualityModule), nameof(PlantWithoutFailing));
        harmony.Patch(
            AccessTools.Method(typeof(PlayerCraftActivity), nameof(PlayerCraftActivity.GetActionDamage)),
            postfix: plantingHit
        );
        harmony.Patch(
            AccessTools.Method(typeof(ZombieCraftActivity), nameof(ZombieCraftActivity.GetActionDamage)),
            postfix: plantingHit
        );
    }

    /// <summary>
    /// Replaces the growth loop while Perfect Growth is on: a tick below the mastery lock always
    /// grows one cell instead of rolling; at or above it, as many as the game would give.
    /// </summary>
    private static bool GrowWithoutFailing(int ticks, CraftElementBase craftEl)
    {
        if (!_perfectGrowth.Value || craftEl?.ParamsData == null)
        {
            return true;
        }
        int maxPerHit = ConstDef.Get("max_cells_per_one_hit").IntValue;
        for (int i = 0; i < ticks; i++)
        {
            int left = craftEl.TotalProgressTicks - craftEl.ProgressTicks;
            if (left <= 0)
            {
                break;
            }
            int mastery = craftEl.ParamsData.MasteryValue;
            int masteryLock = craftEl.ParamsData.MasteryLock;
            int cells = masteryLock > 0 && mastery >= masteryLock
                ? Math.Max(1, Math.Min(mastery / masteryLock, maxPerHit))
                : 1;
            craftEl.Update(Math.Min(Math.Min(cells, 3), left));
        }
        return false;
    }

    /// <summary>
    /// Does the game's per-cell crop reward itself while Gold Crops Only is on, with every common,
    /// bronze and silver crop added as gold instead. Other rewards are added unchanged.
    /// </summary>
    private static bool AddCropsAsGold(
        WgoData __instance,
        int startTick,
        int endTick,
        CraftElementBase craftElement
    )
    {
        if (!_goldOnly.Value || !(craftElement is CraftElement craft))
        {
            return true;
        }
        foreach (GameResPerProgress reward in craft.Definition.gameresPerSuccessfulProgress)
        {
            if (reward.sucessfulProgressTick > startTick && reward.sucessfulProgressTick <= endTick)
            {
                __instance.AddGameRes(AsGold(reward.gameRes));
            }
        }
        return false;
    }

    private static GameRes AsGold(GameRes reward)
    {
        GameRes gold = reward.Clone();
        foreach (string quality in LowerQualities)
        {
            float amount = gold.Get(quality);
            if (amount > 0f)
            {
                gold.Set(quality, 0f);
                gold.Add(Gold, amount);
            }
        }
        gold.RemoveZeroValues();
        return gold;
    }

    /// <summary>A planting hit that rolled a failure (star crafts only) grows one cell instead.</summary>
    private static void PlantWithoutFailing(object __instance, ref int __result)
    {
        if (!_perfectPlanting.Value || __result > 0)
        {
            return;
        }
        CraftElementBase craft = CurrentCraft(__instance);
        if (craft?.ParamsData?.craftParamsType == CraftParamsData.CraftParamsType.GardenPlanting
            && craft.Def != null
            && craft.Def.isStarCraft)
        {
            __result = 1;
        }
    }

    private static CraftElementBase CurrentCraft(object activity)
    {
        switch (activity)
        {
            case PlayerCraftActivity player:
                return player.CraftComponent?.CurrentCraftElement;
            case ZombieCraftActivity zombie:
                return ZombieBedRef(zombie)?.CraftComponent?.CurrentCraftElement;
            default:
                return null;
        }
    }
}
