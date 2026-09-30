using System.Collections.Generic;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// A legend group: a resource group (Trees, Stones &amp; Ores, …) holding one type per drop item, or
/// a single-type category (Chests, Quest Givers, …).
/// </summary>
internal sealed class MarkerGroup
{
    public MarkerGroup(string key, string title, int order, bool isResource)
    {
        Key = key;
        Title = title;
        Order = order;
        IsResource = isResource;
    }

    public string Key { get; }

    public string Title { get; }

    public int Order { get; }

    public bool IsResource { get; }

    public List<MarkerType> Types { get; } = new List<MarkerType>();
}

/// <summary>One tick in the legend: what's filtered on, with its icon and how many are on the map.</summary>
internal sealed class MarkerType
{
    public MarkerType(string key, string label, Sprite icon, MarkerGroup group)
    {
        Key = key;
        Label = label;
        Icon = icon;
        Group = group;
    }

    /// <summary>Saved in the filter list, e.g. <c>res:stones:iron_ore</c> or <c>chest</c>.</summary>
    public string Key { get; }

    public string Label { get; }

    /// <summary>Null draws a plain dot.</summary>
    public Sprite Icon { get; }

    public MarkerGroup Group { get; }

    /// <summary>How many the last scan found on the map.</summary>
    public int Count { get; set; }
}

/// <summary>One world object to mark.</summary>
internal sealed class MarkerEntry
{
    public WgoData Wgo;
    public MarkerType Type;
    public string Name;

    /// <summary>NPCs walk around, so their markers follow them every frame.</summary>
    public bool Moves;

    /// <summary>A quest ready to hand in: drawn tinted gold.</summary>
    public bool Highlight;
}
