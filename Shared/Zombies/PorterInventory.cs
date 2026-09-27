using HarmonyLib;

namespace AKeepersNeed2.Shared.Zombies;

/// <summary>
/// A porter's carrying inventory (<c>ZombieWgoData.porterInventory</c>, private). The game
/// creates it with 4 slots when the zombie is attached to a porter station and drops it when
/// detached. Every porter path reads <c>InventorySize - InventoryFillSize</c> live, so resizing is
/// safe; shrinking below the current load keeps the items and the porter just takes nothing new
/// until it has room. The size is saved with the porter.
/// </summary>
internal static class PorterInventory
{
    public const int VanillaSize = 4;

    private static readonly AccessTools.FieldRef<ZombieWgoData, Inventory> Field =
        AccessTools.FieldRefAccess<ZombieWgoData, Inventory>("porterInventory");

    /// <summary>The porter's current slot count, or null when it has no porter inventory.</summary>
    public static int? Size(ZombieWgoData zombie)
    {
        Inventory inventory = zombie != null
            ? Field(zombie)
            : null;
        return inventory?.Data?.InventorySize;
    }

    /// <summary>Sets the slot count; returns false when the zombie has no porter inventory.</summary>
    public static bool Resize(ZombieWgoData zombie, int size)
    {
        Inventory inventory = zombie != null
            ? Field(zombie)
            : null;
        if (inventory?.Data == null)
        {
            return false;
        }
        inventory.Data.InventorySize = size;
        return true;
    }
}
