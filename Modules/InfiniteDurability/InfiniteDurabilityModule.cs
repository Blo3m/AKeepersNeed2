using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.InfiniteDurability;

/// <summary>
/// Items never lose durability. There are two wear points: the player's tool on every swing
/// (<c>ToolComponent.UseTool</c>, only the player has a <c>ToolComponent</c>) and a craft's
/// durability-use item when the craft finishes (<c>CraftElement.Finish</c>, player or zombie).
/// Both subtract through <c>DurabilitySerializedItemProperty.Durability</c>'s setter, which Mono
/// may inline, so each method is wrapped instead: the prefix remembers the durability and the
/// postfix puts it back.
/// </summary>
internal sealed class InfiniteDurabilityModule : HarmonyModule
{
    private static ConfigEntry<bool> _enabled;

    private static readonly AccessTools.FieldRef<ToolComponent, Item> ToolInUse =
        AccessTools.FieldRefAccess<ToolComponent, Item>("toolInUse");

    private static readonly AccessTools.FieldRef<CraftElement, Item> DurabilityUse =
        AccessTools.FieldRefAccess<CraftElement, Item>("durabilityUse");

    public override string Name => "InfiniteDurability";

    protected override ConfigEntry<bool> EnabledFlag => _enabled;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile(
            "InfiniteDurability",
            "Enabled",
            false,
            "Tools and items used up by durability in crafts (yours and zombies') never wear down."
        );
        settings.Toggle(MenuSection.Tools, 10, "Infinite Durability", _enabled);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(ToolComponent), "UseTool"),
            prefix: new HarmonyMethod(typeof(InfiniteDurabilityModule), nameof(RememberTool)),
            postfix: new HarmonyMethod(typeof(InfiniteDurabilityModule), nameof(Restore))
        );
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(CraftElement), nameof(CraftElement.Finish)),
            prefix: new HarmonyMethod(typeof(InfiniteDurabilityModule), nameof(RememberCraftItem)),
            postfix: new HarmonyMethod(typeof(InfiniteDurabilityModule), nameof(Restore))
        );
    }

    private static void RememberTool(ToolComponent __instance, out DurabilitySnapshot __state)
    {
        __state = DurabilitySnapshot.Of(ToolInUse(__instance));
    }

    private static void RememberCraftItem(CraftElement __instance, out DurabilitySnapshot __state)
    {
        __state = DurabilitySnapshot.Of(DurabilityUse(__instance));
    }

    private static void Restore(DurabilitySnapshot __state)
    {
        __state?.Restore();
    }
}
