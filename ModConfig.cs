using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2;

/// <summary>
/// Central config for the mod's playground features. Values persist in
/// BepInEx/config/&lt;GUID&gt;.cfg and are the single source of truth that both the
/// in-game menu and (future) Harmony patches read/write.
/// </summary>
internal static class ModConfig
{
    // --- Menu ---
    public static ConfigEntry<KeyCode> MenuHotkey;
    public static ConfigEntry<float> UiScale; // menu size multiplier, 0.5 .. 3.0

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

    // --- Map (display-only; nothing is written to the save) ---
    public static ConfigEntry<bool> RevealMapEnabled;
    public static ConfigEntry<bool> UnlockMilestonesEnabled;

    public static void Init(ConfigFile config)
    {
        MenuHotkey = config.Bind("Menu", "Hotkey", KeyCode.F1,
            "Key that toggles the mod menu.");
        UiScale = config.Bind("Menu", "UiScale", 1f,
            new ConfigDescription("Menu size multiplier (1 = game default).",
                new AcceptableValueRange<float>(0.5f, 3f)));

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

        RevealMapEnabled = config.Bind("MapReveal", "Enabled", false,
            "Show every map zone and area label as if explored. Display-only.");

        UnlockMilestonesEnabled = config.Bind("MapMilestones", "Enabled", false,
            "Draw every map milestone as activated (teleportable). Display-only.");
    }

    // --- Mapping helpers between a UISlider's 0..100 space and a multiplier ---
    // UISlider always reports 0..100; map it onto each feature's real range.

    public static float SliderToMultiplier(float slider0to100, float min, float max)
        => Mathf.Lerp(min, max, Mathf.Clamp01(slider0to100 / 100f));

    public static float MultiplierToSlider(float multiplier, float min, float max)
        => Mathf.InverseLerp(min, max, multiplier) * 100f;
}
