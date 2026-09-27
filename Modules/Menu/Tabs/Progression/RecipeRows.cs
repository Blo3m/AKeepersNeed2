using System;
using System.Linq;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Progression;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs.Progression;

/// <summary>
/// The Progression tab's Recipes section: one "Kind (N locked) [Unlock]" row per
/// <see cref="RecipeKind"/> plus "All recipes", each behind a confirm (it's permanent). Counts are
/// re-read on every page sync.
/// </summary>
internal static class RecipeRows
{
    public static void Build(MenuPage page, MenuDialogHost dialogs)
    {
        foreach (RecipeKind kind in Enum.GetValues(typeof(RecipeKind)))
        {
            RecipeKind target = kind;
            Row(
                page,
                dialogs,
                Label(kind),
                () => RecipeUnlocker.Locked(target).Count,
                () => RecipeUnlocker.Unlock(target)
            );
        }
        Row(
            page,
            dialogs,
            "All Recipes",
            () => Enum.GetValues(typeof(RecipeKind)).Cast<RecipeKind>().Sum(kind => RecipeUnlocker.Locked(kind).Count),
            () => Enum.GetValues(typeof(RecipeKind)).Cast<RecipeKind>().Sum(RecipeUnlocker.Unlock)
        );
    }

    private static string Label(RecipeKind kind)
    {
        switch (kind)
        {
            case RecipeKind.Buildings:
                return "Buildings";
            case RecipeKind.TownBuildings:
                return "Town Buildings";
            case RecipeKind.Alchemy:
                return "Alchemy Formulas";
            default:
                return "Crafting Recipes";
        }
    }

    private static void Row(MenuPage page, MenuDialogHost dialogs, string label, Func<int> locked, Func<int> unlock)
    {
        RectTransform band = page.Band(30f, 8f);
        TextMeshProUGUI text = MenuUi.CreateText("Label", band, 13f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(text);
        MenuUi.SetRect(text.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(-92f, 0f));

        LazyButton button = MenuUi.CreateButton(
            "Unlock",
            band,
            "Unlock",
            Anchors.Right,
            new Vector2(-84f, 3f),
            new Vector2(0f, -3f)
        );
        TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonLabel != null)
        {
            buttonLabel.fontSize = 12f;
        }

        void Sync()
        {
            text.text = MainGame.Instance?.GameSave == null
                ? label
                : $"{label}  <size=11>({locked()} locked)</size>";
        }

        button.onClick.AddListener(() =>
        {
            int count = locked();
            if (count == 0)
            {
                Toast.Show("Nothing to unlock");
                return;
            }
            dialogs.ShowConfirm(
                $"Unlock {label.ToLower()}?",
                $"{count} locked entries become available. Retired recipes the game blocks stay blocked. "
                    + "This changes your save permanently.",
                "Unlock",
                () =>
                {
                    int unlocked = unlock();
                    Toast.Show($"Unlocked {unlocked} {label.ToLower()}");
                    Sync();
                },
                150f
            );
        });
        Sync();
        page.AddSync(Sync);
    }
}
