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
}
