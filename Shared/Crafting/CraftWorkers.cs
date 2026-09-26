using System;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Crafting;

/// <summary>Who is doing a craft: the player or a zombie worker.</summary>
internal static class CraftWorkers
{
    // CraftParamsData.WgoData is private.
    private static readonly Func<CraftParamsData, WgoData> GetWgoData =
        AccessTools.MethodDelegate<Func<CraftParamsData, WgoData>>(
            AccessTools.PropertyGetter(typeof(CraftParamsData), "WgoData")
        );

    /// <summary>
    /// True when a zombie works this craft, using its own mastery. Mirrors
    /// <c>CraftParamsData.MasteryValue</c>'s own split: a worker that isn't a Unity object is a
    /// zombie; otherwise the player's mastery applies.
    /// </summary>
    public static bool IsZombieCraft(CraftParamsData data)
    {
        WgoData wgo = GetWgoData(data);
        return wgo != null && wgo.Worker != null && !(wgo.Worker is UnityEngine.Object);
    }
}
