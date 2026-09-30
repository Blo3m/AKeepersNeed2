using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Progression;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Sorts the world scene's objects into legend groups and types. Resource nodes
/// (<c>WGODef.InteractionType.Work</c>) are grouped by <c>wgoGroup</c> and what they drop, and typed
/// by their first drop item (its name and icon), since the nodes themselves have no readable names.
/// The rest: chests, stockpiles (conveyor and timber stockpiles), NPCs with something to say (a
/// talk event, an active quest, or a quest ready to hand in), fishing spots and doors (the game's
/// <c>tp_…</c> / <c>gate_…</c> transitions). Hidden objects are skipped. Groups and types persist for
/// the session, so the legend keeps its rows; <see cref="Scan"/> refreshes the entries and counts.
/// </summary>
internal sealed class MarkerCatalog
{
    private readonly Dictionary<string, MarkerGroup> _groups = new Dictionary<string, MarkerGroup>();
    private readonly Dictionary<string, MarkerType> _types = new Dictionary<string, MarkerType>();
    private readonly List<MarkerEntry> _entries = new List<MarkerEntry>();
    private readonly HashSet<string> _questNpcs = new HashSet<string>();

    /// <summary>Groups in legend order; types within a group by name.</summary>
    public List<MarkerGroup> Groups { get; } = new List<MarkerGroup>();

    public IReadOnlyList<MarkerEntry> Entries => _entries;

    /// <summary>Set when a scan adds a group or type the legend hasn't drawn yet.</summary>
    public bool LayoutChanged { get; set; }

    public void Scan()
    {
        _entries.Clear();
        foreach (MarkerType type in _types.Values)
        {
            type.Count = 0;
        }
        string sceneId = MapProjection.WorldSceneId;
        GameSceneData scene = sceneId != null
            ? MainGame.WorldData?.GetGameSceneDataById(sceneId)
            : null;
        if (scene == null)
        {
            return;
        }
        CollectQuestNpcs();
        foreach (WgoData wgo in scene.wgoDataList)
        {
            if (wgo == null || wgo.IsHidden || wgo.Definition == null)
            {
                continue;
            }
            MarkerEntry entry = Classify(wgo);
            if (entry != null)
            {
                entry.Type.Count++;
                _entries.Add(entry);
            }
        }
    }

    private void CollectQuestNpcs()
    {
        _questNpcs.Clear();
        foreach (QuestData quest in QuestTools.Active())
        {
            string npc = quest.Definition?.wgoNpcId;
            if (!string.IsNullOrEmpty(npc))
            {
                _questNpcs.Add(npc);
            }
        }
    }

    private MarkerEntry Classify(WgoData wgo)
    {
        WGODef def = wgo.Definition;
        string id = def.id ?? string.Empty;
        switch (def.interactionType)
        {
            case WGODef.InteractionType.Work:
                return Resource(wgo, def);
            case WGODef.InteractionType.Reservoir:
                MarkerType fishing = Single("fishing", "Fishing Spots", 130, "i_fishing_rod_1", "i_b_fishing_1");
                return Entry(wgo, fishing, Pretty(id));
        }
        if (id.StartsWith("tp_", StringComparison.Ordinal) || id.StartsWith("gate_", StringComparison.Ordinal))
        {
            return Entry(wgo, Single("door", "Doors & Entrances", 140, "i_tr_door_hinge"), DoorName(id));
        }
        if (IsStockpile(def))
        {
            MarkerType stockpile = Single("stockpile", "Stockpiles", 110, "i_b_conveyor_chest_1", "i_b_" + id);
            return Entry(wgo, stockpile, Name(id));
        }
        if (def.interactionType == WGODef.InteractionType.Chest)
        {
            return Entry(wgo, Single("chest", "Chests", 100, "i_b_chest_1", "i_b_" + id), Name(id));
        }
        bool ready = MainGame.Instance.GameSave.questSystemData.WgoHasReadyToFinishQuest(id);
        if (ready || _questNpcs.Contains(id) || HasTalk(wgo))
        {
            MarkerType quest = Single("quest", "Quest Givers", 120, "icon-question", "i_slot-question");
            MarkerEntry npc = Entry(wgo, quest, Name(id));
            npc.Moves = true;
            npc.Highlight = ready;
            return npc;
        }
        return null;
    }

