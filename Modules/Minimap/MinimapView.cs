using System.Collections.Generic;
using AKeepersNeed2.Core;
using AKeepersNeed2.Core.MapPins;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Minimap;

/// <summary>
/// The minimap on its own overlay canvas (above the mod menu, so it can be moved while the menu is
/// open). A square frame, anchored by its top-right corner, holds a masked viewport; inside it the
/// map copy (<see cref="MinimapMap"/>) is scaled to the zoom and shifted so the player sits in the
/// middle, under the player arrow. Pins from <see cref="MapPinRegistry"/> ride on the map copy but
/// keep their pixel size. The + / − buttons under the frame always work; dragging the frame and the
/// corner grip only while the mod menu is open (<see cref="MenuState"/>).
/// </summary>
internal sealed class MinimapView : MonoBehaviour
{
    public const float MinSize = 100f;
    public const float MaxSize = 600f;
    public const int ZoomSteps = 7;

    private const int SortingOrder = 455;
    private const float Inset = 7f;
    private const float ButtonSize = 16f;
    private const float ButtonWidth = 11f;
    private const float GripSize = 16f;
    private const float ArrowSize = 9f;
    private const float PinRefreshSeconds = 0.25f;
    private const float TurnThreshold = 0.5f;

    // Share of the map's width shown across the minimap at each zoom step.
    private static readonly float[] ZoomFractions = { 1f / 2f, 1f / 3f, 1f / 4f, 1f / 6f, 1f / 8f, 1f / 12f, 1f / 16f };
    private static readonly Color EditTint = new Color(1f, 0.85f, 0.4f, 1f);

    private readonly List<Image> _pinPool = new List<Image>();

    private RectTransform _frame;
    private Image _frameHit;
    private Image _frameArt;
    private RectTransform _viewport;
    private RectTransform _content;
    private RectTransform _pins;
    private RectTransform _arrow;
    private GameObject _grip;
    private RectTransform _map;
    private Vector2 _mapSize;
    private int _knownZones = -1;
    private float _nextPins;
    private float _scale = 1f;
    private Vector2 _lastPoint;
    private bool _hasLastPoint;
    private bool _editing;
    private Vector3 _applied = new Vector3(float.NaN, 0f, 0f);

    /// <summary>
    /// A world position on the map copy, with the player's corrections for where the game's map picture
    /// really sits (height and sideways; see the module).
    /// </summary>
    private static bool Project(Vector3 world, Vector2 mapSize, out Vector2 point)
    {
        if (!MapProjection.TryWorldToMap(world, mapSize, out point, MinimapModule.HeightCorrection.Value))
        {
            return false;
        }
        point.x += MinimapModule.SideCorrection.Value;
        return true;
    }

    public static MinimapView Create()
    {
        NativeUiSkin.TryCapture();
        Transform uiRoot = MenuUi.FindUiRoot();
        if (uiRoot == null)
        {
            return null;
        }
        var root = new GameObject("AKN_Minimap", typeof(RectTransform));
        root.transform.SetParent(uiRoot, false);
        MenuUi.Stretch((RectTransform)root.transform);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        root.AddComponent<GraphicRaycaster>();

        var view = root.AddComponent<MinimapView>();
        view.Build((RectTransform)root.transform);
        return view;
    }

    private void Build(RectTransform root)
    {
        _frameHit = MenuUi.CreateImage("Frame", root, new Color(0f, 0f, 0f, 0.001f));
        _frame = _frameHit.rectTransform;
        _frame.anchorMin = _frame.anchorMax = _frame.pivot = Vector2.one;
        var mover = _frame.gameObject.AddComponent<DragMover>();
        mover.Target = _frame;
        mover.Released += SaveLayout;

        Image background = MenuUi.CreateImage("Background", _frame, new Color(0.05f, 0.04f, 0.04f, 0.9f));
        MenuUi.SetRect(background.rectTransform, Anchors.Fill, new Vector2(Inset, Inset), new Vector2(-Inset, -Inset));
        background.raycastTarget = false;

        _viewport = MenuUi.CreateRect("Viewport", _frame);
        MenuUi.SetRect(_viewport, Anchors.Fill, new Vector2(Inset, Inset), new Vector2(-Inset, -Inset));
        _viewport.gameObject.AddComponent<RectMask2D>();

        _content = MenuUi.CreateRect("Content", _viewport);
        _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0.5f, 0.5f);
        _pins = MenuUi.CreateRect("Pins", _content);
        _pins.anchorMin = _pins.anchorMax = _pins.pivot = new Vector2(0.5f, 0.5f);
        _pins.sizeDelta = Vector2.zero;

