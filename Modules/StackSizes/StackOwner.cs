namespace AKeepersNeed2.Modules.StackSizes;

/// <summary>Who an inventory belongs to, which decides the stack rule that applies to it.</summary>
internal enum StackOwner
{
    /// <summary>Vendors, trade windows, bodies, ground piles, anything unknown: always vanilla.</summary>
    Other,
    Player,
    ChestOrStation,
    Zombie,
}
