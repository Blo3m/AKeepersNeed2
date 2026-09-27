using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Zombies;
using HarmonyLib;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// One zombie's staged edits. The editor changes these fields only; <see cref="Apply"/> writes
/// them into the save in one go (after the menu's confirm) and <see cref="Revert"/> reloads
/// them from the zombie. Staged edits survive scrolling and closing the menu, but not a reload
/// of the game.
/// </summary>
internal sealed class ZombieEdit
{
    private static readonly System.Action<ZombieWgoData> RestartCrafterWork =
        AccessTools.MethodDelegate<System.Action<ZombieWgoData>>(
            AccessTools.Method(typeof(ZombieWgoData), "CrafterTryPlaceOrderForCurrentCraftOrStartIt")
        );

    private readonly ZombieWgoData _zombie;
    private Snapshot _loaded;

    public ZombieEdit(ZombieWgoData zombie)
    {
        _zombie = zombie;
        Revert();
    }

    public ZombieWgoData Zombie => _zombie;

    public Snapshot Staged { get; private set; }

    public bool IsDirty => Describe().Count > 0;

    public void Revert()
    {
        _loaded = Snapshot.Of(_zombie);
        Staged = Snapshot.Of(_zombie);
    }

    /// <summary>
    /// Turns a perk on (only when its parents are on) or off. Turning one off also turns off
    /// every perk that no longer has its parents. Start perks can't be turned off: the game
    /// gives them back on every load.
    /// </summary>
    public void SetPerk(TalentLevelUpDef def, bool on)
    {
        HashSet<string> learned = Staged.Perks;
        if (on)
        {
            if (ZombiePerks.ParentsMet(def, learned))
            {
                learned.Add(def.id);
            }
            return;
        }
        if (def.availableAtStart)
        {
            return;
        }
        learned.Remove(def.id);
        bool removed = true;
        while (removed)
        {
            removed = false;
            foreach (string id in learned.ToList())
            {
                TalentLevelUpDef child = GameBalance.Me.GetData<TalentLevelUpDef>(id);
                if (child != null && !child.availableAtStart && !ZombiePerks.ParentsMet(child, learned))
                {
                    learned.Remove(id);
                    removed = true;
                }
            }
        }
    }

    /// <summary>One line per staged change, for the confirm dialog.</summary>
    public List<string> Describe()
    {
        var lines = new List<string>();
        Snapshot a = _loaded;
        Snapshot b = Staged;
        DescribeSpeed(lines, "Craft speed", a.CraftSpeed, b.CraftSpeed);
        DescribeSpeed(lines, "Gather speed", a.GatherSpeed, b.GatherSpeed);
        DescribeSpeed(lines, "Walk speed", a.WalkSpeed, b.WalkSpeed);
        foreach (KeyValuePair<string, int> talent in b.Talents)
        {
            if (a.Talents.TryGetValue(talent.Key, out int before) && before != talent.Value)
            {
                lines.Add($"{ZombieLabels.Branch(talent.Key)} talent: {before} → {talent.Value}");
            }
        }
        DescribeInt(lines, "Red tech points", a.TechRed, b.TechRed);
        DescribeInt(lines, "Green tech points", a.TechGreen, b.TechGreen);
        DescribeInt(lines, "Blue tech points", a.TechBlue, b.TechBlue);
        foreach (string id in b.Perks.Where(id => !a.Perks.Contains(id)))
        {
            lines.Add($"Learn perk: {PerkName(id)}");
        }
        foreach (string id in a.Perks.Where(id => !b.Perks.Contains(id)))
        {
            lines.Add($"Remove perk: {PerkName(id)}");
        }
        if (a.IgnorePerkCap != b.IgnorePerkCap)
        {
            lines.Add(b.IgnorePerkCap ? "Ignore perk cap (restores suspended perks)" : "Respect perk cap again");
        }
        if (a.PorterCapacity != b.PorterCapacity)
        {
            lines.Add(b.PorterCapacity.HasValue
                ? $"Porter capacity: {b.PorterCapacity} slots"
                : "Porter capacity: follow global");
        }
        return lines;
    }

