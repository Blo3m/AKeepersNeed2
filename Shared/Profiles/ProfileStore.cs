using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Shared.Profiles;

/// <summary>
/// Owns the settings profiles: one .cfg per profile in <see cref="ModConfig.ProfilesDirectory"/>,
/// with a locked "Default" that always exists. The active profile's values are copied into
/// the live <see cref="ModConfig.ProfileSettings"/> entries (so modules react through their
/// usual <c>SettingChanged</c> hooks), and live edits are written back to the active file
/// after a short debounce so slider drags don't hit the disk every frame.
/// </summary>
internal static class ProfileStore
{
    public const string DefaultName = "Default";
    public const int MaxNameLength = 24;

    private const float SaveDelaySeconds = 0.5f;

    private static readonly MethodInfo BindMethod = typeof(ConfigFile).GetMethods()
        .First(m => m.Name == nameof(ConfigFile.Bind)
            && m.IsGenericMethodDefinition
            && m.GetParameters()[0].ParameterType == typeof(ConfigDefinition));

    private static readonly string[] ReservedFileNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private static readonly List<Profile> _profiles = new List<Profile>();
    private static Profile _active;
    private static bool _applying;
    private static float _saveAt = -1f;

    /// <summary>Raised when the active profile or the profile list changes, or on reset.</summary>
    public static event Action Changed;

    /// <summary>All profiles: Default first, then the rest by name.</summary>
    public static IReadOnlyList<Profile> Profiles => _profiles;

    public static Profile Active => _active;

    public static void Init()
    {
        Directory.CreateDirectory(ModConfig.ProfilesDirectory);

        foreach (string path in Directory.GetFiles(ModConfig.ProfilesDirectory, "*.cfg"))
        {
            _profiles.Add(Load(path));
        }

        Profile defaultProfile = Find(DefaultName);
        if (defaultProfile == null)
        {
            defaultProfile = Load(PathFor(DefaultName));
            _profiles.Add(defaultProfile);
        }
        Sort();

        _active = Find(ModConfig.ActiveProfile.Value) ?? defaultProfile;
        ModConfig.ActiveProfile.Value = _active.Name;
        ApplyToLive();

        ModConfig.ProfileSettings.SettingChanged += OnLiveSettingChanged;
        Plugin.Logger.LogInfo($"[Profiles] {_profiles.Count} profile(s) loaded; active: {_active.Name}.");
    }

    public static void Shutdown()
    {
        ModConfig.ProfileSettings.SettingChanged -= OnLiveSettingChanged;
        Flush();
        _profiles.Clear();
        _active = null;
    }

    /// <summary>Per-frame: writes pending live edits once the debounce has elapsed.</summary>
    public static void Tick()
    {
        if (_saveAt >= 0f && Time.unscaledTime >= _saveAt)
        {
            Flush();
        }
    }

