using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;

namespace AKeepersNeed2.Shared.Progression;

/// <summary>
/// The player's active quests (what the game's journal shows: <c>QuestData.IsActiveQuest</c>) and
/// two ways to finish one,
/// both permanent (the menu confirms first):
/// <list type="bullet">
/// <item><see cref="GiveRequirements"/>: adds the items and resources the quest's hand-in asks for
/// (<c>finishCheck.phraseReqs</c>), so the player finishes it normally by talking to the NPC. The
/// safe route: the hand-in dialogue still runs its rewards and starts the next quest.</item>
/// <item><see cref="ForceComplete"/>: <c>QuestSystemData.CompleteQuest</c>. Skips the hand-in
/// dialogue, whose scripts may start the next quest or give rewards, so a quest chain can
/// stall.</item>
/// </list>
/// </summary>
internal static class QuestTools
{
    private static QuestSystemData Quests => MainGame.Instance?.GameSave?.questSystemData;

    public static List<QuestData> Active()
    {
        QuestSystemData quests = Quests;
        if (quests?.questCollection?.quests == null)
        {
            return new List<QuestData>();
        }
        // The game's journal lists IsActiveQuest: not hidden or unknown, and available, awaiting or
        // in progress.
        return quests.questCollection.quests
            .Where(quest => quest.Definition != null && quest.IsActiveQuest)
            .OrderBy(quest => Name(quest))
            .ToList();
    }

    public static string Name(QuestData quest)
    {
        return LLBase.L(quest.id);
    }

    /// <summary>The quest giver's name, or empty when the quest has none.</summary>
    public static string Giver(QuestData quest)
    {
        string npc = quest.Definition?.wgoNpcId;
        return string.IsNullOrEmpty(npc)
            ? string.Empty
            : LLBase.L(npc);
    }

    /// <summary>Readable hand-in requirements, e.g. "3× Log, 50 money, day 4".</summary>
    public static string Requirements(QuestData quest)
    {
        var parts = new List<string>();
        foreach (QuestPhraseRequirement req in HandIn(quest))
        {
            switch (req.entity)
            {
                case QuestPhraseRequirement.Entity.Item when req.itemCount != null:
                    parts.Add($"{req.itemCount.count}× {ItemName(req.itemCount.itemId)}");
                    break;
                case QuestPhraseRequirement.Entity.GameResAtom when req.gameResAtom != null:
                    parts.Add($"{req.gameResAtom.value:0} {LLBase.L(req.gameResAtom.type)}");
                    break;
                case QuestPhraseRequirement.Entity.Day:
                    parts.Add($"day {req.dayNumber}");
                    break;
                case QuestPhraseRequirement.Entity.Order:
                    parts.Add("an order");
                    break;
            }
        }
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Adds the hand-in's items and resources to the player. Day and order requirements can't be
    /// given. Returns false when there's nothing to give.
    /// </summary>
    public static bool GiveRequirements(QuestData quest)
    {
        PlayerData player = MainGame.PlayerData;
        if (player == null)
        {
            return false;
        }
        bool gave = false;
        foreach (QuestPhraseRequirement req in HandIn(quest))
        {
            if (req.entity == QuestPhraseRequirement.Entity.Item && req.itemCount != null && req.itemCount.count > 0)
            {
                player.Inventory.AddItemToInventory(new Item(req.itemCount.itemId, req.itemCount.count));
                gave = true;
            }
            else if (req.entity == QuestPhraseRequirement.Entity.GameResAtom && req.gameResAtom != null)
            {
                player.AddRes(req.gameResAtom.type, req.gameResAtom.value);
                gave = true;
            }
        }
        Plugin.Logger.LogInfo($"[Progression] gave the hand-in for quest {quest.id} ({gave}).");
        return gave;
    }

    public static void ForceComplete(QuestData quest)
    {
        Quests?.CompleteQuest(quest.id);
        Plugin.Logger.LogInfo($"[Progression] force-completed quest {quest.id}.");
    }

    private static IEnumerable<QuestPhraseRequirement> HandIn(QuestData quest)
    {
        return quest.Definition?.finishCheck?.phraseReqs ?? Enumerable.Empty<QuestPhraseRequirement>();
    }

    private static string ItemName(string itemId)
    {
        ItemDef def = GameBalance.Me?.GetData<ItemDef>(itemId);
        return def != null
            ? def.GetHeader()
            : itemId;
    }
}
