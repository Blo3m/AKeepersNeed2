using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.ResourceDrops;

/// <summary>
/// Multiplies loot dropped when a world object is destroyed/harvested — gathering nodes
/// (trees, rocks, ore) as well as corpses, enemies, and breakable props — by
/// <c>ModConfig.ResourceDropMultiplier</c>. Object death runs through
/// <c>WgoData.RunLogicsAfterDeath</c>, which spawns loot via <c>WgoData.MakeDrop</c>. A
/// depth flag set around the death method scopes the scaling to death loot, so craft
/// output, player-dropped items, and container "take all" are untouched.
/// </summary>
internal sealed class ResourceDropsModule : HarmonyModule
{
    private static int _deathDepth;

    public override string Name => "ResourceDrops";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.ResourceDropsEnabled;

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(WgoData), "RunLogicsAfterDeath"),
            prefix: new HarmonyMethod(typeof(ResourceDropsModule), nameof(EnterDeath)),
            finalizer: new HarmonyMethod(typeof(ResourceDropsModule), nameof(ExitDeath))
        );
        harmony.Patch(
            AccessTools.Method(typeof(WgoData), nameof(WgoData.MakeDrop), new[] { typeof(Item) }),
            prefix: new HarmonyMethod(typeof(ResourceDropsModule), nameof(ScaleDeathDrop))
        );
    }

    private static void EnterDeath()
    {
        _deathDepth++;
    }

    private static void ExitDeath()
    {
        _deathDepth--;
    }

    private static void ScaleDeathDrop(Item item)
    {
        if (_deathDepth <= 0 || item == null || item.Count <= 0)
        {
            return;
        }
        float multiplier = ModConfig.ResourceDropMultiplier.Value;
        item.Count = Mathf.Max(1, Mathf.RoundToInt(item.Count * multiplier));
    }
}
