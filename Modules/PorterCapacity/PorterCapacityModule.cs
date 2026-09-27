using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
using AKeepersNeed2.Shared.Zombies;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.PorterCapacity;

/// <summary>
/// How many slots zombie porters carry (vanilla 4). The game creates a porter's inventory with 4
/// slots in <c>ZombieWgoData.AttachToPorterStation</c>; a postfix resizes it to the porter's own
/// capacity (set in the Zombies tab editor) or, while the global setting is on, the global one.
/// The size is saved with the porter: turning the global setting on is the consent for new
/// porters, and "Apply to All Porters" (behind a confirm) resizes existing ones. Patched always,
/// because a porter's own capacity must survive re-attaching while the global setting is off.
/// </summary>
internal sealed class PorterCapacityModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _capacity;

    public override string Name => "PorterCapacity";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    protected override bool AlwaysPatched => true;

    private static int GlobalCapacity => Mathf.RoundToInt(_capacity.Value);

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "PorterCapacity",
            "Enabled",
            false,
            "New zombie porters carry this many slots (saved on each porter)."
        );
        _capacity = settings.Profile(
            "PorterCapacity",
            "Capacity",
            8f,
            "Porter carrying slots (vanilla 4).",
            new AcceptableValueRange<float>(PorterInventory.VanillaSize, 50f)
        );
        settings.Toggle(MenuSection.ZombiePorters, 10, "Porter Capacity", _enabled);
        settings.Slider(MenuSection.ZombiePorters, 20, _capacity, PorterInventory.VanillaSize, 50f, "0");
        settings.Button(
            MenuSection.ZombiePorters,
            30,
            "Existing Porters",
            "Apply to All",
            "Resize all porters?",
            () => $"Every porter without its own capacity will carry {GlobalCapacity} slots. "
                + "This is saved and permanent.",
            ApplyToAll
        );
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieWgoData), nameof(ZombieWgoData.AttachToPorterStation)),
            postfix: new HarmonyMethod(typeof(PorterCapacityModule), nameof(ResizeNewPorter))
        );
    }

    private static void ResizeNewPorter(ZombieWgoData __instance)
    {
        int? size = ZombieOverrides.PorterCapacity(__instance) ?? (_enabled.Value ? GlobalCapacity : (int?)null);
        if (size.HasValue)
        {
            PorterInventory.Resize(__instance, size.Value);
        }
    }

    private static void ApplyToAll()
    {
        int resized = 0;
        foreach (ZombieWgoData zombie in ZombieRoster.Placed())
        {
            if (ZombieOverrides.PorterCapacity(zombie) == null && PorterInventory.Resize(zombie, GlobalCapacity))
            {
                resized++;
            }
        }
        Plugin.Logger.LogInfo($"[PorterCapacity] resized {resized} porters to {GlobalCapacity} slots.");
        Toast.Show($"{resized} porters now carry {GlobalCapacity} slots");
    }
}
