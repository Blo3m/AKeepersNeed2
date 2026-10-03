namespace AKeepersNeed2.Shared.Garden;

/// <summary>
/// Garden and vineyard beds. A bed with a crop carries the seed's mastery requirement in its
/// <c>seed_mastery_lock</c> resource (set when planting, still set when the grown bed is harvested),
/// so that marks a crop bed; the game itself uses it to treat harvesting as 1 damage per hit.
/// </summary>
internal static class GardenBeds
{
    private const string SeedLock = "seed_mastery_lock";

    /// <summary>True for a bed holding a crop, including a ready one being harvested.</summary>
    public static bool HasCrop(WgoData wgo)
    {
        return wgo != null && wgo.GetGameResInt(SeedLock) > 0;
    }

    /// <summary>
    /// Any garden or vineyard bed, empty or planted. The game's <c>GardenBedNavigation.IsGardenPlot</c>
    /// only knows the empty bed objects (wgoGroup <c>garden_bed</c> / <c>vineyard_objects</c>); a
    /// planted bed is swapped for a crop object outside those groups, so it's recognised by its crop.
    /// </summary>
    public static bool IsBed(WgoData wgo)
    {
        return GardenBedNavigation.IsGardenPlot(wgo) || HasCrop(wgo);
    }
}
