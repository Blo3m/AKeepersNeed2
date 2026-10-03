using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using LazyBearTechnology;

namespace AKeepersNeed2.Modules.GardenQuality;

/// <summary>
/// Crops that never fail and come out at their best quality. A bed's growing craft ticks in
/// <c>CraftComponent.UpdateGardenGrowingCraft</c>: below the mastery lock each tick is a roll, and a
/// failed tick still uses up a cell (no crop for it). Every successful cell adds the bed resources
/// the craft lists for it (<c>crop</c>, <c>crop_b</c>, <c>crop_s</c>, <c>crop_g</c>: common to gold),
/// which the harvest pays out. Planting is a craft worked by hand whose successful cells are the
/// crop's head start; for star crafts each hit is a roll too. Gardens and vineyards alike, beds
/// tended by zombies included. Only crops grown while on are affected; the save holds them as usual.
/// </summary>
internal sealed class GardenQualityModule : HarmonyModule
{
    // Bed counters per quality, lowest first. A crop's harvest formulas only read the tiers its own
    // rewards use (tier 1 pumpkin: bronze and silver; wheat: plain only), so each crop is upgraded to
    // its own best tier, never to one nothing reads.
    private static readonly string[] CropTiers = { "crop", "crop_b", "crop_s", "crop_g" };
    private static readonly string[] SeedTiers = { "seed", "seed_b", "seed_s", "seed_g" };

    private static readonly Dictionary<CraftDef, string[]> BestTiers = new Dictionary<CraftDef, string[]>();

    private static readonly AccessTools.FieldRef<ZombieCraftActivity, WgoData> ZombieBedRef =
        AccessTools.FieldRefAccess<ZombieCraftActivity, WgoData>("wgoData");

    private static ConfigEntry<bool> _perfectGrowth;
    private static ConfigEntry<bool> _bestQuality;
    private static ConfigEntry<bool> _perfectPlanting;

    public override string Name => "GardenQuality";

    protected override ConfigEntry<bool> EnabledFlag => _perfectGrowth;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags =>
        new[] { _perfectGrowth, _bestQuality, _perfectPlanting };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _perfectGrowth = settings.Profile(
            "GardenQuality",
            "PerfectGrowth",
            false,
            "Every growth tick succeeds on every bed, so no crop cell is lost."
        );
        _bestQuality = settings.Profile(
            "GardenQuality",
            "BestQuality",
            false,
            "Every crop and seed grows as the best quality that crop has (gold where it has gold)."
        );
        _perfectPlanting = settings.Profile(
            "GardenQuality",
            "PerfectPlanting",
            false,
            "Planting work never fails a cell, so crops start with the full head start."
        );
        settings.Toggle(MenuSection.Crops, 10, "Perfect Growth", _perfectGrowth);
        settings.Toggle(MenuSection.Crops, 20, "Best Quality Crops", _bestQuality);
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
            prefix: new HarmonyMethod(typeof(GardenQualityModule), nameof(AddAsBestQuality))
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
    /// Does the game's per-cell reward itself while Best Quality Crops is on, with every crop and seed
    /// added as the best tier this crop's rewards use. Other rewards are added unchanged.
    /// </summary>
    private static bool AddAsBestQuality(
        WgoData __instance,
        int startTick,
        int endTick,
        CraftElementBase craftElement
    )
    {
        if (!_bestQuality.Value || !(craftElement is CraftElement craft))
        {
            return true;
        }
        foreach (GameResPerProgress reward in craft.Definition.gameresPerSuccessfulProgress)
        {
            if (reward.sucessfulProgressTick > startTick && reward.sucessfulProgressTick <= endTick)
            {
                __instance.AddGameRes(AsBest(reward.gameRes, Best(craft.Definition)));
            }
        }
        return false;
    }

    /// <summary>The best crop and seed tier <paramref name="def"/>'s rewards use (null when none).</summary>
    private static string[] Best(CraftDef def)
    {
        if (BestTiers.TryGetValue(def, out string[] best))
        {
            return best;
        }
        best = new[] { BestUsed(def, CropTiers), BestUsed(def, SeedTiers) };
        BestTiers[def] = best;
        return best;
    }

    private static string BestUsed(CraftDef def, string[] tiers)
    {
        string best = null;
        foreach (GameResPerProgress reward in def.gameresPerSuccessfulProgress)
        {
            for (int i = tiers.Length - 1; i >= 0; i--)
            {
                if (reward.gameRes.Get(tiers[i]) > 0f)
                {
                    if (best == null || i > Array.IndexOf(tiers, best))
                    {
                        best = tiers[i];
                    }
                    break;
                }
            }
        }
        return best;
    }

    private static GameRes AsBest(GameRes reward, string[] best)
    {
        GameRes upgraded = reward.Clone();
        MoveTo(upgraded, CropTiers, best[0]);
        MoveTo(upgraded, SeedTiers, best[1]);
        upgraded.RemoveZeroValues();
        return upgraded;
    }

    private static void MoveTo(GameRes reward, string[] tiers, string best)
    {
        if (best == null)
        {
            return;
        }
        foreach (string tier in tiers)
        {
            float amount = reward.Get(tier);
            if (tier != best && amount > 0f)
            {
                reward.Set(tier, 0f);
                reward.Add(best, amount);
            }
        }
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
