using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombieNoShovel;

/// <summary>
/// Zombie gardeners work without a shovel. A gardener only takes a garden order with a shovel in hand
/// (<c>ZombieWgoData.GardenerTryGetNewOrder</c>) and stops working when its tool changes to anything
/// else (<c>GardenerOnToolChanged</c>). Those two checks read <c>ZombieWgoData.Hand</c>; a transpiler
/// points them at <see cref="HandForShovelCheck"/>, which answers with a shovel while this is on. The
/// zombie's real hand is unchanged, and nothing is written to the save.
/// </summary>
internal sealed class ZombieNoShovelModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static Item _shovel;

    public override string Name => "ZombieNoShovel";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("ZombieNoShovel", "Enabled", false, "Zombie gardeners work without a shovel.");
        settings.Toggle(MenuSection.ZombieWork, 70, "Gardeners Need No Shovel", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        var swap = new HarmonyMethod(typeof(ZombieNoShovelModule), nameof(SwapHand));
        harmony.Patch(AccessTools.Method(typeof(ZombieWgoData), "GardenerTryGetNewOrder"), transpiler: swap);
        harmony.Patch(
            AccessTools.Method(typeof(ZombieWgoData), nameof(ZombieWgoData.GardenerOnToolChanged)),
            transpiler: swap
        );
    }

    private static IEnumerable<CodeInstruction> SwapHand(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.PropertyGetter(typeof(ZombieWgoData), nameof(ZombieWgoData.Hand)),
            AccessTools.Method(typeof(ZombieNoShovelModule), nameof(HandForShovelCheck)),
            "ZombieNoShovel"
        );
    }

    /// <summary>The zombie's hand, or a stand-in shovel when the toggle is on and it holds none.</summary>
    private static Item HandForShovelCheck(ZombieWgoData zombie)
    {
        Item hand = zombie.Hand;
        if (!_enabled.Value || (hand != null && !hand.IsEmpty && hand.Definition.type == ItemType.Shovel))
        {
            return hand;
        }
        return Shovel() ?? hand;
    }

    // Any shovel item works: the checks only look at the item type.
    private static Item Shovel()
    {
        if (_shovel != null)
        {
            return _shovel;
        }
        List<ItemDef> defs = GameBalance.Me?.itemDefs;
        if (defs == null)
        {
            return null;
        }
        foreach (ItemDef def in defs)
        {
            if (def.type == ItemType.Shovel)
            {
                _shovel = new Item(def.id, 1);
                return _shovel;
            }
        }
        return null;
    }
}
