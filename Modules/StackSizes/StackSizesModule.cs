using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;

namespace AKeepersNeed2.Modules.StackSizes;

/// <summary>
/// Bigger item stacks, set separately for the player's inventory, chests and stations, and zombie
/// inventories. The limit is <c>ItemDef.stackCount</c>, one field shared by every inventory, so
/// instead of changing it the few game methods that read it are transpiled to ask
/// <see cref="Limit"/>, which picks the rule of the container being filled (see
/// <see cref="StackContext"/>, <see cref="InventoryOwners"/>).
/// <para>
/// The patches stay installed with every rule off (<see cref="HarmonyModule.AlwaysPatched"/>): the
/// same transpilers clamp a stack's free room at 0. Vanilla computes <c>stackCount - Count</c>,
/// which goes negative for a stack left bigger than vanilla after turning a rule off, and that
/// blocked adding more of the item even to empty slots, or pulled items out of the big stack.
/// Clamped, oversized stacks just sit there; nothing is ever split and the save isn't touched.
/// </para>
/// </summary>
internal sealed class StackSizesModule : HarmonyModule
{
    private const float MaxMultiplier = 100f;
    private const float MaxFixed = 9999f;

    private static readonly FieldInfo StackCountField = AccessTools.Field(typeof(ItemDef), nameof(ItemDef.stackCount));
    private static readonly MethodInfo CountGetter = AccessTools.PropertyGetter(typeof(Item), nameof(Item.Count));

    private static StackRule _player;
    private static StackRule _chests;
    private static StackRule _zombies;

    public override string Name => "StackSizes";

    protected override ConfigEntry<bool> EnabledFlag => _player.Enabled;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags =>
        new[] { _player.Enabled, _chests.Enabled, _zombies.Enabled };

    protected override bool AlwaysPatched => true;

    private static bool AnyOn => _player.Enabled.Value || _chests.Enabled.Value || _zombies.Enabled.Value;

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _player = new StackRule(
            settings.Profile("StackSizes", "PlayerEnabled", false, "Bigger stacks in the player's inventory."),
            settings.Profile("StackSizes", "PlayerMode", StackMode.Multiplier, "Multiply vanilla stacks, or one size."),
            settings.Profile(
                "StackSizes",
                "PlayerMultiplier",
                10f,
                "Player stacks: vanilla stack x this.",
                new AcceptableValueRange<float>(1f, MaxMultiplier)
            ),
            settings.Profile(
                "StackSizes",
                "PlayerFixed",
                999f,
                "Player stacks: this size for every stackable item (never below vanilla).",
                new AcceptableValueRange<float>(1f, MaxFixed)
            )
        );
        settings.Toggle(MenuSection.PlayerStacks, 10, "Bigger Player Stacks", _player.Enabled);
        settings.Choice(MenuSection.PlayerStacks, 20, "Player Stack Mode", _player.Mode, () => _player.Enabled.Value);
        settings.Slider(
            MenuSection.PlayerStacks,
            30,
            _player.Multiplier,
            1f,
            MaxMultiplier,
            "'x'0",
            () => _player.Enabled.Value,
            () => _player.Mode.Value == StackMode.Multiplier
        );
        settings.Slider(
            MenuSection.PlayerStacks,
            40,
            _player.FixedSize,
            1f,
            MaxFixed,
            "0",
            () => _player.Enabled.Value,
            () => _player.Mode.Value == StackMode.Fixed
        );

        _chests = new StackRule(
            settings.Profile("StackSizes", "ChestEnabled", false, "Bigger stacks in chests, stockpiles and stations."),
            settings.Profile("StackSizes", "ChestMode", StackMode.Multiplier, "Multiply vanilla stacks, or one size."),
            settings.Profile(
                "StackSizes",
                "ChestMultiplier",
                10f,
                "Chest and station stacks: vanilla stack x this.",
                new AcceptableValueRange<float>(1f, MaxMultiplier)
            ),
            settings.Profile(
                "StackSizes",
                "ChestFixed",
                999f,
                "Chest and station stacks: this size for every stackable item (never below vanilla).",
                new AcceptableValueRange<float>(1f, MaxFixed)
            )
        );
        settings.Toggle(MenuSection.ChestStacks, 10, "Bigger Chest Stacks", _chests.Enabled);
        settings.Choice(MenuSection.ChestStacks, 20, "Chest Stack Mode", _chests.Mode, () => _chests.Enabled.Value);
        settings.Slider(
            MenuSection.ChestStacks,
            30,
            _chests.Multiplier,
            1f,
            MaxMultiplier,
            "'x'0",
            () => _chests.Enabled.Value,
            () => _chests.Mode.Value == StackMode.Multiplier
        );
        settings.Slider(
            MenuSection.ChestStacks,
            40,
            _chests.FixedSize,
            1f,
            MaxFixed,
            "0",
            () => _chests.Enabled.Value,
            () => _chests.Mode.Value == StackMode.Fixed
        );

