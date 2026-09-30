using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// The legend on the map page's left edge: a "Markers" tab that opens a scrollable panel with a tick
/// per category (resource groups fold open to a tick per drop), each with how many are on the map,
/// plus All / None. It starts closed every time the map opens. Parented to the map page itself, not
/// the map, so it doesn't pan. Clicks on it never reach the map (<see cref="IMapClickTarget"/>), so
/// a shift-click here can't teleport.
/// </summary>
internal sealed class MarkerLegend : MonoBehaviour, IMapClickTarget
{
    private const float TabWidth = 104f;
    private const float TabHeight = 26f;
    private const float PanelWidth = 236f;
    private const float Margin = 12f;
    private const float GroupHeight = 24f;
    private const float TypeHeight = 22f;
    private const float Indent = 18f;
    private const float Tick = 12f;
    private const float FoldWidth = 30f;
    private const float FrameInset = 13f;

    private static readonly Color TickOff = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color TickOn = new Color(0.95f, 0.75f, 0.25f, 1f);
    private static readonly Color TickSome = new Color(0.95f, 0.75f, 0.25f, 0.45f);

    private readonly HashSet<string> _expanded = new HashSet<string>();
    private readonly List<Action> _syncs = new List<Action>();

    private MarkerLayer _layer;
    private RectTransform _panel;
    private RectTransform _content;
    private TextMeshProUGUI _tabLabel;
    private bool _open;

    public bool HandlesShiftClick => true;

    public static MarkerLegend Create(Transform page, MarkerLayer layer)
    {
        RectTransform root = MenuUi.CreateRect("AKN_MarkerLegend", page);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.offsetMin = new Vector2(Margin, Margin);
        root.offsetMax = new Vector2(Margin + PanelWidth, -Margin);

        var legend = root.gameObject.AddComponent<MarkerLegend>();
        legend._layer = layer;
        legend.Build(root);
        root.SetAsLastSibling();
        return legend;
    }

    /// <summary>Redraws counts and ticks; rebuilds the rows when the catalog found new types.</summary>
    public void Refresh()
    {
        if (_layer.Catalog.LayoutChanged)
        {
            _layer.Catalog.LayoutChanged = false;
            BuildRows();
        }
        foreach (Action sync in _syncs)
        {
            sync();
        }
    }

    private void OnEnable()
    {
        _open = false;
        ApplyOpen();
    }

