using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>Player-focused tweaks: energy regeneration and drain.</summary>
internal sealed class PlayerTab : IMenuTab
{
    private MenuPage _page;

    public string Title => "Player";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Energy");
        _page.ToggleRow(
            "Energy Regen",
            () => ModConfig.EnergyRegenEnabled.Value,
            v => ModConfig.EnergyRegenEnabled.Value = v);
        _page.SliderRow(
            0.1f, 10f, "0.0",
            () => ModConfig.EnergyRegenRate.Value,
            v => ModConfig.EnergyRegenRate.Value = v);
        _page.ToggleRow(
            "No Energy Drain",
            () => ModConfig.NoEnergyDrainEnabled.Value,
            v => ModConfig.NoEnergyDrainEnabled.Value = v);
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
