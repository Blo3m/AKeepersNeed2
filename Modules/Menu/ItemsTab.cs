using System.Collections.Generic;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Item adder: a searchable, category-filtered list of every item definition
/// (<c>GameBalance.Me.itemDefs</c>). Each row shows the item's icon, name and id with a
/// per-row amount field and a Give button that adds it to the player's inventory. The list is
/// virtualized: the scroll content spans every filtered item, and a small row pool is
/// repositioned and repopulated for whatever is in view, so all ~800 items stay reachable.
/// </summary>
internal sealed class ItemsTab : IMenuTab
{
    private const float RowHeight = 40f;

    private readonly List<Entry> _all = new List<Entry>();
    private readonly List<Entry> _filtered = new List<Entry>();
    // Keyed by item id because pooled rows are reused for different items while scrolling.
    private readonly Dictionary<string, string> _amounts = new Dictionary<string, string>();
    private readonly List<ItemType?> _catTypes = new List<ItemType?>();
    private readonly List<string> _catLabels = new List<string>();

    private readonly List<ItemRow> _rows = new List<ItemRow>();
    private RectTransform _listContent;
    private ScrollRect _scroll;
    private TMP_InputField _search;
    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _categoryLabel;
    private int _categoryIndex;

    public string Title => "Items";

    public void Build(RectTransform content)
    {
        BuildCache();
        BuildCategories();

        _search = MenuUi.CreateInputField("Search", content, "Search…", 12f);
        MenuUi.SetRect((RectTransform)_search.transform, Anchors.Top, new Vector2(0f, -28f), new Vector2(0f, 0f));
        _search.onValueChanged.AddListener(_ => Refilter());

        BuildCategoryRow(content);
        BuildList(content);
        Refilter();
    }

    // Nothing here is backed by config.
    public void Refresh()
    {
    }

    private void BuildCache()
    {
        List<ItemDef> defs = GameBalance.Me?.itemDefs;
        if (defs == null)
        {
            return;
        }
        foreach (ItemDef def in defs)
        {
            if (def == null || string.IsNullOrEmpty(def.id) || def.id == ItemDef.EMPTY_ITEM_ID)
            {
                continue;
            }
            string name;
            try
            {
                name = def.GetHeader();
            }
            catch
            {
                name = def.id;
            }
            if (string.IsNullOrEmpty(name))
            {
                name = def.id;
            }
            _all.Add(new Entry
            {
                Def = def,
                Name = name,
                Type = def.type,
                LowerId = def.id.ToLowerInvariant(),
                LowerName = name.ToLowerInvariant(),
            });
        }
    }

    private void BuildCategories()
    {
        _catTypes.Add(null);
        _catLabels.Add("All");
        var seen = new HashSet<ItemType>();
        foreach (Entry e in _all)
        {
            if (seen.Add(e.Type))
            {
                _catTypes.Add(e.Type);
                _catLabels.Add(e.Type == ItemType.None ? "Misc" : e.Type.ToString());
            }
        }
    }

