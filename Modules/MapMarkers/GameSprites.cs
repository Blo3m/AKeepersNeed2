using System.Collections.Generic;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Looks up the game's UI sprites by name without its "missing sprite" warnings: the collection's
/// private name → atlas table is checked first, since icons like <c>i_b_&lt;object id&gt;</c> only
/// exist for some objects.
/// </summary>
internal static class GameSprites
{
    private static readonly AccessTools.FieldRef<EasySpritesCollection, Dictionary<string, string>> NamesRef =
        AccessTools.FieldRefAccess<EasySpritesCollection, Dictionary<string, string>>("hash");

    /// <summary>The first of <paramref name="names"/> the game has, or null.</summary>
    public static Sprite First(params string[] names)
    {
        EasySpritesCollection sprites = EasySpritesCollection.Instance;
        if (sprites == null)
        {
            return null;
        }
        sprites.Initialize();
        Dictionary<string, string> known = NamesRef(sprites);
        foreach (string name in names)
        {
            if (!string.IsNullOrEmpty(name) && known != null && known.ContainsKey(name))
            {
                return sprites.GetSprite(name);
            }
        }
        return null;
    }
}
