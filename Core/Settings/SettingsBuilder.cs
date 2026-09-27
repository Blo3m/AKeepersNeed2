using System;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Core.Settings;

/// <summary>
/// Passed to <see cref="ISettingsDeclarer.DeclareSettings"/>. Binds config entries into the
/// live profile file (<see cref="Profile{T}"/>) or the main config (<see cref="Global{T}"/>),
/// and declares menu rows into <see cref="SettingsRegistry"/>. The AKeepersNeed2.Analyzers
/// project checks these calls at compile time: config section/key, row section, order and label
/// must be constants, and duplicates fail the build.
/// </summary>
internal sealed class SettingsBuilder
{
    private readonly string _owner;

    public SettingsBuilder(string owner)
    {
        _owner = owner;
    }

    /// <summary>A per-profile setting (switches with the active profile).</summary>
    public ConfigEntry<T> Profile<T>(
        string section,
        string key,
        T defaultValue,
        string description,
        AcceptableValueBase acceptableValues = null
    )
    {
        return ModConfig.ProfileSettings.Bind(
            section,
            key,
            defaultValue,
            new ConfigDescription(description, acceptableValues)
        );
    }

    /// <summary>A global setting in the plugin's main .cfg (the same for every profile).</summary>
    public ConfigEntry<T> Global<T>(
        string section,
        string key,
        T defaultValue,
        string description,
        AcceptableValueBase acceptableValues = null
    )
    {
        return ModConfig.Main.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));
    }

    public void Toggle(
        MenuSection section,
        int order,
        string label,
        ConfigEntry<bool> entry,
        Func<bool> enabledWhen = null
    )
    {
        Add(new ToggleSettingRow { Entry = entry, EnabledWhen = enabledWhen }, section, order, label);
    }

    public void Slider(MenuSection section, int order, ConfigEntry<float> entry, float min, float max, string format)
    {
        var row = new SliderSettingRow { Entry = entry, Min = min, Max = max, Format = format };
        Add(row, section, order, string.Empty);
    }

    public void Key(
        MenuSection section,
        int order,
        string label,
        ConfigEntry<KeyCode> entry,
        bool isUnbindable = false,
        Func<bool> enabledWhen = null
    )
    {
        var row = new KeySettingRow { Entry = entry, IsUnbindable = isUnbindable, EnabledWhen = enabledWhen };
        Add(row, section, order, label);
    }

    /// <summary>
    /// A cycler over every value of <typeparamref name="T"/>, shown with spaced names
    /// ("HoldKey" → "Hold Key").
    /// </summary>
    public void Choice<T>(
        MenuSection section,
        int order,
        string label,
        ConfigEntry<T> entry,
        Func<bool> enabledWhen = null
    )
        where T : struct, Enum
    {
        var values = (T[])Enum.GetValues(typeof(T));
        var row = new ChoiceSettingRow
        {
            Current = () => SpaceWords(entry.Value.ToString()),
            Step = delta =>
            {
                int index = Array.IndexOf(values, entry.Value);
                entry.Value = values[((index + delta) % values.Length + values.Length) % values.Length];
            },
            EnabledWhen = enabledWhen,
        };
        Add(row, section, order, label);
    }

    private static string SpaceWords(string name)
    {
        var spaced = new StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                spaced.Append(' ');
            }
            spaced.Append(name[i]);
        }
        return spaced.ToString();
    }

    private void Add(SettingRow row, MenuSection section, int order, string label)
    {
        row.Section = section;
        row.Order = order;
        row.Label = label;
        row.Owner = _owner;
        SettingsRegistry.Add(row);
    }
}
