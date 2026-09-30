using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Shared.Bookmarks;

/// <summary>
/// One saved teleport spot: a name, the scene it's in, the position there, the lighting preset that
/// was active (a teleport re-applies it, so interiors stay lit as interiors) and an optional hotkey.
/// Backed by entries in the owning profile's .cfg (see <see cref="BookmarkStore"/>).
/// </summary>
internal sealed class Bookmark
{
    public Bookmark(
        string id,
        ConfigEntry<string> name,
        ConfigEntry<string> scene,
        ConfigEntry<string> position,
        ConfigEntry<string> preset,
        ConfigEntry<KeyCode> hotkey
    )
    {
        Id = id;
        NameEntry = name;
        SceneEntry = scene;
        PositionEntry = position;
        PresetEntry = preset;
        Hotkey = hotkey;
    }

    public string Id { get; }

    public string Name => NameEntry.Value;

    public string Scene => SceneEntry.Value;

    public string Preset => PresetEntry.Value;

    public Vector3 Position => ParsePosition(PositionEntry.Value);

    /// <summary>Key that teleports here; <c>None</c> when unbound.</summary>
    public ConfigEntry<KeyCode> Hotkey { get; }

    internal ConfigEntry<string> NameEntry { get; }

    internal ConfigEntry<string> SceneEntry { get; }

    internal ConfigEntry<string> PositionEntry { get; }

    internal ConfigEntry<string> PresetEntry { get; }

    internal static string FormatPosition(Vector3 position)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:R};{1:R};{2:R}", position.x, position.y, position.z);
    }

    private static Vector3 ParsePosition(string text)
    {
        string[] parts = (text ?? string.Empty).Split(';');
        if (parts.Length != 3)
        {
            return Vector3.zero;
        }
        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
        return new Vector3(x, y, z);
    }
}
