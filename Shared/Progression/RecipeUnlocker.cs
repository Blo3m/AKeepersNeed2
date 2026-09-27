using System.Collections.Generic;
using System.Linq;

namespace AKeepersNeed2.Shared.Progression;

/// <summary>What <see cref="RecipeUnlocker"/> unlocks.</summary>
internal enum RecipeKind
{
    Crafts,
    Buildings,
    TownBuildings,
    Alchemy,
}

/// <summary>
/// Unlocks recipes (permanent; the menu confirms first) through the game's own
/// <c>KnowledgeSystem.UnlockCraft</c> / <c>UnlockBuilding</c> / <c>UnlockTownBuilding</c> /
/// <c>UnlockAlchemyFormula</c>, which only record the id (no notifications). Locked means: the def
/// needs unlocking (<c>isNeedsUnlock</c>, or <c>hiddenAtStart</c> for formulas) and isn't unlocked
/// yet. Recipes on the game's blocklists (<c>blackListCrafts</c>, <c>lockedBuildings</c>,
/// <c>lockedTownBuildings</c>, used for retired recipes) are skipped and stay blocked.
/// </summary>
internal static class RecipeUnlocker
{
    public static List<string> Locked(RecipeKind kind)
    {
        GameBalance balance = GameBalance.Me;
        KnowledgeSystem knowledge = MainGame.Instance?.GameSave?.knowledgeSystem;
        if (balance == null || knowledge == null)
        {
            return new List<string>();
        }
        switch (kind)
        {
            case RecipeKind.Buildings:
                return balance.buildingDefs
                    .Where(def => def.isNeedsUnlock)
                    .Select(def => def.id)
                    .Where(id => !knowledge.unlockedBuildings.Contains(id) && !knowledge.lockedBuildings.Contains(id))
                    .Distinct()
                    .ToList();
            case RecipeKind.TownBuildings:
                return balance.townBuildingDefs
                    .Where(def => def.isNeedsUnlock)
                    .Select(def => def.id)
                    .Where(id => !knowledge.unlockedTownBuildings.Contains(id)
                        && !knowledge.lockedTownBuildings.Contains(id))
                    .Distinct()
                    .ToList();
            case RecipeKind.Alchemy:
                return balance.alchemyFormulaDefs
                    .Where(def => def.hiddenAtStart)
                    .Select(def => def.id)
                    .Where(id => !knowledge.unlockedAlchemyFormulas.Contains(id))
                    .Distinct()
                    .ToList();
            default:
                return balance.craftDefs
                    .Where(def => def.isNeedsUnlock)
                    .Select(def => def.id)
                    .Where(id => !knowledge.unlockedCrafts.Contains(id) && !knowledge.blackListCrafts.Contains(id))
                    .Distinct()
                    .ToList();
        }
    }

    /// <summary>Unlocks every locked recipe of <paramref name="kind"/>; returns how many.</summary>
    public static int Unlock(RecipeKind kind)
    {
        KnowledgeSystem knowledge = MainGame.Instance?.GameSave?.knowledgeSystem;
        if (knowledge == null)
        {
            return 0;
        }
        List<string> ids = Locked(kind);
        foreach (string id in ids)
        {
            switch (kind)
            {
                case RecipeKind.Buildings:
                    knowledge.UnlockBuilding(id);
                    break;
                case RecipeKind.TownBuildings:
                    knowledge.UnlockTownBuilding(id);
                    break;
                case RecipeKind.Alchemy:
                    knowledge.UnlockAlchemyFormula(id);
                    break;
                default:
                    knowledge.UnlockCraft(id);
                    break;
            }
        }
        Plugin.Logger.LogInfo($"[Progression] unlocked {ids.Count} {kind}.");
        return ids.Count;
    }
}
