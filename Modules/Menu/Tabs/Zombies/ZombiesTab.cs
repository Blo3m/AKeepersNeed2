using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using AKeepersNeed2.Shared.Zombies;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// The Zombies tab. On top, a "Global settings" fold holding the rows the zombie modules declared
/// (speeds, max mastery, porter capacity). Below it, a search box, a job filter and the
/// <see cref="ZombieList"/> of placed zombies, sorted by zone then name. The list is reloaded
/// from the save whenever the menu opens (and with the refresh button).
/// </summary>
internal sealed class ZombiesTab : IMenuTab
{
    private const float HeaderHeight = 28f;
    private const float FilterHeight = 64f;

    private readonly AKNMenuWindow _window;
    private readonly Dictionary<ZombieWgoData, ZombieEntry> _entries =
        new Dictionary<ZombieWgoData, ZombieEntry>();
    private readonly ZombieType?[] _jobs;

    private MenuPage _settings;
    private RectTransform _settingsGroup;
    private RectTransform _lower;
    private TextMeshProUGUI _foldLabel;
    private TMP_InputField _search;
    private TextMeshProUGUI _jobLabel;
    private TextMeshProUGUI _countText;
    private ZombieList _list;
    private bool _settingsOpen;
    private int _jobIndex;

    public ZombiesTab(AKNMenuWindow window)
    {
        _window = window;
        var jobs = new List<ZombieType?> { null };
        foreach (ZombieType type in Enum.GetValues(typeof(ZombieType)))
        {
            jobs.Add(type);
        }
        _jobs = jobs.ToArray();
    }

    public string Title => "Zombies";

    public void Build(RectTransform content)
    {
        LazyButton fold = MenuUi.CreateButton(
            "SettingsFold",
            content,
            string.Empty,
            Anchors.Top,
            new Vector2(0f, -HeaderHeight),
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
            new Vector2(0f, -HeaderHeight)
        );
        _settings = new MenuPage(_settingsGroup);
        RegistrySections.Build(_settings, MenuTab.Zombies, _window);

        _lower = MenuUi.CreateRect("Zombies", content);
        BuildFilters(_lower);
        RectTransform listArea = MenuUi.CreateRect("List", _lower);
        MenuUi.SetRect(listArea, Anchors.Fill, Vector2.zero, new Vector2(0f, -FilterHeight));
        _list = new ZombieList(listArea, _window.Dialogs, OpenInGame);

        Layout();
    }

    public void Refresh()
    {
        _settings?.Sync();
        Reload();
    }

    /// <summary>Settings fold open: top half settings, bottom half list. Closed: list fills it all.</summary>
    private void Layout()
    {
        if (_foldLabel != null)
        {
            _foldLabel.text = _settingsOpen ? "▾ Global settings" : "▸ Global settings";
        }
        _settingsGroup.gameObject.SetActive(_settingsOpen);
        if (_settingsOpen)
        {
            MenuUi.SetRect(
                _lower,
                new Anchors(Vector2.zero, new Vector2(1f, 0.5f)),
                Vector2.zero,
                new Vector2(0f, -6f)
            );
        }
        else
        {
            MenuUi.SetRect(_lower, Anchors.Fill, Vector2.zero, new Vector2(0f, -HeaderHeight - 6f));
        }
    }

    private void BuildFilters(RectTransform parent)
    {
        _search = MenuUi.CreateInputField("Search", parent, "Search name…", 12f);
        MenuUi.SetRect((RectTransform)_search.transform, Anchors.Top, new Vector2(0f, -28f), Vector2.zero);
        _search.onValueChanged.AddListener(_ => Refilter());

        RectTransform row = MenuUi.CreateRect("JobRow", parent);
        MenuUi.SetRect(row, Anchors.Top, new Vector2(0f, -60f), new Vector2(0f, -32f));

        LazyButton reload = MenuUi.CreateButton(
            "Reload",
            row,
            "↻",
            Anchors.Right,
            new Vector2(-28f, 0f),
            Vector2.zero
        );
        reload.onClick.AddListener(Reload);

        _countText = MenuUi.CreateText("Count", row, 11f, TextAlignmentOptions.Right);
        MenuUi.ApplyLabelText(_countText);
        MenuUi.SetRect(_countText.rectTransform, Anchors.Right, new Vector2(-104f, 0f), new Vector2(-32f, 0f));

        RectTransform cycler = MenuUi.CreateRect("Job", row);
        MenuUi.SetRect(cycler, Anchors.Fill, Vector2.zero, new Vector2(-108f, 0f));

        Image field = MenuUi.CreateImage("Field", cycler, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _jobLabel = MenuUi.CreateText("Value", cycler, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_jobLabel);
        MenuUi.SetRect(_jobLabel.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        _jobLabel.text = JobLabel();

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<", Anchors.Left, Vector2.zero, new Vector2(26f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">", Anchors.Right, new Vector2(-26f, 0f), Vector2.zero);
        prev.onClick.AddListener(() => CycleJob(-1));
        next.onClick.AddListener(() => CycleJob(1));
    }

    private string JobLabel()
    {
        ZombieType? job = _jobs[_jobIndex];
        return job.HasValue
            ? ZombieLabels.Job(job.Value)
            : "All jobs";
    }

    private void CycleJob(int step)
    {
        _jobIndex = (_jobIndex + step + _jobs.Length) % _jobs.Length;
        _jobLabel.text = JobLabel();
        Refilter();
    }

    /// <summary>Syncs the entries with the placed zombies, keeping existing entries' state.</summary>
    private void Reload()
    {
        var placed = new HashSet<ZombieWgoData>(ZombieRoster.Placed());
        foreach (ZombieWgoData gone in _entries.Keys.Where(zombie => !placed.Contains(zombie)).ToList())
        {
            ZombieList.DropEditor(_entries[gone]);
            _entries.Remove(gone);
        }
        foreach (ZombieWgoData zombie in placed)
        {
            if (_entries.TryGetValue(zombie, out ZombieEntry entry))
            {
                entry.RefreshLabels();
            }
            else
            {
                _entries[zombie] = new ZombieEntry(zombie);
            }
        }
        Refilter();
    }

    private void Refilter()
    {
        if (_list == null)
        {
            return;
        }
        string query = _search != null ? _search.text.Trim() : string.Empty;
        ZombieType? job = _jobs[_jobIndex];
        List<ZombieEntry> shown = _entries.Values
            .Where(entry => job == null || entry.Zombie.ZombieType == job.Value)
            .Where(entry => query.Length == 0 || entry.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(entry => entry.Zone, StringComparer.CurrentCulture)
            .ThenBy(entry => entry.Name, StringComparer.CurrentCulture)
            .ToList();
        var shownSet = new HashSet<ZombieEntry>(shown);
        _countText.text = shown.Count == _entries.Count
            ? $"{shown.Count} zombies"
            : $"{shown.Count} of {_entries.Count}";
        _list.SetEntries(shown, _entries.Values.Where(entry => !shownSet.Contains(entry)));
    }

    private void OpenInGame(ZombieEntry entry)
    {
        UIZombieWorkerWindow window = LazyUI.GetWindow<UIZombieWorkerWindow>();
        if (window == null)
        {
            Toast.Show("Can't open the zombie window");
            return;
        }
        _window.Close();
        window.Open(new UIZombieWorkerWindowData(entry.Zombie));
    }
}
