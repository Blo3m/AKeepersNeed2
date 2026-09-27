using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Progression;
using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// The World tab: the Time rows from the WorldTime module, plus a hand-built Quality section with
/// a setter per zone that shows a quality (<see cref="ZoneQuality"/>) and one for the town. The
/// zones come from the loaded save, so the rows are rebuilt on every <see cref="Refresh"/>.
/// Setting quality writes the save and can pay milestone rewards, so each confirm says so.
/// </summary>
internal sealed class WorldTab : IMenuTab
{
    private const float RowHeight = 30f;
    private const float RowGap = 8f;
    private const string QualityWarning =
        "Going above the zone's best quality ever pays its milestone rewards once (graveyard 200 "
        + "also unlocks an achievement). Lowering it never takes rewards back.";

    private readonly AKNMenuWindow _window;
    private MenuPage _page;
    private RectTransform _quality;

    public WorldTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "World";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        var handBuilt = new Dictionary<MenuSection, Action>
        {
            [MenuSection.ZoneQuality] = () => _quality = _page.Band(0f, 8f),
        };
        RegistrySections.Build(_page, MenuTab.World, _window, handBuilt);
    }

    public void Refresh()
    {
        _page?.Sync();
        RebuildQuality();
    }

    private void RebuildQuality()
    {
        if (_quality == null)
        {
            return;
        }
        for (int i = _quality.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(_quality.GetChild(i).gameObject);
        }

        float cursor = 0f;
        RectTransform NextRow()
        {
            if (cursor < 0f)
            {
                cursor -= RowGap;
            }
            RectTransform row = MenuUi.CreateRect("Zone", _quality);
            MenuUi.SetRect(row, Anchors.Top, new Vector2(0f, cursor - RowHeight), new Vector2(0f, cursor));
            cursor -= RowHeight;
            return row;
        }

        foreach (WorldZoneData zone in ZoneQuality.Zones())
        {
            WorldZoneData target = zone;
            ValueRow.Build(
                NextRow(),
                _window.Dialogs,
                $"{ZoneQuality.Name(zone)} Quality",
                _ => ZoneQuality.Total(target),
                (_, value) => ZoneQuality.SetTotal(target, value),
                new ValueRow.Options { Warning = QualityWarning }
            );
        }
        if (ZoneQuality.TownQuality() != null)
        {
            ValueRow.Build(
                NextRow(),
                _window.Dialogs,
                "Town Quality",
                _ => ZoneQuality.TownQuality(),
                (_, value) => ZoneQuality.SetTownQuality(value),
                new ValueRow.Options { Warning = "Town quality is also the happiness cap." }
            );
        }
        if (cursor == 0f)
        {
            EmptyLabel(NextRow(), "Load a game to set zone quality.");
        }
        _page.SetBandHeight(_quality, -cursor);
    }

    private static void EmptyLabel(RectTransform row, string text)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Empty", row, 12f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(label);
        MenuUi.Stretch(label.rectTransform);
        label.text = text;
    }
}
