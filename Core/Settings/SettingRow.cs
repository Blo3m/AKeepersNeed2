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

/// <summary>A full-width slider with its value shown in <see cref="Format"/>.</summary>
internal sealed class SliderSettingRow : SettingRow
{
    public ConfigEntry<float> Entry { get; set; }

    public float Min { get; set; }

    public float Max { get; set; }

    public string Format { get; set; }
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
}
