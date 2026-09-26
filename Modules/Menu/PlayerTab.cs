using System;
using System.Collections.Generic;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Player-focused tweaks: energy regeneration and drain, plus live setters for the red/green/blue
/// tech points and per-NPC reputation. The setters write straight into the current save's
/// <see cref="PlayerData"/>, so they are not part of any profile.
/// </summary>
internal sealed class PlayerTab : IMenuTab
{
    private static readonly string[,] TechPoints =
    {
        { "tech_red", "Red Tech Points" },
        { "tech_green", "Green Tech Points" },
        { "tech_blue", "Blue Tech Points" },
        { "happiness", "Happiness" },
    };

    /// <summary>NPC ids (<c>WGODef.id</c>) hidden from the reputation picker.</summary>
    private static readonly HashSet<string> ExcludedReps = new HashSet<string> { "npc_fake_villagers" };

    private readonly List<RepEntry> _reps = new List<RepEntry>();
    private MenuPage _page;
    private TextMeshProUGUI _repLabel;
    private int _repIndex;

    public string Title => "Player";

    public void Build(RectTransform content)
    {
        _page = new MenuPage(content);

        _page.SectionHeader("Energy");
        _page.ToggleRow(
            "Energy Regen",
            () => ModConfig.EnergyRegenEnabled.Value,
            v => ModConfig.EnergyRegenEnabled.Value = v
        );
        _page.SliderRow(
            0.1f,
            10f,
            "0.0",
            () => ModConfig.EnergyRegenRate.Value,
            v => ModConfig.EnergyRegenRate.Value = v
        );
        _page.ToggleRow(
            "No Energy Drain",
            () => ModConfig.NoEnergyDrainEnabled.Value,
            v => ModConfig.NoEnergyDrainEnabled.Value = v
        );

        _page.SectionHeader("Tech Points");
        for (int i = 0; i < TechPoints.GetLength(0); i++)
        {
            string res = TechPoints[i, 0];
            ValueRow(
                TechPoints[i, 1],
                player => player.GetResInt(res),
                (player, value) => player.SetRes(res, value)
            );
        }

        _page.SectionHeader("Reputation");
        RepCyclerRow();
        ValueRow(
            "Reputation",
            player => CurrentRep() is RepEntry rep ? player.GetNPCRep(rep.Res) : (int?)null,
            (player, value) =>
            {
                if (CurrentRep() is RepEntry rep)
                {
                    player.SetNPCRep(rep.Res, value);
                }
            }
        );
    }

    public void Refresh()
    {
        if (_reps.Count == 0)
        {
            LoadReps();
        }
        _page?.Sync();
    }

    /// <summary>
    /// "Label [value] [Set]": the field shows the live value on every sync, and Set (or Enter)
    /// writes the typed value. Blank while no game is loaded.
    /// </summary>
    private void ValueRow(string label, Func<PlayerData, int?> get, Action<PlayerData, int> set)
    {
        RectTransform band = _page.Band(30f, 8f);

        TextMeshProUGUI name = MenuUi.CreateText("Name", band, 13f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(name);
        MenuUi.SetRect(name.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(-124f, 0f));
        name.text = label;

        TMP_InputField field = MenuUi.CreateInputField(
            "Value",
            band,
            "0",
            12f,
            TMP_InputField.ContentType.IntegerNumber
        );
        MenuUi.SetRect((RectTransform)field.transform, Anchors.Right, new Vector2(-120f, 3f), new Vector2(-50f, -3f));

        LazyButton button = MenuUi.CreateButton(
            "Set",
            band,
            "Set",
            Anchors.Right,
            new Vector2(-46f, 3f),
            new Vector2(0f, -3f)
        );
        TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonLabel != null)
        {
            buttonLabel.fontSize = 12f;
        }

        void Sync()
        {
            PlayerData player = MainGame.PlayerData;
            int? value = player != null
                ? get(player)
                : null;
            field.SetTextWithoutNotify(value?.ToString() ?? string.Empty);
        }

        void Apply()
        {
            PlayerData player = MainGame.PlayerData;
            if (player == null)
            {
                Plugin.Logger.LogWarning($"[Player] can't set {label} — no active game/player.");
                return;
            }
            if (!int.TryParse(field.text, out int value))
            {
                Sync();
                return;
            }
            set(player, value);
            Plugin.Logger.LogInfo($"[Player] set {label} to {value}.");
            Sync();
        }

        button.onClick.AddListener(Apply);
        field.onSubmit.AddListener(_ => Apply());
        _page.AddSync(Sync);
    }

    /// <summary>A `&lt; NPC name &gt;` cycler picking which reputation the row below edits.</summary>
    private void RepCyclerRow()
    {
        RectTransform cycler = _page.Band(28f, 8f);

        Image field = MenuUi.CreateImage("Field", cycler, new Color(0.2f, 0.1f, 0.07f, 1f));
        MenuUi.SetRect(field.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));
        field.raycastTarget = false;
        MenuUi.ApplyCell(field);

        _repLabel = MenuUi.CreateText("Value", cycler, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_repLabel);
        MenuUi.SetRect(_repLabel.rectTransform, Anchors.Fill, new Vector2(28f, 0f), new Vector2(-28f, 0f));

        LazyButton prev = MenuUi.CreateButton("Prev", cycler, "<", Anchors.Left, Vector2.zero, new Vector2(26f, 0f));
        LazyButton next = MenuUi.CreateButton("Next", cycler, ">", Anchors.Right, new Vector2(-26f, 0f), Vector2.zero);
        prev.onClick.AddListener(() => CycleRep(-1));
        next.onClick.AddListener(() => CycleRep(1));

        _page.AddSync(() =>
        {
            _repLabel.text = CurrentRep() is RepEntry rep
                ? rep.Name
                : "No NPCs";
        });
    }

    private void CycleRep(int step)
    {
        if (_reps.Count == 0)
        {
            return;
        }
        _repIndex = (_repIndex + step + _reps.Count) % _reps.Count;
        _page.Sync();
    }

    private RepEntry? CurrentRep()
    {
        return _repIndex < _reps.Count
            ? _reps[_repIndex]
            : (RepEntry?)null;
    }

    /// <summary>
    /// Every NPC with a reputation resource (<c>WGODef.repResName</c>), deduped by resource since
    /// the same NPC can have several object definitions.
    /// </summary>
    private void LoadReps()
    {
        if (GameBalance.Me == null)
        {
            return;
        }
        var seen = new HashSet<string>();
        foreach (WGODef def in GameBalance.Me.wgoDefs)
        {
            if (def == null
                || string.IsNullOrEmpty(def.repResName)
                || ExcludedReps.Contains(def.id)
                || !seen.Add(def.repResName))
            {
                continue;
            }
            string name = LLBase.L(def.id);
            _reps.Add(new RepEntry { Res = def.repResName, Name = string.IsNullOrEmpty(name) ? def.id : name });
        }
        _reps.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _repIndex = 0;
    }

    private struct RepEntry
    {
        public string Res;
        public string Name;
    }
}