        _zombies = new StackRule(
            settings.Profile("StackSizes", "ZombieEnabled", false, "Bigger stacks in zombie and porter inventories."),
            settings.Profile("StackSizes", "ZombieMode", StackMode.Multiplier, "Multiply vanilla stacks, or one size."),
            settings.Profile(
                "StackSizes",
                "ZombieMultiplier",
                10f,
                "Zombie stacks: vanilla stack x this.",
                new AcceptableValueRange<float>(1f, MaxMultiplier)
            ),
            settings.Profile(
                "StackSizes",
                "ZombieFixed",
                999f,
                "Zombie stacks: this size for every stackable item (never below vanilla).",
                new AcceptableValueRange<float>(1f, MaxFixed)
            )
        );
        settings.Toggle(MenuSection.ZombieStacks, 10, "Bigger Zombie Stacks", _zombies.Enabled);
        settings.Choice(MenuSection.ZombieStacks, 20, "Zombie Stack Mode", _zombies.Mode, () => _zombies.Enabled.Value);
        settings.Slider(
            MenuSection.ZombieStacks,
            30,
            _zombies.Multiplier,
            1f,
            MaxMultiplier,
            "'x'0",
            () => _zombies.Enabled.Value,
            () => _zombies.Mode.Value == StackMode.Multiplier
        );
        settings.Slider(
            MenuSection.ZombieStacks,
            40,
            _zombies.FixedSize,
            1f,
            MaxFixed,
            "0",
            () => _zombies.Enabled.Value,
            () => _zombies.Mode.Value == StackMode.Fixed
        );
    }

    protected override void Apply(Harmony harmony)
    {
        var enter = new HarmonyMethod(typeof(StackSizesModule), nameof(EnterContainer));
        var leave = new HarmonyMethod(typeof(StackSizesModule), nameof(LeaveContainer));
        var limit = new HarmonyMethod(typeof(StackSizesModule), nameof(SwapLimit));
        var limitAndRoom = new HarmonyMethod(typeof(StackSizesModule), nameof(SwapLimitAndClampRoom));

        harmony.Patch(
            AccessTools.Method(
                typeof(Item),
                nameof(Item.AddItemToInventory),
                new[] { typeof(Item), typeof(List<Item>).MakeByRefType(), typeof(Item), typeof(bool) }
            ),
            prefix: enter,
            finalizer: leave,
            transpiler: limit
        );
        harmony.Patch(
            AccessTools.Method(
                typeof(Item),
                nameof(Item.CanAddItemCountToInventory),
                new[] { typeof(ItemDef), typeof(int), typeof(bool), typeof(Item), typeof(bool) }
            ),
            prefix: enter,
            finalizer: leave,
            transpiler: limitAndRoom
        );
        harmony.Patch(
            AccessTools.Method(
                typeof(Item),
                nameof(Item.CanAddItemsToInventory),
                new[] { typeof(List<ItemCount>), typeof(List<ItemCount>).MakeByRefType() }
            ),
            prefix: enter,
            finalizer: leave,
            transpiler: limit
        );
        harmony.Patch(
            AccessTools.Method(typeof(Item), nameof(Item.CanAddItemCount), new[] { typeof(Item), typeof(int) }),
            transpiler: limitAndRoom
        );
        harmony.Patch(
            AccessTools.Method(typeof(MultiInventory), "CanAddItemsToInventories"),
            prefix: new HarmonyMethod(typeof(StackSizesModule), nameof(EnterInventories)),
            finalizer: leave,
            transpiler: limit
        );
    }

    private static void EnterContainer(Item __instance)
    {
        StackOwner parent = StackContext.Current;
        StackContext.Push(AnyOn ? InventoryOwners.Resolve(__instance, parent) : StackOwner.Other);
    }

    // The player's inventory plus tool belt, or a station's several inventories: all one owner.
    private static void EnterInventories(List<Inventory> inventories)
    {
        Item first = inventories != null && inventories.Count > 0
            ? inventories[0]?.Data
            : null;
        EnterContainer(first);
    }

    private static void LeaveContainer()
    {
        StackContext.Pop();
    }

    /// <summary>Replaces every read of <c>ItemDef.stackCount</c>.</summary>
    private static int Limit(ItemDef def)
    {
        int vanilla = def.stackCount;
        switch (StackContext.Current)
        {
            case StackOwner.Player:
                return _player.Limit(vanilla);
            case StackOwner.ChestOrStation:
                return _chests.Limit(vanilla);
            case StackOwner.Zombie:
                return _zombies.Limit(vanilla);
            default:
                return vanilla;
        }
    }

    /// <summary>Replaces <c>limit - stack.Count</c>: a stack's free room, never negative.</summary>
    private static int Room(int limit, int count)
    {
        return Math.Max(0, limit - count);
    }

    private static IEnumerable<CodeInstruction> SwapLimit(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        return Swap(instructions, original, false);
    }

    private static IEnumerable<CodeInstruction> SwapLimitAndClampRoom(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        return Swap(instructions, original, true);
    }

    private static IEnumerable<CodeInstruction> Swap(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original,
        bool clampRoom
    )
    {
        var code = new List<CodeInstruction>(instructions);
        int limits = 0;
        int rooms = 0;
        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].LoadsField(StackCountField))
            {
                code[i].opcode = OpCodes.Call;
                code[i].operand = AccessTools.Method(typeof(StackSizesModule), nameof(Limit));
                limits++;
            }
            else if (clampRoom && code[i].opcode == OpCodes.Sub && i > 0 && code[i - 1].Calls(CountGetter))
            {
                code[i].opcode = OpCodes.Call;
                code[i].operand = AccessTools.Method(typeof(StackSizesModule), nameof(Room));
                rooms++;
            }
        }
        if (limits == 0 || (clampRoom && rooms == 0))
        {
            Plugin.Logger.LogWarning(
                $"[StackSizes] {original?.DeclaringType?.Name}.{original?.Name}: found {limits} stack reads and "
                    + $"{rooms} room sums; the game may have changed."
            );
        }
        return code;
    }
}
