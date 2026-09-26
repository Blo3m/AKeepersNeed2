using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>Yield multipliers: resource/harvest drops, tech points, and craft output.</summary>
internal sealed class DropsTab : IMenuTab
{
    private MenuPage _page;

    public string Title => "Drops";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Drops");
        _page.ToggleRow(
            "Resource Drops",
            () => ModConfig.ResourceDropsEnabled.Value,
            v => ModConfig.ResourceDropsEnabled.Value = v
        );
        _page.SliderRow(
            1f,
            10f,
            "0.0",
            () => ModConfig.ResourceDropMultiplier.Value,
            v => ModConfig.ResourceDropMultiplier.Value = v
        );
        _page.ToggleRow(
            "Tech Points",
            () => ModConfig.TechPointsEnabled.Value,
            v => ModConfig.TechPointsEnabled.Value = v
        );
        _page.SliderRow(
            1f,
            10f,
            "0.0",
            () => ModConfig.TechPointsMultiplier.Value,
            v => ModConfig.TechPointsMultiplier.Value = v
        );

        _page.SectionHeader("Crafting");
        _page.ToggleRow(
            "Craft Output",
            () => ModConfig.CraftDropsEnabled.Value,
            v => ModConfig.CraftDropsEnabled.Value = v
        );
        _page.SliderRow(
            1f,
            10f,
            "0.0",
            () => ModConfig.CraftDropMultiplier.Value,
            v => ModConfig.CraftDropMultiplier.Value = v
        );
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
