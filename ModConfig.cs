using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace AKeepersNeed2;

/// <summary>
/// The mod's config files. Modules bind their own entries into these through
/// <c>Core.Settings.SettingsBuilder</c>; nothing feature-specific lives here. Global settings
/// persist in BepInEx/config/&lt;GUID&gt;.cfg (<see cref="Main"/>). Gameplay settings live in the
/// in-memory <see cref="ProfileSettings"/> file, which <c>Shared.Profiles.ProfileStore</c> fills
/// from and writes back to the active profile's own .cfg.
/// </summary>
internal static class ModConfig
{
    public static readonly string ProfilesDirectory =
        Path.Combine(Path.Combine(Paths.ConfigPath, "AKeepersNeed2"), "profiles");

    /// <summary>The plugin's main .cfg (global settings).</summary>
    public static ConfigFile Main { get; private set; }

    /// <summary>
    /// Live gameplay settings. Never saved to disk itself — it only mirrors the active
    /// profile, so every per-profile entry must be bound here.
    /// </summary>
    public static ConfigFile ProfileSettings { get; private set; }

    public static void Init(ConfigFile main)
    {
        Main = main;
        // The path is never written: SaveOnConfigSet is off and Save() is never called.
        ProfileSettings = new ConfigFile(Path.Combine(ProfilesDirectory, ".live"), false)
        {
            SaveOnConfigSet = false,
        };
    }
}
