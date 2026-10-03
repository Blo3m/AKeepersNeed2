using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.MoneyIncome;

/// <summary>
/// Multiplies money you earn, with separate controls for trading and everything else. Spending
/// is never touched.
/// <list type="bullet">
/// <item><b>Trade:</b> a postfix on <c>Trading.GetSingleItemCostInPlayerInventory</c>, the per-item
/// sell price the trade window shows and the deal total adds up, so the boosted price is visible
/// and the vendor pays it. Ignore Vendor Money swaps the vendor's money reads in
/// <c>CanAcceptDeal</c>/<c>EnoughMoney</c> for "enough", and clamps the vendor at 0 when the
/// deal is paid (<c>DoAcceptDeal</c>). Deals pay through <c>PlayerMoneyGameResSystem.Set</c>, so
/// the income multiplier below doesn't apply to them a second time.</item>
/// <item><b>Other income:</b> a prefix on <c>PlayerMoneyGameResSystem.Add</c> scales positive
/// amounts (quest rewards, sermons, …). Spending is <c>Add</c> with a negative amount. The menu's
/// money setter uses <c>Set</c>, so it isn't scaled either.</item>
/// </list>
/// <c>Vendor.CurMoney</c> is a trivial property Mono may inline, hence the call swaps rather than
/// a getter patch.
/// </summary>
internal sealed class MoneyIncomeModule : HarmonyModule
{
    private static ConfigEntry<bool> _trade;
    private static ConfigEntry<float> _tradeMultiplier;
    private static ConfigEntry<bool> _ignoreVendorMoney;
    private static ConfigEntry<bool> _other;
    private static ConfigEntry<float> _otherMultiplier;

    public override string Name => "MoneyIncome";

    protected override ConfigEntry<bool> EnabledFlag => _trade;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags => new[] { _trade, _other };

    private static bool IgnoringVendorMoney => _trade.Value && _ignoreVendorMoney.Value;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _trade = settings.Profile(
            "MoneyIncome",
            "TradeEnabled",
            false,
            "Multiply the prices vendors pay for what you sell."
        );
        _tradeMultiplier = settings.Profile(
            "MoneyIncome",
            "TradeMultiplier",
            1f,
            "Sell price multiplier.",
            new AcceptableValueRange<float>(1f, 50f)
        );
        _ignoreVendorMoney = settings.Profile(
            "MoneyIncome",
            "IgnoreVendorMoney",
            false,
            "Vendors buy even when they can't afford it (their money stops at 0)."
        );
        _other = settings.Profile(
            "MoneyIncome",
            "OtherEnabled",
            false,
            "Multiply money earned from everything except trading (quests, sermons, …)."
        );
        _otherMultiplier = settings.Profile(
            "MoneyIncome",
            "OtherMultiplier",
            1f,
            "Other income multiplier.",
            new AcceptableValueRange<float>(1f, 50f)
        );
        settings.Toggle(MenuSection.Money, 10, "Trade Income", _trade);
        settings.Slider(MenuSection.Money, 20, _tradeMultiplier, 1f, 50f, "'x'0.#");
        settings.Toggle(MenuSection.Money, 30, "    Ignore Vendor Money", _ignoreVendorMoney, () => _trade.Value);
        settings.Toggle(MenuSection.Money, 40, "Other Income", _other);
        settings.Slider(MenuSection.Money, 50, _otherMultiplier, 1f, 50f, "'x'0.#");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(Trading),
                "GetSingleItemCostInPlayerInventory",
                new[] { typeof(Item), typeof(int) }
            ),
            postfix: new HarmonyMethod(typeof(MoneyIncomeModule), nameof(ScaleSellPrice))
        );
        var swapMoneyRead = new HarmonyMethod(typeof(MoneyIncomeModule), nameof(SwapVendorMoneyRead));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Trading), "CanAcceptDeal"), transpiler: swapMoneyRead);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Trading), "EnoughMoney"), transpiler: swapMoneyRead);
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Trading), "DoAcceptDeal"),
            transpiler: new HarmonyMethod(typeof(MoneyIncomeModule), nameof(SwapVendorMoneyWrite))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(PlayerMoneyGameResSystem),
                nameof(PlayerMoneyGameResSystem.Add),
                new[] { typeof(float), typeof(bool) }
            ),
            prefix: new HarmonyMethod(typeof(MoneyIncomeModule), nameof(ScaleIncome))
        );
    }

    private static void ScaleSellPrice(ref int __result)
    {
        if (_trade.Value && __result > 0)
        {
            __result = Mathf.RoundToInt(__result * _tradeMultiplier.Value);
        }
    }

    private static void ScaleIncome(ref float value)
    {
        if (_other.Value && value > 0f)
        {
            value *= _otherMultiplier.Value;
        }
    }

    private static IEnumerable<CodeInstruction> SwapVendorMoneyRead(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.PropertyGetter(typeof(Vendor), nameof(Vendor.CurMoney)),
            AccessTools.Method(typeof(MoneyIncomeModule), nameof(VendorMoneyForCheck)),
            "MoneyIncome"
        );
    }

    private static IEnumerable<CodeInstruction> SwapVendorMoneyWrite(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.PropertySetter(typeof(Vendor), nameof(Vendor.CurMoney)),
            AccessTools.Method(typeof(MoneyIncomeModule), nameof(SetVendorMoney)),
            "MoneyIncome"
        );
    }

    private static int VendorMoneyForCheck(Vendor vendor)
    {
        return IgnoringVendorMoney
            ? int.MaxValue
            : vendor.CurMoney;
    }

    private static void SetVendorMoney(Vendor vendor, int value)
    {
        vendor.CurMoney = IgnoringVendorMoney
            ? Mathf.Max(0, value)
            : value;
    }
}
