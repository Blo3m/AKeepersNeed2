using System;
using System.Collections.Generic;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Vertical layout helper for a tab's content: stacks section dividers, toggle rows and
/// slider rows from the top down inside a content <see cref="RectTransform"/>. The static
/// <c>Fill*</c> builders are reused for one-off controls outside a page (e.g. the footer).
/// Controls only read their value when built, so <see cref="Sync"/> re-reads them after the
/// underlying config changes behind their back (e.g. a profile switch).
/// </summary>
internal sealed class MenuPage
{
    private const float DisabledAlpha = 0.45f;
    private const float BottomPadding = 8f;
    private const float MaskOverhang = 10f;

    private readonly RectTransform _content;
    private readonly List<Action> _syncs = new List<Action>();
    private float _cursorTop;

    /// <summary>
    /// Wraps <paramref name="page"/> in a vertical <see cref="ScrollRect"/>. Rows go into its
    /// content, which grows with every band, so pages taller than the window scroll.
    /// </summary>
    public MenuPage(RectTransform page)
    {
        var scroll = page.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 24f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // The mask is widened past the page so slider handles, which overhang the track's ends
        // at min/max, aren't clipped. The content is inset by the same amount to stay aligned.
        RectTransform viewport = MenuUi.CreateRect("Viewport", page);
        MenuUi.SetRect(viewport, Anchors.Fill, new Vector2(-MaskOverhang, 0f), new Vector2(MaskOverhang, 0f));
        // Near-transparent so the empty space between rows still catches wheel/drag input.
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        _content = MenuUi.CreateRect("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.offsetMin = new Vector2(MaskOverhang, 0f);
        _content.offsetMax = new Vector2(-MaskOverhang, 0f);
        scroll.content = _content;
    }

    /// <summary>Re-reads every row's value without firing its setter.</summary>
    public void Sync()
    {
        foreach (Action sync in _syncs)
        {
            sync();
        }
    }

    /// <summary>Includes a custom control's re-read in <see cref="Sync"/>.</summary>
    public void AddSync(Action sync)
    {
        _syncs.Add(sync);
    }

    /// <summary>A `----- Title -----` divider: centered label flanked by rules.</summary>
    public void SectionHeader(string title)
    {
        RectTransform band = NewBand(22f, 16f);

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

        TextMeshProUGUI name = MenuUi.CreateText("Section", band, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyLabelText(name);
        var element = name.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 0f;
        name.text = title;

        AddRule(band, "RuleR", ruleColor);
    }

    /// <summary>
    /// A row with the label on the left and an On/Off toggle on the right. With
    /// <paramref name="enabledWhen"/>, the row is greyed out and non-interactable while it
    /// returns false (re-checked on every <see cref="Sync"/>).
    /// </summary>
    public void ToggleRow(string label, Func<bool> get, Action<bool> set, Func<bool> enabledWhen = null)
    {
        RectTransform band = NewBand(30f, 8f);
        if (enabledWhen != null)
        {
            var group = band.gameObject.AddComponent<CanvasGroup>();
            void SyncEnabled()
            {
                bool enabled = enabledWhen();
                group.interactable = enabled;
                group.blocksRaycasts = enabled;
                group.alpha = enabled ? 1f : DisabledAlpha;
            }
            SyncEnabled();
            _syncs.Add(SyncEnabled);
        }

        TextMeshProUGUI name = MenuUi.CreateText("Name", band, 13f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(name);
        MenuUi.SetRect(name.rectTransform, Anchors.Fill, new Vector2(0f, 0f), new Vector2(-84f, 0f));
        name.text = label;

        RectTransform rect = MenuUi.CreateRect("Toggle", band);
        MenuUi.SetRect(rect, Anchors.Right, new Vector2(-76f, 3f), new Vector2(0f, -3f));
        _syncs.Add(FillToggle(rect, get, set));
    }

    /// <summary>A full-width slider with its value overlaid in the centre.</summary>
    public void SliderRow(float min, float max, string format, Func<float> get, Action<float> set)
    {
        RectTransform band = NewBand(24f, 6f);
        FillSlider(band, min, max, format, get, set, out Action sync);
        _syncs.Add(sync);
    }

    /// <summary>Reserves a full-width row at the cursor and returns it for custom content.</summary>
    public RectTransform Band(float height, float topGap)
    {
        return NewBand(height, topGap);
    }

    /// <summary>
    /// Builds a toggle (background + On/Off label) that fills <paramref name="rect"/>. Returns
    /// an action that re-reads <paramref name="get"/> without calling <paramref name="set"/>.
    /// </summary>
    public static Action FillToggle(RectTransform rect, Func<bool> get, Action<bool> set)
    {
        var bg = rect.gameObject.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.12f, 0.07f, 1f);
        MenuUi.ApplyCell(bg);

        var toggle = rect.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        rect.gameObject.AddComponent<GamepadNavigationItem>();

        TextMeshProUGUI value = MenuUi.CreateText("Value", rect, 13f, TextAlignmentOptions.Center);
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

        return () =>
        {
            toggle.SetIsOnWithoutNotify(get());
            Refresh(toggle.isOn);
        };
    }

    /// <summary>
    /// Builds a slider with a centered value label that fills <paramref name="parent"/>.
    /// <paramref name="sync"/> re-reads <paramref name="get"/> without calling <paramref name="set"/>.
    /// </summary>
    public static Slider FillSlider(
        RectTransform parent,
        float min,
        float max,
        string format,
        Func<float> get,
        Action<float> set,
        out Action sync
    )
    {
        RectTransform area = MenuUi.CreateRect("Slider", parent);
        MenuUi.Stretch(area);
        GameObject go = area.gameObject;
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

        TextMeshProUGUI value = MenuUi.CreateText("Value", parent, 12f, TextAlignmentOptions.Center);
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

        sync = () =>
        {
            slider.SetValueWithoutNotify(get());
            Refresh(slider.value);
        };
        return slider;
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

    /// <summary>
    /// Creates a full-width row of the given height at the current vertical cursor (relative
    /// to the content's top) and advances the cursor past it, including a gap above.
    /// </summary>
    private RectTransform NewBand(float height, float topGap)
    {
        _cursorTop -= topGap;

        RectTransform band = MenuUi.CreateRect("Band", _content);
        MenuUi.SetRect(band, Anchors.Top, new Vector2(0f, _cursorTop - height), new Vector2(0f, _cursorTop));

        _cursorTop -= height;
        _content.sizeDelta = new Vector2(-2f * MaskOverhang, -_cursorTop + BottomPadding);
        return band;
    }
}