    private void Build(RectTransform root)
    {
        BuildTab(root);

        _panel = MenuUi.CreateRect("Panel", root);
        MenuUi.SetRect(_panel, Anchors.Fill, Vector2.zero, new Vector2(0f, -TabHeight - 4f));
        // Near-transparent so the whole panel, frame included, catches clicks and wheel input.
        _panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        // Inset like the mod menu's own panel: the game's frame sprite has a margin outside its border.
        float inset = NativeUiSkin.IsReady
            ? FrameInset
            : 0f;
        Image background = MenuUi.CreateImage("Background", _panel, new Color(0.08f, 0.05f, 0.04f, 0.95f));
        MenuUi.SetRect(background.rectTransform, Anchors.Fill, new Vector2(inset, inset), new Vector2(-inset, -inset));
        background.raycastTarget = false;
        if (NativeUiSkin.IsReady)
        {
            background.color = new Color(0.105f, 0.112f, 0.14f, 0.96f);
            Image frame = MenuUi.CreateImage("Frame", _panel, Color.white);
            MenuUi.Stretch(frame.rectTransform);
            MenuUi.ApplyFrame(frame);
            frame.raycastTarget = false;
        }

        RectTransform buttons = MenuUi.CreateRect("AllNone", _panel);
        MenuUi.SetRect(buttons, Anchors.Top, new Vector2(16f, -42f), new Vector2(-16f, -16f));
        var left = new Anchors(Vector2.zero, new Vector2(0.5f, 1f));
        var right = new Anchors(new Vector2(0.5f, 0f), Vector2.one);
        MakeButton(buttons, "All", left, Vector2.zero, new Vector2(-3f, 0f))
            .onClick.AddListener(() => SetAll(true));
        MakeButton(buttons, "None", right, new Vector2(3f, 0f), Vector2.zero)
            .onClick.AddListener(() => SetAll(false));

        RectTransform listArea = MenuUi.CreateRect("List", _panel);
        MenuUi.SetRect(listArea, Anchors.Fill, new Vector2(14f, 14f), new Vector2(-14f, -48f));
        RectTransform scrollArea = ScrollHints.SplitArea(listArea, out RectTransform up, out RectTransform down);
        var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 24f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = MenuUi.CreateRect("Viewport", scrollArea);
        MenuUi.Stretch(viewport);
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;
        _content = MenuUi.CreateRect("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.offsetMin = Vector2.zero;
        _content.offsetMax = Vector2.zero;
        scroll.content = _content;
        _ = new ScrollHints(scroll, up, down);

        ApplyOpen();
    }

    private void BuildTab(RectTransform root)
    {
        LazyButton tab = MakeButton(root, string.Empty, Anchors.Top, Vector2.zero, Vector2.zero);
        var tabRect = (RectTransform)tab.transform;
        tabRect.anchorMin = tabRect.anchorMax = new Vector2(0f, 1f);
        tabRect.pivot = new Vector2(0f, 1f);
        tabRect.sizeDelta = new Vector2(TabWidth, TabHeight);
        tabRect.anchoredPosition = Vector2.zero;
        _tabLabel = tab.GetComponentInChildren<TextMeshProUGUI>(true);
        tab.onClick.AddListener(() =>
        {
            _open = !_open;
            ApplyOpen();
        });
    }

    private void ApplyOpen()
    {
        if (_panel != null)
        {
            _panel.gameObject.SetActive(_open);
        }
        if (_tabLabel != null)
        {
            _tabLabel.text = _open ? "▾ Markers" : "▸ Markers";
        }
    }

    private void BuildRows()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Destroy(_content.GetChild(i).gameObject);
        }
        _syncs.Clear();

