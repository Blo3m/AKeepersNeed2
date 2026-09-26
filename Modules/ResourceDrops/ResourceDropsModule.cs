using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.ResourceDrops;

/// <summary>
/// Multiplies loot dropped when a world object is destroyed/harvested — gathering nodes
/// (trees, rocks, ore) as well as corpses, enemies, and breakable props — by
/// the module's multiplier setting. Object death runs through
/// <c>WgoData.RunLogicsAfterDeath</c>, which spawns loot via <c>WgoData.MakeDrop</c>. A
/// <see cref="PatchScope"/> around the death method limits the scaling to death loot, so craft
/// output, player-dropped items, and container "take all" are untouched.
/// </summary>
internal sealed class ResourceDropsModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    private static readonly PatchScope Death = new PatchScope("ResourceDrops");

    public override string Name => "ResourceDrops";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "ResourceDrops",
            "Enabled",
            false,
            "Multiply loot dropped when a world object is destroyed/harvested."
        );
        _multiplier = settings.Profile(
            "ResourceDrops",
            "Multiplier",
            1f,
            "Resource drop multiplier.",
            new AcceptableValueRange<float>(1f, 10f)
        );
        settings.Toggle(MenuSection.Drops, 10, "Resource Drops", _enabled);
        settings.Slider(MenuSection.Drops, 20, _multiplier, 1f, 10f, "0.0");
    }

    protected override void Apply(Harmony harmony)
    {
        Death.Wrap(harmony, AccessTools.Method(typeof(WgoData), "RunLogicsAfterDeath"));
        harmony.Patch(
            AccessTools.Method(typeof(WgoData), nameof(WgoData.MakeDrop), new[] { typeof(Item) }),
            prefix: new HarmonyMethod(typeof(ResourceDropsModule), nameof(ScaleDeathDrop))
        );
    }

    private static void ScaleDeathDrop(Item item)
    {
        if (!Death.Active || item == null || item.Count <= 0)
        {
            return;
        }
        float multiplier = _multiplier.Value;
        item.Count = Mathf.Max(1, Mathf.RoundToInt(item.Count * multiplier));
    }
}
