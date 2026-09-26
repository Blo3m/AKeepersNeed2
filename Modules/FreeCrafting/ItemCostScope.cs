using System.Collections.Generic;
using System.Reflection;
using AKeepersNeed2.Shared.Patching;
using HarmonyLib;

namespace AKeepersNeed2.Modules.FreeCrafting;

/// <summary>
/// Makes inventory "has items" checks pass and item removals no-op, but only while execution
/// is inside a method wrapped with <see cref="Wrap"/>. The underlying <c>MultiInventory</c> /
/// <c>Item</c> methods are shared by quests, trades and more, so they must never be bypassed
/// outside a craft check or consume path.
/// </summary>
internal static class ItemCostScope
{
    // Headroom so a caller that adds to a faked count can't overflow.
    private const int FakeTotalCount = int.MaxValue / 2;

    private static readonly PatchScope Scope = new PatchScope("FreeCrafting");

    private static bool Active => Scope.Active;

    /// <summary>Makes the inventory fakes active while <paramref name="method"/> runs.</summary>
    public static void Wrap(Harmony harmony, MethodBase method)
    {
        Scope.Wrap(harmony, method);
    }

    public static void PatchInventoryChecks(Harmony harmony)
    {
        PassCheck(
            harmony,
            AccessTools.Method(
                typeof(MultiInventory),
                nameof(MultiInventory.HasItemsById),
                new[] { typeof(List<NeedItemData>), typeof(int), typeof(WgoData) }
            )
        );
        PassCheck(
            harmony,
            AccessTools.Method(
                typeof(MultiInventory),
                nameof(MultiInventory.HasItemsById),
                new[] { typeof(List<Item>) }
            )
        );
        PassCheck(
            harmony,
            AccessTools.Method(
                typeof(Item),
                nameof(Item.HasItemsWithIds),
                new[] { typeof(List<NeedItemData>), typeof(int), typeof(WgoData) }
            )
        );
        PassCheck(
            harmony,
            AccessTools.Method(
                typeof(Item),
                nameof(Item.HasItemWithEnoughDurability),
                new[] { typeof(string), typeof(float) }
            )
        );
        harmony.Patch(
            AccessTools.Method(typeof(MultiInventory), nameof(MultiInventory.GetTotalCount), new[] { typeof(string) }),
            prefix: new HarmonyMethod(typeof(ItemCostScope), nameof(FakeCount))
        );
        harmony.Patch(
            AccessTools.Method(
                typeof(MultiInventory),
                nameof(MultiInventory.RemoveItems),
                new[] { typeof(List<NeedItemData>), typeof(int), typeof(WgoData) }
            ),
            prefix: new HarmonyMethod(typeof(ItemCostScope), nameof(SkipRemoveItems))
        );
        harmony.Patch(
            AccessTools.Method(typeof(MultiInventory), nameof(MultiInventory.RemoveItem), new[] { typeof(Item) }),
            prefix: new HarmonyMethod(typeof(ItemCostScope), nameof(SkipRemoveItem))
        );
    }

    private static void PassCheck(Harmony harmony, MethodBase method)
    {
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(ItemCostScope), nameof(ForceTrue)));
    }

    private static bool ForceTrue(ref bool __result)
    {
        if (!Active)
        {
            return true;
        }
        __result = true;
        return false;
    }

    private static bool FakeCount(ref int __result)
    {
        if (!Active)
        {
            return true;
        }
        __result = FakeTotalCount;
        return false;
    }

    private static bool SkipRemoveItems(ref List<Item> __result)
    {
        if (!Active)
        {
            return true;
        }
        __result = new List<Item>();
        return false;
    }

    private static bool SkipRemoveItem()
    {
        return !Active;
    }
}
