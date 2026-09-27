using System;
using System.Collections.Generic;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// The zombie list: a virtualized scroll list whose entries have different heights (a row, plus
/// the editor while expanded, several at once). Each entry's top is recomputed on every layout
/// change; only rows in view get a pooled <see cref="ZombieRow"/>, and an expanded editor is
/// hidden while it's scrolled out of view.
/// </summary>
internal sealed class ZombieList
{
    private const float EditorGap = 2f;

    private readonly MenuDialogHost _dialogs;
    private readonly Action<ZombieEntry> _onOpen;
    private readonly ScrollRect _scroll;
    private readonly RectTransform _content;
    private readonly List<ZombieRow> _pool = new List<ZombieRow>();
    private readonly List<float> _tops = new List<float>();
    private IReadOnlyList<ZombieEntry> _entries = Array.Empty<ZombieEntry>();

    public ZombieList(RectTransform area, MenuDialogHost dialogs, Action<ZombieEntry> onOpen)
    {
        _dialogs = dialogs;
        _onOpen = onOpen;

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
        viewport.gameObject.AddComponent<ResizeNotifier>().Resized += UpdateVisible;
        _scroll.viewport = viewport;

        _content = MenuUi.CreateRect("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.offsetMin = Vector2.zero;
        _content.offsetMax = Vector2.zero;
        _scroll.content = _content;
        _scroll.onValueChanged.AddListener(_ => UpdateVisible());
        _ = new ScrollHints(_scroll, up, down);
    }

    /// <summary>Shows <paramref name="entries"/> (already filtered and sorted).</summary>
    public void SetEntries(IReadOnlyList<ZombieEntry> entries, IEnumerable<ZombieEntry> hidden)
    {
        _entries = entries;
        foreach (ZombieEntry entry in hidden)
        {
            if (entry.Editor != null)
            {
                entry.Editor.Root.gameObject.SetActive(false);
            }
        }
        Relayout();
    }

    /// <summary>Destroys an entry's editor UI (its staged edits stay on the entry).</summary>
    public static void DropEditor(ZombieEntry entry)
    {
        if (entry.Editor != null)
        {
            UnityEngine.Object.Destroy(entry.Editor.Root.gameObject);
            entry.Editor = null;
        }
    }

    public void Toggle(ZombieEntry entry)
    {
        entry.Expanded = !entry.Expanded;
        if (!entry.Expanded)
        {
            DropEditor(entry);
        }
        Relayout();
    }

    /// <summary>Recomputes every entry's top and the content height, then refreshes what's in view.</summary>
    public void Relayout()
    {
        _tops.Clear();
        float y = 0f;
        foreach (ZombieEntry entry in _entries)
        {
            _tops.Add(y);
            y += ZombieRow.Height;
            if (entry.Expanded)
            {
                y += EnsureEditor(entry).Height + EditorGap;
            }
        }
        _content.sizeDelta = new Vector2(0f, y);
        UpdateVisible();
    }

    private ZombieEditor EnsureEditor(ZombieEntry entry)
    {
        if (entry.Editor != null)
        {
            return entry.Editor;
        }
        entry.Edit = entry.Edit ?? new ZombieEdit(entry.Zombie);
        RectTransform root = MenuUi.CreateRect("ZombieEditor", _content);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        // The editor reports its height while building; the list relayouts once it's attached.
        entry.Editor = new ZombieEditor(
            root,
            entry.Edit,
            _dialogs,
            () => OnEditorResized(entry),
            () => RefreshRow(entry)
        );
        return entry.Editor;
    }

    private void OnEditorResized(ZombieEntry entry)
    {
        if (entry.Editor != null)
        {
            Relayout();
        }
    }

    private void RefreshRow(ZombieEntry entry)
    {
        foreach (ZombieRow row in _pool)
        {
            if (row.Entry == entry)
            {
                row.Bind(entry);
            }
        }
    }

    private void UpdateVisible()
    {
        if (_scroll == null || _scroll.viewport == null)
        {
            return;
        }
        float top = _content.anchoredPosition.y;
        float bottom = top + _scroll.viewport.rect.height;
        var visible = new HashSet<ZombieEntry>();

        for (int i = 0; i < _entries.Count; i++)
        {
            ZombieEntry entry = _entries[i];
            float rowTop = _tops[i];
            if (rowTop < bottom && rowTop + ZombieRow.Height > top)
            {
                visible.Add(entry);
            }
            if (entry.Editor != null)
            {
                float editorTop = rowTop + ZombieRow.Height;
                bool inView = editorTop < bottom && editorTop + entry.Editor.Height > top;
                entry.Editor.Root.anchoredPosition = new Vector2(0f, -editorTop);
                entry.Editor.Root.gameObject.SetActive(inView);
            }
        }

        foreach (ZombieRow row in _pool)
        {
            if (row.Entry != null && !visible.Contains(row.Entry))
            {
                row.Unbind();
            }
        }
        for (int i = 0; i < _entries.Count; i++)
        {
            ZombieEntry entry = _entries[i];
            if (!visible.Contains(entry))
            {
                continue;
            }
            ZombieRow row = RowFor(entry);
            row.Bind(entry);
            row.Root.anchoredPosition = new Vector2(0f, -_tops[i]);
            row.Root.gameObject.SetActive(true);
        }
    }

    private ZombieRow RowFor(ZombieEntry entry)
    {
        ZombieRow free = null;
        foreach (ZombieRow row in _pool)
        {
            if (row.Entry == entry)
            {
                return row;
            }
            if (free == null && row.Entry == null)
            {
                free = row;
            }
        }
        if (free != null)
        {
            return free;
        }
        var created = new ZombieRow(_content, Toggle, _onOpen);
        _pool.Add(created);
        return created;
    }
}
