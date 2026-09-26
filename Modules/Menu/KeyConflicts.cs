using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AKeepersNeed2.Shared.Profiles;
using BepInEx.Configuration;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>One binding that already uses a key: a mod hotkey or a game action.</summary>
internal sealed class KeyConflict
{
    public string Name;

    /// <summary>False for the menu key and for every game action (the mod never unbinds those).</summary>
    public bool Replaceable;

    /// <summary>Unbinds the key from this binding; null when not <see cref="Replaceable"/>.</summary>
    public Action Unbind;
}

/// <summary>
/// Finds what else uses a key, within the active profile: the menu, previous/next and every
/// profile's switch key (all global), the active profile's teleport key, and the game's own
/// keyboard bindings (<c>LazyInput.GameBindings.keyBindings</c>). Game actions are only
/// reported, never unbound; players change those in the game's Controls menu.
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
        AddModConflicts(conflicts, key, self);
        AddGameConflicts(conflicts, key);
        return conflicts;
    }

    private static void AddModConflicts(List<KeyConflict> conflicts, KeyCode key, ConfigEntry<KeyCode> self)
    {
        void Check(string name, ConfigEntry<KeyCode> entry, bool replaceable, Action unbind)
        {
            if (entry == null || entry == self || entry.Value != key)
            {
                return;
            }
            conflicts.Add(new KeyConflict { Name = name, Replaceable = replaceable, Unbind = unbind });
        }

        Check("Menu Key", ModConfig.MenuHotkey, false, null);
        Check(
            "Previous Profile",
            ModConfig.PreviousProfileKey,
            true,
            () => ModConfig.PreviousProfileKey.Value = KeyCode.None
        );
        Check("Next Profile", ModConfig.NextProfileKey, true, () => ModConfig.NextProfileKey.Value = KeyCode.None);
        foreach (Profile profile in ProfileStore.Profiles)
        {
            Profile target = profile;
            Check($"{profile.Name} profile key", profile.Hotkey, true, () => target.SetHotkey(KeyCode.None));
        }
        Check(
            "Teleport Key",
            ModConfig.TeleportToCursorKey,
            true,
            () => ModConfig.TeleportToCursorKey.Value = KeyCode.None
        );
    }

    private static void AddGameConflicts(List<KeyConflict> conflicts, KeyCode key)
    {
        List<KeyBinding> bindings = LazyInput.GameBindings?.keyBindings;
        if (bindings == null)
        {
            return;
        }
        foreach (KeyBinding binding in bindings)
        {
            // Modifier combos (e.g. Ctrl+key) don't fire on the plain key.
            if (binding.keyCode != key || binding.additionalKeyCodes.Length > 0)
            {
                continue;
            }
            // The game mirrors Interaction's key onto SpeechSkip2; listing both would just repeat it.
            if (binding.gameKey.value == GameKey.SpeechSkip2.value)
            {
                continue;
            }
            conflicts.Add(new KeyConflict { Name = $"{GameActionName(binding)} (game)", Replaceable = false });
        }
    }

    /// <summary>
    /// The localized action name the game's Controls window shows. Keys that aren't listed
    /// there often have no locale entry, so fall back to the <c>GameKey</c> field name
    /// ("OpenMap" → "Open Map").
    /// </summary>
    private static string GameActionName(KeyBinding binding)
    {
        string localized = string.IsNullOrEmpty(binding.localeId)
            ? null
            : LLBase.L(binding.localeId);
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }
        string field = Enumeration.GetNameOfStaticField<GameKey>(binding.gameKey.value);
        return string.IsNullOrEmpty(field)
            ? $"action {binding.gameKey.value}"
            : Regex.Replace(field, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}
