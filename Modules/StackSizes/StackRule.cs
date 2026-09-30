using System;
using BepInEx.Configuration;
using UnityEngine;

namespace AKeepersNeed2.Modules.StackSizes;

/// <summary>One owner's stack settings: on/off, mode, and the value for each mode.</summary>
internal sealed class StackRule
{
    public StackRule(
        ConfigEntry<bool> enabled,
        ConfigEntry<StackMode> mode,
        ConfigEntry<float> multiplier,
        ConfigEntry<float> fixedSize
    )
    {
        Enabled = enabled;
        Mode = mode;
        Multiplier = multiplier;
        FixedSize = fixedSize;
    }

    public ConfigEntry<bool> Enabled { get; }

    public ConfigEntry<StackMode> Mode { get; }

    public ConfigEntry<float> Multiplier { get; }

    public ConfigEntry<float> FixedSize { get; }

    /// <summary>
    /// The limit for an item whose vanilla stack is <paramref name="vanilla"/>. Never below vanilla,
    /// and items that don't stack (vanilla 1: tools, bags, unique items) are left alone, because the
    /// game treats a limit of 1 as "unique" and moves such items differently.
    /// </summary>
    public int Limit(int vanilla)
    {
        if (!Enabled.Value || vanilla <= 1)
        {
            return vanilla;
        }
        // Rounded like the sliders show them ("x3", "250").
        float wanted = Mode.Value == StackMode.Fixed
            ? Mathf.Round(FixedSize.Value)
            : vanilla * Mathf.Round(Multiplier.Value);
        return Math.Max(vanilla, Mathf.RoundToInt(Mathf.Min(wanted, int.MaxValue / 2f)));
    }
}