        float y = 0f;
        foreach (MarkerGroup group in _layer.Catalog.Groups)
        {
            y = GroupRow(group, y);
            if (group.IsResource && _expanded.Contains(group.Key))
            {
                foreach (MarkerType type in group.Types)
                {
                    y = TypeRow(type, y);
                }
            }
        }
        if (y == 0f)
        {
            TextMeshProUGUI empty = Text(_content, "Nothing to mark here.", 11f);
            MenuUi.SetRect(empty.rectTransform, Anchors.Top, new Vector2(4f, -20f), Vector2.zero);
            y = -20f;
        }
        _content.sizeDelta = new Vector2(0f, -y);
    }

    private float GroupRow(MarkerGroup group, float top)
    {
        float foldWidth = group.IsResource
            ? FoldWidth + 2f
            : 0f;
        RectTransform row = Row(top, GroupHeight, 0f, foldWidth, () => Toggle(group.Types));
        Image tick = TickBox(row);
        TextMeshProUGUI label = Text(row, string.Empty, 12f);
        float right = group.IsResource
            ? -FoldWidth - 4f
            : 0f;
        MenuUi.SetRect(label.rectTransform, Anchors.Fill, new Vector2(Tick + 8f, 0f), new Vector2(right, 0f));
        _syncs.Add(() =>
        {
            label.text = $"{group.Title} ({group.Types.Sum(t => t.Count)})";
            int shown = group.Types.Count(t => MarkerFilters.IsShown(t.Key));
            tick.color = shown == 0
                ? TickOff
                : shown == group.Types.Count ? TickOn : TickSome;
        });

        if (group.IsResource)
        {
            bool open = _expanded.Contains(group.Key);
            string arrow = open
                ? "▾"
                : "▸";
            UnityEngine.UI.Button fold = FoldButton(row, arrow);
            fold.onClick.AddListener(() =>
            {
                if (!_expanded.Remove(group.Key))
                {
                    _expanded.Add(group.Key);
                }
                BuildRows();
                Refresh();
            });
        }
        return top - GroupHeight;
    }

    private float TypeRow(MarkerType type, float top)
    {
        RectTransform row = Row(top, TypeHeight, Indent, 0f, () => Toggle(new[] { type }));
        Image tick = TickBox(row);
        float textLeft = Tick + 8f;
        if (type.Icon != null)
        {
            Image icon = MenuUi.CreateImage("Icon", row, Color.white);
            icon.sprite = type.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            MenuUi.SetRect(icon.rectTransform, Anchors.Left, new Vector2(Tick + 6f, 2f), new Vector2(Tick + 24f, -2f));
            textLeft = Tick + 28f;
        }
        TextMeshProUGUI label = Text(row, string.Empty, 11f);
        MenuUi.SetRect(label.rectTransform, Anchors.Fill, new Vector2(textLeft, 0f), Vector2.zero);
        _syncs.Add(() =>
        {
            label.text = $"{type.Label} ({type.Count})";
            tick.color = MarkerFilters.IsShown(type.Key) ? TickOn : TickOff;
        });
        return top - TypeHeight;
    }

    private void Toggle(IList<MarkerType> types)
    {
        bool allShown = types.All(t => MarkerFilters.IsShown(t.Key));
        MarkerFilters.Set(types.Select(t => t.Key), !allShown);
        _layer.ApplyFilters();
        Refresh();
    }

    private void SetAll(bool shown)
    {
        MarkerFilters.Set(_layer.Catalog.Groups.SelectMany(g => g.Types).Select(t => t.Key), shown);
        _layer.ApplyFilters();
        Refresh();
    }

    /// <summary>
    /// A full-width row, <paramref name="indent"/> from the left, whose area up to
    /// <paramref name="rightGap"/> from the right edge toggles on click. The gap is left free for
    /// another button: a button inside the toggle area wouldn't get the click.
    /// </summary>
    private RectTransform Row(float top, float height, float indent, float rightGap, Action onClick)
    {
        RectTransform row = MenuUi.CreateRect("Row", _content);
        MenuUi.SetRect(row, Anchors.Top, new Vector2(indent, top - height), new Vector2(0f, top));
        Image hit = MenuUi.CreateImage("Hit", row, new Color(0f, 0f, 0f, 0.001f));
        MenuUi.SetRect(hit.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(-rightGap, 0f));
        var button = hit.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = hit;
        button.onClick.AddListener(() => onClick());
        return row;
    }

    private static Image TickBox(RectTransform row)
    {
        Image box = MenuUi.CreateImage("Tick", row, TickOff);
        box.raycastTarget = false;
        RectTransform rect = box.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(Tick, Tick);
        rect.anchoredPosition = new Vector2(2f, 0f);
        var outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.85f, 0.7f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
        return box;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string text, float size)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Text", parent, size, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(label);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    /// <summary>
    /// The ▸/▾ at a resource group's right. A plain uGUI button, like the rows: the game's
    /// <c>LazyButton</c> never got the click inside the scroll list.
    /// </summary>
    private static UnityEngine.UI.Button FoldButton(RectTransform row, string arrow)
    {
        Image image = MenuUi.CreateImage("Fold", row, new Color(0.28f, 0.12f, 0.07f, 1f));
        MenuUi.ApplyCell(image);
        MenuUi.SetRect(image.rectTransform, Anchors.Right, new Vector2(-FoldWidth, 1f), new Vector2(0f, -1f));
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        TextMeshProUGUI label = MenuUi.CreateText("Arrow", image.rectTransform, 12f, TextAlignmentOptions.Center);
        MenuUi.Stretch(label.rectTransform);
        label.raycastTarget = false;
        label.text = arrow;
        return button;
    }

    private static LazyButton MakeButton(RectTransform parent, string text, Anchors anchors, Vector2 min, Vector2 max)
    {
        LazyButton button = MenuUi.CreateButton(text.Length > 0 ? text : "Tab", parent, text, anchors, min, max);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.fontSize = 11f;
        }
        return button;
    }
}
