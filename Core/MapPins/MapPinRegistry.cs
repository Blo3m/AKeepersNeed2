using System;
using System.Collections.Generic;

namespace AKeepersNeed2.Core.MapPins;

/// <summary>
/// Cross-module list of map pins. Modules that mark things on the world map (map markers,
/// bookmarks, NPC faces) register a source; mod-made maps such as the minimap draw every source's
/// pins without knowing which module they come from. A source returns nothing while its feature
/// is off. Sources are called often, so they should return cached lists.
/// </summary>
internal static class MapPinRegistry
{
    private static readonly List<Func<IEnumerable<MapPin>>> Sources = new List<Func<IEnumerable<MapPin>>>();

    public static void Register(Func<IEnumerable<MapPin>> source)
    {
        if (!Sources.Contains(source))
        {
            Sources.Add(source);
        }
    }

    public static void Unregister(Func<IEnumerable<MapPin>> source)
    {
        Sources.Remove(source);
    }

    /// <summary>Every source's pins; a failing source is logged and skipped.</summary>
    public static List<MapPin> All()
    {
        var pins = new List<MapPin>();
        foreach (Func<IEnumerable<MapPin>> source in Sources)
        {
            try
            {
                IEnumerable<MapPin> fromSource = source();
                if (fromSource != null)
                {
                    pins.AddRange(fromSource);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"[MapPins] a pin source failed: {e.Message}");
            }
        }
        return pins;
    }
}
