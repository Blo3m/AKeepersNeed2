using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombieMaxMastery;

/// <summary>
/// Every zombie works as if it had enough mastery for any job at full output. The game has no
/// maximum mastery: a zombie's work per step is <c>mastery / job lock</c>, capped at
/// <c>max_cells_per_one_hit</c>, and jobs refuse zombies under their lock. Every read of a
/// zombie's mastery goes through <c>ZombieWgoData.GetMasteryLevelForTalentBranch</c>, so a postfix
/// raises the result to <see cref="MasteryCeiling"/> (highest lock × per-hit cap). Nothing is
/// written: the saved talent values are untouched and turning it off shows the real values again.
/// The boosted value shows in the game's zombie window while on.
/// </summary>
internal sealed class ZombieMaxMasteryModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    public override string Name => "ZombieMaxMastery";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "ZombieMaxMastery",
            "Enabled",
            false,
            "Zombies work with enough mastery for every job at full output. Not saved."
        );
        settings.Toggle(MenuSection.ZombieMastery, 10, "Max Mastery", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(ZombieWgoData),
                nameof(ZombieWgoData.GetMasteryLevelForTalentBranch)
            ),
            postfix: new HarmonyMethod(typeof(ZombieMaxMasteryModule), nameof(RaiseToCeiling))
        );
    }

    private static void RaiseToCeiling(ref int __result)
    {
        int ceiling = MasteryCeiling.Value;
        if (__result < ceiling)
        {
            __result = ceiling;
        }
    }
}
