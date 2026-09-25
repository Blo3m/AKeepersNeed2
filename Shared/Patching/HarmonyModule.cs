using System;
using AKeepersNeed2.Core;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Patching;

/// <summary>
/// Base for gameplay modules that apply Harmony patches gated on a config flag.
/// Per <c>Docs/ARCHITECTURE.md</c>: the patch is applied only while the module's
/// <see cref="EnabledFlag"/> is on and removed when it's off, reacting to runtime
/// toggles. Multiplier-style values are read live inside the patch body, so only the
/// on/off flag drives patch/unpatch — changing a multiplier takes effect immediately.
/// </summary>
internal abstract class HarmonyModule : IModule
{
    private Harmony _harmony;
    private bool _applied;

    public abstract string Name { get; }

    public virtual int Order => 100;

    /// <summary>The config flag that gates this module's patch.</summary>
    protected abstract ConfigEntry<bool> EnabledFlag { get; }

    /// <summary>Apply the module's patch(es) with the given Harmony instance.</summary>
    protected abstract void Apply(Harmony harmony);

    public void Enable()
    {
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{Name}");
        EnabledFlag.SettingChanged += OnFlagChanged;
        if (EnabledFlag.Value)
        {
            ApplyPatches();
        }
    }

    public void Disable()
    {
        EnabledFlag.SettingChanged -= OnFlagChanged;
        RemovePatches();
        _harmony = null;
    }

    private void OnFlagChanged(object sender, EventArgs e)
    {
        if (EnabledFlag.Value)
        {
            ApplyPatches();
        }
        else
        {
            RemovePatches();
        }
    }

    private void ApplyPatches()
    {
        if (_applied)
        {
            return;
        }
        Apply(_harmony);
        _applied = true;
        Plugin.Logger.LogInfo($"[{Name}] patches applied.");
    }

    private void RemovePatches()
    {
        if (!_applied)
        {
            return;
        }
        _harmony.UnpatchSelf();
        _applied = false;
        Plugin.Logger.LogInfo($"[{Name}] patches removed.");
    }
}
