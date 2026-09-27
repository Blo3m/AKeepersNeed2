using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// What drives tech-tree progression. This tab builds the live setters itself: red, green and
/// blue tech points and per-NPC reputation (<see cref="ValueRow"/>, <see cref="ReputationPicker"/>).
/// Those write straight into the current save's <see cref="PlayerData"/>, so they are not part
/// of any profile. Module rows (e.g. the reputation multiplier) follow them
/// (<see cref="RegistrySections"/>).
/// </summary>
internal sealed class ProgressionTab : IMenuTab
{
    private static readonly string[,] TechPoints =
    {
        { "tech_red", "Red Tech Points" },
        { "tech_green", "Green Tech Points" },
        { "tech_blue", "Blue Tech Points" },
    };

    private readonly AKNMenuWindow _window;
    private readonly ReputationPicker _reputation = new ReputationPicker();
    private MenuPage _page;

    public ProgressionTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Progression";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        var handBuilt = new Dictionary<MenuSection, Action>
        {
            [MenuSection.TechPoints] = BuildTechPoints,
            [MenuSection.Reputation] = BuildReputation,
        };
        RegistrySections.Build(_page, MenuTab.Progression, _window.Rebinder, handBuilt);
    }

    public void Refresh()
    {
        _reputation.EnsureLoaded();
        _page?.Sync();
    }

    private void BuildTechPoints()
    {
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
    }

    private void BuildReputation()
    {
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
}
