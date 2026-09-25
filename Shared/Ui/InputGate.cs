using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// Tells hotkey pollers when a keypress belongs to the mod's UI instead: a key rebind is
/// listening, or a text field has focus (so typing a bound letter doesn't fire it).
/// </summary>
internal static class InputGate
{
    /// <summary>Set by the menu while a rebind row is waiting for a key.</summary>
    public static bool KeyCaptureActive { get; set; }

    public static bool IsBlocked => KeyCaptureActive || IsTextInputFocused();

    private static bool IsTextInputFocused()
    {
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        if (selected == null)
        {
            return false;
        }
        TMP_InputField input = selected.GetComponent<TMP_InputField>();
        return input != null && input.isFocused;
    }
}
