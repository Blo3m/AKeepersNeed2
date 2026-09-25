using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Menu-level settings: the profile selector, the UI-scale control (preview + confirm), and
/// the menu-hotkey rebind. The scale and rebind controls need window state (ghost/dialog,
/// per-frame key capture), so they're built by <see cref="AKNMenuWindow"/>.
/// </summary>
internal sealed class SettingsTab : IMenuTab
{
    private readonly AKNMenuWindow _window;

    public SettingsTab(AKNMenuWindow window)
    {
        _window = window;
    }

    public string Title => "Settings";

    public void Build(RectTransform content)
    {
        var page = new MenuPage(content);

        page.SectionHeader("Profile");
        BuildProfileSelector(page.Band(32f, 4f));

        page.SectionHeader("Interface");
        _window.BuildUiScaleControl(page.Band(28f, 8f));

        page.SectionHeader("Controls");
        _window.BuildRebindRow(page.Band(30f, 8f));
    }

    /// <summary>
    /// Placeholder profile picker: `[<] name [>]`. Cycles through in-memory names only —
    /// a stand-in for future save/load of configured option profiles.
    /// </summary>
    private static void BuildProfileSelector(RectTransform band)
    {
        // TODO: back these with persisted config profiles (save/load option sets).
        string[] profiles = { "Default", "Profile 1", "Profile 2" };
        int index = 0;

        Image field = MenuUi.CreateImage("ProfileField", band, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform,
            new Vector2(40f, 0f), new Vector2(-40f, 0f), Vector2.zero, Vector2.one);
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        TextMeshProUGUI name = MenuUi.CreateText("ProfileName", band, 13f,
            TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyValueText(name);
        MenuUi.Stretch(name.rectTransform);
        name.text = profiles[index];

        LazyButton prev = MenuUi.CreateButton("Prev", band, "<",
            new Vector2(0f, 0f), new Vector2(34f, 0f), Vector2.zero, new Vector2(0f, 1f));
        LazyButton next = MenuUi.CreateButton("Next", band, ">",
            new Vector2(-34f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.one);

        void Cycle(int step)
        {
            index = (index + step + profiles.Length) % profiles.Length;
            name.text = profiles[index];
        }

        prev.onClick.AddListener(() => Cycle(-1));
        next.onClick.AddListener(() => Cycle(1));
    }
}
