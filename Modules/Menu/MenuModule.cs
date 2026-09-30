using System;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The menu module: owns the single window instance, polls the toggle hotkey, and
/// opens/closes the window. Owns the menu's own settings (hotkey, UI scale) and keeps
/// <see cref="Toast.Scale"/> in step with the UI scale. The window is built from scratch and
/// styled from the game's assets — see <see cref="AKNMenuWindow"/>.
/// </summary>
internal sealed class MenuModule : IModule, IUpdatable, ISettingsDeclarer
{
    private ConfigEntry<KeyCode> _hotkey;
    private AKNMenuWindow _window;

    public string Name => "Menu";

    public int Order => 0;

    /// <summary>Menu size multiplier, 0.5 .. 3.0. Also scales toasts.</summary>
    internal static ConfigEntry<float> UiScale { get; private set; }

    /// <summary>
    /// Where the panel was dragged to: its offset from the right-docked default, in canvas units.
    /// Global, since it depends on the screen rather than the gameplay profile.
    /// </summary>
    internal static ConfigEntry<float> PanelX { get; private set; }

    internal static ConfigEntry<float> PanelY { get; private set; }

    public void DeclareSettings(SettingsBuilder settings)
    {
        PanelX = settings.Global("Menu", "PanelX", 0f, "Horizontal offset of the dragged menu (0 = right edge).");
        PanelY = settings.Global("Menu", "PanelY", 0f, "Vertical offset of the dragged menu (0 = centred).");
        settings.Button(MenuSection.Interface, 10, "Menu Position", "Reset", ResetPosition);
        _hotkey = settings.Global("Menu", "Hotkey", KeyCode.F1, "Key that toggles the mod menu.");
        UiScale = settings.Global(
            "Menu",
            "UiScale",
            1f,
            "Menu size multiplier (1 = game default).",
            new AcceptableValueRange<float>(0.5f, 3f)
        );
        settings.Key(MenuSection.Controls, 10, "Menu Key", _hotkey, isUnbindable: true);
    }

    public void Enable()
    {
        Toast.Scale = UiScale.Value;
        UiScale.SettingChanged += OnUiScaleChanged;
        Plugin.Logger.LogInfo($"[Menu] press {_hotkey.Value} in-game to open the mod menu.");
    }

    public void Disable()
    {
        UiScale.SettingChanged -= OnUiScaleChanged;
        if (_window != null && _window.IsShown)
        {
            _window.Close();
        }
    }

    public void Tick()
    {
        // While the menu is capturing a key for a rebind, it owns this frame's input.
        if (_window != null && _window.TickInput())
        {
            return;
        }
        if (Input.GetKeyDown(_hotkey.Value))
        {
            Toggle();
        }
    }

    private static void OnUiScaleChanged(object sender, EventArgs e)
    {
        Toast.Scale = UiScale.Value;
    }

    private void ResetPosition()
    {
        PanelX.Value = 0f;
        PanelY.Value = 0f;
        _window?.ApplySavedPosition();
    }

    private void Toggle()
    {
        if (_window == null)
        {
            _window = AKNMenuWindow.CreateInstance();
            if (_window == null)
            {
                return;
            }
        }

        if (_window.IsShown)
        {
            _window.Close();
        }
        else
        {
            _window.Open(null);
        }
    }
}
