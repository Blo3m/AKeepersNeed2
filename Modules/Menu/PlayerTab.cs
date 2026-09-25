using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>Player-focused tweaks: energy regeneration and drain.</summary>
internal sealed class PlayerTab : IMenuTab
{
    public string Title => "Player";

    public void Build(RectTransform content)
    {
        var page = new MenuPage(content);

        page.SectionHeader("Energy");
        page.ToggleRow(
            "Energy Regen",
            () => ModConfig.EnergyRegenEnabled.Value,
            v => ModConfig.EnergyRegenEnabled.Value = v);
        page.SliderRow(
            0.1f, 10f, "0.0",
            () => ModConfig.EnergyRegenRate.Value,
            v => ModConfig.EnergyRegenRate.Value = v);
        page.ToggleRow(
            "No Energy Drain",
            () => ModConfig.NoEnergyDrainEnabled.Value,
            v => ModConfig.NoEnergyDrainEnabled.Value = v);
    }
}
