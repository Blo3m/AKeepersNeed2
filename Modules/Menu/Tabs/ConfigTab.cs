using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// A tab built entirely from the rows modules declared for it (<see cref="RegistrySections"/>).
/// Used for the World, Drops, Crafting, Garden, Fishing and Map tabs.
/// </summary>
internal sealed class ConfigTab : IMenuTab
{
    private readonly MenuTab _tab;
    private readonly AKNMenuWindow _window;
    private MenuPage _page;

    public ConfigTab(MenuTab tab, AKNMenuWindow window)
    {
        _tab = tab;
        _window = window;
    }

    public string Title => _tab.ToString();

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        RegistrySections.Build(_page, _tab, _window);
    }

    public void Refresh()
    {
        _page?.Sync();
    }
}
