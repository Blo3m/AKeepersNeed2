using System;
using System.Collections.Generic;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// A `&lt; NPC name &gt;` cycler over every NPC with a reputation resource, picking which
/// reputation the Progression tab's reputation row edits.
/// </summary>
internal sealed class ReputationPicker
{
    /// <summary>NPC ids (<c>WGODef.id</c>) hidden from the picker.</summary>
    private static readonly HashSet<string> ExcludedNpcs = new HashSet<string> { "npc_fake_villagers" };

    private readonly List<Npc> _npcs = new List<Npc>();
    private MenuPage _page;
    private TextMeshProUGUI _label;
    private int _index;

    /// <summary>The selected NPC, or null when none are loaded.</summary>
    public Npc? Current => _index < _npcs.Count
        ? _npcs[_index]
        : (Npc?)null;

    /// <summary>Adds the cycler row to <paramref name="page"/>; cycling re-syncs the page.</summary>
    public void Build(MenuPage page)
    {
        _page = page;
        RectTransform cycler = page.Band(28f, 8f);

        Image field = MenuUi.CreateImage("Field", cycler, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _label = MenuUi.CreateText("Value", cycler, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_label);
        MenuUi.SetRect(_label.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<", Anchors.Left, Vector2.zero, new Vector2(26f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">", Anchors.Right, new Vector2(-26f, 0f), Vector2.zero);
        prev.onClick.AddListener(() => Cycle(-1));
        next.onClick.AddListener(() => Cycle(1));

        page.AddSync(() =>
        {
            _label.text = Current is Npc npc
                ? npc.Name
                : "No NPCs";
        });
    }

    /// <summary>
    /// Loads the NPC list once the game data exists. Every <c>WGODef</c> with a
    /// <c>repResName</c>, deduped by resource since one NPC can have several object definitions,
    /// sorted by localized name.
    /// </summary>
    public void EnsureLoaded()
    {
        if (_npcs.Count > 0 || GameBalance.Me == null)
        {
            return;
        }
        var seen = new HashSet<string>();
        foreach (WGODef def in GameBalance.Me.wgoDefs)
        {
            if (def == null
                || string.IsNullOrEmpty(def.repResName)
                || ExcludedNpcs.Contains(def.id)
                || !seen.Add(def.repResName))
            {
                continue;
            }
            string name = LLBase.L(def.id);
            _npcs.Add(new Npc { Res = def.repResName, Name = string.IsNullOrEmpty(name) ? def.id : name });
        }
        _npcs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _index = 0;
    }

    private void Cycle(int step)
    {
        if (_npcs.Count == 0)
        {
            return;
        }
        _index = (_index + step + _npcs.Count) % _npcs.Count;
        _page.Sync();
    }

    public struct Npc
    {
        public string Res;
        public string Name;
    }
}
