using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Profiles;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Shared.Bookmarks;

/// <summary>
/// The active profile's teleport bookmarks. They live in the profile's own .cfg, next to its
/// settings: <c>[Bookmarks] Index</c> lists the ids in order and each bookmark has a
/// <c>[Bookmark.&lt;id&gt;]</c> section. BepInEx keeps entries nobody has bound when it saves a file,
/// so the profile store's own loads, renames and saves carry them along, deleting a profile deletes
/// its bookmarks, and switching profile switches the list. Profile files don't save on every set,
/// so every change here saves the file itself. Nothing goes into the game save.
/// </summary>
internal static class BookmarkStore
{
    public const int MaxNameLength = 24;

    private const string IndexSection = "Bookmarks";
    private const string SectionPrefix = "Bookmark.";

    private static readonly List<Bookmark> _bookmarks = new List<Bookmark>();
    private static Profile _loadedFor;
    private static ConfigEntry<string> _index;

    /// <summary>The active profile's bookmarks, in list order.</summary>
    public static IReadOnlyList<Bookmark> All
    {
        get
        {
            EnsureLoaded();
            return _bookmarks;
        }
    }

    /// <summary>Returns an error message, or null when <paramref name="name"/> is usable.</summary>
    public static string ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Enter a name.";
        }
        if (name.Length > MaxNameLength)
        {
            return $"Max {MaxNameLength} characters.";
        }
        return null;
    }

    public static string SuggestName()
    {
        for (int i = 1; ; i++)
        {
            string name = $"Bookmark {i}";
            if (All.All(b => !string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return name;
            }
        }
    }

    /// <summary>Saves where the player stands now. Returns an error message, or null.</summary>
    public static string AddCurrent(string name)
    {
        name = name?.Trim();
        string error = ValidateName(name);
        if (error != null)
        {
            return error;
        }
        PlayerData player = MainGame.PlayerData;
        if (player == null || string.IsNullOrEmpty(player.currentGameSceneId) || !EnsureLoaded())
        {
            return "Load a game first.";
        }
        EnvironmentEngine engine = EnvironmentEngine.Instance;
        string preset = engine != null && engine.TimesOfDay != null
            ? engine.TimesOfDay.name
            : string.Empty;

        Bookmark bookmark = Bind(_loadedFor.File, Guid.NewGuid().ToString("N").Substring(0, 8));
        bookmark.NameEntry.Value = name;
        bookmark.SceneEntry.Value = player.currentGameSceneId;
        bookmark.PositionEntry.Value = Bookmark.FormatPosition(player.position.Value);
        bookmark.PresetEntry.Value = preset;
        _bookmarks.Add(bookmark);
        Save();
        Plugin.Logger.LogInfo($"[Bookmarks] saved '{name}' at {player.position.Value} in {player.currentGameSceneId}.");
        return null;
    }

    public static string Rename(Bookmark bookmark, string name)
    {
        name = name?.Trim();
        string error = ValidateName(name);
        if (error != null)
        {
            return error;
        }
        bookmark.NameEntry.Value = name;
        Save();
        return null;
    }

    public static void SetHotkey(Bookmark bookmark, KeyCode key)
    {
        bookmark.Hotkey.Value = key;
        Save();
    }

    public static void Delete(Bookmark bookmark)
    {
        if (!_bookmarks.Remove(bookmark) || _loadedFor == null)
        {
            return;
        }
        ConfigFile file = _loadedFor.File;
        file.Remove(bookmark.NameEntry.Definition);
        file.Remove(bookmark.SceneEntry.Definition);
        file.Remove(bookmark.PositionEntry.Definition);
        file.Remove(bookmark.PresetEntry.Definition);
        file.Remove(bookmark.Hotkey.Definition);
        Save();
    }

    /// <summary>Loads the active profile's list if it isn't loaded yet; false with no profile.</summary>
    private static bool EnsureLoaded()
    {
        Profile active = ProfileStore.Active;
        if (active == _loadedFor)
        {
            return active != null;
        }
        _bookmarks.Clear();
        _loadedFor = active;
        if (active == null)
        {
            _index = null;
            return false;
        }
        _index = active.File.Bind(
            IndexSection,
            "Index",
            string.Empty,
            "Ids of this profile's teleport bookmarks, in list order."
        );
        foreach (string id in _index.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Bookmark bookmark = Bind(active.File, id.Trim());
            if (!string.IsNullOrEmpty(bookmark.Scene))
            {
                _bookmarks.Add(bookmark);
            }
        }
        return true;
    }

    private static Bookmark Bind(ConfigFile file, string id)
    {
        string section = SectionPrefix + id;
        return new Bookmark(
            id,
            file.Bind(section, "Name", string.Empty, "Bookmark name."),
            file.Bind(section, "Scene", string.Empty, "Game scene id the bookmark is in."),
            file.Bind(section, "Position", string.Empty, "World position x;y;z."),
            file.Bind(section, "Preset", string.Empty, "Lighting preset active when saved (e.g. indoor)."),
            file.Bind(section, "Hotkey", KeyCode.None, "Key that teleports here (None = unbound).")
        );
    }

    private static void Save()
    {
        if (_loadedFor == null || _index == null)
        {
            return;
        }
        _index.Value = string.Join(",", _bookmarks.Select(b => b.Id).ToArray());
        _loadedFor.File.Save();
    }
}
