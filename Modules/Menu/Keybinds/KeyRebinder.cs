using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Keybinds;

/// <summary>
/// "Label [Key]" rows that capture the next keypress to rebind a hotkey. Esc, Backspace and
/// Delete unbind (unbindable rows, like the menu key, cancel instead). Middle/extra mouse buttons can be bound;
/// left/right click can't.
/// A key is bound to one thing at a time (see <see cref="KeyConflicts"/>): if it's already used
/// by another mod hotkey, a Replace/Cancel dialog names it and Replace unbinds it there. If the
/// key is unbindable (the menu key) or a game action uses it, an OK message names it and nothing
/// changes. Unbindable keys can't be unbound either, so e.g. the menu can't be locked out.
/// </summary>
internal sealed class KeyRebinder
{
    private static readonly KeyCode[] AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    private readonly AKNMenuWindow _window;
    private readonly List<Row> _rows = new List<Row>();
    private Row _listening;
    private int _escHandledFrame = -1;

    public KeyRebinder(AKNMenuWindow window)
    {
        _window = window;
    }

    private sealed class Row
    {
        public TextMeshProUGUI Label;
        public Func<ConfigEntry<KeyCode>> Entry;
        public Action<KeyCode> Set;
        public bool IsUnbindable;

        public KeyCode Get()
        {
            return Entry()?.Value ?? KeyCode.None;
        }

        public void Show()
        {
            if (Label != null)
            {
                Label.text = KeyName(Get());
            }
        }
    }

    /// <summary>
    /// Builds a rebind row into <paramref name="band"/> for the hotkey stored in
    /// <paramref name="entry"/> (a getter, since e.g. the active profile's entry changes).
    /// <paramref name="set"/> defaults to writing the entry. Returns the row's re-read action.
    /// </summary>
    public Action BuildRow(
        RectTransform band,
        string label,
        Func<ConfigEntry<KeyCode>> entry,
        Action<KeyCode> set = null,
        bool isUnbindable = false
    )
    {
        TextMeshProUGUI name = MenuUi.CreateText("Label", band, 13f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(name);
        MenuUi.SetRect(name.rectTransform, Anchors.Fill, new Vector2(0f, 0f), new Vector2(-114f, 0f));
        name.text = label;

        LazyButton button = MenuUi.CreateButton(
            "Rebind",
            band,
            string.Empty,
            Anchors.Right,
            new Vector2(-110f, 2f),
            new Vector2(0f, -2f)
        );
        var row = new Row
        {
            Label = button.GetComponentInChildren<TextMeshProUGUI>(true),
            Entry = entry,
            Set = set ?? (key =>
            {
                ConfigEntry<KeyCode> target = entry();
                if (target != null)
                {
                    target.Value = key;
                }
            }),
            IsUnbindable = isUnbindable,
        };
        if (row.Label != null)
        {
            row.Label.fontSize = 12f;
        }
        button.onClick.AddListener(() => Begin(row));
        _rows.Add(row);

        row.Show();
        return row.Show;
    }

    /// <summary>Per-frame capture. Returns true while listening (the keypress is consumed).</summary>
    public bool Tick()
    {
        if (_listening == null)
        {
            return false;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _escHandledFrame = Time.frameCount;
            UnbindOrCancel();
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            UnbindOrCancel();
            return true;
        }
        foreach (KeyCode key in AllKeys)
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
            {
                continue;
            }
            // Left/right click would bind the click that pressed the Rebind button (or a stray
            // one); middle and extra mouse buttons are fair game.
            if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1)
            {
                continue;
            }
            if (Input.GetKeyDown(key))
            {
                Assign(key);
                return true;
            }
        }
        return true;
    }

    /// <summary>
    /// The window's Back (Esc / gamepad B) handler asks this first. During a capture Back
    /// unbinds the key (cancels for unbindable rows). It also claims the Back press when
    /// <see cref="Tick"/> already handled Esc earlier this frame (the two run in no fixed
    /// order), so the menu stays open.
    /// </summary>
    public bool HandleBack()
    {
        if (_listening != null)
        {
            UnbindOrCancel();
            return true;
        }
        return _escHandledFrame == Time.frameCount;
    }

    // Unbindable rows (e.g. the menu key) cancel instead, so they can never end up unbound.
    private void UnbindOrCancel()
    {
        if (_listening.IsUnbindable)
        {
            Cancel();
        }
        else
        {
            Assign(KeyCode.None);
        }
    }

    public void Cancel()
    {
        if (_listening == null)
        {
            return;
        }
        Row row = _listening;
        StopListening();
        row.Show();
    }

    private static string KeyName(KeyCode key)
    {
        return key == KeyCode.None ? "None" : key.ToString();
    }

    private void Begin(Row row)
    {
        Cancel();
        _listening = row;
        InputGate.KeyCaptureActive = true;
        if (row.Label != null)
        {
            row.Label.text = "Press a key…";
        }
    }

    private void Assign(KeyCode key)
    {
        Row row = _listening;
        StopListening();
        row.Show();

        if (key == row.Get())
        {
            return;
        }
        if (key == KeyCode.None)
        {
            Apply(row, key);
            return;
        }

        List<KeyConflict> conflicts = KeyConflicts.Find(key, row.Entry());
        if (conflicts.Count == 0)
        {
            Apply(row, key);
            return;
        }

        string names = Names(conflicts);
        // Unbindable rows (the menu key) and game actions can't be taken, so there's nothing to replace: just say so.
        if (conflicts.Any(c => !c.Replaceable))
        {
            _window.Dialogs.ShowMessage("Key in use", $"{KeyName(key)} is used by {names}.", 150f);
            return;
        }

        _window.Dialogs.ShowConfirm(
            "Key in use",
            $"{KeyName(key)} is used by {names}.\nReplace it? {names} will be unbound.",
            "Replace",
            () =>
            {
                foreach (KeyConflict conflict in conflicts)
                {
                    conflict.Unbind();
                }
                Apply(row, key);
            },
            160f
        );
    }

    private void Apply(Row row, KeyCode key)
    {
        row.Set(key);
        foreach (Row other in _rows)
        {
            other.Show();
        }
    }

    private static string Names(List<KeyConflict> conflicts)
    {
        return string.Join(", ", conflicts.Select(c => c.Name));
    }

    private void StopListening()
    {
        _listening = null;
        InputGate.KeyCaptureActive = false;
    }
}
