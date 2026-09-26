using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Player-focused tweaks: energy, combat and teleport-to-cursor toggles, plus live setters for
/// the red/green/blue tech points, happiness and per-NPC reputation (<see cref="ValueRow"/>,
/// <see cref="ReputationPicker"/>). The setters write straight into the current save's
/// <see cref="PlayerData"/>, so they are not part of any profile.
/// </summary>
internal sealed class PlayerTab : IMenuTab
{
    private static readonly string[,] TechPoints =
    {
        { "tech_red", "Red Tech Points" },
        { "tech_green", "Green Tech Points" },
        { "tech_blue", "Blue Tech Points" },
        { "happiness", "Happiness" },
    };

    private readonly AKNMenuWindow _window;
    private readonly ReputationPicker _reputation = new ReputationPicker();
    private MenuPage _page;

    public PlayerTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Player";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Energy");
        _page.ToggleRow(
            "Energy Regen",
            () => ModConfig.EnergyRegenEnabled.Value,
            v => ModConfig.EnergyRegenEnabled.Value = v
        );
        _page.SliderRow(
            0.1f,
            10f,
            "0.0",
            () => ModConfig.EnergyRegenRate.Value,
            v => ModConfig.EnergyRegenRate.Value = v
        );
        _page.ToggleRow(
            "Infinite Energy",
            () => ModConfig.InfiniteEnergyEnabled.Value,
            v => ModConfig.InfiniteEnergyEnabled.Value = v
        );

        _page.SectionHeader("Combat");
        _page.ToggleRow(
            "Infinite Health",
            () => ModConfig.InfiniteHealthEnabled.Value,
            v => ModConfig.InfiniteHealthEnabled.Value = v
        );
        _page.ToggleRow(
            "Infinite Stamina",
            () => ModConfig.InfiniteStaminaEnabled.Value,
            v => ModConfig.InfiniteStaminaEnabled.Value = v
        );

        _page.SectionHeader("Movement");
        _page.ToggleRow(
            "Teleport to Cursor",
            () => ModConfig.TeleportToCursorEnabled.Value,
            v => ModConfig.TeleportToCursorEnabled.Value = v
        );
        _page.AddSync(_window.Rebinder.BuildRow(
            _page.Band(30f, 6f),
            "Teleport Key",
            () => ModConfig.TeleportToCursorKey
        ));

        _page.SectionHeader("Tech Points");
        for (int i = 0; i < TechPoints.GetLength(0); i++)
        {
            string res = TechPoints[i, 0];
            ValueRow.Add(
                _page,
                TechPoints[i, 1],
                player => player.GetResInt(res),
                (player, value) => player.SetRes(res, value)
            );
        }

        _page.SectionHeader("Reputation");
        _reputation.Build(_page);
        ValueRow.Add(
            _page,
            "Reputation",
            player => _reputation.Current is ReputationPicker.Npc npc ? player.GetNPCRep(npc.Res) : (int?)null,
            (player, value) =>
            {
                if (_reputation.Current is ReputationPicker.Npc npc)
                {
                    player.SetNPCRep(npc.Res, value);
                }
            },
            () => _reputation.Current is ReputationPicker.Npc npc
                ? $"{npc.Name} reputation"
                : "Reputation"
        );
    }

    public void Refresh()
    {
        _reputation.EnsureLoaded();
        _page?.Sync();
    }
}
