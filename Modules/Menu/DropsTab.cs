using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>Yield multipliers: resource/harvest drops, tech points, and craft output.</summary>
internal sealed class DropsTab : IMenuTab
{
    public string Title => "Drops";

    public void Build(RectTransform content)
    {
        var page = new MenuPage(content);

        page.SectionHeader("Drops");
        page.ToggleRow(
            "Resource Drops",
            () => ModConfig.ResourceDropsEnabled.Value,
            v => ModConfig.ResourceDropsEnabled.Value = v);
        page.SliderRow(
            1f, 10f, "0.0",
            () => ModConfig.ResourceDropMultiplier.Value,
            v => ModConfig.ResourceDropMultiplier.Value = v);
        page.ToggleRow(
            "Tech Points",
            () => ModConfig.TechPointsEnabled.Value,
            v => ModConfig.TechPointsEnabled.Value = v);
        page.SliderRow(
            1f, 10f, "0.0",
            () => ModConfig.TechPointsMultiplier.Value,
            v => ModConfig.TechPointsMultiplier.Value = v);

        page.SectionHeader("Crafting");
        page.ToggleRow(
            "Craft Output",
            () => ModConfig.CraftDropsEnabled.Value,
            v => ModConfig.CraftDropsEnabled.Value = v);
        page.SliderRow(
            1f, 10f, "0.0",
            () => ModConfig.CraftDropMultiplier.Value,
            v => ModConfig.CraftDropMultiplier.Value = v);
    }
}