    private void BuildCategoryRow(RectTransform content)
    {
        RectTransform row = MenuUi.CreateRect("CategoryRow", content);
        MenuUi.SetRect(row, Anchors.Top, new Vector2(0f, -60f), new Vector2(0f, -32f));

        _countText = MenuUi.CreateText("Count", row, 11f, TextAlignmentOptions.Right);
        MenuUi.ApplyLabelText(_countText);
        MenuUi.SetRect(_countText.rectTransform, Anchors.Right, new Vector2(-72f, 0f), new Vector2(0f, 0f));

        RectTransform cycler = MenuUi.CreateRect("Category", row);
        MenuUi.SetRect(cycler, Anchors.Fill, new Vector2(0f, 0f), new Vector2(-78f, 0f));

        Image field = MenuUi.CreateImage("Field", cycler, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _categoryLabel = MenuUi.CreateText("Value", cycler, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_categoryLabel);
        MenuUi.SetRect(_categoryLabel.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        _categoryLabel.text = _catLabels[_categoryIndex];

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<", Anchors.Left, Vector2.zero, new Vector2(26f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">", Anchors.Right, new Vector2(-26f, 0f), Vector2.zero);
        prev.onClick.AddListener(() => CycleCategory(-1));
        next.onClick.AddListener(() => CycleCategory(1));
    }

    private void BuildList(RectTransform content)
    {
        RectTransform scrollRect = MenuUi.CreateRect("ItemList", content);
        MenuUi.SetRect(scrollRect, Anchors.Fill, new Vector2(0f, 0f), new Vector2(0f, -64f));
        _scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.scrollSensitivity = 24f;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = MenuUi.CreateRect("Viewport", scrollRect);
        MenuUi.Stretch(viewport);
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<ResizeNotifier>().Resized += UpdateVisibleRows;
        _scroll.viewport = viewport;

        _listContent = MenuUi.CreateRect("Content", viewport);
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.offsetMin = new Vector2(0f, 0f);
        _listContent.offsetMax = new Vector2(0f, 0f);
        _listContent.sizeDelta = new Vector2(0f, 0f);
        _scroll.content = _listContent;
        _scroll.onValueChanged.AddListener(_ => UpdateVisibleRows());
    }

    private ItemRow CreateRow()
    {
        var row = new ItemRow();

        row.Root = MenuUi.CreateRect("Row", _listContent);
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
        give.onClick.AddListener(() => GiveItem(row));

        return row;
    }

    private void CycleCategory(int step)
    {
        _categoryIndex = (_categoryIndex + step + _catLabels.Count) % _catLabels.Count;
        _categoryLabel.text = _catLabels[_categoryIndex];
        Refilter();
    }

    private void Refilter()
    {
        string query = _search != null ? _search.text.Trim().ToLowerInvariant() : string.Empty;
        ItemType? category = _catTypes[_categoryIndex];

        _filtered.Clear();
        foreach (Entry e in _all)
        {
            if (category.HasValue && e.Type != category.Value)
            {
                continue;
            }
            if (query.Length > 0 && !e.LowerId.Contains(query) && !e.LowerName.Contains(query))
            {
                continue;
            }
            _filtered.Add(e);
        }

        _listContent.sizeDelta = new Vector2(0f, _filtered.Count * RowHeight);
        _scroll.StopMovement();
        _listContent.anchoredPosition = Vector2.zero;
        _countText.text = $"{_filtered.Count} items";

        foreach (ItemRow row in _rows)
        {
            row.Index = -1;
        }
        UpdateVisibleRows();
    }

    private void UpdateVisibleRows()
    {
        if (_scroll == null || _scroll.viewport == null || _listContent == null)
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

        int first = Mathf.Max(0, Mathf.FloorToInt(_listContent.anchoredPosition.y / RowHeight));
        for (int index = first; index < first + _rows.Count; index++)
        {
            // Slotting by index keeps a row on its item while in view, so scrolling only
            // repopulates the rows entering at the edges.
            ItemRow row = _rows[index % _rows.Count];
            if (index >= _filtered.Count)
            {
                row.Index = -1;
                row.Root.gameObject.SetActive(false);
                continue;
            }
            if (row.Index != index)
            {
                row.Index = index;
                Populate(row, _filtered[index]);
                row.Root.anchoredPosition = new Vector2(0f, -index * RowHeight);
            }
            row.Root.gameObject.SetActive(true);
        }
    }

    private void RememberAmount(ItemRow row, string text)
    {
        if (row.Def != null)
        {
            _amounts[row.Def.id] = text;
        }
    }

    private void Populate(ItemRow row, Entry entry)
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

    private static void GiveItem(ItemRow row)
    {
        if (row.Def == null)
        {
            return;
        }
        PlayerData player = MainGame.PlayerData;
        if (player == null || player.Inventory == null)
        {
            Plugin.Logger.LogWarning("[Items] can't give — no active game/player.");
            return;
        }
        int amount = 1;
        if (int.TryParse(row.Amount.text, out int parsed) && parsed > 0)
        {
            amount = parsed;
        }
        player.Inventory.AddItemToInventory(new Item(row.Def.id, amount));
        Plugin.Logger.LogInfo($"[Items] gave {amount}x {row.Def.id}.");
    }

    private struct Entry
    {
        public ItemDef Def;
        public string Name;
        public string LowerId;
        public string LowerName;
        public ItemType Type;
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
