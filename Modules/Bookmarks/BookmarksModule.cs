using System.Collections.Generic;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.MapPins;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Bookmarks;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Patching;
using AKeepersNeed2.Shared.Ui;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.Bookmarks;

/// <summary>
/// Teleport bookmarks outside the menu: each bookmark's own hotkey, and (while "Show Bookmarks" is
/// on) a marker per bookmark on the world map, name on hover, shift-click to teleport. The list and
/// its storage are <see cref="BookmarkStore"/>; the menu's Map tab edits it. A postfix on
/// <c>MapPageWidget.Redraw</c> attaches a <see cref="BookmarkMarkerLayer"/>, like the NPC markers.
/// </summary>
internal sealed class BookmarksModule : HarmonyModule, IUpdatable
{
    private static ConfigEntry<bool> _showOnMap;

    public override string Name => "Bookmarks";

    internal static bool ShowOnMap => _showOnMap.Value;

    protected override ConfigEntry<bool> EnabledFlag => _showOnMap;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _showOnMap = settings.Profile(
            "Bookmarks",
            "ShowOnMap",
            true,
            "Show teleport bookmarks on the world map; shift-click one to teleport there."
        );
        settings.Toggle(MenuSection.Map, 60, "Show Bookmarks on Map", _showOnMap);
    }

    protected override void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(MapPageWidget), nameof(MapPageWidget.Redraw)),
            postfix: new HarmonyMethod(typeof(BookmarksModule), nameof(AfterRedraw))
        );
    }

    protected override void OnEnabled()
    {
        MapPinRegistry.Register(Pins);
    }

    protected override void OnDisabled()
    {
        MapPinRegistry.Unregister(Pins);
    }

    /// <summary>The world scene's bookmarks as gold diamonds for the minimap.</summary>
    private static IEnumerable<MapPin> Pins()
    {
        string worldScene = MapProjection.WorldSceneId;
        if (!_showOnMap.Value || worldScene == null)
        {
            yield break;
        }
        foreach (Bookmark bookmark in BookmarkStore.All)
        {
            if (bookmark.Scene == worldScene)
            {
                yield return new MapPin
                {
                    World = bookmark.Position,
                    Color = BookmarkMarker.Fill,
                    Size = 9f,
                    Diamond = true,
                    Layer = 30,
                };
            }
        }
    }

    public void Tick()
    {
        // Paused covers the mod menu and the game's own menus: a teleport there would stall mid-fade.
        if (InputGate.IsBlocked || MainGame.PlayerData == null || MainGame.IsGamePaused)
        {
            return;
        }
        foreach (Bookmark bookmark in BookmarkStore.All)
        {
            KeyCode key = bookmark.Hotkey.Value;
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                BookmarkTeleport.Go(bookmark);
                return;
            }
        }
    }

    private static void AfterRedraw(MapPageWidget __instance)
    {
        RectTransform mapRect = MapProjection.GetMapRect(__instance);
        if (mapRect != null)
        {
            BookmarkMarkerLayer.Ensure(mapRect).Rebuild();
        }
    }
}
