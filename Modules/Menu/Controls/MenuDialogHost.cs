using System;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Controls;

/// <summary>
/// Keeps at most one <see cref="MenuDialog"/> open under the menu window: showing a dialog
/// closes the previous one, and the window closes it on hide or Back.
/// </summary>
internal sealed class MenuDialogHost
{
    private readonly Transform _parent;
    private GameObject _dialog;

    public MenuDialogHost(Transform parent)
    {
        _parent = parent;
    }

    /// <summary>True while a dialog is open (Unity's null check: its buttons destroy it directly).</summary>
    public bool IsOpen => _dialog != null;

    public void ShowConfirm(string title, string message, string confirmLabel, Action onConfirm, float height = 132f)
    {
        Close();
        _dialog = MenuDialog.ShowConfirm(_parent, title, message, confirmLabel, onConfirm, height);
    }

    public void ShowMessage(string title, string message, float height = 132f)
    {
        Close();
        _dialog = MenuDialog.ShowMessage(_parent, title, message, height);
    }

    public void ShowNamePrompt(string title, string initial, int maxLength, Func<string, string> submit)
    {
        Close();
        _dialog = MenuDialog.ShowNamePrompt(_parent, title, initial, maxLength, submit);
    }

    public void Close()
    {
        if (_dialog != null)
        {
            UnityEngine.Object.Destroy(_dialog);
            _dialog = null;
        }
    }
}
