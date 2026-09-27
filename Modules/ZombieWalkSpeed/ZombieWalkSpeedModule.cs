using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Zombies;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.ZombieWalkSpeed;

/// <summary>
/// Zombies walk faster (or slower). Every zombie path is started at the same default speed and
/// advanced by <c>MovementComponent.Update(deltaTime)</c>. A prefix scales that time when the
/// component belongs to a zombie (its private <c>assignedMovableObject</c>), so the player, NPCs
/// and the saved path speed are untouched. Covers every placed zombie, fighters included.
/// </summary>
internal sealed class ZombieWalkSpeedModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    private static readonly AccessTools.FieldRef<MovementComponent, IMovable> Owner =
        AccessTools.FieldRefAccess<MovementComponent, IMovable>("assignedMovableObject");

    public override string Name => "ZombieWalkSpeed";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    // A zombie's own speed (Zombies tab editor) applies even while the global toggle is off.
    protected override bool AlwaysPatched => true;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("ZombieWalkSpeed", "Enabled", false, "Change how fast zombies walk.");
        _multiplier = settings.Profile(
            "ZombieWalkSpeed",
            "Multiplier",
            2f,
            "Zombie walking speed multiplier.",
            new AcceptableValueRange<float>(0.5f, 10f)
        );
        settings.Toggle(MenuSection.ZombieWork, 50, "Zombie Walk Speed", _enabled);
        settings.Slider(MenuSection.ZombieWork, 60, _multiplier, 0.5f, 10f, "0.0");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(MovementComponent), nameof(MovementComponent.Update)),
            prefix: new HarmonyMethod(typeof(ZombieWalkSpeedModule), nameof(ScaleTime))
        );
    }

    private static void ScaleTime(MovementComponent __instance, ref float deltaTime)
    {
        if (Owner(__instance) is ZombieWgoData zombie)
        {
            deltaTime *= ZombieOverrides.WalkSpeed(zombie) ?? (_enabled.Value ? _multiplier.Value : 1f);
        }
    }
}
