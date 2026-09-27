namespace AKeepersNeed2.Modules.InfiniteDurability;

/// <summary>An item's durability captured before a wear point, so it can be put back after.</summary>
internal sealed class DurabilitySnapshot
{
    private readonly DurabilitySerializedItemProperty _property;
    private readonly float _durability;

    private DurabilitySnapshot(DurabilitySerializedItemProperty property)
    {
        _property = property;
        _durability = property.Durability;
    }

    /// <summary>Null when the item is missing or has no durability.</summary>
    public static DurabilitySnapshot Of(Item item)
    {
        if (item == null || !item.TryGetProperty(out DurabilitySerializedItemProperty property))
        {
            return null;
        }
        return new DurabilitySnapshot(property);
    }

    public void Restore()
    {
        _property.Durability = _durability;
    }
}
