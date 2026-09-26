using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// A short native-styled notice in the top-left corner of the screen that holds, then fades out.
/// Runs on unscaled time so it still fades while the game is paused. One shared instance is
/// created on first use; any module posts to it through <see cref="Show"/>. A new message
/// replaces the current one; callers that merge repeats (e.g. a running total) pass a key and
/// check <see cref="IsShowing"/> first.
/// </summary>
internal sealed class Toast : MonoBehaviour
{
    private const float HoldSeconds = 1.5f;
    private const float FadeSeconds = 0.5f;

    private static Toast _instance;

    private CanvasGroup _group;
    private RectTransform _panel;
    private TextMeshProUGUI _text;
    private float _shownAt;
    private string _key;

    /// <summary>True while the toast last posted with <paramref name="key"/> is still on screen.</summary>
    public static bool IsShowing(string key)
    {
        return key != null
            && _instance != null
            && _instance.gameObject.activeSelf
            && _instance._key == key;
    }

    public static void Show(string message, string key = null)
    {
        // Unity's null check: the instance dies with the UI root on a scene reload.
        if (_instance == null)
        {
            _instance = Create();
            if (_instance == null)
            {
                Plugin.Logger.LogInfo($"[Toast] {message}");
                return;
            }
        }
        _instance.Display(message, key);
    }

    private static Toast Create()
    {
        NativeUiSkin.TryCapture();

        Transform uiRoot = MenuUi.FindUiRoot();
        if (uiRoot == null)
        {
            return null;
        }

        var root = new GameObject("AKN_Toast", typeof(RectTransform));
        root.transform.SetParent(uiRoot, false);
        MenuUi.Stretch((RectTransform)root.transform);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 460;

        var toast = root.AddComponent<Toast>();
        toast.Build();
        root.SetActive(false);
        return toast;
    }

    private void Build()
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;

        Image panel = MenuUi.CreateImage("Panel", transform,
            NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 0.95f));
        panel.raycastTarget = false;
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            panel.sprite = NativeUiSkin.HeaderSprite;
            panel.type = Image.Type.Sliced;
        }
        _panel = panel.rectTransform;
        _panel.anchorMin = _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _panel.sizeDelta = new Vector2(260f, 30f);
        _panel.anchoredPosition = new Vector2(20f, -40f);

        _text = MenuUi.CreateText("Text", _panel, 13f, TextAlignmentOptions.Center);
        MenuUi.ApplyHeaderText(_text);
        MenuUi.SetRect(_text.rectTransform, Anchors.Fill, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void Display(string message, string key)
    {
        _key = key;
        float scale = Mathf.Clamp(ModConfig.UiScale.Value, 0.5f, 3f);
        _panel.localScale = new Vector3(scale, scale, 1f);
        _text.text = message;
        _shownAt = Time.unscaledTime;
        _group.alpha = 1f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float elapsed = Time.unscaledTime - _shownAt;
        if (elapsed >= HoldSeconds + FadeSeconds)
        {
            gameObject.SetActive(false);
            return;
        }
        _group.alpha = 1f - Mathf.Clamp01((elapsed - HoldSeconds) / FadeSeconds);
    }
}
