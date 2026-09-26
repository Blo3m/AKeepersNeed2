using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2;

/// <summary>
/// Central config for the mod's playground features — the single source of truth that both
/// the in-game menu and the Harmony patches read/write. Global settings (menu, profile keys)
/// persist in BepInEx/config/&lt;GUID&gt;.cfg. Gameplay settings live in the in-memory
/// <see cref="ProfileSettings"/> file, which <c>Shared.Profiles.ProfileStore</c> fills from and
/// writes back to the active profile's own .cfg.
/// </summary>
internal static class ModConfig
{
    public static readonly string ProfilesDirectory =
        Path.Combine(Path.Combine(Paths.ConfigPath, "AKeepersNeed2"), "profiles");

    /// <summary>The plugin's main .cfg (global settings only).</summary>
    public static ConfigFile Main;

    /// <summary>
    /// Live gameplay settings. Never saved to disk itself — it only mirrors the active
    /// profile, so every gameplay entry must be bound here.
    /// </summary>
    public static ConfigFile ProfileSettings;

    // --- Menu ---
    public static ConfigEntry<KeyCode> MenuHotkey;
    public static ConfigEntry<float> UiScale; // menu size multiplier, 0.5 .. 3.0

    // --- Profiles ---
    public static ConfigEntry<string> ActiveProfile;
    public static ConfigEntry<KeyCode> PreviousProfileKey;
    public static ConfigEntry<KeyCode> NextProfileKey;

    // --- Energy regen (passive, while not sleeping) ---
    public static ConfigEntry<bool> EnergyRegenEnabled;
    public static ConfigEntry<float> EnergyRegenRate; // energy per 5s, 0.1 .. 10.0

    // --- No energy drain ---
    public static ConfigEntry<bool> NoEnergyDrainEnabled;

    // --- Resource drops (loot from destroyed/harvested world objects) ---
    public static ConfigEntry<bool> ResourceDropsEnabled;
    public static ConfigEntry<float> ResourceDropMultiplier; // 1x .. 10x

    // --- Craft drops (craft-station output quantity) ---
    public static ConfigEntry<bool> CraftDropsEnabled;
    public static ConfigEntry<float> CraftDropMultiplier; // 1x .. 10x

    // --- Technology points (red/green/blue rewards) ---
    public static ConfigEntry<bool> TechPointsEnabled;
    public static ConfigEntry<float> TechPointsMultiplier; // 1x .. 10x

    // --- Crafting & building costs ---
    public static ConfigEntry<bool> FreeCraftingEnabled;
    public static ConfigEntry<bool> FreeBuildingEnabled;
    public static ConfigEntry<bool> InstantCraftEnabled;

    // --- Map (display-only; nothing is written to the save) ---
    public static ConfigEntry<bool> RevealMapEnabled;
    public static ConfigEntry<bool> UnlockMilestonesEnabled;
    public static ConfigEntry<bool> MapNpcsEnabled;

    // --- Map teleport (moves the player; not display-only) ---
    public static ConfigEntry<bool> MapNpcsTeleportEnabled;
    public static ConfigEntry<bool> MapTeleportEnabled;

    public static void Init(ConfigFile config)
    {
        Main = config;

        MenuHotkey = config.Bind("Menu", "Hotkey", KeyCode.F1,
            "Key that toggles the mod menu.");
        UiScale = config.Bind("Menu", "UiScale", 1f,
            new ConfigDescription("Menu size multiplier (1 = game default).",
                new AcceptableValueRange<float>(0.5f, 3f)));

        ActiveProfile = config.Bind("Profiles", "Active", "Default",
            "Name of the active settings profile (a file in config/AKeepersNeed2/profiles).");
        PreviousProfileKey = config.Bind("Profiles", "PreviousKey", KeyCode.F2,
            "Key that switches to the previous profile.");
        NextProfileKey = config.Bind("Profiles", "NextKey", KeyCode.F3,
            "Key that switches to the next profile.");

        // The path is never written: SaveOnConfigSet is off and Save() is never called.
        ProfileSettings = new ConfigFile(Path.Combine(ProfilesDirectory, ".live"), false)
        {
            SaveOnConfigSet = false,
        };
        BindProfileSettings(ProfileSettings);
    }

    private static void BindProfileSettings(ConfigFile config)
    {
        EnergyRegenEnabled = config.Bind("EnergyRegen", "Enabled", false,
            "Passively regenerate energy while not sleeping.");
        EnergyRegenRate = config.Bind("EnergyRegen", "RatePer5s", 1f,
            new ConfigDescription("Energy regenerated every 5 seconds while not sleeping.",
                new AcceptableValueRange<float>(0.1f, 10f)));

        NoEnergyDrainEnabled = config.Bind("NoEnergyDrain", "Enabled", false,
            "Prevent all energy loss — energy never drains.");

        ResourceDropsEnabled = config.Bind("ResourceDrops", "Enabled", false,
            "Multiply loot dropped when a world object is destroyed/harvested.");
        ResourceDropMultiplier = config.Bind("ResourceDrops", "Multiplier", 1f,
            new ConfigDescription("Resource drop multiplier.",
                new AcceptableValueRange<float>(1f, 10f)));

        CraftDropsEnabled = config.Bind("CraftDrops", "Enabled", false,
            "Multiply the output quantity of craft stations.");
        CraftDropMultiplier = config.Bind("CraftDrops", "Multiplier", 1f,
            new ConfigDescription("Craft output multiplier.",
                new AcceptableValueRange<float>(1f, 10f)));

        TechPointsEnabled = config.Bind("TechPoints", "Enabled", false,
            "Multiply technology-point rewards (red/green/blue).");
        TechPointsMultiplier = config.Bind("TechPoints", "Multiplier", 1f,
            new ConfigDescription("Technology point multiplier.",
                new AcceptableValueRange<float>(1f, 10f)));

        FreeCraftingEnabled = config.Bind("FreeCrafting", "Enabled", false,
            "Craft without the required items; nothing is consumed (includes alchemy and tool durability).");
        FreeBuildingEnabled = config.Bind("FreeBuilding", "Enabled", false,
            "Build without the required items; nothing is consumed (includes town buildings).");
        InstantCraftEnabled = config.Bind("InstantCraft", "Enabled", false,
            "Crafts finish immediately. Garden growing, star and autopsy crafts keep their normal time.");

        RevealMapEnabled = config.Bind("MapReveal", "Enabled", false,
            "Show every map zone and area label as if explored. Display-only.");

        UnlockMilestonesEnabled = config.Bind("MapMilestones", "Enabled", false,
            "Draw every map milestone as activated (teleportable). Display-only.");

        MapNpcsEnabled = config.Bind("MapNpcs", "Enabled", false,
            "Show NPCs that have a portrait on the world map, with their face and name on hover.");
        MapNpcsTeleportEnabled = config.Bind("MapNpcs", "ShiftClickTeleport", false,
            "Shift-click an NPC's face on the map to teleport next to them. Needs MapNpcs on.");

        MapTeleportEnabled = config.Bind("MapTeleport", "Enabled", false,
            "Shift-click the world map to teleport to the nearest walkable spot under the cursor.");
    }

    // --- Mapping helpers between a UISlider's 0..100 space and a multiplier ---
    // UISlider always reports 0..100; map it onto each feature's real range.

    public static float SliderToMultiplier(float slider0to100, float min, float max)
        => Mathf.Lerp(min, max, Mathf.Clamp01(slider0to100 / 100f));

    public static float MultiplierToSlider(float multiplier, float min, float max)
        => Mathf.InverseLerp(min, max, multiplier) * 100f;
}
