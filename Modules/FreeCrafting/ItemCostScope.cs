using System;
using System.Collections.Generic;
using System.Reflection;
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

    [ThreadStatic]
    private static int _depth;

    private static bool Active => _depth > 0;

    public static void Wrap(Harmony harmony, MethodBase method)
    {
        if (method == null)
        {
            Plugin.Logger.LogWarning("[FreeCrafting] scope target not found; a game update may have renamed it.");
            return;
        }
        harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(ItemCostScope), nameof(Enter)),
            finalizer: new HarmonyMethod(typeof(ItemCostScope), nameof(Exit))
        );
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

    // __state keeps Enter/Exit balanced even if another mod's prefix interferes.
    private static void Enter(out bool __state)
    {
        _depth++;
        __state = true;
    }

    private static void Exit(bool __state)
    {
        if (__state)
        {
            _depth--;
        }
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
