using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Bookmarks;
using AKeepersNeed2.Shared.GameInput;
using AKeepersNeed2.Shared.Profiles;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Keybinds;

/// <summary>One binding that already uses a key: a mod hotkey or a game action.</summary>
internal sealed class KeyConflict
{
    public string Name;

    /// <summary>False for unbindable keys (the menu key) and every game action (never unbound).</summary>
    public bool Replaceable;

    /// <summary>Unbinds the key from this binding; null when not <see cref="Replaceable"/>.</summary>
    public Action Unbind;
}

/// <summary>
/// Finds what else uses a key, within the active profile: every hotkey a module declared
/// (<see cref="SettingsRegistry.Hotkeys"/>: menu, previous/next profile, the active profile's
/// teleport key …), every profile's switch key (they work from any profile), the active profile's
/// bookmark keys, and the game's own
/// keyboard bindings (<see cref="GameKeybinds"/>). Game actions are only reported, never unbound;
/// players change those in the game's Controls menu.
/// </summary>
internal static class KeyConflicts
{
    public static List<KeyConflict> Find(KeyCode key, ConfigEntry<KeyCode> self)
    {
        var conflicts = new List<KeyConflict>();
        if (key == KeyCode.None)
        {
            return conflicts;
        }

        foreach (KeySettingRow hotkey in SettingsRegistry.Hotkeys)
        {
            ConfigEntry<KeyCode> entry = hotkey.Entry;
            if (entry == self || entry.Value != key)
            {
                continue;
            }
            conflicts.Add(new KeyConflict
            {
                Name = hotkey.Label,
                Replaceable = !hotkey.IsUnbindable,
                Unbind = () => entry.Value = KeyCode.None,
            });
        }

        foreach (Profile profile in ProfileStore.Profiles)
        {
            if (profile.Hotkey == self || profile.Hotkey.Value != key)
            {
                continue;
            }
            Profile target = profile;
            conflicts.Add(new KeyConflict
            {
                Name = $"{profile.Name} profile key",
                Replaceable = true,
                Unbind = () => target.SetHotkey(KeyCode.None),
            });
        }

        foreach (Bookmark bookmark in BookmarkStore.All)
        {
            if (bookmark.Hotkey == self || bookmark.Hotkey.Value != key)
            {
                continue;
            }
            Bookmark target = bookmark;
            conflicts.Add(new KeyConflict
            {
                Name = $"{bookmark.Name} bookmark key",
                Replaceable = true,
                Unbind = () => BookmarkStore.SetHotkey(target, KeyCode.None),
            });
        }

        foreach (string action in GameKeybinds.ActionsOn(key))
        {
            conflicts.Add(new KeyConflict { Name = $"{action} (game)", Replaceable = false });
        }
        return conflicts;
    }
}