    private MarkerEntry Resource(WgoData wgo, WGODef def)
    {
        string drop = def.deathChanceItems?.chanceOutputItems?.FirstOrDefault()?.id;
        ResourceGroup group = ResourceGroup.For(def.wgoGroup, drop);
        MarkerGroup markerGroup = Group("res:" + group.Key, group.Title, group.Order, isResource: true);
        string typeKey = $"res:{group.Key}:{drop ?? "none"}";
        if (!_types.TryGetValue(typeKey, out MarkerType type))
        {
            ItemDef item = string.IsNullOrEmpty(drop)
                ? null
                : GameBalance.Me.GetData<ItemDef>(drop);
            string label = item != null
                ? ItemName(item)
                : "Breakables";
            type = AddType(typeKey, label, item != null ? GameSprites.First(item.iconId) : null, markerGroup);
        }
        return new MarkerEntry { Wgo = wgo, Type = type, Name = type.Label };
    }

    private static MarkerEntry Entry(WgoData wgo, MarkerType type, string name)
    {
        return new MarkerEntry { Wgo = wgo, Type = type, Name = name };
    }

    /// <summary>
    /// A category with a single type. Its icon is the first of <paramref name="sprites"/> the game
    /// has (item-style icons first: they're square and centred), else a dot.
    /// </summary>
    private MarkerType Single(string key, string title, int order, params string[] sprites)
    {
        if (_types.TryGetValue(key, out MarkerType type))
        {
            return type;
        }
        MarkerGroup group = Group(key, title, order, isResource: false);
        return AddType(key, title, GameSprites.First(sprites), group);
    }

    private MarkerGroup Group(string key, string title, int order, bool isResource)
    {
        if (_groups.TryGetValue(key, out MarkerGroup group))
        {
            return group;
        }
        group = new MarkerGroup(key, title, order, isResource);
        _groups.Add(key, group);
        Groups.Add(group);
        Groups.Sort((a, b) => a.Order.CompareTo(b.Order));
        LayoutChanged = true;
        return group;
    }

    private MarkerType AddType(string key, string label, Sprite icon, MarkerGroup group)
    {
        var type = new MarkerType(key, label, icon, group);
        _types.Add(key, type);
        group.Types.Add(type);
        group.Types.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        LayoutChanged = true;
        return type;
    }

    private static bool IsStockpile(WGODef def)
    {
        bool storage = def.interactionType == WGODef.InteractionType.Chest
            || def.interactionType == WGODef.InteractionType.CustomInteraction;
        return storage
            && (def.id.Contains("_container") || def.id.StartsWith("wood_container", StringComparison.Ordinal));
    }

    private static bool HasTalk(WgoData wgo)
    {
        foreach (InteractionEvent e in wgo.Events)
        {
            if (e != null && e.type == InteractionEvent.Type.Talk)
            {
                return true;
            }
        }
        return false;
    }

    private static string ItemName(ItemDef item)
    {
        try
        {
            string name = item.GetHeader();
            return string.IsNullOrEmpty(name)
                ? item.id
                : name;
        }
        catch (Exception)
        {
            return item.id;
        }
    }

    /// <summary>The game's name for an object, or its id made readable when it has none.</summary>
    private static string Name(string id)
    {
        string name = LLBase.L(id);
        return string.IsNullOrEmpty(name) || name == id
            ? Pretty(id)
            : name;
    }

    // "tp_RT_home_enter" → "Home (enter)".
    private static string DoorName(string id)
    {
        string rest = id.StartsWith("tp_RT_", StringComparison.Ordinal)
            ? id.Substring(6)
            : id;
        foreach (string suffix in new[] { "enter", "exit" })
        {
            if (rest.EndsWith("_" + suffix, StringComparison.Ordinal))
            {
                return $"{Pretty(rest.Substring(0, rest.Length - suffix.Length - 1))} ({suffix})";
            }
        }
        return Pretty(rest);
    }

    private static string Pretty(string id)
    {
        string text = id.Replace('_', ' ').Trim();
        return text.Length == 0
            ? id
            : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
