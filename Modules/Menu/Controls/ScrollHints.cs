using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Controls;

/// <summary>
/// Up/down "there's more" hints for a vertical scroll view, the vertical twin of the tab bar's
/// &lt; / &gt; hints: the same label-coloured glyph, rotated to point up or down, shown only while
/// content is hidden in that direction. Display-only. The hint areas must sit outside the scroll
/// view's mask.
/// </summary>
internal sealed class ScrollHints
{
    /// <summary>Height of the strip each hint sits in.</summary>
    public const float StripHeight = 14f;

    private const float GlyphSize = 14f;

    private readonly ScrollRect _scroll;
    private readonly TextMeshProUGUI _up;
    private readonly TextMeshProUGUI _down;

    public ScrollHints(ScrollRect scroll, RectTransform upArea, RectTransform downArea)
    {
        _scroll = scroll;
        // "<" turned a quarter clockwise points up, a quarter anticlockwise points down.
        _up = CreateHint("Up", upArea, -90f);
        _down = CreateHint("Down", downArea, 90f);

        _scroll.onValueChanged.AddListener(_ => Update());
        _scroll.viewport.gameObject.AddComponent<ResizeNotifier>().Resized += Update;
        // The content grows as rows are added, which doesn't raise onValueChanged.
        _scroll.content.gameObject.AddComponent<ResizeNotifier>().Resized += Update;
        Update();
    }

    /// <summary>
    /// Splits <paramref name="area"/> into a strip at the top and bottom for the hints and returns
    /// the rect between them, where the scroll view goes. Pass the strips to the constructor once
    /// the scroll view exists.
    /// </summary>
    public static RectTransform SplitArea(RectTransform area, out RectTransform up, out RectTransform down)
    {
        up = MenuUi.CreateRect("ScrollUp", area);
        MenuUi.SetRect(up, Anchors.Top, new Vector2(0f, -StripHeight), Vector2.zero);
        down = MenuUi.CreateRect("ScrollDown", area);
        MenuUi.SetRect(down, Anchors.Bottom, Vector2.zero, new Vector2(0f, StripHeight));
        RectTransform inner = MenuUi.CreateRect("ScrollArea", area);
        MenuUi.SetRect(inner, Anchors.Fill, new Vector2(0f, StripHeight), new Vector2(0f, -StripHeight));
        return inner;
    }

    private static TextMeshProUGUI CreateHint(string name, RectTransform area, float rotation)
    {
        TextMeshProUGUI hint = MenuUi.CreateText(name, area, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyLabelText(hint);
        RectTransform rect = hint.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(GlyphSize, GlyphSize);
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        hint.raycastTarget = false;
        hint.text = "<";
        return hint;
    }

    public void Update()
    {
        if (_scroll == null || _scroll.viewport == null || _scroll.content == null)
        {
            return;
        }
        float viewHeight = _scroll.viewport.rect.height;
        float hiddenAbove = _scroll.content.anchoredPosition.y;
        float hiddenBelow = _scroll.content.rect.height - hiddenAbove - viewHeight;
        // Half-pixel slack so clamped float positions don't leave a hint stuck on.
        _up.enabled = viewHeight > 0f && hiddenAbove > 0.5f;
        _down.enabled = viewHeight > 0f && hiddenBelow > 0.5f;
    }
}
