using System;
using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// The UI-scale control on the Settings tab. Dragging the slider shows a translucent ghost of
/// the menu panel at the new scale; releasing it opens a non-modal Apply/Cancel dialog. Only
/// Apply writes <see cref="ModConfig.UiScale"/> and rescales the real panel.
/// </summary>
internal sealed class UiScalePreview
{
    private const float MinScale = 0.5f;
    private const float MaxScale = 3f;

    private readonly Transform _dialogParent;
    private readonly RectTransform _panel;

    private Slider _slider;
    private Action _sync;
    private bool _active;
    private GameObject _ghost;
    private GameObject _dialog;
    private TextMeshProUGUI _dialogValue;

    public UiScalePreview(Transform dialogParent, RectTransform panel)
    {
        _dialogParent = dialogParent;
        _panel = panel;
    }

    public void BuildControl(RectTransform band)
    {
        TextMeshProUGUI label = MenuUi.CreateText("Label", band, 11f, TextAlignmentOptions.Left);
        MenuUi.ApplyLabelText(label);
        MenuUi.SetRect(label.rectTransform, Anchors.Fill, Vector2.zero, new Vector2(54f, 0f));
        label.text = "UI Scale";

        RectTransform sliderArea = MenuUi.CreateRect("Slider", band);
        MenuUi.SetRect(sliderArea, Anchors.Fill, new Vector2(58f, 0f), Vector2.zero);

        _slider = MenuPage.FillSlider(
            sliderArea,
            MinScale,
            MaxScale,
            "0.0",
            () => ModConfig.UiScale.Value,
            OnChanged,
            out _sync
        );
        _slider.gameObject.AddComponent<PointerUpNotifier>().Released += OnReleased;
    }

    /// <summary>Scales the panel to the saved config value.</summary>
    public void ApplySaved()
    {
        float scale = Mathf.Clamp(ModConfig.UiScale.Value, MinScale, MaxScale);
        _panel.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Drops an in-progress preview and resets the slider to the saved value.</summary>
    public void Cancel()
    {
        if (!_active)
        {
            return;
        }
        _sync();
        Exit();
    }

    private void OnChanged(float value)
    {
        // Dragging only updates the ghost; the confirm dialog waits for release.
        if (!_active)
        {
            Enter();
        }
        Show(value);
    }

    private void OnReleased()
    {
        if (_active && _dialog == null)
        {
            BuildDialog();
            Show(_slider.value);
        }
    }

    private void Enter()
    {
        _active = true;

        _ghost = UnityEngine.Object.Instantiate(_panel.gameObject, _panel.parent);
        _ghost.name = "ScaleGhost";
        CanvasGroup group = _ghost.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = _ghost.AddComponent<CanvasGroup>();
        }
        group.alpha = 0.45f;
        group.interactable = false;
        group.blocksRaycasts = false;
        _ghost.transform.SetAsLastSibling();
    }

    private void Show(float value)
    {
        if (_ghost != null)
        {
            _ghost.transform.localScale = new Vector3(value, value, 1f);
        }
        if (_dialogValue != null)
        {
            _dialogValue.text = $"UI Scale: {value:0.0}";
        }
    }

    private void BuildDialog()
    {
        // Not modal: the slider stays draggable while the dialog is up.
        _dialog = MenuDialog.CreateFrame(
            _dialogParent,
            "ScaleDialog",
            new Vector2(240f, 116f),
            "Apply UI scale?",
            false,
            out RectTransform frame
        );

        _dialogValue = MenuUi.CreateText("Value", frame, 12f, TextAlignmentOptions.Center);
        MenuUi.ApplyValueText(_dialogValue);
        MenuUi.SetRect(_dialogValue.rectTransform, Anchors.Top, new Vector2(10f, -60f), new Vector2(-10f, -38f));

        MenuDialog.AddButtons(frame, "Apply", Apply, Cancel);
    }

    private void Apply()
    {
        ModConfig.UiScale.Value = _slider.value;
        ApplySaved();
        Exit();
    }

    private void Exit()
    {
        _active = false;
        if (_ghost != null)
        {
            UnityEngine.Object.Destroy(_ghost);
            _ghost = null;
        }
        if (_dialog != null)
        {
            UnityEngine.Object.Destroy(_dialog);
            _dialog = null;
        }
        _dialogValue = null;
    }
}
