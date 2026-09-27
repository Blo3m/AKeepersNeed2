using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using LazyBearTechnology;

namespace AKeepersNeed2.Modules.ReputationMultiplier;

/// <summary>
/// Multiplies reputation gains with NPCs and districts; losses are unchanged. Reputation is a
/// plain resource in the player's <c>GameRes</c>, and every gain (script <c>AddRep</c>,
/// <c>PlayerData.AddNPCRep</c>, <c>AddRes(GameRes)</c> rewards) ends in
/// <c>GameRes.Add(string, float)</c>. That method runs for every resource everywhere, so the
/// prefix bails early unless the value is positive, the <c>GameRes</c> is the player's and the
/// resource is a reputation (<see cref="ReputationTypes"/>). The menu's reputation setter uses
/// <c>Set</c>, so it isn't scaled.
/// </summary>
internal sealed class ReputationMultiplierModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<float> _multiplier;

    private static readonly AccessTools.FieldRef<PlayerData, GameRes> PlayerRes =
        AccessTools.FieldRefAccess<PlayerData, GameRes>("res");

    public override string Name => "ReputationMultiplier";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "ReputationMultiplier",
            "Enabled",
            false,
            "Multiply reputation gained with NPCs and districts. Losses are unchanged."
        );
        _multiplier = settings.Profile(
            "ReputationMultiplier",
            "Multiplier",
            2f,
            "Reputation gain multiplier.",
            new AcceptableValueRange<float>(1f, 20f)
        );
        settings.Toggle(MenuSection.Reputation, 10, "Reputation Multiplier", _enabled);
        settings.Slider(MenuSection.Reputation, 20, _multiplier, 1f, 20f, "0.0");
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(GameRes), nameof(GameRes.Add), new[] { typeof(string), typeof(float) }),
            prefix: new HarmonyMethod(typeof(ReputationMultiplierModule), nameof(ScaleGain))
        );
    }

    private static void ScaleGain(GameRes __instance, string stype, ref float value)
    {
        if (value <= 0f)
        {
            return;
        }
        PlayerData player = MainGame.PlayerData;
        if (player == null || !ReferenceEquals(__instance, PlayerRes(player)))
        {
            return;
        }
        if (ReputationTypes.Contains(stype))
        {
            value *= _multiplier.Value;
        }
    }
}
