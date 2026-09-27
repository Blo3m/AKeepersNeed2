using System;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// A pooled list row: portrait, name (with a dot while it has unapplied changes), job · station ·
/// zone, an expand arrow and an "Open" button for the game's own zombie window. Clicking the row
/// expands or collapses the zombie's editor.
/// </summary>
internal sealed class ZombieRow
{
    public const float Height = 52f;
    private const float PortraitSize = 44f;

    private readonly ZombiePortrait _portrait;
    private readonly TextMeshProUGUI _name;
    private readonly TextMeshProUGUI _details;
    private readonly TextMeshProUGUI _arrow;

    public ZombieRow(RectTransform parent, Action<ZombieEntry> onToggle, Action<ZombieEntry> onOpen)
    {
        Root = MenuUi.CreateRect("ZombieRow", parent);
        Root.anchorMin = new Vector2(0f, 1f);
        Root.anchorMax = new Vector2(1f, 1f);
        Root.pivot = new Vector2(0.5f, 1f);
        Root.sizeDelta = new Vector2(0f, Height);

        Image background = MenuUi.CreateImage("Background", Root, new Color(0.25f, 0.12f, 0.07f, 0.6f));
        MenuUi.SetRect(background.rectTransform, Anchors.Fill, new Vector2(0f, 2f), new Vector2(0f, -2f));
        MenuUi.ApplyCell(background);
        var toggle = background.gameObject.AddComponent<Button>();
        toggle.targetGraphic = background;
        toggle.onClick.AddListener(() =>
        {
            if (Entry != null)
            {
                onToggle(Entry);
            }
        });
        background.gameObject.AddComponent<GamepadNavigationItem>();

        _arrow = MenuUi.CreateText("Arrow", Root, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_arrow);
        MenuUi.SetRect(_arrow.rectTransform, Anchors.Left, new Vector2(2f, 0f), new Vector2(18f, 0f));

        RectTransform slot = MenuUi.CreateRect("PortraitSlot", Root);
        MenuUi.SetRect(slot, Anchors.Left, new Vector2(20f, 4f), new Vector2(20f + PortraitSize, -4f));
        _portrait = new ZombiePortrait(slot, PortraitSize);

        float textLeft = 26f + PortraitSize;
        _name = MenuUi.CreateText("Name", Root, 13f, TextAlignmentOptions.BottomLeft);
        MenuUi.SetRect(
            _name.rectTransform,
            new Anchors(new Vector2(0f, 0.5f), Vector2.one),
            new Vector2(textLeft, 0f),
            new Vector2(-64f, -4f)
        );
        _name.textWrappingMode = TextWrappingModes.NoWrap;
        _name.overflowMode = TextOverflowModes.Ellipsis;

        _details = MenuUi.CreateText("Details", Root, 10f, TextAlignmentOptions.TopLeft);
        MenuUi.ApplyLabelText(_details);
        MenuUi.SetRect(
            _details.rectTransform,
            new Anchors(Vector2.zero, new Vector2(1f, 0.5f)),
            new Vector2(textLeft, 4f),
            new Vector2(-64f, 0f)
        );
        _details.textWrappingMode = TextWrappingModes.NoWrap;
        _details.overflowMode = TextOverflowModes.Ellipsis;

        LazyButton open = MenuUi.CreateButton(
            "Open",
            Root,
            "Open",
            Anchors.Right,
            new Vector2(-58f, 12f),
            new Vector2(-6f, -12f)
        );
        TextMeshProUGUI openLabel = open.GetComponentInChildren<TextMeshProUGUI>(true);
        if (openLabel != null)
        {
            openLabel.fontSize = 11f;
        }
        open.onClick.AddListener(() =>
        {
            if (Entry != null)
            {
                onOpen(Entry);
            }
        });
    }

    public RectTransform Root { get; }

    public ZombieEntry Entry { get; private set; }

    public void Bind(ZombieEntry entry)
    {
        if (Entry != entry)
        {
            Entry = entry;
            _portrait.Show(entry.Zombie);
        }
        _name.text = entry.IsDirty
            ? $"{entry.Name} <color=#E8A040>•</color>"
            : entry.Name;
        _details.text = entry.Details;
        _arrow.text = entry.Expanded ? "▾" : "▸";
    }

    public void Unbind()
    {
        Entry = null;
        Root.gameObject.SetActive(false);
    }
}
