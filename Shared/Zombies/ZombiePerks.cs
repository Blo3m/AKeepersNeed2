using System.Collections.Generic;

namespace AKeepersNeed2.Shared.Zombies;

/// <summary>
/// A zombie's perks: the <c>TalentLevelUpDef</c>s with <c>isZombiePerk</c>, one tree per talent
/// branch. Learned ids live in the branch's <c>studiedLevelUps</c>; ids the red-skull cap
/// suspended are also in <c>disabledTalentLevelUps</c> (no mastery bonus, no linked perk).
/// Granting uses the game's own <c>PurchaseTalentLevelUp(def, free: true)</c>. The game has no
/// removal, so <see cref="Remove"/> reverses what purchasing did.
/// </summary>
internal static class ZombiePerks
{
    /// <summary>The branch's perks in balance order; hidden ones only when asked for.</summary>
    public static List<TalentLevelUpDef> ForBranch(string talentId, bool includeHidden)
    {
        var perks = new List<TalentLevelUpDef>();
        GameBalance balance = GameBalance.Me;
        if (balance == null)
        {
            return perks;
        }
        List<string> hidden = MainGame.Instance?.GameSave?.knowledgeSystem?.hiddenTalentLevelUps;
        foreach (TalentLevelUpDef def in balance.talentLevelUpDefs)
        {
            if (!def.isZombiePerk || def.talentId != talentId)
            {
                continue;
            }
            if (!includeHidden && hidden != null && hidden.Contains(def.id))
            {
                continue;
            }
            perks.Add(def);
        }
        return perks;
    }

    public static HashSet<string> Learned(ZombieWgoData zombie)
    {
        var learned = new HashSet<string>();
        foreach (ZombieTalentData branch in zombie.talentData)
        {
            learned.UnionWith(branch.studiedLevelUps);
        }
        return learned;
    }

    /// <summary>
    /// Whether <paramref name="def"/>'s parents are in <paramref name="learned"/>: all of them
    /// (<c>LockType.All</c>) or at least one (<c>Any</c>). A perk without parents always is.
    /// </summary>
    public static bool ParentsMet(TalentLevelUpDef def, ICollection<string> learned)
    {
        if (def.parents == null || def.parents.Count == 0)
        {
            return true;
        }
        if (def.lockType == TalentLevelUpDef.LockType.Any)
        {
            foreach (string parent in def.parents)
            {
                if (learned.Contains(parent))
                {
                    return true;
                }
            }
            return false;
        }
        foreach (string parent in def.parents)
        {
            if (!learned.Contains(parent))
            {
                return false;
            }
        }
        return true;
    }

    public static void Grant(ZombieWgoData zombie, TalentLevelUpDef def)
    {
        if (!zombie.IsTalentLevelUpStudied(def.id))
        {
            zombie.PurchaseTalentLevelUp(def, free: true);
        }
    }

    /// <summary>Undoes a learned perk: its mastery bonus and linked perk (unless suspended), then the id.</summary>
    public static void Remove(ZombieWgoData zombie, TalentLevelUpDef def)
    {
        ZombieTalentData branch = zombie.GetTalentBranch(def.talentId);
        if (branch == null || !branch.studiedLevelUps.Contains(def.id))
        {
            return;
        }
        if (!zombie.disabledTalentLevelUps.Remove(def.id))
        {
            branch.curTalentValue -= def.talentValueAdd;
            if (!string.IsNullOrEmpty(def.linkedPerk))
            {
                zombie.RemovePerk(def.linkedPerk);
            }
        }
        branch.studiedLevelUps.Remove(def.id);
    }

    /// <summary>Brings back every perk the red-skull cap suspended, with its bonus and linked perk.</summary>
    public static void RestoreDisabled(ZombieWgoData zombie)
    {
        GameBalance balance = GameBalance.Me;
        foreach (string id in new List<string>(zombie.disabledTalentLevelUps))
        {
            zombie.disabledTalentLevelUps.Remove(id);
            TalentLevelUpDef def = balance?.GetData<TalentLevelUpDef>(id);
            if (def == null)
            {
                continue;
            }
            if (!string.IsNullOrEmpty(def.linkedPerk))
            {
                zombie.AddPerk(def.linkedPerk);
            }
            zombie.GetTalentBranch(def.talentId).curTalentValue += def.talentValueAdd;
        }
    }
}
