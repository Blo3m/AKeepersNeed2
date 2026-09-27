using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Modules.Menu.Keybinds;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// A tab built entirely from the rows modules declared for it (<see cref="RegistrySections"/>).
/// Used for the Drops, Crafting, Zombies, Garden and Map tabs.
/// </summary>
internal sealed class ConfigTab : IMenuTab
{
    private readonly MenuTab _tab;
    private readonly KeyRebinder _rebinder;
    private MenuPage _page;

    public ConfigTab(MenuTab tab, KeyRebinder rebinder)
    {
        _tab = tab;
        _rebinder = rebinder;
    }

    public string Title => _tab.ToString();

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        RegistrySections.Build(_page, _tab, _rebinder);
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
