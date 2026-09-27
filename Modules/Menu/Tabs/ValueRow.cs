using System;
using AKeepersNeed2.Modules.Menu.Controls;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu.Tabs;

/// <summary>
/// A "Label [value] [Set]" row for a live save value. The field shows the current value on every
/// sync (blank while no game is loaded). Set (or Enter) asks for confirmation, since the value is
/// written into the save, then writes it and toasts the result.
/// </summary>
internal static class ValueRow
{
    /// <summary>Optional parts of a value row.</summary>
    internal sealed class Options
    {
        /// <summary>Overrides the label in the toast/log (the reputation row names the selected NPC).</summary>
        public Func<string> ToastLabel;

        /// <summary>Returns why a value can't be set (shown as a toast), or null when it's fine.</summary>
        public Func<PlayerData, int, string> Validate;

        /// <summary>An extra sentence for the confirm dialog (e.g. side effects).</summary>
        public string Warning;
    }

    /// <summary>
    /// Adds the row to <paramref name="page"/>. <paramref name="get"/> returning null means there's
    /// nothing to set right now.
    /// </summary>
    public static void Add(
        MenuPage page,
        MenuDialogHost dialogs,
        string label,
        Func<PlayerData, int?> get,
        Action<PlayerData, int> set,
        Func<string> toastLabel = null
    )
    {
        page.AddSync(Build(page.Band(30f, 8f), dialogs, label, get, set, new Options { ToastLabel = toastLabel }));
    }

    /// <summary>Builds the row into <paramref name="band"/>; returns its re-read action.</summary>
    public static Action Build(
        RectTransform band,
        MenuDialogHost dialogs,
        string label,
        Func<PlayerData, int?> get,
        Action<PlayerData, int> set,
        Options options = null
    )
    {
        options = options ?? new Options();

        TextMeshProUGUI name = MenuUi.CreateText("Name", band, 13f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(name);
        MenuUi.SetRect(name.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(-124f, 0f));
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
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
            string target = options.ToastLabel?.Invoke() ?? label;
            PlayerData player = MainGame.PlayerData;
            if (player == null)
            {
                Plugin.Logger.LogWarning($"[Menu] can't set {target} — no active game/player.");
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
            string problem = options.Validate?.Invoke(player, value);
            if (problem != null)
            {
                Toast.Show(problem);
                Sync();
                return;
            }
            string message = $"{target} will be set to {value} in your current save. This is permanent.";
            if (!string.IsNullOrEmpty(options.Warning))
            {
                message += "\n" + options.Warning;
            }
            dialogs.ShowConfirm(
                $"Set {target}?",
                message,
                "Set",
                () => Write(target, value),
                string.IsNullOrEmpty(options.Warning) ? 132f : 164f
            );
        }

        void Write(string target, int value)
        {
            PlayerData player = MainGame.PlayerData;
            if (player == null)
            {
                Toast.Show("No active game");
                return;
            }
            set(player, value);
            Plugin.Logger.LogInfo($"[Menu] set {target} to {value}.");
            Toast.Show($"{target} set to {value}");
            Sync();
        }

        button.onClick.AddListener(Apply);
        field.onSubmit.AddListener(_ => Apply());
        Sync();
        return Sync;
    }
}
