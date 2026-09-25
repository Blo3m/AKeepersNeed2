namespace AKeepersNeed2.Core;

/// <summary>
/// A self-contained feature of the mod, discovered and enabled at startup by
/// <see cref="ModuleRegistry"/>. Each module owns exactly one concern.
///
/// Convention for gameplay (Harmony) modules: apply the patch in <see cref="Enable"/>
/// only when the module's <c>ModConfig</c> flag is on, and remove it in
/// <see cref="Disable"/> — patch application is tied to the config flag rather than
/// left always-on. React to runtime toggles via the config entry's change event.
/// </summary>
internal interface IModule
{
    string Name { get; }

    /// <summary>Lower values enable first. Leave at 0 when order doesn't matter.</summary>
    int Order { get; }

    void Enable();

    void Disable();
}
