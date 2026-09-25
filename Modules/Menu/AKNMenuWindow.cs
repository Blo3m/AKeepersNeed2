using System.Reflection;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The mod's menu: a from-scratch uGUI window styled from the game's own assets
/// (<see cref="NativeUiSkin"/>) and subclassing the game's <c>LazyWindow</c> so it joins the
/// native window stack/input. A title header and a footer (bottom tab bar + Close) frame a
/// content area that swaps between <see cref="IMenuTab"/> pages. The Settings tab's UI-scale
/// and hotkey-rebind controls are built here because they need window state (a scaling ghost
/// + confirm dialog, and per-frame key capture). See docs/UI.md.
/// </summary>
internal sealed class AKNMenuWindow : LazyWindow<LazyWidgetDataBase>
{
    private const float PanelWidth = 340f;
    private const float SidePadding = 20f;
    private const float TabWidth = 74f;

    private static readonly KeyCode[] AllKeys = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    private IMenuTab[] _tabs;
    private RectTransform _panelRect;
    private RectTransform _content;
    private RectTransform[] _pages;
    private TextMeshProUGUI[] _tabLabels;
    private GamepadNavigationItem[] _tabNavItems;
    private ScrollRect _tabScroll;

    // UI-scale preview state.
    private Slider _scaleSlider;
    private TextMeshProUGUI _scaleValueLabel;
    private bool _scalePreviewActive;
    private GameObject _scaleGhost;
    private GameObject _scaleDialog;
    private TextMeshProUGUI _scaleDialogValue;

    // Hotkey rebind state.
    private TextMeshProUGUI _rebindButtonLabel;
    private bool _rebindListening;

