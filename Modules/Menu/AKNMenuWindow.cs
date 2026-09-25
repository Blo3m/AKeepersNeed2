using System;
using System.Reflection;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The mod's menu, built from scratch as a uGUI window and styled with the game's
/// own assets captured by <see cref="NativeUiSkin"/>. Subclasses the game's
/// <c>LazyWindow</c> so it participates in the native window stack, input and gamepad
/// handling. Structure follows GK2-Mod-Framework's ModsMenuWindow, reduced to a
/// single settings page for this mod's features. See docs/UI.md.
/// </summary>
internal sealed class AKNMenuWindow : LazyWindow<LazyWidgetDataBase>
{
    private const float PanelWidth = 340f;
    private const float SidePadding = 20f;

    private RectTransform _panelRect;
    private float _nextRowTop;

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

        _nextRowTop = -48f;
        BuildProfileSelector();

        BuildSectionHeader("Energy");
        BuildToggleRow(
            "Energy Regen",
            () => ModConfig.EnergyRegenEnabled.Value,
            v => ModConfig.EnergyRegenEnabled.Value = v);
        BuildSliderRow(
            0.1f, 10f, "0.0",
            () => ModConfig.EnergyRegenRate.Value,
            v => ModConfig.EnergyRegenRate.Value = v);
        BuildToggleRow(
            "No Energy Drain",
            () => ModConfig.NoEnergyDrainEnabled.Value,
            v => ModConfig.NoEnergyDrainEnabled.Value = v);

        BuildSectionHeader("Drops");
        BuildToggleRow(
            "Resource Drops",
            () => ModConfig.ResourceDropsEnabled.Value,
            v => ModConfig.ResourceDropsEnabled.Value = v);
        BuildSliderRow(
            1f, 10f, "0.0",
            () => ModConfig.ResourceDropMultiplier.Value,
            v => ModConfig.ResourceDropMultiplier.Value = v);
        BuildToggleRow(
            "Tech Points",
            () => ModConfig.TechPointsEnabled.Value,
            v => ModConfig.TechPointsEnabled.Value = v);
        BuildSliderRow(
            1f, 10f, "0.0",
            () => ModConfig.TechPointsMultiplier.Value,
            v => ModConfig.TechPointsMultiplier.Value = v);

        BuildSectionHeader("Crafting");
        BuildToggleRow(
            "Craft Output",
            () => ModConfig.CraftDropsEnabled.Value,
            v => ModConfig.CraftDropsEnabled.Value = v);
        BuildSliderRow(
            1f, 10f, "0.0",
            () => ModConfig.CraftDropMultiplier.Value,
            v => ModConfig.CraftDropMultiplier.Value = v);

        closeButton = MenuUi.CreateButton("Close", _panelRect, "Close",
            new Vector2(SidePadding, 14f), new Vector2(-SidePadding, 46f),
            new Vector2(0f, 0f), new Vector2(1f, 0f));
        MenuUi.ApplyDialogButton(closeButton);
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

