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
/// per-row amount field and a Give button that adds it to the player's inventory. Rows are
/// pooled and repopulated on filter changes to stay responsive across ~800 items.
/// </summary>
internal sealed class ItemsTab : IMenuTab
{
    private const int MaxRows = 100;
    private const float RowHeight = 40f;

    private readonly List<Entry> _all = new List<Entry>();
    private readonly List<Entry> _filtered = new List<Entry>();
    private readonly List<ItemType?> _catTypes = new List<ItemType?>();
    private readonly List<string> _catLabels = new List<string>();

    private ItemRow[] _rows;
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
        MenuUi.SetRect((RectTransform)_search.transform,
            new Vector2(0f, -28f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        _search.onValueChanged.AddListener(_ => Refilter());

        BuildCategoryRow(content);
        BuildList(content);
        BuildRowPool();
        Refilter();
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
        var rowGo = new GameObject("CategoryRow", typeof(RectTransform));
        var row = (RectTransform)rowGo.transform;
        row.SetParent(content, false);
        MenuUi.SetRect(row,
            new Vector2(0f, -60f), new Vector2(0f, -32f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        _countText = MenuUi.CreateText("Count", row, 11f, TextAlignmentOptions.Right, Color.white);
        MenuUi.ApplyLabelText(_countText);
        MenuUi.SetRect(_countText.rectTransform,
            new Vector2(-72f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f));

        var cyclerGo = new GameObject("Category", typeof(RectTransform));
        var cycler = (RectTransform)cyclerGo.transform;
        cycler.SetParent(row, false);
        MenuUi.SetRect(cycler,
            new Vector2(0f, 0f), new Vector2(-78f, 0f), Vector2.zero, Vector2.one);

        Image field = MenuUi.CreateImage("Field", cycler, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform,
            new Vector2(28f, 0f), new Vector2(-28f, 0f), Vector2.zero, Vector2.one);
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _categoryLabel = MenuUi.CreateText("Value", cycler, 12f, TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(_categoryLabel);
        MenuUi.SetRect(_categoryLabel.rectTransform,
            new Vector2(28f, 0f), new Vector2(-28f, 0f), Vector2.zero, Vector2.one);
        _categoryLabel.text = _catLabels[_categoryIndex];

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<",
            new Vector2(0f, 0f), new Vector2(26f, 0f), Vector2.zero, new Vector2(0f, 1f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">",
            new Vector2(-26f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.one);
        prev.onClick.AddListener(() => CycleCategory(-1));
        next.onClick.AddListener(() => CycleCategory(1));
    }

    private void BuildList(RectTransform content)
    {
        var scrollGo = new GameObject("ItemList", typeof(RectTransform));
        var scrollRect = (RectTransform)scrollGo.transform;
        scrollRect.SetParent(content, false);
        MenuUi.SetRect(scrollRect,
            new Vector2(0f, 0f), new Vector2(0f, -64f), Vector2.zero, Vector2.one);
        _scroll = scrollGo.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.scrollSensitivity = 24f;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        var viewport = (RectTransform)viewportGo.transform;
        viewport.SetParent(scrollRect, false);
        MenuUi.Stretch(viewport);
        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportGo.AddComponent<RectMask2D>();
        _scroll.viewport = viewport;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        _listContent = (RectTransform)contentGo.transform;
        _listContent.SetParent(viewport, false);
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.offsetMin = new Vector2(0f, 0f);
        _listContent.offsetMax = new Vector2(0f, 0f);
        _listContent.sizeDelta = new Vector2(0f, 0f);
        _scroll.content = _listContent;
    }

    private void BuildRowPool()
    {
        _rows = new ItemRow[MaxRows];
        for (int i = 0; i < MaxRows; i++)
        {
            _rows[i] = CreateRow(i);
        }
    }

    private ItemRow CreateRow(int index)
    {
        var row = new ItemRow();

        var go = new GameObject("Row", typeof(RectTransform));
        row.Root = (RectTransform)go.transform;
        row.Root.SetParent(_listContent, false);
        row.Root.anchorMin = new Vector2(0f, 1f);
        row.Root.anchorMax = new Vector2(1f, 1f);
        row.Root.pivot = new Vector2(0.5f, 1f);
        row.Root.sizeDelta = new Vector2(0f, RowHeight);
        row.Root.anchoredPosition = new Vector2(0f, -index * RowHeight);

        row.Icon = MenuUi.CreateImage("Icon", row.Root, Color.white);
        MenuUi.SetRect(row.Icon.rectTransform,
            new Vector2(2f, 6f), new Vector2(30f, -6f), new Vector2(0f, 0f), new Vector2(0f, 1f));
        row.Icon.raycastTarget = false;
        row.Icon.preserveAspect = true;

        row.Name = MenuUi.CreateText("Name", row.Root, 12f, TextAlignmentOptions.BottomLeft, Color.white);
        MenuUi.SetRect(row.Name.rectTransform,
            new Vector2(36f, 0f), new Vector2(-88f, -1f), new Vector2(0f, 0.5f), new Vector2(1f, 1f));

        row.Id = MenuUi.CreateText("Id", row.Root, 10f, TextAlignmentOptions.TopLeft, Color.white);
        MenuUi.ApplyLabelText(row.Id);
        MenuUi.SetRect(row.Id.rectTransform,
            new Vector2(36f, 1f), new Vector2(-88f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0.5f));

        row.Amount = MenuUi.CreateInputField("Amount", row.Root, "1", 12f,
            TMP_InputField.ContentType.IntegerNumber);
        MenuUi.SetRect((RectTransform)row.Amount.transform,
            new Vector2(-84f, 6f), new Vector2(-48f, -6f), new Vector2(1f, 0f), new Vector2(1f, 1f));
        row.Amount.text = "1";

        LazyButton give = MenuUi.CreateButton("Give", row.Root, "Give",
            new Vector2(-44f, 6f), new Vector2(-4f, -6f), new Vector2(1f, 0f), new Vector2(1f, 1f));
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

        int shown = Mathf.Min(_filtered.Count, MaxRows);
        for (int i = 0; i < _rows.Length; i++)
        {
            if (i < shown)
            {
                Populate(_rows[i], _filtered[i]);
                _rows[i].Root.gameObject.SetActive(true);
            }
            else
            {
                _rows[i].Root.gameObject.SetActive(false);
            }
        }

        _listContent.sizeDelta = new Vector2(0f, shown * RowHeight);
        _scroll.verticalNormalizedPosition = 1f;
        _countText.text = _filtered.Count > MaxRows
            ? $"{MaxRows} of {_filtered.Count}"
            : $"{_filtered.Count} items";
    }

    private static void Populate(ItemRow row, Entry entry)
    {
        row.Def = entry.Def;
        row.Name.text = entry.Name;
        row.Id.text = entry.Def.id;

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
    }
}
