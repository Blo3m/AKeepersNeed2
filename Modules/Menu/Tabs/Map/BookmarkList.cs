using System;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Bookmarks;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Map;

/// <summary>
/// The Map tab's Bookmarks section: "Save Bookmark" (name prompt, saves where the player stands),
/// then one row per bookmark with its name, Go, Rename and Delete, and below that its scene and a
/// hotkey rebind. Go closes the menu first (it pauses the game) and teleports, across scenes too.
/// Delete asks first. Bookmarks belong to the profile, not the save, so nothing here needs the
/// save-change warning. Rebuilt on every <see cref="Reload"/>.
/// </summary>
internal sealed class BookmarkList
{
    private const float RowHeight = 62f;
    private const float RowGap = 6f;

    private readonly MenuPage _page;
    private readonly AKNMenuWindow _window;
    private readonly RectTransform _list;

    public BookmarkList(MenuPage page, AKNMenuWindow window)
    {
        _page = page;
        _window = window;
        page.ButtonRow("Current Location", "Save Bookmark", SaveCurrent);
        _list = page.Band(0f, 6f);
    }

    public void Reload()
    {
        for (int i = _list.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(_list.GetChild(i).gameObject);
        }
        float y = 0f;
        foreach (Bookmark bookmark in BookmarkStore.All)
        {
            BuildRow(bookmark, y);
            y -= RowHeight + RowGap;
        }
        if (y == 0f)
        {
            TextMeshProUGUI empty = MenuUi.CreateText("Empty", _list, 12f, TextAlignmentOptions.Left);
            MenuUi.ApplyLabelText(empty);
            MenuUi.SetRect(empty.rectTransform, Anchors.Top, new Vector2(4f, -24f), Vector2.zero);
            empty.text = "No bookmarks yet. Stand somewhere and press Save Bookmark.";
            y = -24f;
        }
        _page.SetBandHeight(_list, -y);
    }

    private void SaveCurrent()
    {
        if (MainGame.PlayerData == null)
        {
            Toast.Show("Load a game first");
            return;
        }
        _window.Dialogs.ShowNamePrompt(
            "Name this bookmark",
            BookmarkStore.SuggestName(),
            BookmarkStore.MaxNameLength,
            name =>
            {
                string error = BookmarkStore.AddCurrent(name);
                if (error == null)
                {
                    Toast.Show($"Bookmark {name.Trim()} saved");
                    Reload();
                }
                return error;
            }
        );
    }

    private void BuildRow(Bookmark bookmark, float top)
    {
        RectTransform row = MenuUi.CreateRect("Bookmark", _list);
        MenuUi.SetRect(row, Anchors.Top, new Vector2(0f, top - RowHeight), new Vector2(0f, top));

        Image background = MenuUi.CreateImage("Background", row, new Color(0f, 0f, 0f, 0.18f));
        MenuUi.Stretch(background.rectTransform);
        background.raycastTarget = false;

        TextMeshProUGUI name = MenuUi.CreateText("Name", row, 13f, TextAlignmentOptions.Left);
        MenuUi.SetRect(name.rectTransform, Anchors.Top, new Vector2(8f, -28f), new Vector2(-176f, -2f));
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.text = bookmark.Name;

        Button(row, "Go", -172f, -128f).onClick.AddListener(() => BookmarkTeleport.Go(bookmark, _window.Close));
        Button(row, "Rename", -124f, -64f).onClick.AddListener(() => _window.Dialogs.ShowNamePrompt(
            "Rename bookmark",
            bookmark.Name,
            BookmarkStore.MaxNameLength,
            text =>
            {
                string error = BookmarkStore.Rename(bookmark, text);
                if (error == null)
                {
                    Reload();
                }
                return error;
            }
        ));
        Button(row, "Delete", -60f, -4f).onClick.AddListener(() => _window.Dialogs.ShowConfirm(
            $"Delete {bookmark.Name}?",
            "The bookmark and its hotkey are removed from this profile.",
            "Delete",
            () =>
            {
                BookmarkStore.Delete(bookmark);
                Reload();
            }
        ));

        RectTransform keyBand = MenuUi.CreateRect("Key", row);
        MenuUi.SetRect(keyBand, Anchors.Top, new Vector2(8f, -58f), new Vector2(-4f, -32f));
        Action sync = _window.Rebinder.BuildRow(
            keyBand,
            bookmark.Scene,
            () => bookmark.Hotkey,
            key => BookmarkStore.SetHotkey(bookmark, key)
        );
        sync();
    }

    private static LazyButton Button(RectTransform row, string text, float left, float right)
    {
        LazyButton button = MenuUi.CreateButton(
            text,
            row,
            text,
            new Anchors(Vector2.one, Vector2.one),
            new Vector2(left, -28f),
            new Vector2(right, -3f)
        );
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.fontSize = 11f;
        }
        return button;
    }
}
