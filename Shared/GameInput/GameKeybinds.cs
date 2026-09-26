using System.Collections.Generic;
using System.Text.RegularExpressions;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Shared.GameInput;

/// <summary>
/// Read-only view of the game's own keyboard bindings (<c>LazyInput.GameBindings.keyBindings</c>),
/// for telling the player which game action already uses a key.
/// </summary>
internal static class GameKeybinds
{
    /// <summary>
    /// Display names of the game actions that fire on the plain <paramref name="key"/>. Modifier
    /// combos (e.g. Ctrl+key) are skipped, since they don't fire on the plain key, and so is
    /// <c>SpeechSkip2</c>, which the game keeps mirrored to Interaction's key.
    /// </summary>
    public static List<string> ActionsOn(KeyCode key)
    {
        var names = new List<string>();
        List<KeyBinding> bindings = LazyInput.GameBindings?.keyBindings;
        if (bindings == null || key == KeyCode.None)
        {
            return names;
        }
        foreach (KeyBinding binding in bindings)
        {
            if (binding.keyCode != key || binding.additionalKeyCodes.Length > 0)
            {
                continue;
            }
            if (binding.gameKey.value == GameKey.SpeechSkip2.value)
            {
                continue;
            }
            names.Add(ActionName(binding));
        }
        return names;
    }

    /// <summary>
    /// The localized action name the game's Controls window shows. Keys that aren't listed
    /// there often have no locale entry, so fall back to the <c>GameKey</c> field name
    /// ("OpenMap" → "Open Map").
    /// </summary>
    private static string ActionName(KeyBinding binding)
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
