using System;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Which legend group a resource node goes in, from its <c>wgoGroup</c> and first drop. Seen in a
/// save: trees/stumps drop wood or sticks, bushes sticks, collectable bushes berries and flowers,
/// stones drop stone, iron ore or (broken barrels) flitch; mushrooms, flowers, clay, sand, marble
/// and honey spots have no group.
/// </summary>
internal sealed class ResourceGroup
{
    private static readonly ResourceGroup Trees = new ResourceGroup("trees", "Trees", 10);
    private static readonly ResourceGroup Bushes = new ResourceGroup("bushes", "Bushes", 20);
    private static readonly ResourceGroup Plants = new ResourceGroup("plants", "Berries & Plants", 30);
    private static readonly ResourceGroup Mushrooms = new ResourceGroup("mushrooms", "Mushrooms", 40);
    private static readonly ResourceGroup Stones = new ResourceGroup("stones", "Stones & Ores", 50);
    private static readonly ResourceGroup Junk = new ResourceGroup("junk", "Junk", 60);
    private static readonly ResourceGroup Other = new ResourceGroup("other", "Other Resources", 70);

    private ResourceGroup(string key, string title, int order)
    {
        Key = key;
        Title = title;
        Order = order;
    }

    public string Key { get; }

    public string Title { get; }

    public int Order { get; }

    public static ResourceGroup For(string wgoGroup, string drop)
    {
        drop = drop ?? string.Empty;
        switch (wgoGroup)
        {
            case "trees":
            case "stumps":
                return Trees;
            case "bushes":
                return Bushes;
            case "collectable_bushes":
                return Plants;
        }
        if (drop.Length == 0 || drop == "flitch")
        {
            return Junk;
        }
        if (drop.StartsWith("mushroom", StringComparison.Ordinal))
        {
            return Mushrooms;
        }
        if (wgoGroup == "stones" || IsMineral(drop))
        {
            return Stones;
        }
        if (drop.StartsWith("flower", StringComparison.Ordinal)
            || drop.StartsWith("berry", StringComparison.Ordinal)
            || drop == "honey")
        {
            return Plants;
        }
        return Other;
    }

    private static bool IsMineral(string drop)
    {
        return drop.Contains("ore") || drop.Contains("stone") || drop.Contains("marble") || drop.Contains("nugget")
            || drop == "clay" || drop == "sand";
    }
}
