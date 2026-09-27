using System;
using System.Collections.Generic;
using System.Linq;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using AKeepersNeed2.Shared.Zombies;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// The expanded part of a zombie's list entry: speed overrides, talents, tech points, porter
/// capacity and perks, all editing the entry's <see cref="ZombieEdit"/>. Nothing is written until
/// Apply, which asks for confirmation first. The editor rebuilds itself whenever its shape changes
/// (a fold opens, an override is turned on, a perk cascade) and reports its new height.
/// </summary>
internal sealed class ZombieEditor
{
    private const float ControlWidth = 76f;
    private const float FieldWidth = 64f;

    private readonly RectTransform _root;
    private readonly ZombieEdit _edit;
    private readonly MenuDialogHost _dialogs;
    private readonly Action _onResized;
    private readonly Action _onChanged;
    private readonly HashSet<string> _openBranches = new HashSet<string>();
    private bool _includeHidden;

    public ZombieEditor(
        RectTransform root,
        ZombieEdit edit,
        MenuDialogHost dialogs,
        Action onResized,
        Action onChanged
    )
    {
        _root = root;
        _edit = edit;
        _dialogs = dialogs;
        _onResized = onResized;
        _onChanged = onChanged;
        Rebuild();
    }

    public float Height { get; private set; }

    public RectTransform Root => _root;

