namespace AKeepersNeed2.Core.Settings;

/// <summary>
/// Every menu section, defined once so no two modules can disagree about a section's place.
/// Modules put their rows in one of these; the menu fills the ones it builds by hand
/// (profiles, UI scale, tech point/reputation setters). Orders step by 10 so a section can be
/// slotted in between. Add a member here to create a new section.
/// </summary>
internal enum MenuSection
{
    [Section(MenuTab.Settings, "Profile", 10)]
    Profile,

    [Section(MenuTab.Settings, "Interface", 20)]
    Interface,

    [Section(MenuTab.Settings, "Controls", 30)]
    Controls,

    [Section(MenuTab.Player, "Energy", 10)]
    Energy,

    [Section(MenuTab.Player, "Combat", 20)]
    Combat,

    [Section(MenuTab.Player, "Gathering", 30)]
    Gathering,

    [Section(MenuTab.Player, "Movement", 40)]
    Movement,

    [Section(MenuTab.Player, "Tools", 50)]
    Tools,

    [Section(MenuTab.Player, "Money", 60)]
    Money,

    [Section(MenuTab.World, "Time", 10)]
    Time,

    [Section(MenuTab.Progression, "Tech Points", 10)]
    TechPoints,

    [Section(MenuTab.Progression, "Reputation", 20)]
    Reputation,

    [Section(MenuTab.Drops, "Drops", 10)]
    Drops,

    [Section(MenuTab.Drops, "Crafting", 20)]
    CraftOutput,

    [Section(MenuTab.Crafting, "Crafting", 10)]
    Crafting,

    [Section(MenuTab.Crafting, "Building", 20)]
    Building,

    [Section(MenuTab.Zombies, "Work", 10)]
    ZombieWork,

    [Section(MenuTab.Zombies, "Mastery", 20)]
    ZombieMastery,

    [Section(MenuTab.Zombies, "Porters", 30)]
    ZombiePorters,

    [Section(MenuTab.Garden, "Growth", 10)]
    Growth,

    [Section(MenuTab.Fishing, "Fishing", 10)]
    Fishing,

    [Section(MenuTab.Map, "Map", 10)]
    Map,
}