    /// <summary>
    /// Placeholder profile picker: `[<] name [>]`. It cycles through in-memory names
    /// only — a stand-in for future save/load of configured option profiles.
    /// </summary>
    private void BuildProfileSelector()
    {
        // TODO: back these with persisted config profiles (save/load option sets).
        string[] profiles = { "Default", "Profile 1", "Profile 2" };
        int index = 0;

        RectTransform band = NewBand(32f, topGap: 4f);

        Image field = MenuUi.CreateImage("ProfileField", band, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform,
            new Vector2(40f, 0f), new Vector2(-40f, 0f), Vector2.zero, Vector2.one);
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        TextMeshProUGUI name = MenuUi.CreateText("ProfileName", band, 13f,
            TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(name);
        MenuUi.Stretch(name.rectTransform);
        name.text = profiles[index];

        LazyButton prev = MenuUi.CreateButton("Prev", band, "<",
            new Vector2(0f, 0f), new Vector2(34f, 0f), Vector2.zero, new Vector2(0f, 1f));
        LazyButton next = MenuUi.CreateButton("Next", band, ">",
            new Vector2(-34f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.one);

        void Cycle(int step)
        {
            index = (index + step + profiles.Length) % profiles.Length;
            name.text = profiles[index];
        }

        prev.onClick.AddListener(() => Cycle(-1));
        next.onClick.AddListener(() => Cycle(1));
    }

    /// <summary>A `----- Option -----` divider: centered label flanked by rules.</summary>
    private void BuildSectionHeader(string title)
    {
        RectTransform band = NewBand(22f, topGap: 16f);

        var layout = band.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;

        Color ruleColor = NativeUiSkin.IsReady
            ? new Color(
                NativeUiSkin.LabelColor.r,
                NativeUiSkin.LabelColor.g,
                NativeUiSkin.LabelColor.b,
                0.35f
            )
            : new Color(1f, 1f, 1f, 0.25f);

        AddRule(band, "RuleL", ruleColor);

        TextMeshProUGUI name = MenuUi.CreateText("Section", band, 12f,
            TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyLabelText(name);
        var nameElement = name.gameObject.AddComponent<LayoutElement>();
        nameElement.flexibleWidth = 0f;
        name.text = title;

        AddRule(band, "RuleR", ruleColor);
    }

    private static void AddRule(RectTransform parent, string name, Color color)
    {
        Image rule = MenuUi.CreateImage(name, parent, color);
        rule.raycastTarget = false;
        var element = rule.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.minHeight = 2f;
        element.preferredHeight = 2f;
    }

    /// <summary>A row with the label on the left and an On/Off toggle on the right.</summary>
    private void BuildToggleRow(string label, Func<bool> get, Action<bool> set)
    {
        RectTransform band = NewBand(30f, topGap: 8f);

        TextMeshProUGUI name = MenuUi.CreateText("Name", band, 13f,
            TextAlignmentOptions.Left, Color.white);
        MenuUi.ApplyLabelText(name);
        MenuUi.SetRect(name.rectTransform,
            new Vector2(0f, 0f), new Vector2(-84f, 0f), Vector2.zero, Vector2.one);
        name.text = label;

        var go = new GameObject("Toggle", typeof(RectTransform));
        go.transform.SetParent(band, false);
        MenuUi.SetRect((RectTransform)go.transform,
            new Vector2(-76f, 3f), new Vector2(0f, -3f),
            new Vector2(1f, 0f), new Vector2(1f, 1f));

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.12f, 0.07f, 1f);
        MenuUi.ApplyCell(bg);

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        go.AddComponent<GamepadNavigationItem>();

        TextMeshProUGUI value = MenuUi.CreateText("Value", go.transform, 13f,
            TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(value);
        MenuUi.Stretch(value.rectTransform);

        void Refresh(bool on)
        {
            value.text = on ? "On" : "Off";
        }

        toggle.isOn = get();
        Refresh(toggle.isOn);
        toggle.onValueChanged.AddListener(on =>
        {
            set(on);
            Refresh(on);
        });
    }

    /// <summary>A full-width slider with its value overlaid in the centre.</summary>
    private void BuildSliderRow(float min, float max, string format,
        Func<float> get, Action<float> set)
    {
        RectTransform band = NewBand(24f, topGap: 6f);

        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(band, false);
        MenuUi.Stretch((RectTransform)go.transform);

        var slider = go.AddComponent<Slider>();

        Image bg = MenuUi.CreateImage("Background", go.transform, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.Stretch(bg.rectTransform);
        if (NativeUiSkin.IsReady && NativeUiSkin.ProgressBackgroundSprite != null)
        {
            bg.sprite = NativeUiSkin.ProgressBackgroundSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }

        Image fill = MenuUi.CreateImage("Fill", go.transform, new Color(0.75f, 0.32f, 0.12f, 1f));
        MenuUi.Stretch(fill.rectTransform);
        if (NativeUiSkin.IsReady && NativeUiSkin.ProgressFillSprite != null)
        {
            fill.sprite = NativeUiSkin.ProgressFillSprite;
            fill.type = Image.Type.Sliced;
            fill.color = Color.white;
        }
        slider.fillRect = fill.rectTransform;

        Image handle = MenuUi.CreateImage("Handle", go.transform, new Color(1f, 0.75f, 0.35f, 1f));
        handle.rectTransform.sizeDelta = new Vector2(12f, NativeUiSkin.IsReady ? 18f : 24f);
        if (NativeUiSkin.IsReady && NativeUiSkin.SliderHandleSprite != null)
        {
            handle.sprite = NativeUiSkin.SliderHandleSprite;
            handle.type = Image.Type.Sliced;
            handle.color = Color.white;
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.colors = NativeUiSkin.SliderColors;
            slider.spriteState = NativeUiSkin.SliderSpriteState;
        }
        slider.targetGraphic = handle;
        slider.handleRect = handle.rectTransform;
        go.AddComponent<GamepadNavigationItem>();

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = get();

        TextMeshProUGUI value = MenuUi.CreateText("Value", band, 12f,
            TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(value);
        MenuUi.Stretch(value.rectTransform);

        void Refresh(float v)
        {
            value.text = v.ToString(format);
        }

        Refresh(slider.value);
        slider.onValueChanged.AddListener(v =>
        {
            set(v);
            Refresh(v);
        });
    }

    /// <summary>
    /// Creates a full-width row of the given height at the current vertical cursor
    /// and advances the cursor (including a gap above the row).
    /// </summary>
    private RectTransform NewBand(float height, float topGap)
    {
        _nextRowTop -= topGap;

        var go = new GameObject("Band", typeof(RectTransform));
        var band = (RectTransform)go.transform;
        band.SetParent(_panelRect, false);
        MenuUi.SetRect(band,
            new Vector2(SidePadding, _nextRowTop - height), new Vector2(-SidePadding, _nextRowTop),
            new Vector2(0f, 1f), new Vector2(1f, 1f));

        _nextRowTop -= height;
        return band;
    }

    public override void Open(LazyWidgetDataBase data)
    {
        // LazyWindow wires its gamepad controller before activating; a runtime-built
        // window must be active first so navigation items are discoverable.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
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
