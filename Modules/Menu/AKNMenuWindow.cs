using System;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Modules.Menu.Keybinds;
using AKeepersNeed2.Modules.Menu.Tabs;
using AKeepersNeed2.Shared.Profiles;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The mod's menu: a from-scratch uGUI window styled from the game's own assets
/// (<see cref="NativeUiSkin"/>, built by <see cref="MenuChrome"/>) and subclassing the game's
/// <c>LazyWindow</c> so it joins the native window stack/input. A title header (showing the
/// active profile) and a footer (<see cref="MenuTabBar"/>) frame a content area that swaps
/// between <see cref="IMenuTab"/> pages. It also owns the state tabs share: the
/// <see cref="UiScalePreview"/>, the <see cref="KeyRebinder"/>, and the
/// <see cref="MenuDialogHost"/>. Tabs re-read their values on open and whenever
/// <see cref="ProfileStore"/> changes. See docs/UI.md.
/// </summary>
internal sealed class AKNMenuWindow : LazyWindow<LazyWidgetDataBase>
{
    private const float SidePadding = 20f;
    private const float TabBarBottom = 26f;
    private const float TabBarHeight = 32f;
    private const float ContentBottom = TabBarBottom + TabBarHeight + 4f;
    private const string MenuTitle = "A Keeper's Need 2";

    private KeyRebinder _rebinder;
    private MenuDialogHost _dialogs;
    private bool _textInputFocusedLastFrame;

    private IMenuTab[] _tabs;
    private RectTransform _panelRect;
    private RectTransform[] _pages;
    private TextMeshProUGUI _title;
    private MenuTabBar _tabBar;
    private UiScalePreview _uiScale;

    public KeyRebinder Rebinder => _rebinder;

    public UiScalePreview UiScale => _uiScale;

    /// <summary>The menu's one-at-a-time dialogs (confirm, message, name prompt).</summary>
    public MenuDialogHost Dialogs => _dialogs;

    public static AKNMenuWindow CreateInstance()
    {
        NativeUiSkin.TryCapture();

        Transform uiRoot = MenuUi.FindUiRoot();
        if (uiRoot == null)
        {
            Plugin.Logger.LogWarning("[Menu] no UI root found — open the menu once you're past the loading screen.");
            return null;
        }

        GameObject root = MenuUi.CreateRect("AKN_ModMenu", uiRoot).gameObject;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 450;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<GamepadNavigationController>();

        var window = root.AddComponent<AKNMenuWindow>();
        window.BuildUi();
        window.Init();
        return window;
    }

    private void BuildUi()
    {
        _rebinder = new KeyRebinder(this);
        _dialogs = new MenuDialogHost(transform);

        // Built here (not a field initializer) so tabs can capture this window.
        _tabs = new IMenuTab[]
        {
            new SettingsTab(this),
            new PlayerTab(this),
            new ConfigTab(MenuTab.Drops, _rebinder),
            new ConfigTab(MenuTab.Crafting, _rebinder),
            new ItemsTab(),
            new ConfigTab(MenuTab.Map, _rebinder),
        };

        MenuUi.Stretch((RectTransform)transform);
        _panelRect = MenuChrome.BuildPanel(transform);
        _uiScale = new UiScalePreview(transform, _panelRect);
        _title = MenuChrome.BuildHeader(_panelRect, MenuTitle);

        BuildContent();
        BuildFooter();
        // Settings sits first but is rarely what you open the menu for.
        SelectTab(1);

        ProfileStore.Changed += RefreshAll;
    }

    private void RefreshAll()
    {
        foreach (IMenuTab tab in _tabs)
        {
            tab.Refresh();
        }
        Profile active = ProfileStore.Active;
        _title.text = active != null
            ? $"{MenuTitle} — {active.Name}"
            : MenuTitle;
    }

