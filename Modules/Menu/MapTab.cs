using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>World-map display tweaks: zone reveal and milestone unlock.</summary>
internal sealed class MapTab : IMenuTab
{
    public string Title => "Map";

    public void Build(RectTransform content)
    {
        var page = new MenuPage(content);

        page.SectionHeader("Map");
        page.ToggleRow(
            "Reveal Map",
            () => ModConfig.RevealMapEnabled.Value,
            v => ModConfig.RevealMapEnabled.Value = v);
        page.ToggleRow(
            "Unlock Milestones",
            () => ModConfig.UnlockMilestonesEnabled.Value,
            v => ModConfig.UnlockMilestonesEnabled.Value = v);
    }
}
