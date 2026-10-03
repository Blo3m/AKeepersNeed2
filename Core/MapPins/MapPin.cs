using UnityEngine;

namespace AKeepersNeed2.Core.MapPins;

/// <summary>One thing a module wants drawn on mod-made maps (the minimap): where and how.</summary>
internal sealed class MapPin
{
    public Vector3 World;

    /// <summary>Null draws a plain square in <see cref="Color"/>.</summary>
    public Sprite Icon;

    public Color Color = Color.white;

    /// <summary>For icons that need a special shader (NPC portraits); null for the default.</summary>
    public Material Material;

    /// <summary>Size in screen pixels; it doesn't change with the map's zoom.</summary>
    public float Size = 12f;

    /// <summary>Turned 45° (the bookmark diamond).</summary>
    public bool Diamond;

    /// <summary>Drawing order: higher draws on top.</summary>
    public int Layer;
}
