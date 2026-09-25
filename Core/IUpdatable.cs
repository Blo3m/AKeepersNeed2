namespace AKeepersNeed2.Core;

/// <summary>
/// Implemented by a module that needs a per-frame tick (e.g. polling input).
/// <see cref="ModuleRegistry"/> forwards <c>Plugin.Update</c> to it.
/// </summary>
internal interface IUpdatable
{
    void Tick();
}
