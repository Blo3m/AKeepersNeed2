using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// Builds a tab's sections from <see cref="SettingsRegistry"/>: every section of the tab in its
/// declared order, each with a header, then any content the tab builds by hand for it, then
/// the rows modules declared there. Sections with neither are skipped.
/// </summary>
internal static class RegistrySections
{
    public static void Build(
        MenuPage page,
        MenuTab tab,
        AKNMenuWindow window,
        IReadOnlyDictionary<MenuSection, Action> handBuilt = null
    )
    {
        foreach (MenuSection section in SettingsRegistry.SectionsIn(tab))
        {
            Action content = null;
            handBuilt?.TryGetValue(section, out content);
            List<SettingRow> rows = SettingsRegistry.RowsIn(section);
            if (content == null && rows.Count == 0)
            {
                continue;
            }

            page.SectionHeader(SettingsRegistry.Info(section).Title);
            content?.Invoke();
            foreach (SettingRow row in rows)
            {
                BuildRow(page, window, row);
            }
        }
    }

    private static void BuildRow(MenuPage page, AKNMenuWindow window, SettingRow row)
    {
        switch (row)
        {
            case ToggleSettingRow toggle:
                // Re-sync so rows greyed out by this toggle (EnabledWhen) update at once.
                page.ToggleRow(
                    toggle.Label,
                    () => toggle.Entry.Value,
                    value =>
                    {
                        toggle.Entry.Value = value;
                        page.Sync();
                    },
                    toggle.EnabledWhen
                );
                break;
            case SliderSettingRow slider:
                page.SliderRow(
                    slider.Min,
                    slider.Max,
                    slider.Format,
                    () => slider.Entry.Value,
                    value => slider.Entry.Value = value
                );
                break;
            case ChoiceSettingRow choice:
                page.ChoiceRow(
                    choice.Label,
                    choice.Current,
                    delta =>
                    {
                        choice.Step(delta);
                        page.Sync();
                    },
                    choice.EnabledWhen
                );
                break;
            case ButtonSettingRow button:
                page.ButtonRow(
                    button.Label,
                    button.ButtonText,
                    () => window.Dialogs.ShowConfirm(
                        button.ConfirmTitle,
                        button.ConfirmMessage(),
                        button.ButtonText,
                        () =>
                        {
                            button.Action();
                            page.Sync();
                        }
                    ),
                    button.EnabledWhen
                );
                break;
            case KeySettingRow key:
                RectTransform band = page.Band(30f, 6f);
                page.GreyOutUnless(band, key.EnabledWhen);
                page.AddSync(window.Rebinder.BuildRow(
                    band,
                    key.Label,
                    () => key.Entry,
                    isUnbindable: key.IsUnbindable
                ));
                break;
            default:
                Plugin.Logger.LogWarning($"[Menu] no row builder for {row.GetType().Name} ('{row.Label}').");
                break;
        }
    }
}
