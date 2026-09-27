using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.Settings;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Patching;

/// <summary>
/// Base for gameplay modules that apply Harmony patches gated on a config flag.
/// The patch is applied only while the module's <see cref="EnabledFlag"/> is on and removed
/// when it's off, reacting to runtime toggles. Multiplier-style values are read live inside the
/// patch body, so only the on/off flag drives patch/unpatch — changing a multiplier takes effect
/// immediately.
/// A module with several independent toggles lists them in <see cref="GateFlags"/>; the
/// patches stay applied while any of them is on.
/// </summary>
internal abstract class HarmonyModule : IModule, ISettingsDeclarer
{
    private Harmony _harmony;
    private bool _applied;

    public abstract string Name { get; }

    public virtual int Order => 100;

    /// <summary>The config flag that gates this module's patch (bound in <see cref="DeclareSettings"/>).</summary>
    protected abstract ConfigEntry<bool> EnabledFlag { get; }

    /// <summary>Every flag that needs the patches; defaults to just <see cref="EnabledFlag"/>.</summary>
    protected virtual IEnumerable<ConfigEntry<bool>> GateFlags
    {
        get
        {
            yield return EnabledFlag;
        }
    }

    /// <summary>
    /// True keeps the patches installed regardless of the flags. Only for modules whose effect
    /// also comes from data outside the config (per-zombie overrides stored in the save), so the
    /// patch body must check the flags itself.
    /// </summary>
    protected virtual bool AlwaysPatched => false;

    private bool AnyFlagOn => AlwaysPatched || GateFlags.Any(flag => flag.Value);

    /// <summary>Binds the module's config (including <see cref="EnabledFlag"/>) and declares its menu rows.</summary>
    public abstract void DeclareSettings(SettingsBuilder settings);

    /// <summary>Apply the module's patch(es) with the given Harmony instance.</summary>
    protected abstract void Apply(Harmony harmony);

    public void Enable()
    {
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{Name}");
        foreach (ConfigEntry<bool> flag in GateFlags)
        {
            flag.SettingChanged += OnFlagChanged;
        }
        if (AnyFlagOn)
        {
            ApplyPatches();
        }
    }

    public void Disable()
    {
        foreach (ConfigEntry<bool> flag in GateFlags)
        {
            flag.SettingChanged -= OnFlagChanged;
        }
        RemovePatches();
        _harmony = null;
    }

    private void OnFlagChanged(object sender, EventArgs e)
    {
        if (AnyFlagOn)
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
