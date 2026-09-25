using System;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Shared.Profiles;

/// <summary>
/// One named settings profile, backed by its own .cfg under
/// <see cref="ModConfig.ProfilesDirectory"/>. The file holds a copy of every gameplay entry in
/// <see cref="ModConfig.ProfileSettings"/> plus the profile's own switch hotkey.
/// </summary>
internal sealed class Profile
{
    public Profile(string name, ConfigFile file, ConfigEntry<KeyCode> hotkey)
    {
        Name = name;
        File = file;
        Hotkey = hotkey;
    }

    public string Name { get; }

    public ConfigFile File { get; }

    /// <summary>Key that switches straight to this profile; <c>None</c> when unbound.</summary>
    public ConfigEntry<KeyCode> Hotkey { get; }

    public bool IsDefault => string.Equals(Name, ProfileStore.DefaultName, StringComparison.OrdinalIgnoreCase);

    public void SetHotkey(KeyCode key)
    {
        Hotkey.Value = key;
        File.Save();
    }
}
