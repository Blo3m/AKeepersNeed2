using System;
using AKeepersNeed2.Shared.Ui;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Small centred dialogs for the menu, styled like the game's framed popups: a bare frame
/// (for custom content), a confirm prompt, and a single-line name prompt with inline
/// validation. Modal dialogs sit on a full-screen blocker so the menu behind can't be clicked.
/// </summary>
internal static class MenuDialog
{
    private static readonly Color InnerColor = new Color(0.08f, 0.09f, 0.11f, 0.99f);
    private static readonly Color ErrorColor = new Color(0.9f, 0.45f, 0.35f, 1f);

    /// <summary>
    /// Creates a framed dialog with a title. Returns the root to destroy on close;
    /// <paramref name="frame"/> is where content goes.
    /// </summary>
    public static GameObject CreateFrame(
        Transform parent,
        string name,
        Vector2 size,
        string title,
        bool modal,
        out RectTransform frame
    )
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(parent, false);
        if (modal)
        {
            MenuUi.Stretch(rootRect);
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        }
        else
        {
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = size;
        }

        var go = new GameObject("Frame", typeof(RectTransform));
        frame = (RectTransform)go.transform;
        frame.SetParent(rootRect, false);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = size;
        frame.anchoredPosition = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = InnerColor;
        if (NativeUiSkin.IsReady && NativeUiSkin.FrameSprite != null)
        {
            bg.sprite = NativeUiSkin.FrameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            Image innerBg = MenuUi.CreateImage("Inner", frame, InnerColor);
            MenuUi.SetRect(innerBg.rectTransform,
                new Vector2(8f, 8f), new Vector2(-8f, -8f), Vector2.zero, Vector2.one);
            innerBg.raycastTarget = false;
        }

        TextMeshProUGUI titleText = MenuUi.CreateText("Title", frame, 13f, TextAlignmentOptions.Center, Color.white);
        MenuUi.SetRect(titleText.rectTransform,
            new Vector2(10f, -36f), new Vector2(-10f, -12f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        titleText.text = title;

        root.transform.SetAsLastSibling();
        return root;
    }

    public static GameObject ShowConfirm(
        Transform parent,
        string title,
        string message,
        string confirmLabel,
        Action onConfirm
    )
    {
        GameObject root = CreateFrame(parent, "ConfirmDialog", new Vector2(260f, 132f), title, true,
            out RectTransform frame);

        TextMeshProUGUI body = MenuUi.CreateText("Message", frame, 12f, TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyLabelText(body);
        MenuUi.SetRect(body.rectTransform,
            new Vector2(14f, 50f), new Vector2(-14f, -38f), Vector2.zero, Vector2.one);
        body.text = message;

        AddButtons(frame, confirmLabel, () =>
        {
            UnityEngine.Object.Destroy(root);
            onConfirm();
        }, () => UnityEngine.Object.Destroy(root));
        return root;
    }

    /// <summary>
    /// Asks for a name. <paramref name="submit"/> returns an error to show inline, or null to
    /// accept (which closes the dialog).
    /// </summary>
    public static GameObject ShowNamePrompt(
        Transform parent,
        string title,
        string initial,
        int maxLength,
        Func<string, string> submit
    )
    {
        GameObject root = CreateFrame(parent, "NameDialog", new Vector2(260f, 150f), title, true,
            out RectTransform frame);

        TMP_InputField input = MenuUi.CreateInputField("Name", frame, "Name", 12f);
        MenuUi.SetRect((RectTransform)input.transform,
            new Vector2(16f, -70f), new Vector2(-16f, -42f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        input.characterLimit = maxLength;
        input.text = initial;

        TextMeshProUGUI error = MenuUi.CreateText("Error", frame, 11f, TextAlignmentOptions.Center, ErrorColor);
        MenuUi.SetRect(error.rectTransform,
            new Vector2(12f, -92f), new Vector2(-12f, -72f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        void Submit()
        {
            string problem = submit(input.text);
            if (problem == null)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                error.text = problem;
            }
        }

        input.onSubmit.AddListener(_ => Submit());
        input.onValueChanged.AddListener(_ => error.text = string.Empty);
        AddButtons(frame, "OK", Submit, () => UnityEngine.Object.Destroy(root));

        input.Select();
        input.ActivateInputField();
        return root;
    }

    private static void AddButtons(RectTransform frame, string confirmLabel, Action onConfirm, Action onCancel)
    {
        LazyButton confirm = MenuUi.CreateButton("Confirm", frame, confirmLabel,
            new Vector2(12f, 12f), new Vector2(-6f, 44f), new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        MenuUi.ApplyDialogButton(confirm);
        confirm.onClick.AddListener(() => onConfirm());

        LazyButton cancel = MenuUi.CreateButton("Cancel", frame, "Cancel",
            new Vector2(6f, 12f), new Vector2(-12f, 44f), new Vector2(0.5f, 0f), new Vector2(1f, 0f));
        cancel.onClick.AddListener(() => onCancel());
    }
}