    public void Rebuild()
    {
        for (int i = _root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(_root.GetChild(i).gameObject);
        }

        Image background = MenuUi.CreateImage("Background", _root, new Color(0f, 0f, 0f, 0.18f));
        MenuUi.Stretch(background.rectTransform);
        background.raycastTarget = false;

        RectTransform body = MenuUi.CreateRect("Body", _root);
        MenuUi.SetRect(body, Anchors.Fill, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        var stack = new EditorStack(body);
        ZombieEdit.Snapshot staged = _edit.Staged;

        stack.Heading("Speed (empty = follow the global setting)");
        SpeedRows(stack, "Own Craft Speed", staged.CraftSpeed, 1f, 50f, value => staged.CraftSpeed = value);
        SpeedRows(stack, "Own Gather Speed", staged.GatherSpeed, 1f, 50f, value => staged.GatherSpeed = value);
        SpeedRows(stack, "Own Walk Speed", staged.WalkSpeed, 0.5f, 10f, value => staged.WalkSpeed = value);

        stack.Heading("Talents (base value, effective mastery in brackets)");
        foreach (string talentId in staged.Talents.Keys.ToList())
        {
            int effective = _edit.Zombie.GetMasteryLevelForTalentBranch(talentId);
            IntRow(
                stack,
                $"{ZombieLabels.BranchWithIcon(talentId)}  ({effective})",
                staged.Talents[talentId],
                value => staged.Talents[talentId] = value
            );
        }

        stack.Heading("Tech Points");
        IntRow(stack, "Red Tech Points", staged.TechRed, value => staged.TechRed = value);
        IntRow(stack, "Green Tech Points", staged.TechGreen, value => staged.TechGreen = value);
        IntRow(stack, "Blue Tech Points", staged.TechBlue, value => staged.TechBlue = value);

        if (_edit.Zombie.ZombieType == ZombieType.Porter)
        {
            int? now = PorterInventory.Size(_edit.Zombie);
            stack.Heading($"Porter (now carries {now?.ToString() ?? "?"} slots)");
            ToggleRow(stack, "Own Capacity", staged.PorterCapacity.HasValue, on =>
            {
                staged.PorterCapacity = on
                    ? now ?? PorterInventory.VanillaSize
                    : (int?)null;
                Rebuild();
            });
            if (staged.PorterCapacity.HasValue)
            {
                SliderRow(
                    stack,
                    PorterInventory.VanillaSize,
                    50f,
                    "0",
                    staged.PorterCapacity.Value,
                    value => staged.PorterCapacity = Mathf.RoundToInt(value)
                );
            }
        }

        stack.Heading("Perks");
        ToggleRow(stack, "Ignore Perk Cap", staged.IgnorePerkCap, on =>
        {
            staged.IgnorePerkCap = on;
            Changed();
        });
        ToggleRow(stack, "Include Hidden Perks", _includeHidden, on =>
        {
            _includeHidden = on;
            Rebuild();
        });
        foreach (string talentId in staged.Talents.Keys)
        {
            PerkBranch(stack, talentId, staged.Perks);
        }

        Footer(stack.Band(28f, 12f));

        Height = stack.Height + 10f;
        _root.sizeDelta = new Vector2(_root.sizeDelta.x, Height);
        _onResized();
    }

    private void Changed()
    {
        _onChanged();
    }

    private void SpeedRows(EditorStack stack, string label, float? value, float min, float max, Action<float?> set)
    {
        ToggleRow(stack, label, value.HasValue, on =>
        {
            set(on ? 1f : (float?)null);
            Rebuild();
        });
        if (value.HasValue)
        {
            SliderRow(stack, min, max, "0.0", value.Value, v => set(v));
        }
    }

    private void ToggleRow(EditorStack stack, string label, bool value, Action<bool> set, bool interactable = true)
    {
        RectTransform band = stack.Band(26f);
        EditorStack.Label(band, label, ControlWidth + 8f);
        RectTransform rect = MenuUi.CreateRect("Toggle", band);
        MenuUi.SetRect(rect, Anchors.Right, new Vector2(-ControlWidth, 2f), new Vector2(0f, -2f));
        bool current = value;
        MenuPage.FillToggle(rect, () => current, on =>
        {
            current = on;
            set(on);
        });
        if (!interactable)
        {
            var group = band.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.alpha = 0.45f;
        }
    }

    private void SliderRow(EditorStack stack, float min, float max, string format, float value, Action<float> set)
    {
        RectTransform band = stack.Band(22f);
        float current = value;
        void OnSlide(float v)
        {
            current = v;
            set(v);
            Changed();
        }
        MenuPage.FillSlider(band, min, max, format, () => current, OnSlide, out _);
    }

    private void IntRow(EditorStack stack, string label, int value, Action<int> set)
    {
        RectTransform band = stack.Band(26f);
        EditorStack.Label(band, label, FieldWidth + 8f);
        TMP_InputField field = MenuUi.CreateInputField(
            "Value",
            band,
            "0",
            12f,
            TMP_InputField.ContentType.IntegerNumber
        );
        MenuUi.SetRect(
            (RectTransform)field.transform,
            Anchors.Right,
            new Vector2(-FieldWidth, 2f),
            new Vector2(0f, -2f)
        );
        field.SetTextWithoutNotify(value.ToString());
        field.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int parsed))
            {
                set(parsed);
                Changed();
            }
        });
    }

    private void PerkBranch(EditorStack stack, string talentId, HashSet<string> learned)
    {
        List<TalentLevelUpDef> perks = ZombiePerks.ForBranch(talentId, _includeHidden);
        if (perks.Count == 0)
        {
            return;
        }
        bool open = _openBranches.Contains(talentId);
        int count = perks.Count(def => learned.Contains(def.id));
        string arrow = open ? "▾" : "▸";

        RectTransform band = stack.Band(26f, 6f);
        LazyButton fold = MenuUi.CreateButton(
            "Fold",
            band,
            $"{arrow} {ZombieLabels.BranchWithIcon(talentId)} perks ({count}/{perks.Count})",
            Anchors.Fill,
            Vector2.zero,
            Vector2.zero
        );
        TextMeshProUGUI foldLabel = fold.GetComponentInChildren<TextMeshProUGUI>(true);
        if (foldLabel != null)
        {
            foldLabel.fontSize = 12f;
            foldLabel.alignment = TextAlignmentOptions.Left;
            foldLabel.margin = new Vector4(8f, 0f, 0f, 0f);
        }
        fold.onClick.AddListener(() =>
        {
            if (!_openBranches.Remove(talentId))
            {
                _openBranches.Add(talentId);
            }
            Rebuild();
        });
        if (!open)
        {
            return;
        }

        List<string> suspended = _edit.Zombie.disabledTalentLevelUps;
        foreach (TalentLevelUpDef def in perks)
        {
            bool on = learned.Contains(def.id);
            string label = "    " + ZombieLabels.Perk(def);
            if (def.availableAtStart)
            {
                label += " (start perk)";
            }
            else if (on && suspended.Contains(def.id))
            {
                label += " (suspended by cap)";
            }
            bool canChange = on
                ? !def.availableAtStart
                : ZombiePerks.ParentsMet(def, learned);
            ToggleRow(
                stack,
                label,
                on,
                value =>
                {
                    _edit.SetPerk(def, value);
                    Changed();
                    Rebuild();
                },
                canChange
            );
        }
    }

    private void Footer(RectTransform band)
    {
        LazyButton revert = MenuUi.CreateButton(
            "Revert",
            band,
            "Revert",
            Anchors.BottomLeftHalf,
            new Vector2(0f, 0f),
            new Vector2(-4f, 28f)
        );
        LazyButton apply = MenuUi.CreateButton(
            "Apply",
            band,
            "Apply",
            Anchors.BottomRightHalf,
            new Vector2(4f, 0f),
            new Vector2(0f, 28f)
        );
        revert.onClick.AddListener(() =>
        {
            _edit.Revert();
            Changed();
            Rebuild();
        });
        apply.onClick.AddListener(ConfirmApply);
    }

    private void ConfirmApply()
    {
        List<string> changes = _edit.Describe();
        if (changes.Count == 0)
        {
            Toast.Show("No changes to apply");
            return;
        }
        const int shown = 8;
        List<string> lines = changes.Take(shown).ToList();
        if (changes.Count > shown)
        {
            lines.Add($"…and {changes.Count - shown} more");
        }
        string name = ZombieLabels.Name(_edit.Zombie);
        _dialogs.ShowConfirm(
            $"Apply changes to {name}?",
            string.Join("\n", lines) + "\n\nThis changes your save permanently.",
            "Apply",
            () =>
            {
                _edit.Apply();
                Plugin.Logger.LogInfo($"[Menu] applied {changes.Count} zombie changes to {name}.");
                Toast.Show($"{name}: {changes.Count} changes applied");
                Changed();
                Rebuild();
            },
            132f + 16f * lines.Count
        );
    }
}
