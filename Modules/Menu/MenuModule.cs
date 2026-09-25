using AKeepersNeed2.Core;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The menu module: owns the single window instance, polls the toggle hotkey, and
/// opens/closes the window. The window is built from scratch and styled from the
/// game's assets — see <see cref="AKNMenuWindow"/>.
/// </summary>
internal sealed class MenuModule : IModule, IUpdatable
{
    private AKNMenuWindow _window;

    public string Name => "Menu";

    public int Order => 0;

    public void Enable()
    {
    }

    public void Disable()
    {
        if (_window != null && _window.IsShown)
        {
            _window.Close();
        }
    }

    public void Tick()
    {
        if (Input.GetKeyDown(ModConfig.MenuHotkey.Value))
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        if (_window == null)
        {
            _window = AKNMenuWindow.CreateInstance();
            if (_window == null)
            {
                return;
            }
        }

        if (_window.IsShown)
        {
            _window.Close();
        }
        else
        {
            _window.Open(null);
        }
    }
}
