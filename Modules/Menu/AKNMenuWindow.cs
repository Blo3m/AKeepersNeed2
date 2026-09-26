using System;
using AKeepersNeed2.Shared.Profiles;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The mod's menu: a from-scratch uGUI window styled from the game's own assets
/// (<see cref="NativeUiSkin"/>) and subclassing the game's <c>LazyWindow</c> so it joins the
/// native window stack/input. A title header (showing the active profile) and a footer
/// (<see cref="MenuTabBar"/>) frame a content area that swaps between
/// <see cref="IMenuTab"/> pages. It also owns the state tabs share: the
/// <see cref="UiScalePreview"/>, the <see cref="KeyRebinder"/>, and the one open
/// <see cref="MenuDialog"/>. Tabs re-read their values on open and whenever
/// <see cref="ProfileStore"/> changes. See docs/UI.md.
/// </summary>
internal sealed class AKNMenuWindow : LazyWindow<LazyWidgetDataBase>
{
    private const float PanelWidth = 340f;
    private const float SidePadding = 20f;
    private const float TabBarBottom = 26f;
    private const float TabBarHeight = 32f;
    private const float ContentBottom = TabBarBottom + TabBarHeight + 4f;
    private const string MenuTitle = "A Keeper's Need 2";

    private readonly KeyRebinder _rebinder = new KeyRebinder();

    private IMenuTab[] _tabs;
    private RectTransform _panelRect;
    private RectTransform[] _pages;
    private TextMeshProUGUI _title;
    private MenuTabBar _tabBar;
    private UiScalePreview _uiScale;
    private GameObject _dialog;

    public KeyRebinder Rebinder => _rebinder;

    public UiScalePreview UiScale => _uiScale;

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
        // Built here (not a field initializer) so tabs can capture this window.
        _tabs = new IMenuTab[]
        {
            new SettingsTab(this),
            new PlayerTab(),
            new DropsTab(),
            new CraftingTab(),
            new ItemsTab(),
            new MapTab(),
        };

        MenuUi.Stretch((RectTransform)transform);

        float shadeAlpha = NativeUiSkin.IsReady ? 0.4f : 0.72f;
        Image shade = MenuUi.CreateImage("Shade", transform, new Color(0f, 0f, 0f, shadeAlpha));
        MenuUi.Stretch(shade.rectTransform);

        Color panelColor = NativeUiSkin.IsReady ? Color.clear : new Color(0.055f, 0.035f, 0.03f, 0.99f);
        Image panel = MenuUi.CreateImage("Panel", transform, panelColor);
        _panelRect = panel.rectTransform;
        _panelRect.anchorMin = new Vector2(1f, 0f);
        _panelRect.anchorMax = new Vector2(1f, 1f);
        _panelRect.pivot = new Vector2(1f, 0.5f);
        _panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
        _panelRect.anchoredPosition = Vector2.zero;
        _uiScale = new UiScalePreview(transform, _panelRect);

        if (NativeUiSkin.IsReady)
        {
            Image inner = MenuUi.CreateImage("InnerBackground", _panelRect, new Color(0.105f, 0.112f, 0.14f, 1f));
            MenuUi.SetRect(inner.rectTransform, Anchors.Fill, new Vector2(13f, 13f), new Vector2(-13f, -38f));
            inner.raycastTarget = false;

            Image frame = MenuUi.CreateImage("Frame", _panelRect, Color.white);
            MenuUi.Stretch(frame.rectTransform);
            MenuUi.ApplyFrame(frame);
            frame.raycastTarget = false;
            frame.transform.SetAsLastSibling();
        }

        BuildHeader();
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

    private void BuildHeader()
    {
        Color headerColor = NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 1f);
        Image header = MenuUi.CreateImage("Header", _panelRect, headerColor);
        MenuUi.SetRect(header.rectTransform, Anchors.Top, new Vector2(12f, -36f), new Vector2(-12f, -12f));
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            header.sprite = NativeUiSkin.HeaderSprite;
            header.type = Image.Type.Sliced;
            header.color = Color.white;
        }

        float titleSize = NativeUiSkin.IsReady ? 13f : 18f;
        _title = MenuUi.CreateText("Title", header.rectTransform, titleSize, TextAlignmentOptions.Center);
        MenuUi.SetRect(_title.rectTransform, Anchors.Fill, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        MenuUi.ApplyHeaderText(_title);
        _title.textWrappingMode = TextWrappingModes.NoWrap;
        _title.overflowMode = TextOverflowModes.Ellipsis;
        _title.text = MenuTitle;
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

    // --- Dialogs (one at a time; closed with the window) ---

    public void ShowConfirm(string title, string message, string confirmLabel, Action onConfirm)
    {
        CloseDialog();
        _dialog = MenuDialog.ShowConfirm(transform, title, message, confirmLabel, onConfirm);
    }

    public void ShowNamePrompt(string title, string initial, Func<string, string> submit)
    {
        CloseDialog();
        _dialog = MenuDialog.ShowNamePrompt(transform, title, initial, ProfileStore.MaxNameLength, submit);
    }

    private void CloseDialog()
    {
        if (_dialog != null)
        {
            Destroy(_dialog);
            _dialog = null;
        }
    }

    /// <summary>
    /// Per-frame input hook, called from <c>MenuModule.Tick</c>. Returns true while a rebind is
    /// listening so the caller doesn't also treat the keypress as a menu toggle.
    /// </summary>
    public bool TickInput()
    {
        return _rebinder.Tick();
    }

    private void ResetTransientState()
    {
        _uiScale.Cancel();
        _rebinder.Cancel();
        CloseDialog();
    }

    // LazyWindow only closes on Back (Esc / gamepad B) when it has a closeButton; the menu has
    // none (the hotkey closes it), so Back would otherwise be swallowed and strand gamepad users.
    protected override bool OnPressedBack()
    {
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
