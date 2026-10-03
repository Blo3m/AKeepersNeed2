using System;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Modules.Minimap;

/// <summary>
/// A minimap in a screen corner: a copy of the world map centred on the player, with an arrow for
/// the player and every registered map pin (map markers, bookmarks, NPC faces), zoomed with + / −
/// buttons under it. Shown while on, not hidden by its hotkey, in a game, out in the world the map
/// covers and while the world map isn't open. While the mod menu is open it can be dragged and
/// resized; position, size and zoom are saved per profile. Display-only.
/// </summary>
internal sealed class MinimapModule : IModule, IUpdatable, ISettingsDeclarer
{
    internal const float DefaultSize = 200f;
    internal const float DefaultOffset = -16f;
    internal const int DefaultZoom = 3;
    private const float MaxHeightCorrection = 5f;
    private const float MaxSideCorrection = 50f;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<KeyCode> _key;
    private static bool _hiddenByKey;
    private static bool _inGame;

    private MinimapView _view;

    public string Name => "Minimap";

    public int Order => 0;

    internal static ConfigEntry<float> X { get; private set; }

    internal static ConfigEntry<float> Y { get; private set; }

    internal static ConfigEntry<float> Size { get; private set; }

    internal static ConfigEntry<int> Zoom { get; private set; }

    internal static ConfigEntry<float> HeightCorrection { get; private set; }

    internal static ConfigEntry<float> SideCorrection { get; private set; }

    public void DeclareSettings(SettingsBuilder settings)
    {
        _enabled = settings.Profile("Minimap", "Enabled", false, "Show a minimap centred on the player.");
        _key = settings.Profile("Minimap", "Key", KeyCode.None, "Key that shows or hides the minimap.");
        X = settings.Profile("Minimap", "X", DefaultOffset, "Minimap offset from the right edge (set by dragging).");
        Y = settings.Profile("Minimap", "Y", DefaultOffset, "Minimap offset from the top edge (set by dragging).");
        Size = settings.Profile(
            "Minimap",
            "Size",
            DefaultSize,
            "Minimap size in pixels (set by resizing).",
            new AcceptableValueRange<float>(MinimapView.MinSize, MinimapView.MaxSize)
        );
        Zoom = settings.Profile(
            "Minimap",
            "Zoom",
            DefaultZoom,
            "Minimap zoom step (set with its + and - buttons).",
            new AcceptableValueRange<int>(0, MinimapView.ZoomSteps - 1)
        );
        HeightCorrection = settings.Profile(
            "Minimap",
            "HeightCorrection",
            0.8f,
            "Moves you and the markers up the minimap by your height times this; the game's own map "
                + "places you too low on higher ground (0 = like the game).",
            new AcceptableValueRange<float>(0f, MaxHeightCorrection)
        );
        SideCorrection = settings.Profile(
            "Minimap",
            "SideCorrection",
            3f,
            "Moves you and the markers sideways on the minimap, in map units (negative = left), to line "
                + "them up with the map picture.",
            new AcceptableValueRange<float>(-MaxSideCorrection, MaxSideCorrection)
        );
        settings.Toggle(MenuSection.Map, 90, "Minimap", _enabled);
        settings.Slider(
            MenuSection.Map,
            95,
            HeightCorrection,
            0f,
            MaxHeightCorrection,
            "'Height correction '0.0",
            () => _enabled.Value
        );
        settings.Slider(
            MenuSection.Map,
            97,
            SideCorrection,
            -MaxSideCorrection,
            MaxSideCorrection,
            "'Sideways correction '0;'Sideways correction -'0",
            () => _enabled.Value
        );
        settings.Key(MenuSection.Map, 100, "Minimap Key", _key, enabledWhen: () => _enabled.Value);
        settings.Button(MenuSection.Map, 110, "Minimap Position", "Reset", ResetPosition, () => _enabled.Value);
    }

    public void Enable()
    {
        MainGame.OnGameStarted += OnGameStarted;
        MainGame.OnGoToMainMenu += OnGoToMainMenu;
    }

    public void Disable()
    {
        MainGame.OnGameStarted -= OnGameStarted;
        MainGame.OnGoToMainMenu -= OnGoToMainMenu;
        if (_view != null)
        {
            UnityEngine.Object.Destroy(_view.gameObject);
            _view = null;
        }
    }

    public void Tick()
    {
        if (_enabled.Value && _key.Value != KeyCode.None && Input.GetKeyDown(_key.Value) && !InputGate.IsBlocked)
        {
            _hiddenByKey = !_hiddenByKey;
        }
        bool show = _enabled.Value
            && !_hiddenByKey
            && _inGame
            && MainGame.PlayerData != null
            && MapProjection.IsPlayerInWorld()
            && !MapProjection.TryGetOpenMap(out _);
        if (!show)
        {
            if (_view != null && _view.gameObject.activeSelf)
            {
                _view.gameObject.SetActive(false);
            }
            return;
        }
        if (_view == null)
        {
            _view = MinimapView.Create();
            if (_view == null)
            {
                return;
            }
        }
        if (!_view.gameObject.activeSelf)
        {
            _view.gameObject.SetActive(true);
        }
    }

    private static void OnGameStarted()
    {
        _inGame = true;
    }

    private static void OnGoToMainMenu()
    {
        _inGame = false;
    }

    private static void ResetPosition()
    {
        X.Value = DefaultOffset;
        Y.Value = DefaultOffset;
        Size.Value = DefaultSize;
    }
}
