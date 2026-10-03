using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// The Garden tab: the garden modules' rows, plus a hand-built Beds section with setters for the
/// player's bed stats. Farming base is the growing mastery every garden or vineyard bed gets; bed
/// level is only read for the bed icon. They're player stats in the save, so each Set confirms first.
/// </summary>
internal sealed class GardenTab : IMenuTab
{
    private readonly AKNMenuWindow _window;
    private MenuPage _page;

    public GardenTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Garden";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        var handBuilt = new Dictionary<MenuSection, Action>
        {
            [MenuSection.Beds] = BuildBeds,
        };
        RegistrySections.Build(_page, MenuTab.Garden, _window, handBuilt);
    }

    public void Refresh()
    {
        _page?.Sync();
    }

    private void BuildBeds()
    {
        Stat(
            "Garden Farming Mastery",
            "g_garden_farming_base",
            "The growing mastery every garden bed gets (fertilizers add to it)."
        );
        Stat(
            "Vineyard Farming Mastery",
            "g_vineyard_farming_base",
            "The growing mastery every vineyard bed gets (fertilizers add to it)."
        );
        Stat("Garden Bed Level", "g_garden_lvl", "Only changes the bed icon in the bed window.");
        Stat("Vineyard Bed Level", "g_vineyard_lvl", "Only changes the bed icon in the bed window.");
    }

    private void Stat(string label, string stat, string warning)
    {
        _page.AddSync(ValueRow.Build(
            _page.Band(30f, 8f),
            _window.Dialogs,
            label,
            player => player.GetResInt(stat),
            (player, value) => player.SetRes(stat, value),
            new ValueRow.Options { Warning = warning }
        ));
    }
}
