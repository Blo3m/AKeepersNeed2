using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InfiniteHealth;

/// <summary>
/// Makes the player take no HP damage. Every hit on the player (enemy attacks, AOE, turrets,
/// scripted <c>AddPlayerHP(-n)</c>) lands in <c>HPComponent.ApplyDamage</c> on
/// <c>PlayerData.hpComponent</c>, so a prefix that skips the original for that one instance
/// blocks it all while enemies and world objects keep taking damage normally. The game's own
/// <c>IsImmuneToDamage</c> flag isn't used because cutscene scripts set and clear it.
/// </summary>
internal sealed class InfiniteHealthModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "InfiniteHealth";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "InfiniteHealth",
            "Enabled",
            false,
            "The player takes no HP damage."
        );
        settings.Toggle(MenuSection.Combat, 10, "Infinite Health", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(HPComponent), nameof(HPComponent.ApplyDamage), new[] { typeof(int) }),
            prefix: new HarmonyMethod(typeof(InfiniteHealthModule), nameof(BlockPlayerDamage))
        );
    }

    private static bool BlockPlayerDamage(HPComponent __instance)
    {
        return !ReferenceEquals(__instance, MainGame.PlayerData?.hpComponent);
    }
}
