using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AKeepersNeed2.Core;

/// <summary>
/// Discovers every <see cref="IModule"/> in this assembly, enables them in
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
        foreach (IModule module in Discover())
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

    private static IEnumerable<IModule> Discover()
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

        return types
            .Where(t => t != null && !t.IsAbstract && !t.IsInterface && typeof(IModule).IsAssignableFrom(t))
            .Select(t => (IModule)Activator.CreateInstance(t))
            .OrderBy(m => m.Order)
            .ToList();
    }
}
