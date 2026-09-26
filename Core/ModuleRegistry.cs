using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AKeepersNeed2.Core.Settings;

namespace AKeepersNeed2.Core;

/// <summary>
/// Discovers every <see cref="IModule"/> in this assembly, collects their settings
/// (<see cref="ISettingsDeclarer"/>) before anything runs, enables them in
/// <see cref="IModule.Order"/> order, and forwards per-frame ticks to those that
/// implement <see cref="IUpdatable"/>. Enable/Disable/Tick are individually guarded so
/// one misbehaving module cannot take down the others.
/// </summary>
internal static class ModuleRegistry
{
    private static readonly List<IModule> _enabled = new List<IModule>();
    private static readonly List<IUpdatable> _updatables = new List<IUpdatable>();

    public static void EnableAll()
    {
        List<IModule> modules = Discover();

        // Every module's settings are bound before any module enables, so profiles (loaded by the
        // first-enabled Profiles module) see every per-profile entry.
        foreach (IModule module in modules)
        {
            if (!(module is ISettingsDeclarer declarer))
            {
                continue;
            }
            try
            {
                declarer.DeclareSettings(new SettingsBuilder(module.Name));
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Modules] {module.Name} failed to declare settings: {ex}");
            }
        }
        SettingsRegistry.WarnAboutTies();

        foreach (IModule module in modules)
        {
            try
            {
                module.Enable();
                _enabled.Add(module);
                if (module is IUpdatable updatable)
                {
                    _updatables.Add(updatable);
                }
                Plugin.Logger.LogInfo($"[Modules] enabled {module.Name}.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Modules] {module.Name} failed to enable: {ex}");
            }
        }
    }

    public static void Tick()
    {
        foreach (IUpdatable updatable in _updatables)
        {
            try
            {
                updatable.Tick();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Modules] tick failed: {ex}");
            }
        }
    }

    public static void DisableAll()
    {
        for (int i = _enabled.Count - 1; i >= 0; i--)
        {
            try
            {
                _enabled[i].Disable();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Modules] {_enabled[i].Name} failed to disable: {ex}");
            }
        }
        _enabled.Clear();
        _updatables.Clear();
    }

    private static List<IModule> Discover()
    {
        Type[] types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray();
        }

        var modules = new List<IModule>();
        foreach (Type type in types)
        {
            if (type == null || !IsModuleType(type))
            {
                continue;
            }
            try
            {
                modules.Add((IModule)Activator.CreateInstance(type));
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Modules] couldn't create {type.Name}: {ex}");
            }
        }
        return modules.OrderBy(m => m.Order).ToList();
    }

    // Mono resolves a type's fields lazily, so checking a type whose field needs a missing
    // assembly (e.g. System.ValueTuple) throws here. Skip it rather than lose every module.
    private static bool IsModuleType(Type type)
    {
        try
        {
            return !type.IsAbstract && !type.IsInterface && typeof(IModule).IsAssignableFrom(type);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"[Modules] skipped type {type.FullName}: {ex.Message}");
            return false;
        }
    }
}