    public static AKNMenuWindow CreateInstance()
    {
        NativeUiSkin.TryCapture();

        Transform uiRoot = FindUiRoot();
        if (uiRoot == null)
        {
            Plugin.Logger.LogWarning("[Menu] no UI root found — open the menu once you're past the loading screen.");
            return null;
        }

        var root = new GameObject("AKN_ModMenu", typeof(RectTransform));
        root.transform.SetParent(uiRoot, false);

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

    private static Transform FindUiRoot()
    {
        GUIElements gui = GUIElements.Instance;
        if (gui == null)
        {
            return null;
        }

        FieldInfo fitterField = typeof(GUIElements).GetField(
            "uiFitter",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (fitterField?.GetValue(gui) is Component fitter)
        {
            return fitter.transform;
        }
        return gui.Root;
    }

    private void BuildUi()
    {
        // Built here (not a field initializer) so tabs can capture this window.
        _tabs = new IMenuTab[]
        {
            new SettingsTab(this),
            new PlayerTab(),
            new DropsTab(),
            new ItemsTab(),
            new MapTab(),
        };

        MenuUi.Stretch((RectTransform)transform);

        Image shade = MenuUi.CreateImage("Shade", transform,
            new Color(0f, 0f, 0f, NativeUiSkin.IsReady ? 0.4f : 0.72f));
        MenuUi.Stretch(shade.rectTransform);

        Image panel = MenuUi.CreateImage("Panel", transform,
            NativeUiSkin.IsReady ? Color.clear : new Color(0.055f, 0.035f, 0.03f, 0.99f));
        _panelRect = panel.rectTransform;
        _panelRect.anchorMin = new Vector2(1f, 0f);
        _panelRect.anchorMax = new Vector2(1f, 1f);
        _panelRect.pivot = new Vector2(1f, 0.5f);
        _panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
        _panelRect.anchoredPosition = Vector2.zero;

        if (NativeUiSkin.IsReady)
        {
            Image inner = MenuUi.CreateImage("InnerBackground", _panelRect,
                new Color(0.105f, 0.112f, 0.14f, 1f));
            MenuUi.SetRect(inner.rectTransform,
                new Vector2(13f, 13f), new Vector2(-13f, -38f), Vector2.zero, Vector2.one);
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
    }

    private void BuildHeader()
    {
        Image header = MenuUi.CreateImage("Header", _panelRect,
            NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 1f));
        MenuUi.SetRect(header.rectTransform,
            new Vector2(12f, -36f), new Vector2(-12f, -12f),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            header.sprite = NativeUiSkin.HeaderSprite;
            header.type = Image.Type.Sliced;
            header.color = Color.white;
        }

        TextMeshProUGUI title = MenuUi.CreateText("Title", header.rectTransform,
            NativeUiSkin.IsReady ? 13f : 18f, TextAlignmentOptions.Center, Color.white);
        MenuUi.Stretch(title.rectTransform);
        MenuUi.ApplyHeaderText(title);
        title.text = "A Keeper's Need 2";
    }

    private void BuildContent()
    {
        var go = new GameObject("Content", typeof(RectTransform));
        _content = (RectTransform)go.transform;
        _content.SetParent(_panelRect, false);
        MenuUi.SetRect(_content,
            new Vector2(SidePadding, 88f), new Vector2(-SidePadding, -44f),
            Vector2.zero, Vector2.one);

        _pages = new RectTransform[_tabs.Length];
        for (int i = 0; i < _tabs.Length; i++)
        {
            var pageGo = new GameObject($"Page_{_tabs[i].Title}", typeof(RectTransform));
            var page = (RectTransform)pageGo.transform;
            page.SetParent(_content, false);
            MenuUi.Stretch(page);
            _tabs[i].Build(page);
            _pages[i] = page;
        }
    }

    private void BuildFooter()
    {
        BuildTabBar();

        closeButton = MenuUi.CreateButton("Close", _panelRect, "Close",
            new Vector2(SidePadding, 14f), new Vector2(-SidePadding, 46f),
            new Vector2(0f, 0f), new Vector2(1f, 0f));
        MenuUi.ApplyDialogButton(closeButton);
    }

    private void BuildTabBar()
    {
        var bar = (RectTransform)new GameObject("TabBar", typeof(RectTransform)).transform;
        bar.SetParent(_panelRect, false);
        MenuUi.SetRect(bar,
            new Vector2(SidePadding, 52f), new Vector2(-SidePadding, 84f),
            new Vector2(0f, 0f), new Vector2(1f, 0f));

        // Tabs keep a fixed width and the bar scrolls sideways (drag / mouse wheel) once
        // they outgrow it. The clear image catches drags that start between tabs.
        Image hitArea = bar.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        bar.gameObject.AddComponent<RectMask2D>();

        var strip = (RectTransform)new GameObject("Tabs", typeof(RectTransform)).transform;
        strip.SetParent(bar, false);
        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(0f, 1f);
        strip.pivot = new Vector2(0f, 0.5f);
        strip.sizeDelta = new Vector2(_tabs.Length * TabWidth, 0f);
        strip.anchoredPosition = Vector2.zero;

        _tabScroll = bar.gameObject.AddComponent<ScrollRect>();
        _tabScroll.viewport = bar;
        _tabScroll.content = strip;
        _tabScroll.horizontal = true;
        _tabScroll.vertical = false;
        _tabScroll.movementType = ScrollRect.MovementType.Clamped;
        _tabScroll.scrollSensitivity = 20f;

        _tabLabels = new TextMeshProUGUI[_tabs.Length];
        _tabNavItems = new GamepadNavigationItem[_tabs.Length];
        for (int i = 0; i < _tabs.Length; i++)
        {
            int index = i;
            LazyButton button = MenuUi.CreateButton($"Tab_{_tabs[i].Title}", strip, _tabs[i].Title,
                new Vector2(i * TabWidth + 2f, 0f), new Vector2((i + 1) * TabWidth - 2f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f));
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.fontSize = 12f;
            }
            _tabLabels[i] = label;
            _tabNavItems[i] = button.GetComponent<GamepadNavigationItem>();
            button.onClick.AddListener(() => SelectTab(index));
        }

        GamepadNavigationItem.OnFocusStatic += OnNavItemFocused;
    }

    private void OnNavItemFocused(GamepadNavigationItem item)
    {
        int index = System.Array.IndexOf(_tabNavItems, item);
        if (index >= 0)
        {
            ScrollTabIntoView(index);
        }
    }

    private void ScrollTabIntoView(int index)
    {
        RectTransform strip = _tabScroll.content;
        float viewWidth = _tabScroll.viewport.rect.width;
        if (viewWidth <= 0f)
        {
            return;
        }

        float visibleLeft = -strip.anchoredPosition.x;
        float tabLeft = index * TabWidth;
        float tabRight = tabLeft + TabWidth;
        if (tabLeft < visibleLeft)
        {
            visibleLeft = tabLeft;
        }
        else if (tabRight > visibleLeft + viewWidth)
        {
            visibleLeft = tabRight - viewWidth;
        }

        _tabScroll.StopMovement();
        strip.anchoredPosition = new Vector2(-visibleLeft, strip.anchoredPosition.y);
    }

    private void OnDestroy()
    {
        GamepadNavigationItem.OnFocusStatic -= OnNavItemFocused;
    }

    private void SelectTab(int index)
    {
        ScrollTabIntoView(index);
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].gameObject.SetActive(i == index);
            if (_tabLabels[i] != null)
            {
                _tabLabels[i].color = i == index ? NativeUiSkin.ValueColor : NativeUiSkin.LabelColor;
            }
        }
    }

    // --- UI scale (built into the Settings tab; previewed with a ghost + confirm dialog) ---

    public void BuildUiScaleControl(RectTransform band)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Label", band, 11f, TextAlignmentOptions.Left, Color.white);
        MenuUi.ApplyLabelText(label);
        MenuUi.SetRect(label.rectTransform,
            new Vector2(0f, 0f), new Vector2(54f, 0f), Vector2.zero, Vector2.one);
        label.text = "UI Scale";

        var sliderArea = (RectTransform)new GameObject("Slider", typeof(RectTransform)).transform;
        sliderArea.SetParent(band, false);
        MenuUi.SetRect(sliderArea,
            new Vector2(58f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.one);

        _scaleSlider = MenuPage.FillSlider(sliderArea, 0.5f, 3f, "0.0",
            () => ModConfig.UiScale.Value, OnScaleChanged);
        Transform valueTransform = sliderArea.Find("Value");
        _scaleValueLabel = valueTransform != null ? valueTransform.GetComponent<TextMeshProUGUI>() : null;

        var notifier = _scaleSlider.gameObject.AddComponent<PointerUpNotifier>();
        notifier.Released += OnScaleReleased;
    }

    private void OnScaleChanged(float value)
    {
        // Dragging only updates the ghost; the confirm dialog waits for release.
        if (!_scalePreviewActive)
        {
            EnterScalePreview();
        }
        UpdateScalePreview(value);
    }

    private void OnScaleReleased()
    {
        if (_scalePreviewActive && _scaleDialog == null)
        {
            BuildScaleDialog();
            UpdateScalePreview(_scaleSlider.value);
        }
    }

    private void EnterScalePreview()
    {
        _scalePreviewActive = true;

        _scaleGhost = Instantiate(_panelRect.gameObject, _panelRect.parent);
        _scaleGhost.name = "ScaleGhost";
        CanvasGroup group = _scaleGhost.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = _scaleGhost.AddComponent<CanvasGroup>();
        }
        group.alpha = 0.45f;
        group.interactable = false;
        group.blocksRaycasts = false;
        _scaleGhost.transform.SetAsLastSibling();
    }

    private void UpdateScalePreview(float value)
    {
        if (_scaleGhost != null)
        {
            _scaleGhost.transform.localScale = new Vector3(value, value, 1f);
        }
        if (_scaleDialogValue != null)
        {
            _scaleDialogValue.text = $"UI Scale: {value:0.0}";
        }
    }

    private void BuildScaleDialog()
    {
        var go = new GameObject("ScaleDialog", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 116f);
        rect.anchoredPosition = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.11f, 0.99f);
        if (NativeUiSkin.IsReady && NativeUiSkin.FrameSprite != null)
        {
            bg.sprite = NativeUiSkin.FrameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            Image innerBg = MenuUi.CreateImage("Inner", rect, new Color(0.08f, 0.09f, 0.11f, 0.99f));
            MenuUi.SetRect(innerBg.rectTransform,
                new Vector2(8f, 8f), new Vector2(-8f, -8f), Vector2.zero, Vector2.one);
            innerBg.raycastTarget = false;
        }

        TextMeshProUGUI title = MenuUi.CreateText("Title", rect, 13f, TextAlignmentOptions.Center, Color.white);
        MenuUi.SetRect(title.rectTransform,
            new Vector2(10f, -36f), new Vector2(-10f, -12f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        title.text = "Apply UI scale?";

        _scaleDialogValue = MenuUi.CreateText("Value", rect, 12f, TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(_scaleDialogValue);
        MenuUi.SetRect(_scaleDialogValue.rectTransform,
            new Vector2(10f, -60f), new Vector2(-10f, -38f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        LazyButton apply = MenuUi.CreateButton("Apply", rect, "Apply",
            new Vector2(10f, 12f), new Vector2(-6f, 44f), new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        MenuUi.ApplyDialogButton(apply);
        apply.onClick.AddListener(ApplyScale);

        LazyButton cancel = MenuUi.CreateButton("Cancel", rect, "Cancel",
            new Vector2(6f, 12f), new Vector2(-10f, 44f), new Vector2(0.5f, 0f), new Vector2(1f, 0f));
        cancel.onClick.AddListener(CancelScale);

        go.transform.SetAsLastSibling();
        _scaleDialog = go;
    }

    private void ApplyScale()
    {
        ModConfig.UiScale.Value = _scaleSlider.value;
        ApplyUiScale();
        ExitScalePreview();
    }

    private void CancelScale()
    {
        float applied = ModConfig.UiScale.Value;
        _scaleSlider.SetValueWithoutNotify(applied);
        if (_scaleValueLabel != null)
        {
            _scaleValueLabel.text = applied.ToString("0.0");
        }
        ExitScalePreview();
    }

    private void ExitScalePreview()
    {
        _scalePreviewActive = false;
        if (_scaleGhost != null)
        {
            Destroy(_scaleGhost);
            _scaleGhost = null;
        }
        if (_scaleDialog != null)
        {
            Destroy(_scaleDialog);
            _scaleDialog = null;
        }
    }

    private void ApplyUiScale()
    {
        if (_panelRect == null)
        {
            return;
        }
        float scale = Mathf.Clamp(ModConfig.UiScale.Value, 0.5f, 3f);
        _panelRect.localScale = new Vector3(scale, scale, 1f);
    }

    // --- Hotkey rebind (capture driven per-frame from MenuModule.Tick via TickInput) ---

    public void BuildRebindRow(RectTransform band)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Label", band, 13f, TextAlignmentOptions.Left, Color.white);
        MenuUi.ApplyLabelText(label);
        MenuUi.SetRect(label.rectTransform,
            new Vector2(0f, 0f), new Vector2(-114f, 0f), Vector2.zero, Vector2.one);
        label.text = "Menu Key";

        LazyButton button = MenuUi.CreateButton("Rebind", band, ModConfig.MenuHotkey.Value.ToString(),
            new Vector2(-110f, 2f), new Vector2(0f, -2f), new Vector2(1f, 0f), new Vector2(1f, 1f));
        _rebindButtonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (_rebindButtonLabel != null)
        {
            _rebindButtonLabel.fontSize = 12f;
        }
        button.onClick.AddListener(BeginRebind);
    }

    private void BeginRebind()
    {
        _rebindListening = true;
        if (_rebindButtonLabel != null)
        {
            _rebindButtonLabel.text = "Press a key…";
        }
    }

    private void CompleteRebind(KeyCode key)
    {
        ModConfig.MenuHotkey.Value = key;
        _rebindListening = false;
        if (_rebindButtonLabel != null)
        {
            _rebindButtonLabel.text = key.ToString();
        }
    }

    private void CancelRebind()
    {
        _rebindListening = false;
        if (_rebindButtonLabel != null)
        {
            _rebindButtonLabel.text = ModConfig.MenuHotkey.Value.ToString();
        }
    }

    /// <summary>
    /// Per-frame input hook, called from <c>MenuModule.Tick</c>. Returns true while a rebind is
    /// listening so the caller doesn't also treat the keypress as a menu toggle.
    /// </summary>
    public bool TickInput()
    {
        if (!_rebindListening)
        {
            return false;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return true;
        }
        foreach (KeyCode key in AllKeys)
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
            {
                continue;
            }
            int code = (int)key;
            if (code >= (int)KeyCode.Mouse0 && code <= (int)KeyCode.Mouse6)
            {
                continue;
            }
            if (Input.GetKeyDown(key))
            {
                CompleteRebind(key);
                return true;
            }
        }
        return true;
    }

    private void ResetTransientState()
    {
        if (_scalePreviewActive)
        {
            CancelScale();
        }
        if (_rebindListening)
        {
            CancelRebind();
        }
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
        ApplyUiScale();
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
