using System;
using System.Collections.Generic;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// The item adder's scrolling list. Each row shows the item's icon, name and id with an amount
/// field and a Give button. The list is virtualized: the scroll content spans every item, and a
/// small row pool is repositioned and repopulated for whatever is in view, so all ~800 items
/// stay reachable.
/// </summary>
internal sealed class ItemList
{
    private const float RowHeight = 40f;

    private readonly Action<ItemDef, string, int> _onGive;
    private readonly List<ItemRow> _rows = new List<ItemRow>();
    // Keyed by item id because pooled rows are reused for different items while scrolling.
    private readonly Dictionary<string, string> _amounts = new Dictionary<string, string>();
    private readonly ScrollRect _scroll;
    private readonly RectTransform _content;
    private IReadOnlyList<ItemCatalog.Entry> _items = Array.Empty<ItemCatalog.Entry>();

    /// <summary>
    /// Builds the list into <paramref name="area"/>. Give calls <paramref name="onGive"/> with
    /// the item, its display name and the typed amount (at least 1).
    /// </summary>
    public ItemList(RectTransform area, Action<ItemDef, string, int> onGive)
    {
        _onGive = onGive;

        area = ScrollHints.SplitArea(area, out RectTransform up, out RectTransform down);
        _scroll = area.gameObject.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.scrollSensitivity = 24f;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = MenuUi.CreateRect("Viewport", area);
        MenuUi.Stretch(viewport);
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<ResizeNotifier>().Resized += UpdateVisibleRows;
        _scroll.viewport = viewport;

        _content = MenuUi.CreateRect("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.offsetMin = new Vector2(0f, 0f);
        _content.offsetMax = new Vector2(0f, 0f);
        _content.sizeDelta = new Vector2(0f, 0f);
        _scroll.content = _content;
        _scroll.onValueChanged.AddListener(_ => UpdateVisibleRows());
        _ = new ScrollHints(_scroll, up, down);
    }

    /// <summary>Shows <paramref name="items"/>, scrolled back to the top.</summary>
    public void SetItems(IReadOnlyList<ItemCatalog.Entry> items)
    {
        _items = items;
        _content.sizeDelta = new Vector2(0f, _items.Count * RowHeight);
        _scroll.StopMovement();
        _content.anchoredPosition = Vector2.zero;

        foreach (ItemRow row in _rows)
        {
            row.Index = -1;
        }
        UpdateVisibleRows();
    }

    private void UpdateVisibleRows()
    {
        if (_scroll == null || _scroll.viewport == null || _content == null)
        {
            return;
        }
        // +2 covers a partially visible row at each edge. The pool only grows, since the
        // viewport's height depends on screen size and UI scale.
        int needed = Mathf.CeilToInt(_scroll.viewport.rect.height / RowHeight) + 2;
        while (_rows.Count < needed)
        {
            _rows.Add(CreateRow());
        }

        int first = Mathf.Max(0, Mathf.FloorToInt(_content.anchoredPosition.y / RowHeight));
        for (int index = first; index < first + _rows.Count; index++)
        {
            // Slotting by index keeps a row on its item while in view, so scrolling only
            // repopulates the rows entering at the edges.
            ItemRow row = _rows[index % _rows.Count];
            if (index >= _items.Count)
            {
                row.Index = -1;
                row.Root.gameObject.SetActive(false);
                continue;
            }
            if (row.Index != index)
            {
                row.Index = index;
                Populate(row, _items[index]);
                row.Root.anchoredPosition = new Vector2(0f, -index * RowHeight);
            }
            row.Root.gameObject.SetActive(true);
        }
    }

    private ItemRow CreateRow()
    {
        var row = new ItemRow();

        row.Root = MenuUi.CreateRect("Row", _content);
        row.Root.anchorMin = new Vector2(0f, 1f);
        row.Root.anchorMax = new Vector2(1f, 1f);
        row.Root.pivot = new Vector2(0.5f, 1f);
        row.Root.sizeDelta = new Vector2(0f, RowHeight);

        row.Icon = MenuUi.CreateImage("Icon", row.Root, Color.white);
        MenuUi.SetRect(row.Icon.rectTransform, Anchors.Left, new Vector2(2f, 6f), new Vector2(30f, -6f));
        row.Icon.raycastTarget = false;
        row.Icon.preserveAspect = true;

        row.Name = MenuUi.CreateText("Name", row.Root, 12f, TextAlignmentOptions.BottomLeft);
        MenuUi.SetRect(
            row.Name.rectTransform,
            new Anchors(new Vector2(0f, 0.5f), Vector2.one),
            new Vector2(36f, 0f),
            new Vector2(-88f, -1f)
        );

        row.Id = MenuUi.CreateText("Id", row.Root, 10f, TextAlignmentOptions.TopLeft);
        MenuUi.ApplyLabelText(row.Id);
        MenuUi.SetRect(
            row.Id.rectTransform,
            new Anchors(Vector2.zero, new Vector2(1f, 0.5f)),
            new Vector2(36f, 1f),
            new Vector2(-88f, 0f)
        );

        row.Amount = MenuUi.CreateInputField("Amount", row.Root, "1", 12f, TMP_InputField.ContentType.IntegerNumber);
        var amountRect = (RectTransform)row.Amount.transform;
        MenuUi.SetRect(amountRect, Anchors.Right, new Vector2(-84f, 6f), new Vector2(-48f, -6f));
        row.Amount.text = "1";
        row.Amount.onValueChanged.AddListener(text => RememberAmount(row, text));

        LazyButton give = MenuUi.CreateButton(
            "Give",
            row.Root,
            "Give",
            Anchors.Right,
            new Vector2(-44f, 6f),
            new Vector2(-4f, -6f)
        );
        TextMeshProUGUI giveLabel = give.GetComponentInChildren<TextMeshProUGUI>(true);
        if (giveLabel != null)
        {
            giveLabel.fontSize = 12f;
        }
        give.onClick.AddListener(() => Give(row));

        return row;
    }

    private void Populate(ItemRow row, ItemCatalog.Entry entry)
    {
        row.Def = entry.Def;
        row.Name.text = entry.Name;
        row.Id.text = entry.Def.id;
        row.Amount.SetTextWithoutNotify(_amounts.TryGetValue(entry.Def.id, out string amount) ? amount : "1");

        Sprite sprite = EasySpritesCollection.Instance != null
            ? EasySpritesCollection.Instance.GetSprite(entry.Def.iconId)
            : null;
        row.Icon.sprite = sprite;
        row.Icon.enabled = sprite != null;
    }

    private void RememberAmount(ItemRow row, string text)
    {
        if (row.Def != null)
        {
            _amounts[row.Def.id] = text;
        }
    }

    private void Give(ItemRow row)
    {
        if (row.Def == null)
        {
            return;
        }
        int amount = 1;
        if (int.TryParse(row.Amount.text, out int parsed) && parsed > 0)
        {
            amount = parsed;
        }
        _onGive(row.Def, row.Name.text, amount);
    }

    private sealed class ItemRow
    {
        public RectTransform Root;
        public Image Icon;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Id;
        public TMP_InputField Amount;
        public ItemDef Def;
        public int Index = -1;
    }
}
