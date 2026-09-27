using System.Text;
using LazyBearTechnology;

namespace AKeepersNeed2.Shared.Zombies;

/// <summary>
/// Display text for a zombie. The game localizes names, stations (<c>LLBase.L(wgo.id)</c>) and
/// world zones (<c>"wz_" + id</c>), but has no strings for zombie jobs or talent branch names,
/// so those are the mod's own English labels.
/// </summary>
internal static class ZombieLabels
{
    public static string Name(ZombieWgoData zombie)
    {
        return string.IsNullOrEmpty(zombie.Name)
            ? "Zombie"
            : LLBase.L(zombie.Name);
    }

    /// <summary>"ConveyorCrafter" → "Conveyor Crafter"; <c>Free</c> (no station) reads "Free".</summary>
    public static string Job(ZombieType type)
    {
        string name = type.ToString();
        var spaced = new StringBuilder(name.Length + 2);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                spaced.Append(' ');
            }
            spaced.Append(name[i]);
        }
        return spaced.ToString();
    }

    /// <summary>The attached station's name, or empty when it has none.</summary>
    public static string Station(ZombieWgoData zombie)
    {
        WgoData station = zombie.AttachedWgoData;
        return station != null
            ? LLBase.L(station.id)
            : string.Empty;
    }

    /// <summary>The world zone's name, falling back to the scene id.</summary>
    public static string Zone(ZombieWgoData zombie)
    {
        WorldZoneData zone = zombie.WorldZoneData;
        return zone != null
            ? LLBase.L("wz_" + zone.id)
            : zombie.WorldId ?? string.Empty;
    }

    /// <summary>"talent_orange" → "Orange".</summary>
    public static string Branch(string talentId)
    {
        string colour = talentId.StartsWith("talent_")
            ? talentId.Substring("talent_".Length)
            : talentId;
        return colour.Length == 0
            ? talentId
            : char.ToUpper(colour[0]) + colour.Substring(1);
    }

    /// <summary>The game's talent icon glyph followed by the colour name.</summary>
    public static string BranchWithIcon(string talentId)
    {
        return $"<sprite name=\"{talentId}\"> {Branch(talentId)}";
    }

    public static string Perk(TalentLevelUpDef def)
    {
        return LLBase.L(def.id);
    }
}
