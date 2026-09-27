using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Modules.Menu.Tabs.Progression;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Everything about progression, in one scrolling page of foldable sections (several can be open):
/// <list type="bullet">
/// <item>Tech Points &amp; Reputation (open at first): the red/green/blue tech point and per-NPC
/// reputation setters (<see cref="ValueRow"/>, <see cref="ReputationPicker"/>) plus module rows
/// (the reputation multiplier).</item>
/// <item>Tech: the <see cref="TechList"/>.</item>
/// <item>Recipes: <see cref="RecipeRows"/>.</item>
/// <item>Quests: the <see cref="QuestList"/>.</item>
/// </list>
/// Every setter and unlock writes the loaded save, so each asks for confirmation first.
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
    private TechList _techs;
    private QuestList _quests;

    public ProgressionTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Progression";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        Fold("Tech Points & Reputation", true, () =>
        {
            var handBuilt = new Dictionary<MenuSection, Action>
            {
                [MenuSection.TechPoints] = BuildTechPoints,
                [MenuSection.Reputation] = BuildReputation,
            };
            var sections = new[] { MenuSection.TechPoints, MenuSection.Reputation };
            RegistrySections.Build(_page, MenuTab.Progression, _window, handBuilt, sections);
        });
        Fold("Tech", false, () => _techs = new TechList(_page, _window.Dialogs));
        Fold("Recipes", false, () => RecipeRows.Build(_page, _window.Dialogs));
        Fold("Quests", false, () => _quests = new QuestList(_page, _window.Dialogs));
    }

    public void Refresh()
    {
        _reputation.EnsureLoaded();
        _page?.Sync();
        _techs?.Reload();
        _quests?.Reload();
    }

    /// <summary>A fold header and, under it, the bands <paramref name="build"/> adds, shown while open.</summary>
    private void Fold(string title, bool open, Action build)
    {
        bool isOpen = open;
        BandGroup group = null;
        _page.FoldHeader(title, () => isOpen, () =>
        {
            isOpen = !isOpen;
            _page.SetGroupVisible(group, isOpen);
        });
        group = _page.BeginGroup();
        build();
        _page.EndGroup();
        _page.SetGroupVisible(group, isOpen);
    }

    private void BuildTechPoints()
    {
        for (int i = 0; i < TechPoints.GetLength(0); i++)
        {
            string res = TechPoints[i, 0];
            ValueRow.Add(
                _page,
                _window.Dialogs,
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
            _window.Dialogs,
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
