namespace AKeepersNeed2.Modules.ZombieMaxMastery;

/// <summary>
/// The mastery that gives full output on every job: the highest mastery lock of any craft
/// (<c>CraftDefBase.talentLock</c>) or world object (<c>WGODef.MasteryLock</c>), times the
/// per-hit cap (<c>ConstDef max_cells_per_one_hit</c>). Computed once the definitions are loaded.
/// </summary>
internal static class MasteryCeiling
{
    private static int _value;

    public static int Value => _value > 0
        ? _value
        : Compute();

    private static int Compute()
    {
        GameBalance balance = GameBalance.Me;
        ConstDef maxCells = balance != null
            ? ConstDef.Get("max_cells_per_one_hit")
            : null;
        if (maxCells == null)
        {
            return 0;
        }

        int highestLock = 1;
        foreach (CraftDef craft in balance.craftDefs)
        {
            if (craft != null && craft.talentLock > highestLock)
            {
                highestLock = craft.talentLock;
            }
        }
        foreach (WGODef wgo in balance.wgoDefs)
        {
            if (wgo != null && wgo.MasteryLock > highestLock)
            {
                highestLock = wgo.MasteryLock;
            }
        }
        int cells = System.Math.Max(1, maxCells.IntValue);
        _value = highestLock * cells;
        Plugin.Logger.LogInfo($"[ZombieMaxMastery] max mastery is {_value} (lock {highestLock} × {cells}).");
        return _value;
    }
}
