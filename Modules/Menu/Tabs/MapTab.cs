using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Modules.Menu.Tabs.Map;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// The Map tab: the map modules' rows, plus a hand-built Bookmarks section (the active profile's
/// teleport bookmarks, <see cref="BookmarkList"/>), reloaded on every <see cref="Refresh"/> since
/// switching profile switches the list.
/// </summary>
internal sealed class MapTab : IMenuTab
{
    private readonly AKNMenuWindow _window;
    private MenuPage _page;
    private BookmarkList _bookmarks;

    public MapTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Map";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        var handBuilt = new Dictionary<MenuSection, Action>
        {
            [MenuSection.Bookmarks] = () => _bookmarks = new BookmarkList(_page, _window),
        };
        RegistrySections.Build(_page, MenuTab.Map, _window, handBuilt);
    }

    public void Refresh()
    {
        _page?.Sync();
        _bookmarks?.Reload();
    }
}
