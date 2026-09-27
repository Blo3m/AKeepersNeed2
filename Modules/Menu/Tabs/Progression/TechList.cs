using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Progression;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs.Progression;

/// <summary>
/// The Progression tab's Tech section: Include Hidden / Include Reputation toggles (display
/// filters, not saved), a tab filter, a search box, Unlock Tab / Unlock All, and a list of every
/// tech with its state and its own Unlock button. The list lives in one page band that grows with
/// it; rows are created once per reload and only shown, hidden and moved when filtering. Every
/// unlock goes through a confirm (it's permanent) and <see cref="TechUnlocker"/>.
/// </summary>
internal sealed class TechList
{
    private const float RowHeight = 28f;

    private readonly MenuPage _page;
    private readonly MenuDialogHost _dialogs;
    private readonly RectTransform _list;
    private readonly List<Row> _rows = new List<Row>();
    private readonly TextMeshProUGUI _count;
    private readonly TMP_InputField _search;
    private readonly TechTreeTab?[] _tabs;
    private bool _includeHidden;
    private bool _includeReputation;
    private int _tabIndex;

    public TechList(MenuPage page, MenuDialogHost dialogs)
    {
        _page = page;
        _dialogs = dialogs;
        var tabs = new List<TechTreeTab?> { null };
        tabs.AddRange(System.Enum.GetValues(typeof(TechTreeTab)).Cast<TechTreeTab>().Select(tab => (TechTreeTab?)tab));
        _tabs = tabs.ToArray();

        page.ToggleRow("Include Hidden Techs", () => _includeHidden, on =>
        {
            _includeHidden = on;
            Refilter();
        });
        page.ToggleRow("Include Reputation Techs", () => _includeReputation, on =>
        {
            _includeReputation = on;
            Refilter();
        });
        page.ChoiceRow("Tech Tab", TabLabel, step =>
        {
            _tabIndex = (_tabIndex + step + _tabs.Length) % _tabs.Length;
            Refilter();
        });

        RectTransform searchBand = page.Band(28f, 8f);
        _search = MenuUi.CreateInputField("Search", searchBand, "Search techs…", 12f);
        MenuUi.Stretch((RectTransform)_search.transform);
        _search.onValueChanged.AddListener(_ => Refilter());

        RectTransform actions = page.Band(28f, 6f);
        _count = MenuUi.CreateText("Count", actions, 11f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(_count);
        MenuUi.SetRect(_count.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(-212f, 0f));
        LazyButton unlockTab = SmallButton(actions, "Unlock Tab", -208f, -108f);
        LazyButton unlockAll = SmallButton(actions, "Unlock All", -104f, 0f);
        unlockTab.onClick.AddListener(ConfirmUnlockTab);
        unlockAll.onClick.AddListener(ConfirmUnlockAll);

        _list = page.Band(0f, 6f);
    }

    /// <summary>Recreates the rows from the loaded save's techs.</summary>
    public void Reload()
    {
        List<TechDef> techs = TechUnlocker.Techs(null, includeHidden: true, includeReputation: true);
        while (_rows.Count < techs.Count)
        {
            _rows.Add(new Row(_list, ConfirmUnlockOne));
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].Def = i < techs.Count ? techs[i] : null;
            _rows[i].Name = i < techs.Count ? TechUnlocker.Name(techs[i]) : string.Empty;
        }
        Refilter();
    }

    private string TabLabel()
    {
        TechTreeTab? tab = _tabs[_tabIndex];
        return tab.HasValue
            ? TechUnlocker.TabName(tab.Value)
            : "All tabs";
    }

