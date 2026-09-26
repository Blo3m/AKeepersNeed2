using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Builds the menu window's fixed visuals: the screen shade, the right-docked panel with its
/// inner background and game frame, and the title header. Falls back to flat colours when
/// <see cref="NativeUiSkin"/> couldn't harvest the game's sprites.
/// </summary>
internal static class MenuChrome
{
    private const float PanelWidth = 340f;

    /// <summary>Adds the shade and panel under <paramref name="window"/>; returns the panel rect.</summary>
    public static RectTransform BuildPanel(Transform window)
    {
        float shadeAlpha = NativeUiSkin.IsReady ? 0.4f : 0.72f;
        Image shade = MenuUi.CreateImage("Shade", window, new Color(0f, 0f, 0f, shadeAlpha));
        MenuUi.Stretch(shade.rectTransform);

        Color panelColor = NativeUiSkin.IsReady ? Color.clear : new Color(0.055f, 0.035f, 0.03f, 0.99f);
        Image panel = MenuUi.CreateImage("Panel", window, panelColor);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, 0f);
        panelRect.anchoredPosition = Vector2.zero;

        if (NativeUiSkin.IsReady)
        {
            Image inner = MenuUi.CreateImage("InnerBackground", panelRect, new Color(0.105f, 0.112f, 0.14f, 1f));
            // Starts under the header (which ends at -36 and draws on top) so no see-through
            // strip is left between them; the header sprite's bottom edge is partly transparent.
            MenuUi.SetRect(inner.rectTransform, Anchors.Fill, new Vector2(13f, 13f), new Vector2(-13f, -24f));
            inner.raycastTarget = false;

            Image frame = MenuUi.CreateImage("Frame", panelRect, Color.white);
            MenuUi.Stretch(frame.rectTransform);
            MenuUi.ApplyFrame(frame);
            frame.raycastTarget = false;
            frame.transform.SetAsLastSibling();
        }
        return panelRect;
    }

    /// <summary>Adds the title header to <paramref name="panel"/>; returns its title text.</summary>
    public static TextMeshProUGUI BuildHeader(RectTransform panel, string title)
    {
        Color headerColor = NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 1f);
        Image header = MenuUi.CreateImage("Header", panel, headerColor);
        MenuUi.SetRect(header.rectTransform, Anchors.Top, new Vector2(12f, -36f), new Vector2(-12f, -12f));
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            header.sprite = NativeUiSkin.HeaderSprite;
            header.type = Image.Type.Sliced;
            header.color = Color.white;
        }

        float titleSize = NativeUiSkin.IsReady ? 13f : 18f;
        TextMeshProUGUI text = MenuUi.CreateText("Title", header.rectTransform, titleSize, TextAlignmentOptions.Center);
        MenuUi.SetRect(text.rectTransform, Anchors.Fill, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        MenuUi.ApplyHeaderText(text);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.text = title;
        return text;
    }
}
