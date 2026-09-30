using System;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Core.Settings;

/// <summary>A menu row a module declared for one of its settings.</summary>
internal abstract class SettingRow
{
    public MenuSection Section { get; set; }

    /// <summary>Sort order within the section.</summary>
    public int Order { get; set; }

    /// <summary>Row text; empty for rows without one (sliders sit under their toggle).</summary>
    public string Label { get; set; }

    /// <summary>Name of the module that declared the row.</summary>
    public string Owner { get; set; }

    /// <summary>Declaration sequence, the last tie-breaker for a stable order.</summary>
    public int Sequence { get; set; }
}

/// <summary>An On/Off row. Greyed out while <see cref="EnabledWhen"/> returns false.</summary>
internal sealed class ToggleSettingRow : SettingRow
{
    public ConfigEntry<bool> Entry { get; set; }

    public Func<bool> EnabledWhen { get; set; }
}

/// <summary>
/// A full-width slider with its value shown in <see cref="Format"/>. Greyed out while
/// <see cref="EnabledWhen"/> returns false, hidden while <see cref="VisibleWhen"/> does (e.g. one
/// slider per mode of a choice row, only the current mode's shown).
/// </summary>
internal sealed class SliderSettingRow : SettingRow
{
    public ConfigEntry<float> Entry { get; set; }

    public float Min { get; set; }

    public float Max { get; set; }

    public string Format { get; set; }

    public Func<bool> EnabledWhen { get; set; }

    public Func<bool> VisibleWhen { get; set; }
}

/// <summary>
/// A `&lt; Value &gt;` cycler over an enum setting's values. Greyed out while
/// <see cref="EnabledWhen"/> returns false.
/// </summary>
internal sealed class ChoiceSettingRow : SettingRow
{
    /// <summary>The current value's display name.</summary>
    public Func<string> Current { get; set; }

    /// <summary>Moves to the next (+1) or previous (-1) value, wrapping around.</summary>
    public Action<int> Step { get; set; }

    public Func<bool> EnabledWhen { get; set; }
}

/// <summary>
/// A "Label [Button]" row for a one-shot action. An action that changes the save permanently has a
/// <see cref="ConfirmTitle"/>: the menu asks for confirmation first (<see cref="ConfirmTitle"/> /
/// <see cref="ConfirmMessage"/>) and runs <see cref="Action"/> on OK. Without one it runs at once.
/// </summary>
internal sealed class ButtonSettingRow : SettingRow
{
    public string ButtonText { get; set; }

    /// <summary>Null for an action that leaves no lasting change, which then skips the confirm.</summary>
    public string ConfirmTitle { get; set; }

    /// <summary>Built when pressed, so it can include current values.</summary>
    public Func<string> ConfirmMessage { get; set; }

    public Action Action { get; set; }

    public Func<bool> EnabledWhen { get; set; }
}

/// <summary>A rebindable hotkey. Every key row also takes part in key-conflict checks.</summary>
internal sealed class KeySettingRow : SettingRow
{
    public ConfigEntry<KeyCode> Entry { get; set; }

    /// <summary>
    /// True when the key can't be unbound (Esc/Backspace cancel instead) or taken by another row,
    /// only rebound to a different key. Used for the menu key, so the menu can't be locked out.
    /// </summary>
    public bool IsUnbindable { get; set; }

    /// <summary>Greys the row out while this returns false.</summary>
    public Func<bool> EnabledWhen { get; set; }
}
