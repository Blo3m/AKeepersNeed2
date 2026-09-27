using System.Collections.Generic;
using LazyBearTechnology;

namespace AKeepersNeed2.Modules.ReputationMultiplier;

/// <summary>
/// The resource names that are reputations: every NPC's <c>WGODef.repResName</c>, plus every
/// resource a tech locks behind (<c>TechDef.wgoRepLock</c> for NPCs,
/// <c>districtReputationLock</c> for districts). Built once the game's definitions are loaded.
/// </summary>
internal static class ReputationTypes
{
    private static HashSet<string> _types;

    public static bool Contains(string type)
    {
        return type != null && Load().Contains(type);
    }

    private static HashSet<string> Load()
    {
        if (_types != null)
        {
            return _types;
        }
        GameBalance balance = GameBalance.Me;
        if (balance == null)
        {
            return new HashSet<string>();
        }

        var types = new HashSet<string>();
        foreach (WGODef def in balance.wgoDefs)
        {
            if (!string.IsNullOrEmpty(def?.repResName))
            {
                types.Add(def.repResName);
            }
        }
        foreach (TechDef tech in balance.techDefs)
        {
            AddAll(types, tech?.wgoRepLock);
            AddAll(types, tech?.districtReputationLock);
        }
        if (types.Count == 0)
        {
            return types;
        }
        Plugin.Logger.LogInfo($"[ReputationMultiplier] {types.Count} reputation resources found.");
        _types = types;
        return _types;
    }

    private static void AddAll(HashSet<string> types, GameRes res)
    {
        if (res == null)
        {
            return;
        }
        foreach (GameResAtom atom in res.List)
        {
            if (!string.IsNullOrEmpty(atom.type))
            {
                types.Add(atom.type);
            }
        }
    }
}