    /// <summary>Writes every staged change into the zombie (and so the save), then reloads.</summary>
    public void Apply()
    {
        Snapshot a = _loaded;
        Snapshot b = Staged;

        ApplyPerks(a.Perks, b.Perks);
        if (b.IgnorePerkCap != a.IgnorePerkCap)
        {
            ZombieOverrides.SetIgnorePerkCap(_zombie, b.IgnorePerkCap);
            if (b.IgnorePerkCap)
            {
                ZombiePerks.RestoreDisabled(_zombie);
            }
        }
        // Talent values are applied after perks, which shift them by their bonus: a typed value
        // is what the player asked for.
        bool talentsChanged = false;
        foreach (KeyValuePair<string, int> talent in b.Talents)
        {
            if (a.Talents.TryGetValue(talent.Key, out int before) && before != talent.Value)
            {
                ZombieTalentData branch = _zombie.GetTalentBranch(talent.Key);
                if (branch != null)
                {
                    branch.curTalentValue = talent.Value;
                    talentsChanged = true;
                }
            }
        }
        _zombie.techRed = b.TechRed;
        _zombie.techGreen = b.TechGreen;
        _zombie.techBlue = b.TechBlue;

        ZombieOverrides.SetCraftSpeed(_zombie, b.CraftSpeed);
        ZombieOverrides.SetGatherSpeed(_zombie, b.GatherSpeed);
        ZombieOverrides.SetWalkSpeed(_zombie, b.WalkSpeed);
        if (b.PorterCapacity != a.PorterCapacity)
        {
            ZombieOverrides.SetPorterCapacity(_zombie, b.PorterCapacity);
            if (b.PorterCapacity.HasValue)
            {
                PorterInventory.Resize(_zombie, b.PorterCapacity.Value);
            }
        }

        // What PurchaseTalentLevelUp does after a mastery change, so a craft that was blocked by
        // the lock starts.
        if (talentsChanged && _zombie.ZombieType == ZombieType.Crafter)
        {
            RestartCrafterWork(_zombie);
        }
        Revert();
    }

    private void ApplyPerks(HashSet<string> before, HashSet<string> after)
    {
        GameBalance balance = GameBalance.Me;
        // Children first when removing, parents first when learning.
        List<TalentLevelUpDef> removing = before.Where(id => !after.Contains(id))
            .Select(id => balance.GetData<TalentLevelUpDef>(id))
            .Where(def => def != null)
            .ToList();
        var learnedNow = new HashSet<string>(before);
        while (removing.Count > 0)
        {
            TalentLevelUpDef leaf = removing.FirstOrDefault(
                    def => !removing.Any(other => other.parents.Contains(def.id))
                )
                ?? removing[0];
            ZombiePerks.Remove(_zombie, leaf);
            learnedNow.Remove(leaf.id);
            removing.Remove(leaf);
        }

        List<TalentLevelUpDef> adding = after.Where(id => !before.Contains(id))
            .Select(id => balance.GetData<TalentLevelUpDef>(id))
            .Where(def => def != null)
            .ToList();
        while (adding.Count > 0)
        {
            TalentLevelUpDef next = adding.FirstOrDefault(def => ZombiePerks.ParentsMet(def, learnedNow))
                ?? adding[0];
            ZombiePerks.Grant(_zombie, next);
            learnedNow.Add(next.id);
            adding.Remove(next);
        }
    }

    private static string PerkName(string id)
    {
        TalentLevelUpDef def = GameBalance.Me?.GetData<TalentLevelUpDef>(id);
        return def != null
            ? ZombieLabels.Perk(def)
            : id;
    }

    private static void DescribeSpeed(List<string> lines, string label, float? before, float? after)
    {
        if (before == after)
        {
            return;
        }
        lines.Add(after.HasValue
            ? $"{label}: {after.Value:0.0}×"
            : $"{label}: follow global");
    }

    private static void DescribeInt(List<string> lines, string label, int before, int after)
    {
        if (before != after)
        {
            lines.Add($"{label}: {before} → {after}");
        }
    }

    /// <summary>Every editable value of a zombie at one moment.</summary>
    internal sealed class Snapshot
    {
        public float? CraftSpeed;
        public float? GatherSpeed;
        public float? WalkSpeed;
        public Dictionary<string, int> Talents = new Dictionary<string, int>();
        public int TechRed;
        public int TechGreen;
        public int TechBlue;
        public HashSet<string> Perks = new HashSet<string>();
        public bool IgnorePerkCap;
        public int? PorterCapacity;

        public static Snapshot Of(ZombieWgoData zombie)
        {
            var snapshot = new Snapshot
            {
                CraftSpeed = ZombieOverrides.CraftSpeed(zombie),
                GatherSpeed = ZombieOverrides.GatherSpeed(zombie),
                WalkSpeed = ZombieOverrides.WalkSpeed(zombie),
                TechRed = zombie.techRed,
                TechGreen = zombie.techGreen,
                TechBlue = zombie.techBlue,
                Perks = ZombiePerks.Learned(zombie),
                IgnorePerkCap = ZombieOverrides.IgnorePerkCap(zombie),
                PorterCapacity = ZombieOverrides.PorterCapacity(zombie),
            };
            foreach (ZombieTalentData branch in zombie.talentData)
            {
                snapshot.Talents[branch.id] = branch.curTalentValue;
            }
            return snapshot;
        }
    }
}
