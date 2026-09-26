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

    [Section(MenuTab.Player, "Tech Points", 50)]
    TechPoints,

    [Section(MenuTab.Player, "Reputation", 60)]
    Reputation,

    [Section(MenuTab.Drops, "Drops", 10)]
    Drops,

    [Section(MenuTab.Drops, "Crafting", 20)]
    CraftOutput,

    [Section(MenuTab.Crafting, "Crafting", 10)]
    Crafting,

    [Section(MenuTab.Crafting, "Building", 20)]
    Building,

    [Section(MenuTab.Map, "Map", 10)]
    Map,
}
