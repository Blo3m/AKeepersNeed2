using AKeepersNeed2.Core;
using AKeepersNeed2.Shared.Zombies;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombiePerkCap;

/// <summary>
/// Honours a zombie's "Ignore perk cap" (set in the Zombies tab editor, stored on the zombie).
/// Red skulls cap how many perks a zombie can learn: the game's own tree refuses more
/// (<c>IsEnoughFreeSkullsToBuyTalentLevelUp</c>), and removing an organ suspends the newest extra
/// perks (<c>CheckRedSkulls(false)</c>). For flagged zombies the first reports true and the second
/// is skipped. There's no menu row: the flag lives in the save, so the patches are always on and
/// cost one lookup per call.
/// </summary>
internal sealed class ZombiePerkCapModule : IModule
{
    private Harmony _harmony;

    public string Name => "ZombiePerkCap";

    public int Order => 100;

    public void Enable()
    {
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{Name}");
        _harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ZombieWgoData), nameof(ZombieWgoData.CheckRedSkulls)),
            prefix: new HarmonyMethod(typeof(ZombiePerkCapModule), nameof(SkipSuspending))
        );
        _harmony.Patch(
            AccessTools.DeclaredMethod(
                typeof(ZombieWgoData),
                nameof(ZombieWgoData.IsEnoughFreeSkullsToBuyTalentLevelUp)
            ),
            postfix: new HarmonyMethod(typeof(ZombiePerkCapModule), nameof(AllowBuying))
        );
    }

    public void Disable()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private static bool SkipSuspending(ZombieWgoData __instance, bool addRedSkulls)
    {
        return addRedSkulls || !ZombieOverrides.IgnorePerkCap(__instance);
    }

    private static void AllowBuying(ZombieWgoData __instance, ref bool __result)
    {
        if (!__result && ZombieOverrides.IgnorePerkCap(__instance))
        {
            __result = true;
        }
    }
}
