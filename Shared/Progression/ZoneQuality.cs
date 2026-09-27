using System.Collections.Generic;
using LazyBearTechnology;

namespace AKeepersNeed2.Shared.Progression;

/// <summary>
/// Zone quality (graveyard, church, …) and town quality, set permanently (the menu confirms first).
/// A zone's total is its objects' quality plus <c>WorldZoneData.additionalQuality</c>, so setting
/// the total means setting the additional part to the difference. Raising a zone past its best
/// ever quality pays that zone's milestone rewards once (the game never takes them back), and
/// graveyard quality 200 unlocks a Steam achievement. Town quality (<c>TownSystem.Quality</c>) is
/// also the happiness cap.
/// </summary>
internal static class ZoneQuality
{
    /// <summary>The zones the game shows a quality for (its zone widget's rule).</summary>
    public static List<WorldZoneData> Zones()
    {
        var zones = new List<WorldZoneData>();
        WorldData world = MainGame.Instance?.GameSave?.worldData;
        if (world == null)
        {
            return zones;
        }
        foreach (GameSceneData scene in world.gameSceneDataList)
        {
            foreach (WorldZoneData zone in scene.worldZones)
            {
                WorldZoneDef.DisplayType display = zone.Definition?.displayType ?? WorldZoneDef.DisplayType.None;
                if (zone.IsContainer
                    && display != WorldZoneDef.DisplayType.Hidden
                    && display != WorldZoneDef.DisplayType.None)
                {
                    zones.Add(zone);
                }
            }
        }
        return zones;
    }

    public static string Name(WorldZoneData zone)
    {
        return LLBase.L("wz_" + zone.id);
    }

    public static int Total(WorldZoneData zone)
    {
        return UnityEngine.Mathf.RoundToInt(zone.GetTotalQuality());
    }

    /// <summary>Sets the zone's total by adjusting its additional quality.</summary>
    public static void SetTotal(WorldZoneData zone, int total)
    {
        int fromObjects = Total(zone) - zone.AdditionalQuality;
        zone.AdditionalQuality = total - fromObjects;
        Plugin.Logger.LogInfo($"[Progression] {zone.id} quality set to {total}.");
    }

    public static int? TownQuality()
    {
        return MainGame.Instance?.GameSave?.townSystem?.Quality;
    }

    public static void SetTownQuality(int value)
    {
        TownSystem town = MainGame.Instance?.GameSave?.townSystem;
        if (town != null)
        {
            town.Quality = value;
            Plugin.Logger.LogInfo($"[Progression] town quality set to {value}.");
        }
    }
}
