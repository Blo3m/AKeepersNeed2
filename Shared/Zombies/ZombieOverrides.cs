namespace AKeepersNeed2.Shared.Zombies;

/// <summary>
/// Per-zombie settings the mod stores in the zombie's own <c>gameRes</c>, which the game saves
/// with the zombie and copies when it changes role (and gets a new id). A missing or 0 value
/// means "no override": the game drops zero entries when copying, so 0 must never mean anything.
/// Writing these changes the save, so the menu only does it behind a confirm dialog.
/// </summary>
internal static class ZombieOverrides
{
    private const string CraftKey = "akn2_craft_speed";
    private const string GatherKey = "akn2_gather_speed";
    private const string WalkKey = "akn2_walk_speed";
    private const string IgnorePerkCapKey = "akn2_ignore_perk_cap";
    private const string PorterCapacityKey = "akn2_porter_capacity";

    public static float? CraftSpeed(ZombieWgoData zombie)
    {
        return Positive(zombie, CraftKey);
    }

    public static float? GatherSpeed(ZombieWgoData zombie)
    {
        return Positive(zombie, GatherKey);
    }

    public static float? WalkSpeed(ZombieWgoData zombie)
    {
        return Positive(zombie, WalkKey);
    }

    public static bool IgnorePerkCap(ZombieWgoData zombie)
    {
        return zombie != null && zombie.GetGameResInt(IgnorePerkCapKey) > 0;
    }

    public static int? PorterCapacity(ZombieWgoData zombie)
    {
        int value = zombie != null
            ? zombie.GetGameResInt(PorterCapacityKey)
            : 0;
        return value > 0
            ? value
            : (int?)null;
    }

    public static void SetCraftSpeed(ZombieWgoData zombie, float? value)
    {
        zombie.SetGameRes(CraftKey, value ?? 0f);
    }

    public static void SetGatherSpeed(ZombieWgoData zombie, float? value)
    {
        zombie.SetGameRes(GatherKey, value ?? 0f);
    }

    public static void SetWalkSpeed(ZombieWgoData zombie, float? value)
    {
        zombie.SetGameRes(WalkKey, value ?? 0f);
    }

    public static void SetIgnorePerkCap(ZombieWgoData zombie, bool value)
    {
        zombie.SetGameRes(IgnorePerkCapKey, value ? 1 : 0);
    }

    public static void SetPorterCapacity(ZombieWgoData zombie, int? value)
    {
        zombie.SetGameRes(PorterCapacityKey, value ?? 0);
    }

    private static float? Positive(ZombieWgoData zombie, string key)
    {
        float value = zombie != null
            ? zombie.GetGameRes(key)
            : 0f;
        return value > 0f
            ? value
            : (float?)null;
    }
}
