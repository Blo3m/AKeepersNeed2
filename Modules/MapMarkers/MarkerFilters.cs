using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// The ticked legend types, stored per profile as a comma-separated list of type keys. Everything
/// starts unticked. Re-read whenever the profile's value changes (e.g. a profile switch).
/// </summary>
internal static class MarkerFilters
{
    private static readonly HashSet<string> Shown = new HashSet<string>(StringComparer.Ordinal);
    private static ConfigEntry<string> _entry;
    private static string _parsed;

    public static void Bind(ConfigEntry<string> entry)
    {
        _entry = entry;
    }

    public static bool IsShown(string key)
    {
        Sync();
        return Shown.Contains(key);
    }

    public static void Set(IEnumerable<string> keys, bool shown)
    {
        Sync();
        foreach (string key in keys)
        {
            if (shown)
            {
                Shown.Add(key);
            }
            else
            {
                Shown.Remove(key);
            }
        }
        _parsed = string.Join(",", Shown.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        _entry.Value = _parsed;
    }

    private static void Sync()
    {
        string value = _entry?.Value ?? string.Empty;
        if (value == _parsed)
        {
            return;
        }
        _parsed = value;
        Shown.Clear();
        foreach (string key in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Shown.Add(key.Trim());
        }
    }
}