    private void BuildContent()
    {
        RectTransform content = MenuUi.CreateRect("Content", _panelRect);
        MenuUi.SetRect(content, Anchors.Fill, new Vector2(SidePadding, ContentBottom), new Vector2(-SidePadding, -44f));

        _pages = new RectTransform[_tabs.Length];
        for (int i = 0; i < _tabs.Length; i++)
        {
            _pages[i] = MenuUi.CreateRect($"Page_{_tabs[i].Title}", content);
            MenuUi.Stretch(_pages[i]);
            _tabs[i].Build(_pages[i]);
        }
    }

    private void BuildFooter()
    {
        RectTransform bar = MenuUi.CreateRect("TabBar", _panelRect);
        MenuUi.SetRect(
            bar,
            Anchors.Bottom,
            new Vector2(SidePadding, TabBarBottom),
            new Vector2(-SidePadding, TabBarBottom + TabBarHeight)
        );
        RectTransform hints = MenuUi.CreateRect("TabScrollHints", _panelRect);
        MenuUi.SetRect(
            hints,
            Anchors.Bottom,
            new Vector2(SidePadding, TabBarBottom - 16f),
            new Vector2(-SidePadding, TabBarBottom - 2f)
        );
        _tabBar = new MenuTabBar(bar, hints, Array.ConvertAll(_tabs, tab => tab.Title), SelectTab);
    }

    private void SelectTab(int index)
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].gameObject.SetActive(i == index);
        }
        _tabBar.Highlight(index);
    }

    private void OnDestroy()
    {
        _tabBar?.Dispose();
        ProfileStore.Changed -= RefreshAll;
    }

    /// <summary>
    /// Per-frame input hook, called from <c>MenuModule.Tick</c>. Returns true while a rebind is
    /// listening so the caller doesn't also treat the keypress as a menu toggle.
    /// </summary>
    public bool TickInput()
    {
        return _rebinder != null && _rebinder.Tick();
    }

    private void ResetTransientState()
    {
        _uiScale.Cancel();
        _rebinder?.Cancel();
        _dialogs?.Close();
    }

    // Read by OnPressedBack: a text field handles Esc itself (reverts and unfocuses), possibly
    // earlier in the same frame, so "focused at the end of last frame" still counts.
    private void LateUpdate()
    {
        _textInputFocusedLastFrame = InputGate.IsTextInputFocused();
    }

    // LazyWindow only closes on Back (Esc / gamepad B) when it has a closeButton; the menu has
    // none (the hotkey closes it), so Back would otherwise be swallowed and strand gamepad users.
    // Back first backs out of whatever is in progress: a rebind capture, then a dialog, then a
    // focused text field; only then does it close the menu.
    protected override bool OnPressedBack()
    {
        if (_rebinder != null && _rebinder.HandleBack())
        {
            return true;
        }
        if (_dialogs != null && _dialogs.IsOpen)
        {
            _dialogs.Close();
            return true;
        }
        if (_textInputFocusedLastFrame || InputGate.IsTextInputFocused())
        {
            return true;
        }
        Close();
        return true;
    }

    // Every close path (Back, hotkey) funnels through here.
    protected override void HideWindow()
    {
        ResetTransientState();
        base.HideWindow();
    }

    public override void Open(LazyWidgetDataBase data)
    {
        // LazyWindow wires its gamepad controller before activating; a runtime-built
        // window must be active first so navigation items are discoverable.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        ResetTransientState();
        RefreshAll();
        _uiScale.ApplySaved();
        base.Open(data);
    }

    // LazyWindow.PrintTips assumes a tips widget exists; ours has none, so guard.
    protected override void PrintTips()
    {
        if (lazyButtonTips != null)
        {
            base.PrintTips();
        }
    }

    protected override void PrintTips(GamepadNavigationItem gamepadNavigationItem)
    {
        if (lazyButtonTips != null)
        {
            base.PrintTips(gamepadNavigationItem);
        }
    }

    protected override void TestDraw() { }
}