    public static Profile Find(string name)
    {
        return _profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static void Switch(Profile profile)
    {
        if (profile == null || profile == _active)
        {
            return;
        }
        Flush();
        _active = profile;
        ModConfig.ActiveProfile.Value = profile.Name;
        ApplyToLive();
        Plugin.Logger.LogInfo($"[Profiles] switched to {profile.Name}.");
        Changed?.Invoke();
    }

    /// <summary>Switches <paramref name="step"/> places along <see cref="Profiles"/>, wrapping.</summary>
    public static void SwitchRelative(int step)
    {
        int count = _profiles.Count;
        int index = _profiles.IndexOf(_active);
        Switch(_profiles[((index + step) % count + count) % count]);
    }

    /// <summary>Returns an error message, or null when <paramref name="name"/> is usable.</summary>
    public static string ValidateName(string name, Profile renaming = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Enter a name.";
        }
        if (name.Length > MaxNameLength)
        {
            return $"Max {MaxNameLength} characters.";
        }
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.EndsWith(".")
            || ReservedFileNames.Contains(name.ToUpperInvariant()))
        {
            return "Name has invalid characters.";
        }
        Profile existing = Find(name);
        if (existing != null && existing != renaming)
        {
            return "That name is taken.";
        }
        return null;
    }

    public static string SuggestName()
    {
        for (int i = 1; ; i++)
        {
            string name = $"Profile {i}";
            if (Find(name) == null)
            {
                return name;
            }
        }
    }

    /// <summary>Creates a profile with vanilla values and makes it active.</summary>
    public static string Create(string name)
    {
        name = name?.Trim();
        string error = ValidateName(name);
        if (error != null)
        {
            return error;
        }

        Profile profile = Load(PathFor(name));
        _profiles.Add(profile);
        Sort();
        Switch(profile);
        return null;
    }

    public static string Rename(Profile profile, string name)
    {
        name = name?.Trim();
        if (profile.IsDefault)
        {
            return "Default can't be renamed.";
        }
        string error = ValidateName(name, profile);
        if (error != null)
        {
            return error;
        }
        if (name == profile.Name)
        {
            return null;
        }

        if (profile == _active)
        {
            Flush();
        }
        profile.File.Save();

        string from = profile.File.ConfigFilePath;
        string to = PathFor(name);
        try
        {
            // A case-only rename is a no-op move on Windows; hop through a temp name.
            string temp = to + ".tmp";
            File.Move(from, temp);
            File.Move(temp, to);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Plugin.Logger.LogError($"[Profiles] rename {profile.Name} → {name} failed: {ex.Message}");
            return "Couldn't rename the file.";
        }

        Profile renamed = Load(to);
        _profiles[_profiles.IndexOf(profile)] = renamed;
        Sort();
        if (profile == _active)
        {
            _active = renamed;
            ModConfig.ActiveProfile.Value = renamed.Name;
        }
        Changed?.Invoke();
        return null;
    }

    public static void Delete(Profile profile)
    {
        if (profile == null || profile.IsDefault)
        {
            return;
        }
        if (profile == _active)
        {
            Switch(Find(DefaultName));
        }
        _profiles.Remove(profile);
        try
        {
            File.Delete(profile.File.ConfigFilePath);
            Plugin.Logger.LogInfo($"[Profiles] deleted {profile.Name}.");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Plugin.Logger.LogError($"[Profiles] deleting {profile.Name}'s file failed: {ex.Message}");
        }
        Changed?.Invoke();
    }

    /// <summary>Sets every gameplay setting of the active profile back to its default.</summary>
    public static void ResetActive()
    {
        foreach (ConfigEntryBase live in LiveEntries())
        {
            live.BoxedValue = live.DefaultValue;
        }
        Flush();
        Changed?.Invoke();
    }

    private static void OnLiveSettingChanged(object sender, SettingChangedEventArgs e)
    {
        if (_applying || _active == null)
        {
            return;
        }
        _active.File[e.ChangedSetting.Definition].BoxedValue = e.ChangedSetting.BoxedValue;
        _saveAt = Time.unscaledTime + SaveDelaySeconds;
    }

    private static void Flush()
    {
        if (_saveAt < 0f)
        {
            return;
        }
        _saveAt = -1f;
        _active?.File.Save();
    }

    private static void ApplyToLive()
    {
        _applying = true;
        try
        {
            foreach (ConfigEntryBase live in LiveEntries())
            {
                live.BoxedValue = _active.File[live.Definition].BoxedValue;
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private static Profile Load(string path)
    {
        var file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        foreach (ConfigEntryBase live in LiveEntries())
        {
            BindMethod.MakeGenericMethod(live.SettingType)
                .Invoke(file, new[] { live.Definition, live.DefaultValue, live.Description });
        }
        ConfigEntry<KeyCode> hotkey = file.Bind("Profile", "Hotkey", KeyCode.None,
            "Key that switches straight to this profile (None = unbound).");
        file.Save();
        return new Profile(Path.GetFileNameWithoutExtension(path), file, hotkey);
    }

    private static IEnumerable<ConfigEntryBase> LiveEntries()
    {
        ConfigFile live = ModConfig.ProfileSettings;
        return live.Keys.Select(definition => live[definition]).ToList();
    }

    private static string PathFor(string name)
    {
        return Path.Combine(ModConfig.ProfilesDirectory, name + ".cfg");
    }

    private static void Sort()
    {
        _profiles.Sort((a, b) =>
        {
            if (a.IsDefault != b.IsDefault)
            {
                return a.IsDefault ? -1 : 1;
            }
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
    }
}
