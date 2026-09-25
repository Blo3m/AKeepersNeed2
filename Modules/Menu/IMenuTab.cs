using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// A page in the mod menu, shown when its bottom tab is selected. To add a tab, implement
/// this and register it in <see cref="AKNMenuWindow"/>'s tab list.
/// </summary>
internal interface IMenuTab
{
    /// <summary>Label shown on the bottom tab button.</summary>
    string Title { get; }

    /// <summary>Build the tab's content into <paramref name="content"/> (already inset/padded).</summary>
    void Build(RectTransform content);

    /// <summary>
    /// Re-read displayed values from config. Called when the menu opens and after a profile
    /// switch or reset changes the values behind the controls.
    /// </summary>
    void Refresh();
}
