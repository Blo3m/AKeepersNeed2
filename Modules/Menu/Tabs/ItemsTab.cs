using System.Collections.Generic;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Item adder: a search box and category picker over the <see cref="ItemCatalog"/>, feeding
/// the virtualized <see cref="ItemList"/>. Give adds the item to the player's inventory and
/// toasts the result, with a running total while the same item's toast is still showing.
/// </summary>
internal sealed class ItemsTab : IMenuTab
{
    private readonly List<ItemCatalog.Entry> _filtered = new List<ItemCatalog.Entry>();

    private ItemCatalog _catalog;
    private ItemList _list;
    private TMP_InputField _search;
    private TextMeshProUGUI _countText;
    private TextMeshProUGUI _categoryLabel;
    private int _categoryIndex;
    private int _giveTotal;

    public string Title => "Items";

    public void Build(RectTransform content)
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

    // Nothing here is backed by config.
    public void Refresh()
    {
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