    private void Refilter()
    {
        string query = _search != null ? _search.text.Trim() : string.Empty;
        TechTreeTab? tab = _tabs[_tabIndex];
        float y = 0f;
        int shown = 0;
        int locked = 0;
        foreach (Row row in _rows)
        {
            TechDef def = row.Def;
            bool visible = def != null
                && (tab == null || def.tab == tab.Value)
                && (_includeReputation || def.techDefType == TechDefType.Common)
                && (_includeHidden || def.TechState != TechState.Hidden)
                && (query.Length == 0 || row.Name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0);
            row.Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }
            row.Refresh();
            row.Root.anchoredPosition = new Vector2(0f, y);
            y -= RowHeight;
            shown++;
            if (!TechUnlocker.IsUnlocked(def))
            {
                locked++;
            }
        }
        _count.text = shown == 0 && _rows.All(row => row.Def == null)
            ? "Load a game to see techs."
            : $"{shown} techs, {locked} locked";
        _page.SetBandHeight(_list, -y);
    }

    private void ConfirmUnlockOne(TechDef def)
    {
        string name = TechUnlocker.Name(def);
        Confirm($"Unlock {name}?", "It", new[] { def });
    }

    private void ConfirmUnlockTab()
    {
        TechTreeTab? tab = _tabs[_tabIndex];
        if (tab == null)
        {
            Toast.Show("Pick a tab first");
            return;
        }
        List<TechDef> techs = TechUnlocker.Techs(tab, _includeHidden, _includeReputation);
        Confirm($"Unlock the {TechUnlocker.TabName(tab.Value)} tab?", "Every tech in it", techs);
    }

    private void ConfirmUnlockAll()
    {
        List<TechDef> techs = TechUnlocker.Techs(null, _includeHidden, _includeReputation);
        Confirm("Unlock all techs?", "Every tech", techs);
    }

    private void Confirm(string title, string subject, IList<TechDef> techs)
    {
        int locked = techs.Count(def => !TechUnlocker.IsUnlocked(def));
        if (locked == 0)
        {
            Toast.Show("Nothing to unlock");
            return;
        }
        string count = techs.Count > 1 ? $" ({locked} techs)" : string.Empty;
        _dialogs.ShowConfirm(
            title,
            $"{subject}{count} is unlocked with its recipes, buildings and perks, and its unlock scripts "
                + "run. This changes your save permanently.",
            "Unlock",
            () =>
            {
                int unlocked = TechUnlocker.Unlock(techs);
                Toast.Show(unlocked == 1 ? "Unlocked 1 tech" : $"Unlocked {unlocked} techs");
                Refilter();
            },
            150f
        );
    }

    private static LazyButton SmallButton(RectTransform band, string text, float left, float right)
    {
        LazyButton button = MenuUi.CreateButton(
            text.Replace(" ", string.Empty),
            band,
            text,
            Anchors.Right,
            new Vector2(left, 2f),
            new Vector2(right, -2f)
        );
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.fontSize = 11f;
        }
        return button;
    }

    /// <summary>One tech: "Name · Tab [state]" and an Unlock button (gone once unlocked).</summary>
    private sealed class Row
    {
        private readonly TextMeshProUGUI _label;
        private readonly LazyButton _unlock;

        public Row(RectTransform parent, System.Action<TechDef> onUnlock)
        {
            Root = MenuUi.CreateRect("Tech", parent);
            Root.anchorMin = new Vector2(0f, 1f);
            Root.anchorMax = new Vector2(1f, 1f);
            Root.pivot = new Vector2(0.5f, 1f);
            Root.sizeDelta = new Vector2(0f, RowHeight);

            _label = MenuUi.CreateText("Label", Root, 12f, TextAlignmentOptions.Left);
            MenuUi.ApplyLabelText(_label);
            MenuUi.SetRect(_label.rectTransform, Anchors.Fill, new Vector2(4f, 0f), new Vector2(-84f, 0f));
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Ellipsis;

            _unlock = SmallButton(Root, "Unlock", -76f, 0f);
            _unlock.onClick.AddListener(() =>
            {
                if (Def != null)
                {
                    onUnlock(Def);
                }
            });
        }

        public RectTransform Root { get; }

        public TechDef Def { get; set; }

        public string Name { get; set; }

        public void Refresh()
        {
            if (Def == null)
            {
                return;
            }
            string state;
            switch (Def.TechState)
            {
                case TechState.Unlocked:
                    state = "unlocked";
                    break;
                case TechState.Hidden:
                    state = "hidden";
                    break;
                default:
                    state = "locked";
                    break;
            }
            string reputation = Def.techDefType == TechDefType.Common ? string.Empty : ", reputation";
            _label.text = $"{Name}  <size=10>· {TechUnlocker.TabName(Def.tab)} ({state}{reputation})</size>";
            _unlock.gameObject.SetActive(Def.TechState != TechState.Unlocked);
        }
    }
}
