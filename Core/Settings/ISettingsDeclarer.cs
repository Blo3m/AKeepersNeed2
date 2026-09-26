namespace AKeepersNeed2.Core.Settings;

/// <summary>
/// A module that owns config settings. <see cref="ModuleRegistry"/> calls
/// <see cref="DeclareSettings"/> on every module before any is enabled, so all entries exist
/// before profiles load and before modules read them.
/// </summary>
internal interface ISettingsDeclarer
{
    /// <summary>Bind this module's config entries (keep them in fields) and declare their menu rows.</summary>
    void DeclareSettings(SettingsBuilder settings);
}
