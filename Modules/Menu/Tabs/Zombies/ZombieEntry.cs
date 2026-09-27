using AKeepersNeed2.Shared.Zombies;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// One placed zombie in the list, kept across reloads (keyed by the live zombie object) so its
/// expanded state and staged edits survive refiltering and reopening the menu.
/// </summary>
internal sealed class ZombieEntry
{
    public ZombieEntry(ZombieWgoData zombie)
    {
        Zombie = zombie;
        RefreshLabels();
    }

    public ZombieWgoData Zombie { get; }

    public string Name { get; private set; }

    public string Details { get; private set; }

    public string Zone { get; private set; }

    public bool Expanded { get; set; }

    /// <summary>Created on first expand; holds the staged edits.</summary>
    public ZombieEdit Edit { get; set; }

    /// <summary>The editor UI while expanded, else null.</summary>
    public ZombieEditor Editor { get; set; }

    public bool IsDirty => Edit != null && Edit.IsDirty;

    public void RefreshLabels()
    {
        Name = ZombieLabels.Name(Zombie);
        Zone = ZombieLabels.Zone(Zombie);
        string station = ZombieLabels.Station(Zombie);
        string job = ZombieLabels.Job(Zombie.ZombieType);
        Details = string.IsNullOrEmpty(station)
            ? $"{job} · {Zone}"
            : $"{job} · {station} · {Zone}";
    }
}
