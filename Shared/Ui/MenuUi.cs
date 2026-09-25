using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// Small factory of uGUI builders that apply <see cref="NativeUiSkin"/> so controls
/// we create at runtime match the game's look. Adapted from GK2-Mod-Framework
/// (FrameworkUi), trimmed to what this mod's single-page menu needs.
/// </summary>
internal static class MenuUi
{
    public static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, float size,
        TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (NativeUiSkin.IsReady && NativeUiSkin.RegularFont != null)
        {
            text.font = NativeUiSkin.RegularFont;
            if (NativeUiSkin.RegularMaterial != null)
            {
                text.fontSharedMaterial = NativeUiSkin.RegularMaterial;
            }
        }
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Creates a single-line TMP input field (cell-styled) with a placeholder.</summary>
    public static TMP_InputField CreateInputField(string name, Transform parent, string placeholder,
        float size, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.1f, 0.07f, 1f);
        ApplyCell(image);

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = contentType;

        var area = new GameObject("TextArea", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        var areaRect = (RectTransform)area.transform;
        SetRect(areaRect, new Vector2(6f, 2f), new Vector2(-6f, -2f), Vector2.zero, Vector2.one);
        area.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateText("Text", area.transform, size, TextAlignmentOptions.Left, Color.white);
        Stretch(text.rectTransform);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI ph = CreateText("Placeholder", area.transform, size,
            TextAlignmentOptions.Left, new Color(0.6f, 0.56f, 0.53f, 0.7f));
        Stretch(ph.rectTransform);
        ph.textWrappingMode = TextWrappingModes.NoWrap;
        ph.text = placeholder;

        input.textViewport = areaRect;
        input.textComponent = text;
        input.placeholder = ph;
        input.text = string.Empty;
        return input;
    }

    public static LazyButton CreateButton(string name, Transform parent, string label,
        Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        SetRect(rect, offsetMin, offsetMax, anchorMin, anchorMax);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.28f, 0.12f, 0.07f, 1f);
        if (NativeUiSkin.IsReady && NativeUiSkin.CellSprite != null)
        {
            image.sprite = NativeUiSkin.CellSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        var button = go.AddComponent<LazyButton>();
        button.targetGraphic = image;
        go.AddComponent<GamepadNavigationItem>();

        var text = CreateText("Label", go.transform, 17f, TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        text.text = label;
        return button;
    }

    public static void ApplyHeaderText(TextMeshProUGUI text)
    {
        if (text == null || !NativeUiSkin.IsReady)
        {
            return;
        }
        if (NativeUiSkin.BoldFont != null)
        {
            text.font = NativeUiSkin.BoldFont;
        }
        if (NativeUiSkin.BoldMaterial != null)
        {
            text.fontSharedMaterial = NativeUiSkin.BoldMaterial;
        }
        text.color = Color.white;
    }

    public static void ApplyLabelText(TextMeshProUGUI text)
    {
        if (text == null || !NativeUiSkin.IsReady)
        {
            return;
        }
        text.color = NativeUiSkin.LabelColor;
    }

    public static void ApplyValueText(TextMeshProUGUI text)
    {
        if (text == null || !NativeUiSkin.IsReady)
        {
            return;
        }
        text.color = NativeUiSkin.ValueColor;
    }

    public static void ApplyCell(Image image)
    {
        if (image == null || !NativeUiSkin.IsReady || NativeUiSkin.CellSprite == null)
        {
            return;
        }
        image.sprite = NativeUiSkin.CellSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    public static void ApplyFrame(Image image)
    {
        if (image == null || !NativeUiSkin.IsReady || NativeUiSkin.FrameSprite == null)
        {
            return;
        }
        image.sprite = NativeUiSkin.FrameSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    public static void ApplyDialogButton(LazyButton button)
    {
        if (button == null || !NativeUiSkin.IsReady)
        {
            return;
        }
        if (button.targetGraphic is Image image && NativeUiSkin.DialogButtonSprite != null)
        {
            image.sprite = NativeUiSkin.DialogButtonSprite;
            image.type = Image.Type.Tiled;
            image.color = Color.white;
        }
        button.transition = Selectable.Transition.SpriteSwap;
        button.colors = NativeUiSkin.DialogButtonColors;
        button.spriteState = NativeUiSkin.DialogButtonSpriteState;

        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            if (NativeUiSkin.BoldFont != null)
            {
                label.font = NativeUiSkin.BoldFont;
            }
            if (NativeUiSkin.BoldMaterial != null)
            {
                label.fontSharedMaterial = NativeUiSkin.BoldMaterial;
            }
            label.fontSize = 16f;
            label.color = NativeUiSkin.ButtonTextColor;
        }
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
