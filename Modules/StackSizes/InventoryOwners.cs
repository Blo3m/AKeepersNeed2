using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AKeepersNeed2.Shared.Zombies;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.StackSizes;

/// <summary>
/// Works out who a container <c>Item</c> belongs to. An inventory is a container item with no owner
/// field, so this compares references: the player's inventory and tool belt (and bags inside
/// them), then a cache of every world object's storage and craft inventory (chests, stockpiles,
/// stations) and every placed zombie's own and porter inventory. Vendor stock, trade windows and
/// bodies are none of those, so they stay vanilla. The cache is rebuilt on a miss at most every
/// couple of seconds, which picks up newly built chests and new zombies.
/// </summary>
internal static class InventoryOwners
{
    private const float RebuildInterval = 2f;

    private static readonly AccessTools.FieldRef<WgoData, Inventory> WgoInventory =
        AccessTools.FieldRefAccess<WgoData, Inventory>("inventory");
    private static readonly AccessTools.FieldRef<WgoData, Inventory> WgoCraftInventory =
        AccessTools.FieldRefAccess<WgoData, Inventory>("craftInventory");
    private static readonly AccessTools.FieldRef<ZombieWgoData, Inventory> PorterInventory =
        AccessTools.FieldRefAccess<ZombieWgoData, Inventory>("porterInventory");

    private static readonly Dictionary<Item, StackOwner> Cache =
        new Dictionary<Item, StackOwner>(new ReferenceComparer());

    private static WorldData _builtFor;
    private static float _builtAt = float.MinValue;

    /// <summary>
    /// The owner of <paramref name="container"/>; <paramref name="parent"/> (the container whose
    /// method is running further up the call) when it's none of the known ones, e.g. a bag in a chest.
    /// </summary>
    public static StackOwner Resolve(Item container, StackOwner parent)
    {
        if (container == null)
        {
            return parent;
        }
        PlayerData player = MainGame.PlayerData;
        if (player != null && (IsIn(player.inventory, container) || IsIn(player.toolBeltInventory, container)))
        {
            return StackOwner.Player;
        }
        if (Lookup(container, out StackOwner owner))
        {
            return owner;
        }
        return parent;
    }

    private static bool IsIn(Inventory inventory, Item container)
    {
        Item data = inventory?.Data;
        if (data == null)
        {
            return false;
        }
        if (ReferenceEquals(data, container))
        {
            return true;
        }
        foreach (Item item in data.Inventory)
        {
            if (ReferenceEquals(item, container))
            {
                return true;
            }
        }
        return false;
    }

    private static bool Lookup(Item container, out StackOwner owner)
    {
        WorldData world = MainGame.Instance?.GameSave?.worldData;
        if (world == null)
        {
            owner = StackOwner.Other;
            return false;
        }
        if (world == _builtFor && Cache.TryGetValue(container, out owner))
        {
            return true;
        }
        if (world != _builtFor || Time.realtimeSinceStartup - _builtAt >= RebuildInterval)
        {
            Rebuild(world);
            return Cache.TryGetValue(container, out owner);
        }
        owner = StackOwner.Other;
        return false;
    }

    private static void Rebuild(WorldData world)
    {
        Cache.Clear();
        _builtFor = world;
        _builtAt = Time.realtimeSinceStartup;
        foreach (GameSceneData scene in world.gameSceneDataList)
        {
            foreach (WgoData wgo in scene.wgoDataList)
            {
                if (wgo is ZombieWgoData)
                {
                    continue;
                }
                Add(WgoInventory(wgo), StackOwner.ChestOrStation);
                Add(WgoCraftInventory(wgo), StackOwner.ChestOrStation);
            }
        }
        foreach (ZombieWgoData zombie in ZombieRoster.Placed())
        {
            if (zombie.ZombieItem != null)
            {
                Cache[zombie.ZombieItem] = StackOwner.Zombie;
            }
            Add(PorterInventory(zombie), StackOwner.Zombie);
            Add(WgoInventory(zombie), StackOwner.Zombie);
        }
    }

    private static void Add(Inventory inventory, StackOwner owner)
    {
        if (inventory?.Data != null)
        {
            Cache[inventory.Data] = owner;
        }
    }

    // Item may compare by value; ownership is about the exact container object.
    private sealed class ReferenceComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(Item item)
        {
            return RuntimeHelpers.GetHashCode(item);
        }
    }
}
