using System;
using System.Collections.Generic;
using AKeepersNeed2.Shared.Profiles;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// "Label [Key]" rows that capture the next keypress to rebind a hotkey. Esc cancels and
/// Backspace/Delete unbinds. A key is only bound to one thing at a time: assigning it takes
/// it from the previous/next profile keys and every profile's switch key. The menu key is
/// the exception — it can't be unbound or taken, so the menu can't be locked out.
/// </summary>
internal sealed class KeyRebinder
{
    private static readonly KeyCode[] AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    private readonly List<Row> _rows = new List<Row>();
    private Row _listening;

    private sealed class Row
    {
        public TextMeshProUGUI Label;
        public Func<KeyCode> Get;
        public Action<KeyCode> Set;
        public bool IsMenuKey;

        public void Show()
        {
            if (Label != null)
            {
                Label.text = Get() == KeyCode.None ? "None" : Get().ToString();
            }
        }
    }

    /// <summary>Builds a rebind row into <paramref name="band"/>; returns its re-read action.</summary>
    public Action BuildRow(
        RectTransform band,
        string label,
        Func<KeyCode> get,
        Action<KeyCode> set,
        bool isMenuKey = false
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
            Get = get,
            Set = set,
            IsMenuKey = isMenuKey,
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
            Cancel();
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            if (_listening.IsMenuKey)
            {
                Cancel();
            }
            else
            {
                Assign(KeyCode.None);
            }
            return true;
        }
        foreach (KeyCode key in AllKeys)
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
            {
                continue;
            }
            int code = (int)key;
            if (code >= (int)KeyCode.Mouse0 && code <= (int)KeyCode.Mouse6)
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

        if (!row.IsMenuKey && key != KeyCode.None && key == ModConfig.MenuHotkey.Value)
        {
            Plugin.Logger.LogWarning($"[Menu] {key} is the menu key; pick another.");
            row.Show();
            return;
        }

        if (key != KeyCode.None && key != row.Get())
        {
            ReleaseEverywhere(key);
        }
        row.Set(key);

        foreach (Row other in _rows)
        {
            other.Show();
        }
    }

    private static void ReleaseEverywhere(KeyCode key)
    {
        if (ModConfig.PreviousProfileKey.Value == key)
        {
            ModConfig.PreviousProfileKey.Value = KeyCode.None;
        }
        if (ModConfig.NextProfileKey.Value == key)
        {
            ModConfig.NextProfileKey.Value = KeyCode.None;
        }
        foreach (Profile profile in ProfileStore.Profiles)
        {
            if (profile.Hotkey.Value == key)
            {
                profile.SetHotkey(KeyCode.None);
            }
        }
    }

    private void StopListening()
    {
        _listening = null;
        InputGate.KeyCaptureActive = false;
    }
}
