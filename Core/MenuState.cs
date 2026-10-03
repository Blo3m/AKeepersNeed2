namespace AKeepersNeed2.Core;

/// <summary>
/// Whether the mod menu is open. The Menu module sets it; other modules read it (the minimap can
/// only be moved and resized while the menu is open) without referencing the Menu module.
/// </summary>
internal static class MenuState
{
    public static bool IsOpen { get; set; }
}
