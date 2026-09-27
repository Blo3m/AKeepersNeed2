using System;
using System.Collections.Generic;
using AKeepersNeed2.Core.Settings;
using AKeepersNeed2.Shared.Patching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AKeepersNeed2.Modules.Fishing;

/// <summary>
/// Fishing cheats, each its own toggle, all on <c>FishingMiniGame</c> (a plain class driven by
/// <c>UpdateGame</c> every frame):
/// <list type="bullet">
/// <item>Instant Bite: <c>StartGameFlow</c> waits <c>fishWaitTime</c> before the bite; a prefix
/// zeroes it (<c>StartNewGame</c> rolls it first, so a postfix there would be too late).</item>
/// <item>Auto-Hook: while the fish bites, <c>HandleInput</c>'s postfix reports the player pulling,
/// which hooks it. Only during the bite: pulling earlier scares the fish away.</item>
/// <item>Auto-Win: once the fight starts, <c>UpdateGame</c>'s postfix calls <c>HandleSuccess</c>.</item>
/// <item>No Line Snap / No Escape: around <c>UpdateProgress</c>, tension is held at 0 (it breaks at
/// 100) and progress is kept at or above 0 (the fish escapes at -100).</item>
/// <item>Extra Fish: <c>HandleSuccess</c> drops one fish; a prefix drops the extra ones first.</item>
/// <item>Infinite Stock: the <c>SubGameRes</c> call in <c>HandleSuccess</c> that uses up the
/// fishing spot's stock is swapped for a no-op while on.</item>
/// </list>
/// </summary>
internal sealed class FishingModule : HarmonyModule
{
    private static ConfigEntry<bool> _instantBite;
    private static ConfigEntry<bool> _autoHook;
    private static ConfigEntry<bool> _autoWin;
    private static ConfigEntry<bool> _noLineSnap;
    private static ConfigEntry<bool> _noEscape;
    private static ConfigEntry<bool> _extraFish;
    private static ConfigEntry<float> _fishPerCatch;
    private static ConfigEntry<bool> _infiniteStock;

    private static readonly AccessTools.FieldRef<FishingMiniGame, float> WaitTime =
        AccessTools.FieldRefAccess<FishingMiniGame, float>("fishWaitTime");

    private static readonly AccessTools.FieldRef<FishingMiniGame, WgoData> Reservoir =
        AccessTools.FieldRefAccess<FishingMiniGame, WgoData>("reservoir");

    private static readonly AccessTools.FieldRef<FishingMiniGame, FishingDef> Fish =
        AccessTools.FieldRefAccess<FishingMiniGame, FishingDef>("fishingDef");

    private static readonly Action<FishingMiniGame> HandleSuccess =
        AccessTools.MethodDelegate<Action<FishingMiniGame>>(
            AccessTools.Method(typeof(FishingMiniGame), "HandleSuccess")
        );

    public override string Name => "Fishing";

    protected override ConfigEntry<bool> EnabledFlag => _instantBite;

    protected override IEnumerable<ConfigEntry<bool>> GateFlags => new[]
    {
        _instantBite, _autoHook, _autoWin, _noLineSnap, _noEscape, _extraFish, _infiniteStock,
    };

