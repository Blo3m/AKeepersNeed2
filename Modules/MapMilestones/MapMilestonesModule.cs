using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using LazyBearTechnology;

namespace AKeepersNeed2.Modules.MapMilestones;

/// <summary>
/// Draws every visible map milestone as activated, so not-yet-activated ones become
/// teleport targets. <c>MapPageWidget.UpdateMilestones</c> picks
/// <c>DrawAsActivated</c>/<c>DrawAsNotActivated</c> from the milestone's
/// <c>activated_milestone</c> game-res; a prefix on <c>DrawAsNotActivated</c> redirects to
/// <c>DrawAsActivated</c> with the same interactability rule the game uses. Nothing is
/// written to the save.
/// </summary>
internal sealed class MapMilestonesModule : HarmonyModule
{
    private static readonly AccessTools.FieldRef<LazyWidget<MapPageWidgetData>, MapPageWidgetData> DataRef =
        AccessTools.FieldRefAccess<LazyWidget<MapPageWidgetData>, MapPageWidgetData>("data");

    public override string Name => "MapMilestones";

    protected override ConfigEntry<bool> EnabledFlag => ModConfig.UnlockMilestonesEnabled;

    protected override void Apply(Harmony harmony)
    {
        // Patching WgoData.GetGameRes instead would be simpler but it's a one-line method
        // Mono may inline into UpdateMilestones, bypassing the patch.
        harmony.Patch(
            AccessTools.Method(typeof(UIMapMilestone), nameof(UIMapMilestone.DrawAsNotActivated)),
            prefix: new HarmonyMethod(typeof(MapMilestonesModule), nameof(DrawActivatedInstead))
        );
    }

    private static bool DrawActivatedInstead(
        UIMapMilestone __instance,
        UIMapMilestoneData milestoneData,
        WgoData wgoData
    )
    {
        MapPageWidget widget = __instance.GetComponentInParent<MapPageWidget>(true);
        if (widget == null)
        {
            return true;
        }

        MapPageWidgetData data = DataRef(widget);
        bool interactable = data != null
            && data.MilestonesInteractable
            && data.CurrentMilestone != milestoneData.wgoId;
        __instance.DrawAsActivated(milestoneData, wgoData, interactable, widget.OnPressMapMilestone);
        return false;
    }
}
