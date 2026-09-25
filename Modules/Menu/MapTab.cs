using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>World-map display tweaks: zone reveal and milestone unlock.</summary>
internal sealed class MapTab : IMenuTab
{
    private MenuPage _page;

    public string Title => "Map";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Map");
        _page.ToggleRow(
            "Reveal Map",
            () => ModConfig.RevealMapEnabled.Value,
            v => ModConfig.RevealMapEnabled.Value = v);
        _page.ToggleRow(
            "Unlock Milestones",
            () => ModConfig.UnlockMilestonesEnabled.Value,
            v => ModConfig.UnlockMilestonesEnabled.Value = v);
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
