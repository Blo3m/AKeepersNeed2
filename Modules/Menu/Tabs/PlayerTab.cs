using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Player-focused tweaks. The energy, combat, gathering, movement, tools and money-multiplier
/// rows come from the modules that own them (<see cref="RegistrySections"/>). This tab builds
/// the Money section's live setters itself, money and happiness (<see cref="ValueRow"/>). Those
/// write straight into the current save's <see cref="PlayerData"/>, so they are not part of any
/// profile.
/// </summary>
internal sealed class PlayerTab : IMenuTab
{
    private readonly AKNMenuWindow _window;
    private MenuPage _page;

    public PlayerTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Player";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        var handBuilt = new Dictionary<MenuSection, Action>
        {
            [MenuSection.Money] = BuildMoney,
        };
        RegistrySections.Build(_page, MenuTab.Player, _window.Rebinder, handBuilt);
    }

    public void Refresh()
    {
        _page?.Sync();
    }

    private void BuildMoney()
    {
        ValueRow.Add(
            _page,
            _window.Dialogs,
            "Money",
            player => player.GetResInt("money"),
            (player, value) => player.SetRes("money", value)
        );
        ValueRow.Add(
            _page,
            _window.Dialogs,
            "Happiness",
            player => player.GetResInt("happiness"),
            (player, value) => player.SetRes("happiness", value)
        );
    }
}
