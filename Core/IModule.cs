namespace AKeepersNeed2.Core;

/// <summary>
/// A self-contained feature of the mod, discovered and enabled at startup by
/// <see cref="ModuleRegistry"/>. Each module owns exactly one concern.
///
/// A module that owns config also implements <see cref="Settings.ISettingsDeclarer"/>; its
/// settings are declared before any module is enabled. Gameplay (Harmony) modules extend
/// <c>Shared.Patching.HarmonyModule</c>, which applies the patch only while the module's
/// enabled flag is on and reacts to runtime toggles.
/// </summary>
internal interface IModule
{
    string Name { get; }

    /// <summary>Lower values enable first. Leave at 0 when order doesn't matter.</summary>
    int Order { get; }

    void Enable();

    void Disable();
}
