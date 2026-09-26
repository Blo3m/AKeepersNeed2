using System;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The menu's tab strip: fixed-width tab buttons in a bar that scrolls sideways (drag / mouse
/// wheel) once they outgrow it, keeping the selected or gamepad-focused tab in view.
/// <see cref="Dispose"/> unhooks the static gamepad-focus event.
/// </summary>
internal sealed class MenuTabBar : IDisposable
{
    private const float TabWidth = 74f;

    private readonly ScrollRect _scroll;
    private readonly TextMeshProUGUI[] _labels;
    private readonly GamepadNavigationItem[] _navItems;

    /// <summary>Fills <paramref name="bar"/> with one tab per title; clicks call <paramref name="onSelect"/>.</summary>
    public MenuTabBar(RectTransform bar, string[] titles, Action<int> onSelect)
    {
        // The clear image catches drags that start between tabs.
        bar.gameObject.AddComponent<Image>().color = Color.clear;
        bar.gameObject.AddComponent<RectMask2D>();

        RectTransform strip = MenuUi.CreateRect("Tabs", bar);
        strip.anchorMin = Vector2.zero;
        strip.anchorMax = new Vector2(0f, 1f);
        strip.pivot = new Vector2(0f, 0.5f);
        strip.sizeDelta = new Vector2(titles.Length * TabWidth, 0f);
        strip.anchoredPosition = Vector2.zero;

        _scroll = bar.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = bar;
        _scroll.content = strip;
        _scroll.horizontal = true;
        _scroll.vertical = false;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 20f;

        _labels = new TextMeshProUGUI[titles.Length];
        _navItems = new GamepadNavigationItem[titles.Length];
        for (int i = 0; i < titles.Length; i++)
        {
            int index = i;
            LazyButton button = MenuUi.CreateButton(
                $"Tab_{titles[i]}",
                strip,
                titles[i],
                Anchors.Left,
                new Vector2(i * TabWidth + 2f, 0f),
                new Vector2((i + 1) * TabWidth - 2f, 0f)
            );
            _labels[i] = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_labels[i] != null)
            {
                _labels[i].fontSize = 12f;
            }
            _navItems[i] = button.GetComponent<GamepadNavigationItem>();
            button.onClick.AddListener(() => onSelect(index));
        }

        GamepadNavigationItem.OnFocusStatic += OnNavItemFocused;
    }

    /// <summary>Colours the selected tab's label and scrolls it into view.</summary>
    public void Highlight(int index)
    {
        ScrollIntoView(index);
        for (int i = 0; i < _labels.Length; i++)
        {
            if (_labels[i] != null)
            {
                _labels[i].color = i == index ? NativeUiSkin.ValueColor : NativeUiSkin.LabelColor;
            }
        }
    }

    public void Dispose()
    {
        GamepadNavigationItem.OnFocusStatic -= OnNavItemFocused;
    }

    private void OnNavItemFocused(GamepadNavigationItem item)
    {
        int index = Array.IndexOf(_navItems, item);
        if (index >= 0)
        {
            ScrollIntoView(index);
        }
    }

    private void ScrollIntoView(int index)
    {
        RectTransform strip = _scroll.content;
        float viewWidth = _scroll.viewport.rect.width;
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

        _scroll.StopMovement();
        strip.anchoredPosition = new Vector2(-visibleLeft, strip.anchoredPosition.y);
    }
}
