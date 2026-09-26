using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>Cost and time bypasses for crafting and building.</summary>
internal sealed class CraftingTab : IMenuTab
{
    private MenuPage _page;

    public string Title => "Crafting";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Crafting");
        _page.ToggleRow(
            "Free Crafting",
            () => ModConfig.FreeCraftingEnabled.Value,
            v => ModConfig.FreeCraftingEnabled.Value = v
        );
        _page.ToggleRow(
            "Instant Crafting",
            () => ModConfig.InstantCraftEnabled.Value,
            v => ModConfig.InstantCraftEnabled.Value = v
        );

        _page.SectionHeader("Building");
        _page.ToggleRow(
            "Free Building",
            () => ModConfig.FreeBuildingEnabled.Value,
            v => ModConfig.FreeBuildingEnabled.Value = v
        );
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
