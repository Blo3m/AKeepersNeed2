using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// The Items tab. On top, a "Stack sizes" fold holding the rows the StackSizes module declared.
/// Below it, the item adder: a search box and category picker over the <see cref="ItemCatalog"/>,
/// feeding the virtualized <see cref="ItemList"/>. Give adds the item to the player's inventory
/// and toasts the result, with a running total while the same item's toast is still showing.
/// </summary>
internal sealed class ItemsTab : IMenuTab
{
    private const float FoldHeight = 28f;

    private readonly List<ItemCatalog.Entry> _filtered = new List<ItemCatalog.Entry>();
    private readonly AKNMenuWindow _window;

    private ItemCatalog _catalog;
    private ItemList _list;
    private TMP_InputField _search;
    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _categoryLabel;
    private MenuPage _settings;
    private RectTransform _settingsGroup;
    private RectTransform _adder;
    private TextMeshProUGUI _foldLabel;
    private bool _settingsOpen;
    private int _categoryIndex;
    private int _giveTotal;

    public ItemsTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Items";

    public void Build(RectTransform content)
    {
        LazyButton fold = MenuUi.CreateButton(
            "SettingsFold",
            content,
            string.Empty,
            Anchors.Top,
            new Vector2(0f, -FoldHeight),
            Vector2.zero
        );
        _foldLabel = fold.GetComponentInChildren<TextMeshProUGUI>(true);
        if (_foldLabel != null)
        {
            _foldLabel.fontSize = 12f;
        }
        fold.onClick.AddListener(() =>
        {
            _settingsOpen = !_settingsOpen;
            Layout();
        });

        _settingsGroup = MenuUi.CreateRect("Settings", content);
        MenuUi.SetRect(
            _settingsGroup,
            new Anchors(new Vector2(0f, 0.5f), Vector2.one),
            Vector2.zero,
            new Vector2(0f, -FoldHeight)
        );
        _settings = new MenuPage(_settingsGroup);
        RegistrySections.Build(_settings, MenuTab.Items, _window);

        _adder = MenuUi.CreateRect("Adder", content);
        BuildAdder(_adder);
        Layout();
    }

    public void Refresh()
    {
        _settings?.Sync();
    }

    /// <summary>Fold open: top half settings, bottom half the adder. Closed: the adder fills it all.</summary>
    private void Layout()
    {
        if (_foldLabel != null)
        {
            _foldLabel.text = _settingsOpen ? "▾ Stack sizes" : "▸ Stack sizes";
        }
        _settingsGroup.gameObject.SetActive(_settingsOpen);
        if (_settingsOpen)
        {
            MenuUi.SetRect(
                _adder,
                new Anchors(Vector2.zero, new Vector2(1f, 0.5f)),
                Vector2.zero,
                new Vector2(0f, -6f)
            );
        }
        else
        {
            MenuUi.SetRect(_adder, Anchors.Fill, Vector2.zero, new Vector2(0f, -FoldHeight - 6f));
        }
    }

    private void BuildAdder(RectTransform content)
    {
        _catalog = new ItemCatalog();

        _search = MenuUi.CreateInputField("Search", content, "Search…", 12f);
        MenuUi.SetRect((RectTransform)_search.transform, Anchors.Top, new Vector2(0f, -28f), new Vector2(0f, 0f));
        _search.onValueChanged.AddListener(_ => Refilter());

        BuildCategoryRow(content);

        RectTransform listArea = MenuUi.CreateRect("ItemList", content);
        MenuUi.SetRect(listArea, Anchors.Fill, new Vector2(0f, 0f), new Vector2(0f, -64f));
        _list = new ItemList(listArea, GiveItem);

        Refilter();
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
        _categoryLabel.text = _catalog.CategoryLabel(_categoryIndex);

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<", Anchors.Left, Vector2.zero, new Vector2(26f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">", Anchors.Right, new Vector2(-26f, 0f), Vector2.zero);
        prev.onClick.AddListener(() => CycleCategory(-1));
        next.onClick.AddListener(() => CycleCategory(1));
    }

    private void CycleCategory(int step)
    {
        _categoryIndex = (_categoryIndex + step + _catalog.CategoryCount) % _catalog.CategoryCount;
        _categoryLabel.text = _catalog.CategoryLabel(_categoryIndex);
        Refilter();
    }

    private void Refilter()
    {
        string query = _search != null ? _search.text : string.Empty;
        _catalog.Filter(query, _categoryIndex, _filtered);
        _countText.text = $"{_filtered.Count} items";
        _list.SetItems(_filtered);
    }

    private void GiveItem(ItemDef def, string name, int amount)
    {
        PlayerData player = MainGame.PlayerData;
        if (player == null || player.Inventory == null)
        {
            Plugin.Logger.LogWarning("[Items] can't give — no active game/player.");
            Toast.Show("No active game");
            return;
        }
        var item = new Item(def.id, amount);
        if (!player.Inventory.AddItemToInventory(item))
        {
            Plugin.Logger.LogWarning($"[Items] couldn't give {amount}x {def.id} — inventory full.");
            Toast.Show("Inventory full");
            return;
        }
        // AddItemToInventory may add only part of the stack; the unadded rest stays on the source item.
        int added = amount - item.Count;
        Plugin.Logger.LogInfo($"[Items] gave {added}/{amount}x {def.id}.");

        string key = "give:" + def.id;
        _giveTotal = Toast.IsShowing(key)
            ? _giveTotal + added
            : added;
        Toast.Show(
            added < amount
                ? $"Added {_giveTotal}x {name} ({amount - added} not added)"
                : $"Added {_giveTotal}x {name}",
            key
        );
    }
}
