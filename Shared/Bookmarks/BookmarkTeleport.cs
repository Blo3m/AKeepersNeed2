using System;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Movement;
using AKeepersNeed2.Shared.Ui;

namespace AKeepersNeed2.Shared.Bookmarks;

/// <summary>Teleports to a bookmark, from the menu, a hotkey or the map.</summary>
internal static class BookmarkTeleport
{
    /// <summary>
    /// Closes the map if it's open, runs <paramref name="beforeTeleport"/> (e.g. closing the mod
    /// menu, which pauses the game) and teleports.
    /// </summary>
    public static bool Go(Bookmark bookmark, Action beforeTeleport = null)
    {
        bool done = PlayerTeleporter.TeleportToScene(
            bookmark.Scene,
            bookmark.Position,
            bookmark.Preset,
            () =>
            {
                MapProjection.CloseOpenMap();
                beforeTeleport?.Invoke();
            }
        );
        if (done)
        {
            Toast.Show($"Teleported to {bookmark.Name}");
        }
        return done;
    }
}
