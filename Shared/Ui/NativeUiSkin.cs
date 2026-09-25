using System;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// Harvests the game's UI look (fonts, colors, sprites, button/slider states) from
/// the live <c>UIGameSettingsWindow</c> so our from-scratch window matches it. Reads
/// the settings window's child hierarchy; it never clones or mutates it.
///
/// Technique borrowed from SuperMan4eg's GK2-Mod-Framework (NativeUiSkin). The child
/// paths below are the real prefab node names inside the settings window.
/// </summary>
internal static class NativeUiSkin
{
    public static bool IsReady { get; private set; }

    public static TMP_FontAsset RegularFont { get; private set; }
    public static TMP_FontAsset BoldFont { get; private set; }
    public static Material RegularMaterial { get; private set; }
    public static Material BoldMaterial { get; private set; }

    public static Color LabelColor { get; private set; } = new Color(0.59f, 0.55f, 0.53f, 1f);
    public static Color ValueColor { get; private set; } = new Color(1f, 0.74f, 0f, 1f);
    public static Color ButtonTextColor { get; private set; } = new Color(1f, 0.74f, 0f, 1f);

    public static Sprite FrameSprite { get; private set; }
    public static Sprite HeaderSprite { get; private set; }
    public static Sprite HeaderDecorSprite { get; private set; }
    public static Sprite CellSprite { get; private set; }

    public static Sprite DialogButtonSprite { get; private set; }
    public static ColorBlock DialogButtonColors { get; private set; }
    public static SpriteState DialogButtonSpriteState { get; private set; }

    public static Sprite ProgressBackgroundSprite { get; private set; }
    public static Sprite ProgressFillSprite { get; private set; }
    public static Sprite SliderHandleSprite { get; private set; }
    public static ColorBlock SliderColors { get; private set; }
    public static SpriteState SliderSpriteState { get; private set; }

    public static bool TryCapture()
    {
        if (IsReady)
        {
            return true;
        }

        try
        {
            UIGameSettingsWindow window = LazyUI.GetWindow<UIGameSettingsWindow>();
            if (window == null)
            {
                return false;
            }

            Transform root = window.transform;
            // "GenericWIndowLayout" is the game's own (misspelled) node name.
            Transform layout = root.Find("GenericWIndowLayout");
            Transform content = layout?.Find("Content");
            Transform frame = layout?.Find("Frame");
            Transform header = frame?.Find("HeaderGroup");

            FrameSprite = GetImage(frame)?.sprite;
            HeaderSprite = GetImage(header?.Find("Background"))?.sprite;
            HeaderDecorSprite = GetImage(header?.Find("DecorCommonLeft"))?.sprite;

            TextMeshProUGUI headerText = header?.Find("Header")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI labelText =
                content?.Find("ResolutionSwitchBtn/LeftName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI valueText =
                content?.Find("ResolutionSwitchBtn/Back/Value")?.GetComponent<TextMeshProUGUI>();

            RegularFont = labelText?.font;
            RegularMaterial = labelText?.fontSharedMaterial;
            BoldFont = headerText?.font;
            BoldMaterial = headerText?.fontSharedMaterial;
            if (labelText != null)
            {
                LabelColor = labelText.color;
            }
            if (valueText != null)
            {
                ValueColor = valueText.color;
            }

            CellSprite = GetImage(content?.Find("ResolutionSwitchBtn/Back"))?.sprite;

            LazyButton dialogButton =
                content?.Find("DialogueButtonPrefab")?.GetComponent<LazyButton>();
            if (dialogButton?.targetGraphic is Image dialogImage)
            {
                DialogButtonSprite = dialogImage.sprite;
                DialogButtonColors = dialogButton.colors;
                DialogButtonSpriteState = dialogButton.spriteState;
            }

            TextMeshProUGUI dialogText =
                content?.Find("DialogueButtonPrefab/Content/Back/Label")?.GetComponent<TextMeshProUGUI>();
            if (dialogText != null)
            {
                ButtonTextColor = dialogText.color;
            }

            Slider nativeSlider = content?.Find("MasterVolume/Slider")?.GetComponent<Slider>();
            if (nativeSlider != null)
            {
                ProgressBackgroundSprite = GetImage(content.Find("MasterVolume/Slider/Background"))?.sprite;
                ProgressFillSprite = GetImage(content.Find("MasterVolume/Slider/Fill Area/Fill"))?.sprite;
                SliderHandleSprite = GetImage(content.Find("MasterVolume/Slider/Handle Slide Area/Handle"))?.sprite;
                SliderColors = nativeSlider.colors;
                SliderSpriteState = nativeSlider.spriteState;
            }

            IsReady =
                RegularFont != null
                && BoldFont != null
                && FrameSprite != null
                && HeaderSprite != null
                && CellSprite != null
                && DialogButtonSprite != null;

            Plugin.Logger.LogInfo(
                $"[Skin] capture ready={IsReady} regular={RegularFont?.name ?? "<null>"} "
                + $"bold={BoldFont?.name ?? "<null>"} frame={FrameSprite?.name ?? "<null>"} "
                + $"cell={CellSprite?.name ?? "<null>"} slider={ProgressFillSprite?.name ?? "<null>"}");
            return IsReady;
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"[Skin] capture failed, using fallback styling: {ex.Message}");
            return false;
        }
    }

    private static Image GetImage(Transform transform)
    {
        if (transform == null)
        {
            return null;
        }
        return transform.GetComponent<Image>();
    }
}
