namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// A mod-added map element that may handle its own shift-click, so map-point teleport stays
/// out of the way when it does.
/// </summary>
internal interface IMapClickTarget
{
    bool HandlesShiftClick { get; }
}
