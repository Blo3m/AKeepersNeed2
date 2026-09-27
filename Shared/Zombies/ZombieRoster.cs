using System.Collections.Generic;
using LazyBearTechnology;

namespace AKeepersNeed2.Shared.Zombies;

/// <summary>
/// The zombies placed in the world. <c>ZombieSystemData.zombieOnSceneWgoIds</c> lists exactly
/// those (its <c>Cache</c> also holds carried/stored bodies), resolved through
/// <c>WorldData.GetWgoData</c>, which covers every scene, loaded or not.
/// </summary>
internal static class ZombieRoster
{
    /// <summary>Every placed zombie; empty while no game is loaded.</summary>
    public static List<ZombieWgoData> Placed()
    {
        var zombies = new List<ZombieWgoData>();
        ZombieSystemData system = MainGame.ZombieSystemData;
        WorldData world = MainGame.WorldData;
        if (system == null || world == null)
        {
            return zombies;
        }
        foreach (SGuid id in new List<SGuid>(system.zombieOnSceneWgoIds))
        {
            if (world.GetWgoData(id) is ZombieWgoData zombie)
            {
                zombies.Add(zombie);
            }
        }
        return zombies;
    }
}