        Image arrow = MenuUi.CreateImage("Player", _viewport, Color.white);
        arrow.sprite = GeneratedSprites.Arrow;
        arrow.raycastTarget = false;
        var outline = arrow.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.06f, 0.04f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        _arrow = arrow.rectTransform;
        _arrow.anchorMin = _arrow.anchorMax = _arrow.pivot = new Vector2(0.5f, 0.5f);
        _arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);

        _frameArt = MenuUi.CreateImage("Border", _frame, new Color(0.35f, 0.25f, 0.18f, 1f));
        MenuUi.Stretch(_frameArt.rectTransform);
        _frameArt.raycastTarget = false;
        MenuUi.ApplyFrame(_frameArt);
        if (!NativeUiSkin.IsReady)
        {
            _frameArt.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            _frameArt.color = Color.clear;
        }

        ZoomButton("ZoomIn", "+", 0f, 1);
        ZoomButton("ZoomOut", "-", -ButtonWidth, -1);

        Image grip = MenuUi.CreateImage("Grip", _frame, EditTint);
        grip.sprite = GeneratedSprites.Dot;
        RectTransform gripRect = grip.rectTransform;
        gripRect.anchorMin = gripRect.anchorMax = gripRect.pivot = Vector2.zero;
        gripRect.sizeDelta = new Vector2(GripSize, GripSize);
        gripRect.anchoredPosition = new Vector2(-GripSize * 0.4f, -GripSize * 0.4f);
        var handle = grip.gameObject.AddComponent<ResizeHandle>();
        handle.Target = _frame;
        handle.Released += SaveLayout;
        _grip = grip.gameObject;

        _editing = !MenuState.IsOpen;
        UpdateEditing();
        ApplySavedLayout();
    }

    /// <summary>A "+" or "-" in the game's gold text colour; its invisible square is the click area.</summary>
    private void ZoomButton(string name, string text, float right, int step)
    {
        Image image = MenuUi.CreateImage(name, _frame, new Color(0f, 0f, 0f, 0.001f));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonSize);
        rect.anchoredPosition = new Vector2(right - Inset - 2f, Inset + 2f);
        var button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => ChangeZoom(step));
        TextMeshProUGUI label = MenuUi.CreateText(
            "Label",
            rect,
            16f,
            TextAlignmentOptions.Center,
            NativeUiSkin.ValueColor
        );
        MenuUi.Stretch(label.rectTransform);
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        label.text = text;
    }

    private void ChangeZoom(int step)
    {
        MinimapModule.Zoom.Value = Mathf.Clamp(MinimapModule.Zoom.Value + step, 0, ZoomSteps - 1);
        _nextPins = 0f;
    }

    private void SaveLayout()
    {
        MinimapModule.X.Value = _frame.anchoredPosition.x;
        MinimapModule.Y.Value = _frame.anchoredPosition.y;
        MinimapModule.Size.Value = _frame.sizeDelta.x;
        _applied = SavedLayout();
    }

    private static Vector3 SavedLayout()
    {
        return new Vector3(MinimapModule.X.Value, MinimapModule.Y.Value, MinimapModule.Size.Value);
    }

    private void ApplySavedLayout()
    {
        _applied = SavedLayout();
        float size = Mathf.Clamp(MinimapModule.Size.Value, MinSize, MaxSize);
        _frame.sizeDelta = new Vector2(size, size);
        _frame.anchoredPosition = new Vector2(MinimapModule.X.Value, MinimapModule.Y.Value);
        DragMover.ClampToParent(_frame);
    }

    private void OnEnable()
    {
        _nextPins = 0f;
        _hasLastPoint = false;
    }

    private void Update()
    {
        UpdateEditing();
        // The saved layout changes on a profile switch or reset; a drag only saves it on release.
        if (SavedLayout() != _applied)
        {
            ApplySavedLayout();
        }
        if (!EnsureMap())
        {
            return;
        }
        PlayerData player = MainGame.PlayerData;
        if (player == null
            || !Project(player.position.Value, _mapSize, out Vector2 point))
        {
            return;
        }
        float fraction = ZoomFractions[Mathf.Clamp(MinimapModule.Zoom.Value, 0, ZoomSteps - 1)];
        _scale = _viewport.rect.width / Mathf.Max(1f, _mapSize.x * fraction);
        _content.localScale = new Vector3(_scale, _scale, 1f);
        _content.anchoredPosition = -point * _scale;
        TurnArrow(point);
        if (Time.unscaledTime >= _nextPins)
        {
            _nextPins = Time.unscaledTime + PinRefreshSeconds;
            DrawPins();
        }
    }

    private void UpdateEditing()
    {
        bool editing = MenuState.IsOpen;
        if (editing == _editing)
        {
            return;
        }
        _editing = editing;
        _frameHit.raycastTarget = editing;
        _grip.SetActive(editing);
        _frameArt.color = editing
            ? EditTint
            : Color.white;
    }

    /// <summary>Copies the map the first time and again whenever a zone is discovered.</summary>
    private bool EnsureMap()
    {
        int known = MainGame.Instance?.GameSave?.knowledgeSystem?.knownMapZones?.Count ?? -1;
        if (_map != null && known == _knownZones)
        {
            return true;
        }
        if (_map != null)
        {
            Destroy(_map.gameObject);
        }
        _knownZones = known;
        _map = MinimapMap.Build(_content, out _mapSize);
        if (_map == null)
        {
            return false;
        }
        _content.sizeDelta = _mapSize;
        _map.SetAsFirstSibling();
        return true;
    }

    // Points where the player last moved: the game has no facing the map can use.
    private void TurnArrow(Vector2 point)
    {
        if (!_hasLastPoint)
        {
            _lastPoint = point;
            _hasLastPoint = true;
            return;
        }
        Vector2 moved = point - _lastPoint;
        if (moved.sqrMagnitude < TurnThreshold * TurnThreshold)
        {
            return;
        }
        _lastPoint = point;
        float angle = Mathf.Atan2(moved.y, moved.x) * Mathf.Rad2Deg - 90f;
        _arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void DrawPins()
    {
        List<MapPin> pins = MapPinRegistry.All();
        pins.Sort((a, b) => a.Layer.CompareTo(b.Layer));
        float inverse = 1f / Mathf.Max(0.0001f, _scale);
        int used = 0;
        foreach (MapPin pin in pins)
        {
            if (!Project(pin.World, _mapSize, out Vector2 point))
            {
                continue;
            }
            Image image = PinImage(used++);
            image.sprite = pin.Icon;
            image.color = pin.Color;
            image.material = pin.Material;
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(pin.Size, pin.Size);
            rect.localScale = new Vector3(inverse, inverse, 1f);
            rect.localRotation = pin.Diamond
                ? Quaternion.Euler(0f, 0f, 45f)
                : Quaternion.identity;
            rect.anchoredPosition = point;
            rect.SetSiblingIndex(used - 1);
            if (!image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(true);
            }
        }
        for (int i = used; i < _pinPool.Count; i++)
        {
            if (_pinPool[i].gameObject.activeSelf)
            {
                _pinPool[i].gameObject.SetActive(false);
            }
        }
    }

    private Image PinImage(int index)
    {
        while (_pinPool.Count <= index)
        {
            Image image = MenuUi.CreateImage("Pin", _pins, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            _pinPool.Add(image);
        }
        return _pinPool[index];
    }
}
