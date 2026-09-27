using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;

namespace AKeepersNeed2.Shared.Progression;

/// <summary>
/// Unlocks techs (permanent; the menu confirms first). Uses the game's own
/// <c>TechDef.Unlock(free: true)</c>, which also unlocks the tech's crafts, formulas, buildings
/// and perks and runs its unlock scripts, so each tech is unlocked once only (unlocking again
/// would re-add perks and re-run scripts). Techs are unlocked parents first, the order the tree
/// would, with the game's notifications muted so one summary replaces hundreds of pop-ups. A tab
/// the game still has locked is opened once any of its techs is unlocked.
/// </summary>
internal static class TechUnlocker
{
    private static KnowledgeSystem Knowledge => MainGame.Instance?.GameSave?.knowledgeSystem;

    /// <summary>
    /// Techs to list. Hidden ones (not yet revealed by a quest) and reputation ones only when asked;
    /// <paramref name="tab"/> null means every tab.
    /// </summary>
    public static List<TechDef> Techs(TechTreeTab? tab, bool includeHidden, bool includeReputation)
    {
        GameBalance balance = GameBalance.Me;
        if (balance == null || Knowledge == null)
        {
            return new List<TechDef>();
        }
        return balance.techDefs
            .Where(def => tab == null || def.tab == tab.Value)
            .Where(def => includeReputation || def.techDefType == TechDefType.Common)
            .Where(def => includeHidden || def.TechState != TechState.Hidden)
            .ToList();
    }

    public static string Name(TechDef def)
    {
        return LLBase.L(def.id);
    }

    public static string TabName(TechTreeTab tab)
    {
        return LLBase.L("tech_tab_" + tab);
    }

    public static bool IsUnlocked(TechDef def)
    {
        return Knowledge != null && Knowledge.IsTechUnlocked(def.id);
    }

    /// <summary>Unlocks every still-locked tech in <paramref name="techs"/>; returns how many.</summary>
    public static int Unlock(IEnumerable<TechDef> techs)
    {
        KnowledgeSystem knowledge = Knowledge;
        if (knowledge == null)
        {
            return 0;
        }
        List<TechDef> pending = techs.Where(def => !knowledge.IsTechUnlocked(def.id)).ToList();
        var pendingIds = new HashSet<string>(pending.Select(def => def.id));
        var tabs = new HashSet<TechTreeTab>();
        int count = 0;
        bool wasSilent = UINotificator.isSilent;
        UINotificator.isSilent = true;
        try
        {
            while (pending.Count > 0)
            {
                // A parent still waiting in this batch goes first; parents outside it don't block.
                TechDef next = pending.FirstOrDefault(
                        def => def.parents.All(parent => !pendingIds.Contains(parent))
                    )
                    ?? pending[0];
                pending.Remove(next);
                pendingIds.Remove(next.id);
                next.Unlock(free: true);
                tabs.Add(next.tab);
                count++;
            }
        }
        finally
        {
            UINotificator.isSilent = wasSilent;
        }
        foreach (TechTreeTab tab in tabs)
        {
            if (knowledge.lockedTechTabs.Contains(tab))
            {
                knowledge.UnlockTechTab(tab);
            }
        }
        Plugin.Logger.LogInfo($"[Progression] unlocked {count} techs.");
        return count;
    }
}
