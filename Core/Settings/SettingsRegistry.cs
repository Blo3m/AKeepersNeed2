using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AKeepersNeed2.Core.Settings;

/// <summary>
/// Every menu row modules declared, plus section metadata. The menu builds its tabs from here
/// and key-conflict checks read <see cref="Hotkeys"/>, so neither references a module directly.
/// </summary>
internal static class SettingsRegistry
{
    private static readonly List<SettingRow> Rows = new List<SettingRow>();
    private static readonly Dictionary<MenuSection, SectionAttribute> Sections = LoadSections();

    public static IEnumerable<KeySettingRow> Hotkeys => Rows.OfType<KeySettingRow>();

    public static void Add(SettingRow row)
    {
        row.Sequence = Rows.Count;
        Rows.Add(row);
    }

    public static SectionAttribute Info(MenuSection section)
    {
        return Sections[section];
    }

    /// <summary>The tab's sections in display order.</summary>
    public static IEnumerable<MenuSection> SectionsIn(MenuTab tab)
    {
        return Sections
            .Where(pair => pair.Value.Tab == tab)
            .OrderBy(pair => pair.Value.Order)
            .Select(pair => pair.Key);
    }

    /// <summary>
    /// The section's rows in display order: by <see cref="SettingRow.Order"/>, then owning module,
    /// then declaration order, so ties still sort the same way every run.
    /// </summary>
    public static List<SettingRow> RowsIn(MenuSection section)
    {
        return Rows
            .Where(row => row.Section == section)
            .OrderBy(row => row.Order)
            .ThenBy(row => row.Owner, StringComparer.Ordinal)
            .ThenBy(row => row.Sequence)
            .ToList();
    }

    /// <summary>
    /// Logs rows sharing a section and order. The analyzer turns these into build errors, so this
    /// only fires if it was bypassed.
    /// </summary>
    public static void WarnAboutTies()
    {
        // An anonymous type, not a C# tuple: tuples need System.ValueTuple.dll, which the game's
        // Mono runtime doesn't ship for net461 plugins.
        foreach (var tie in Rows.GroupBy(row => new { row.Section, row.Order }))
        {
            if (tie.Count() > 1)
            {
                string rows = string.Join(", ", tie.Select(row => $"'{row.Label}' ({row.Owner})"));
                Plugin.Logger.LogWarning($"[Settings] {tie.Key.Section} order {tie.Key.Order} is shared by {rows}.");
            }
        }
    }

    private static Dictionary<MenuSection, SectionAttribute> LoadSections()
    {
        var sections = new Dictionary<MenuSection, SectionAttribute>();
        foreach (FieldInfo field in typeof(MenuSection).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var section = (MenuSection)field.GetValue(null);
            sections[section] = field.GetCustomAttribute<SectionAttribute>();
        }
        return sections;
    }
}
