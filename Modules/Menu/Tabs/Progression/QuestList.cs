using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Progression;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs.Progression;

/// <summary>
/// The Progression tab's Quests section: one row per active quest (name, giver, what its hand-in
/// asks for, description) with <b>Give Items</b> (adds the hand-in's items; you finish it by talking
/// to the NPC) and <b>Force Complete</b> (strong warning: the hand-in dialogue is skipped and quest
/// chains can stall). Both are permanent and confirmed. Rebuilt on every reload.
/// </summary>
internal sealed class QuestList
{
    private const float RowHeight = 66f;
    private const float RowGap = 6f;

    private readonly MenuPage _page;
    private readonly MenuDialogHost _dialogs;
    private readonly RectTransform _list;

    public QuestList(MenuPage page, MenuDialogHost dialogs)
    {
        _page = page;
        _dialogs = dialogs;
        _list = page.Band(0f, 6f);
    }

    public void Reload()
    {
        for (int i = _list.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(_list.GetChild(i).gameObject);
        }
        float y = 0f;
        foreach (QuestData quest in QuestTools.Active())
        {
            BuildRow(quest, y);
            y -= RowHeight + RowGap;
        }
        if (y == 0f)
        {
            TextMeshProUGUI empty = MenuUi.CreateText("Empty", _list, 12f, TextAlignmentOptions.Left);
            MenuUi.ApplyLabelText(empty);
            MenuUi.SetRect(empty.rectTransform, Anchors.Top, new Vector2(4f, -24f), Vector2.zero);
            empty.text = MainGame.Instance?.GameSave == null
                ? "Load a game to see quests."
                : "No active quests.";
            y = -24f;
        }
        _page.SetBandHeight(_list, -y);
    }

    private void BuildRow(QuestData quest, float top)
    {
        RectTransform row = MenuUi.CreateRect("Quest", _list);
        MenuUi.SetRect(row, Anchors.Top, new Vector2(0f, top - RowHeight), new Vector2(0f, top));

        UnityEngine.UI.Image background = MenuUi.CreateImage("Background", row, new Color(0f, 0f, 0f, 0.18f));
        MenuUi.Stretch(background.rectTransform);
        background.raycastTarget = false;

        string name = QuestTools.Name(quest);
        string giver = QuestTools.Giver(quest);
        string needs = QuestTools.Requirements(quest);

        Line(row, name, 13f, 0f, 22f, header: true);
        string details = string.IsNullOrEmpty(giver) ? string.Empty : $"From {giver}";
        if (!string.IsNullOrEmpty(needs))
        {
            details += (details.Length > 0 ? " · " : string.Empty) + $"Needs {needs}";
        }
        Line(row, details, 10f, 22f, 16f);
        Line(row, quest.Description, 10f, 38f, 26f, wrap: true);

        LazyButton give = Button(row, "Give Items", 4f);
        LazyButton force = Button(row, "Force Complete", 34f);
        give.interactable = !string.IsNullOrEmpty(needs);
        give.onClick.AddListener(() => _dialogs.ShowConfirm(
            $"Give the hand-in for {name}?",
            $"Adds {needs} to your inventory. You still finish the quest by talking to "
                + $"{(string.IsNullOrEmpty(giver) ? "the quest giver" : giver)}. This changes your save.",
            "Give",
            () =>
            {
                Toast.Show(QuestTools.GiveRequirements(quest) ? $"Hand-in for {name} added" : "Nothing to give");
                Reload();
            },
            150f
        ));
        force.onClick.AddListener(() => _dialogs.ShowConfirm(
            $"Force complete {name}?",
            "The quest is marked complete and its finish scripts run, but its hand-in dialogue is skipped: "
                + "its rewards or the next quest in the chain may never come. This is permanent and can "
                + "break quest progress.",
            "Complete",
            () =>
            {
                QuestTools.ForceComplete(quest);
                Toast.Show($"{name} completed");
                Reload();
            },
            170f
        ));
    }

    private static void Line(
        RectTransform row,
        string text,
        float size,
        float top,
        float height,
        bool header = false,
        bool wrap = false
    )
    {
        TextMeshProUGUI label = MenuUi.CreateText("Line", row, size, TextAlignmentOptions.TopLeft);
        if (!header)
        {
            MenuUi.ApplyLabelText(label);
        }
        MenuUi.SetRect(label.rectTransform, Anchors.Top, new Vector2(8f, -top - height), new Vector2(-124f, -top - 2f));
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = text;
    }

    private static LazyButton Button(RectTransform row, string text, float top)
    {
        LazyButton button = MenuUi.CreateButton(
            text.Replace(" ", string.Empty),
            row,
            text,
            new Anchors(Vector2.one, Vector2.one),
            new Vector2(-116f, -top - 26f),
            new Vector2(-4f, -top)
        );
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.fontSize = 11f;
        }
        return button;
    }
}
