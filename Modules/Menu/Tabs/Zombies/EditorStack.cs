using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// Stacks rows top-down inside a zombie editor and tracks the total height, so the list can
/// place the next zombie under it. A lighter <c>MenuPage</c> without its own scrolling: the editor
/// lives inside the zombie list's scroll area.
/// </summary>
internal sealed class EditorStack
{
    private readonly RectTransform _root;
    private float _cursor;

    public EditorStack(RectTransform root)
    {
        _root = root;
    }

    public float Height => -_cursor;

    public RectTransform Band(float height, float gap = 4f)
    {
        _cursor -= gap;
        RectTransform band = MenuUi.CreateRect("Band", _root);
        MenuUi.SetRect(band, Anchors.Top, new Vector2(0f, _cursor - height), new Vector2(0f, _cursor));
        _cursor -= height;
        return band;
    }

    /// <summary>A small left-aligned sub-heading with a faint rule under it.</summary>
    public void Heading(string title)
    {
        RectTransform band = Band(20f, 10f);
        TextMeshProUGUI text = MenuUi.CreateText("Heading", band, 12f, TextAlignmentOptions.BottomLeft);
        MenuUi.ApplyHeaderText(text);
        MenuUi.Stretch(text.rectTransform);
        text.text = title;

        Image rule = MenuUi.CreateImage("Rule", band, new Color(1f, 1f, 1f, 0.15f));
        MenuUi.SetRect(rule.rectTransform, Anchors.Bottom, new Vector2(0f, -2f), new Vector2(0f, -1f));
        rule.raycastTarget = false;
    }

    /// <summary>A left-aligned label filling <paramref name="band"/> minus <paramref name="rightInset"/>.</summary>
    public static TextMeshProUGUI Label(RectTransform band, string text, float rightInset, float size = 12f)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Label", band, size, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(label);
        MenuUi.SetRect(label.rectTransform, Anchors.Fill, new Vector2(4f, 0f), new Vector2(-rightInset, 0f));
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = text;
        return label;
    }
}
