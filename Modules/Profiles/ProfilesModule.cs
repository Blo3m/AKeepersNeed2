using AKeepersNeed2.Core;
using AKeepersNeed2.Shared.Profiles;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;

namespace AKeepersNeed2.Modules.Profiles;

/// <summary>
/// Loads the settings profiles before any gameplay module enables (so they start with the
/// active profile's values), drives the store's debounced saving, and polls the
/// previous/next and per-profile switch hotkeys, confirming hotkey switches with a toast.
/// </summary>
internal sealed class ProfilesModule : IModule, IUpdatable
{
    private ProfileToast _toast;

    public string Name => "Profiles";

    public int Order => -100;

    public void Enable()
    {
        ProfileStore.Init();
    }

    public void Disable()
    {
        ProfileStore.Shutdown();
        if (_toast != null)
        {
            Object.Destroy(_toast.gameObject);
            _toast = null;
        }
    }

    public void Tick()
    {
        ProfileStore.Tick();
        if (InputGate.IsBlocked)
        {
            return;
        }

        Profile before = ProfileStore.Active;
        if (Pressed(ModConfig.PreviousProfileKey.Value))
        {
            ProfileStore.SwitchRelative(-1);
        }
        else if (Pressed(ModConfig.NextProfileKey.Value))
        {
            ProfileStore.SwitchRelative(1);
        }
        else
        {
            foreach (Profile profile in ProfileStore.Profiles)
            {
                if (Pressed(profile.Hotkey.Value))
                {
                    ProfileStore.Switch(profile);
                    break;
                }
            }
        }

        if (ProfileStore.Active != before)
        {
            ShowToast($"Profile: {ProfileStore.Active.Name}");
        }
    }

    private void ShowToast(string message)
    {
        if (_toast == null)
        {
            _toast = ProfileToast.Create();
            if (_toast == null)
            {
                return;
            }
        }
        _toast.Show(message);
    }

    private static bool Pressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }
}
