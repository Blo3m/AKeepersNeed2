using AKeepersNeed2.Shared.Profiles;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Menu-level settings: the profile selector and management (new / rename / delete / reset,
/// plus the profile's own switch key), the UI-scale control, and the hotkey rebinds. The
/// scale control, dialogs and key capture need window state, so they go through
/// <see cref="AKNMenuWindow"/>.
/// </summary>
internal sealed class SettingsTab : IMenuTab
{
    private readonly AKNMenuWindow _window;

    private MenuPage _page;
    private TextMeshProUGUI _profileName;
    private LazyButton _rename;
    private LazyButton _delete;

    public SettingsTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Settings";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);
        KeyRebinder rebinder = _window.Rebinder;

        _page.SectionHeader("Profile");
        BuildProfileSelector(_page.Band(32f, 4f));
        BuildProfileActions(_page.Band(28f, 6f));
        _page.AddSync(rebinder.BuildRow(
            _page.Band(30f, 6f),
            "Profile Key",
            () => ProfileStore.Active?.Hotkey.Value ?? KeyCode.None,
            key => ProfileStore.Active?.SetHotkey(key)
        ));

        _page.SectionHeader("Interface");
        _window.UiScale.BuildControl(_page.Band(28f, 8f));

        _page.SectionHeader("Controls");
        _page.AddSync(rebinder.BuildRow(
            _page.Band(30f, 8f),
            "Menu Key",
            () => ModConfig.MenuHotkey.Value,
            key => ModConfig.MenuHotkey.Value = key,
            isMenuKey: true
        ));
        _page.AddSync(rebinder.BuildRow(
            _page.Band(30f, 6f),
            "Previous Profile",
            () => ModConfig.PreviousProfileKey.Value,
            key => ModConfig.PreviousProfileKey.Value = key
        ));
        _page.AddSync(rebinder.BuildRow(
            _page.Band(30f, 6f),
            "Next Profile",
            () => ModConfig.NextProfileKey.Value,
            key => ModConfig.NextProfileKey.Value = key
        ));

        Refresh();
    }

    public void Refresh()
    {
        Profile active = ProfileStore.Active;
        if (_page == null || active == null)
        {
            return;
        }
        _page.Sync();
        _profileName.text = active.Name;
        _rename.interactable = !active.IsDefault;
        _delete.interactable = !active.IsDefault;
    }

    /// <summary>`[<] name [>]` — cycling switches the active profile immediately.</summary>
    private void BuildProfileSelector(RectTransform band)
    {
        Image field = MenuUi.CreateImage("ProfileField", band, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform, Anchors.Fill, new Vector2(40f, 0f), new Vector2(-40f, 0f));
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _profileName = MenuUi.CreateText("ProfileName", band, 13f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_profileName);
        MenuUi.SetRect(_profileName.rectTransform, Anchors.Fill, new Vector2(46f, 0f), new Vector2(-46f, 0f));
        _profileName.textWrappingMode = TextWrappingModes.NoWrap;
        _profileName.overflowMode = TextOverflowModes.Ellipsis;

        LazyButton prev = MenuUi.CreateButton("Prev", band, "<", Anchors.Left, Vector2.zero, new Vector2(34f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", band, ">", Anchors.Right, new Vector2(-34f, 0f), Vector2.zero);

        prev.onClick.AddListener(() => ProfileStore.SwitchRelative(-1));
        next.onClick.AddListener(() => ProfileStore.SwitchRelative(1));
    }

    private void BuildProfileActions(RectTransform band)
    {
        LazyButton create = ActionButton(band, 0, "New");
        _rename = ActionButton(band, 1, "Rename");
        _delete = ActionButton(band, 2, "Delete");
        LazyButton reset = ActionButton(band, 3, "Reset");

        create.onClick.AddListener(() => _window.ShowNamePrompt(
            "New profile",
            ProfileStore.SuggestName(),
            ProfileStore.Create
        ));
        _rename.onClick.AddListener(() => _window.ShowNamePrompt(
            "Rename profile",
            ProfileStore.Active.Name,
            name => ProfileStore.Rename(ProfileStore.Active, name)
        ));
        _delete.onClick.AddListener(() => _window.ShowConfirm(
            "Delete profile?",
            $"\"{ProfileStore.Active.Name}\" will be removed.",
            "Delete",
            () => ProfileStore.Delete(ProfileStore.Active)
        ));
        reset.onClick.AddListener(() => _window.ShowConfirm(
            "Reset profile?",
            $"Every option in \"{ProfileStore.Active.Name}\" goes back to its default.",
            "Reset",
            ProfileStore.ResetActive
        ));
    }

    private static LazyButton ActionButton(RectTransform band, int slot, string label)
    {
        const float step = 0.25f;
        LazyButton button = MenuUi.CreateButton(
            label,
            band,
            label,
            new Anchors(new Vector2(slot * step, 0f), new Vector2((slot + 1) * step, 1f)),
            new Vector2(slot == 0 ? 0f : 2f, 0f),
            new Vector2(slot == 3 ? 0f : -2f, 0f)
        );
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.fontSize = 12f;
        }
        return button;
    }
}