    public override void DeclareSettings(SettingsBuilder settings)
    {
        _instantBite = settings.Profile("Fishing", "InstantBite", false, "Fish bite as soon as you cast.");
        _autoHook = settings.Profile("Fishing", "AutoHook", false, "Fish are hooked automatically when they bite.");
        _autoWin = settings.Profile("Fishing", "AutoWin", false, "A hooked fish is caught at once, no fight.");
        _noLineSnap = settings.Profile("Fishing", "NoLineSnap", false, "The fishing line never snaps.");
        _noEscape = settings.Profile("Fishing", "NoEscape", false, "A hooked fish can't get away.");
        _extraFish = settings.Profile("Fishing", "ExtraFish", false, "Each catch gives more fish.");
        _fishPerCatch = settings.Profile(
            "Fishing",
            "FishPerCatch",
            2f,
            "Fish per catch (vanilla 1).",
            new AcceptableValueRange<float>(1f, 50f)
        );
        _infiniteStock = settings.Profile(
            "Fishing",
            "InfiniteStock",
            false,
            "Fishing spots never run out of fish."
        );
        settings.Toggle(MenuSection.Fishing, 10, "Instant Bite", _instantBite);
        settings.Toggle(MenuSection.Fishing, 20, "Auto-Hook", _autoHook);
        settings.Toggle(MenuSection.Fishing, 30, "Auto-Win", _autoWin);
        settings.Toggle(MenuSection.Fishing, 40, "No Line Snap", _noLineSnap);
        settings.Toggle(MenuSection.Fishing, 50, "No Escape", _noEscape);
        settings.Toggle(MenuSection.Fishing, 60, "Extra Fish", _extraFish);
        settings.Slider(MenuSection.Fishing, 70, _fishPerCatch, 1f, 50f, "0");
        settings.Toggle(MenuSection.Fishing, 80, "Infinite Stock", _infiniteStock);
    }

    protected override void Apply(Harmony harmony)
    {
        Patch(harmony, "StartGameFlow", prefix: nameof(SkipWait));
        Patch(harmony, "HandleInput", postfix: nameof(HookOnBite));
        Patch(harmony, nameof(FishingMiniGame.UpdateGame), postfix: nameof(WinFight));
        Patch(harmony, "UpdateProgress", prefix: nameof(HoldLine), postfix: nameof(HoldLine));
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(FishingMiniGame), "HandleSuccess"),
            prefix: new HarmonyMethod(typeof(FishingModule), nameof(DropExtraFish)),
            transpiler: new HarmonyMethod(typeof(FishingModule), nameof(SwapStockUse))
        );
    }

    private static void Patch(Harmony harmony, string method, string prefix = null, string postfix = null)
    {
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(FishingMiniGame), method),
            prefix: prefix != null ? new HarmonyMethod(typeof(FishingModule), prefix) : null,
            postfix: postfix != null ? new HarmonyMethod(typeof(FishingModule), postfix) : null
        );
    }

    private static void SkipWait(FishingMiniGame __instance)
    {
        if (_instantBite.Value && WaitTime(__instance) > 0f)
        {
            WaitTime(__instance) = 0f;
        }
    }

    private static void HookOnBite(FishingMiniGame __instance)
    {
        if (_autoHook.Value && __instance.CurrentStage == FishingMiniGame.Stage.Biting)
        {
            __instance.isPlayerPulling = true;
        }
    }

    private static void WinFight(FishingMiniGame __instance)
    {
        if (_autoWin.Value && __instance.CurrentStage == FishingMiniGame.Stage.PlayingWithFish)
        {
            HandleSuccess(__instance);
        }
    }

    private static void HoldLine(FishingMiniGame __instance)
    {
        if (_noLineSnap.Value)
        {
            __instance.tension = 0f;
        }
        if (_noEscape.Value && __instance.progress < 0f)
        {
            __instance.progress = 0f;
        }
    }

    private static void DropExtraFish(FishingMiniGame __instance)
    {
        if (!_extraFish.Value)
        {
            return;
        }
        WgoData reservoir = Reservoir(__instance);
        FishingDef fish = Fish(__instance);
        if (reservoir == null || fish == null)
        {
            return;
        }
        int extra = Mathf.RoundToInt(_fishPerCatch.Value) - 1;
        for (int i = 0; i < extra; i++)
        {
            reservoir.MakeDrop(new Item(fish.fishId));
        }
    }

    private static IEnumerable<CodeInstruction> SwapStockUse(IEnumerable<CodeInstruction> instructions)
    {
        return CallSwap.Replace(
            instructions,
            AccessTools.Method(typeof(WgoData), nameof(WgoData.SubGameRes), new[] { typeof(string), typeof(int) }),
            AccessTools.Method(typeof(FishingModule), nameof(UseStock)),
            "Fishing"
        );
    }

    private static void UseStock(WgoData reservoir, string fishId, int amount)
    {
        if (!_infiniteStock.Value)
        {
            reservoir.SubGameRes(fishId, amount);
        }
    }
}
