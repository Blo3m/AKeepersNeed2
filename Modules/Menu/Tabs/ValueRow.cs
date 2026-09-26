using System;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// A "Label [value] [Set]" row for a live <see cref="PlayerData"/> value. The field shows the
/// current value on every sync (blank while no game is loaded), and Set (or Enter) writes the
/// typed value and toasts the result.
/// </summary>
internal static class ValueRow
{
    /// <summary>
    /// Adds the row to <paramref name="page"/>. <paramref name="get"/> returning null means there's
    /// nothing to set right now. <paramref name="toastLabel"/> overrides <paramref name="label"/> in
    /// the toast/log when the row's target changes (the reputation row names the selected NPC).
    /// </summary>
    public static void Add(
        MenuPage page,
        string label,
        Func<PlayerData, int?> get,
        Action<PlayerData, int> set,
        Func<string> toastLabel = null
    )
    {
        RectTransform band = page.Band(30f, 8f);

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
            string target = toastLabel?.Invoke() ?? label;
            PlayerData player = MainGame.PlayerData;
            if (player == null)
            {
                Plugin.Logger.LogWarning($"[Player] can't set {target} — no active game/player.");
                Toast.Show("No active game");
                return;
            }
            if (get(player) == null)
            {
                Toast.Show($"Can't set {target}");
                return;
            }
            if (!int.TryParse(field.text, out int value))
            {
                Toast.Show("Invalid number");
                Sync();
                return;
            }
            set(player, value);
            Plugin.Logger.LogInfo($"[Player] set {target} to {value}.");
            Toast.Show($"{target} set to {value}");
            Sync();
        }

        button.onClick.AddListener(Apply);
        field.onSubmit.AddListener(_ => Apply());
        page.AddSync(Sync);
    }
}
