using System.Collections.Generic;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Container on the map's <c>mapRect</c> (so markers pan and zoom with the map) holding one icon per
/// world object of a type ticked in the legend. Its <c>Update</c> only runs while the map is open:
/// the world is rescanned every couple of seconds (harvested nodes vanish, NPCs gain or lose
/// something to say) and NPC markers follow their NPC every frame. Markers are display-only: they
/// show a name on hover but let clicks through, so shift-click still teleports to the map point
/// when Shift-Click Map Teleport is on. Removes itself (and its legend) once Map Markers is off.
/// </summary>
internal sealed class MarkerLayer : MonoBehaviour
{
    private const string ObjectName = "AKN_MapMarkers";
    private const float RescanSeconds = 2f;
    private const float LabelGap = 4f;

    internal static readonly Color DotColor = new Color(0.9f, 0.9f, 0.85f, 1f);
    internal static readonly Color ReadyTint = new Color(1f, 0.8f, 0.2f, 1f);

    private readonly Dictionary<WgoData, Marker> _markers = new Dictionary<WgoData, Marker>();
    private readonly HashSet<WgoData> _seen = new HashSet<WgoData>();
    private readonly List<WgoData> _stale = new List<WgoData>();

    private RectTransform _mapRect;
    private MarkerLegend _legend;
    private RectTransform _label;
    private TextMeshProUGUI _labelText;
    private Marker _hovered;
    private float _nextScan;
    private float _size;

    public MarkerCatalog Catalog => MapMarkersModule.Catalog;

    public static MarkerLayer Ensure(MapPageWidget widget, RectTransform mapRect)
    {
        Transform existing = mapRect.Find(ObjectName);
        MarkerLayer layer = existing != null ? existing.GetComponent<MarkerLayer>() : null;
        if (layer == null)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(mapRect, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            layer = go.AddComponent<MarkerLayer>();
            layer._mapRect = mapRect;
            layer.BuildLabel();
            layer._legend = MarkerLegend.Create(widget.transform, layer);
        }
        layer.PlaceUnderOtherModLayers();
        return layer;
    }

    // Above the map itself, but under the other mod layers (NPC faces, bookmarks), which are
    // fewer and more specific.
    private void PlaceUnderOtherModLayers()
    {
        transform.SetAsLastSibling();
        for (int i = 0; i < _mapRect.childCount; i++)
        {
            Transform child = _mapRect.GetChild(i);
            if (child != transform && child.name.StartsWith("AKN_", System.StringComparison.Ordinal))
            {
                transform.SetSiblingIndex(i);
                return;
            }
        }
    }

    /// <summary>Rescans the world and redraws the markers and the legend's counts.</summary>
    public void Rescan()
    {
        _nextScan = Time.unscaledTime + RescanSeconds;
        MapMarkersModule.ScanIfStale(0f);
        ApplyFilters();
        _legend?.Refresh();
    }

    /// <summary>Adds and removes markers to match the ticked types; keeps the existing ones.</summary>
    public void ApplyFilters()
    {
        float size = MapMarkersModule.MarkerSize;
        bool resized = !Mathf.Approximately(size, _size);
        _size = size;
        Vector2 mapSize = _mapRect.sizeDelta;

        _seen.Clear();
        foreach (MarkerEntry entry in Catalog.Entries)
        {
            if (!MarkerFilters.IsShown(entry.Type.Key))
            {
                continue;
            }
            if (!MapProjection.TryWorldToMap(entry.Wgo.Position, mapSize, out Vector2 point))
            {
                continue;
            }
            _seen.Add(entry.Wgo);
            if (!_markers.TryGetValue(entry.Wgo, out Marker marker) || marker == null)
            {
                marker = Marker.Create(this, entry);
                _markers[entry.Wgo] = marker;
                resized = true;
            }
            marker.Entry = entry;
            marker.Image.color = entry.Highlight
                ? ReadyTint
                : entry.Type.Icon != null ? Color.white : DotColor;
            marker.Rect.anchoredPosition = point;
        }

        _stale.Clear();
        foreach (KeyValuePair<WgoData, Marker> pair in _markers)
        {
            if (!_seen.Contains(pair.Key))
            {
                _stale.Add(pair.Key);
            }
        }
        foreach (WgoData wgo in _stale)
        {
            Marker marker = _markers[wgo];
            if (_hovered == marker)
            {
                HideLabel(marker);
            }
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
            _markers.Remove(wgo);
        }
        if (resized)
        {
            foreach (Marker marker in _markers.Values)
            {
                marker.Rect.sizeDelta = new Vector2(_size, _size);
            }
        }
        _label.SetAsLastSibling();
    }

    public void ShowLabel(Marker marker)
    {
        _hovered = marker;
        string name = marker.Entry.Name;
        _labelText.text = name;
        Vector2 textSize = _labelText.GetPreferredValues(name);
        _label.sizeDelta = new Vector2(textSize.x + 16f, textSize.y + 6f);
        _label.gameObject.SetActive(true);
        _label.SetAsLastSibling();
        PlaceLabel();
    }

    public void HideLabel(Marker marker)
    {
        if (marker == null || _hovered != marker)
        {
            return;
        }
        _hovered = null;
        _label.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!MapMarkersModule.Enabled)
        {
            Remove();
        }
    }

    private void OnDisable()
    {
        HideLabel(_hovered);
    }

    private void Update()
    {
        if (!MapMarkersModule.Enabled)
        {
            Remove();
            return;
        }
        if (Time.unscaledTime >= _nextScan)
        {
            Rescan();
            return;
        }
        FollowMovers();
    }

    private void Remove()
    {
        if (_legend != null)
        {
            Destroy(_legend.gameObject);
        }
        Destroy(gameObject);
    }

    private void FollowMovers()
    {
        Vector2 mapSize = _mapRect.sizeDelta;
        foreach (Marker marker in _markers.Values)
        {
            if (marker == null || !marker.Entry.Moves)
            {
                continue;
            }
            if (MapProjection.TryWorldToMap(marker.Entry.Wgo.Position, mapSize, out Vector2 point))
            {
                marker.Rect.anchoredPosition = point;
            }
        }
        if (_hovered != null)
        {
            PlaceLabel();
        }
    }

    private void PlaceLabel()
    {
        _label.anchoredPosition = _hovered.Rect.anchoredPosition + new Vector2(0f, _size * 0.5f + LabelGap);
    }

    private void BuildLabel()
    {
        Image panel = MenuUi.CreateImage(
            "Label",
            transform,
            NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 0.95f)
        );
        panel.raycastTarget = false;
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            panel.sprite = NativeUiSkin.HeaderSprite;
            panel.type = Image.Type.Sliced;
        }
        _label = panel.rectTransform;
        _label.anchorMin = _label.anchorMax = new Vector2(0.5f, 0.5f);
        _label.pivot = new Vector2(0.5f, 0f);

        _labelText = MenuUi.CreateText("Text", _label, 13f, TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyHeaderText(_labelText);
        MenuUi.Stretch(_labelText.rectTransform);
        _labelText.textWrappingMode = TextWrappingModes.NoWrap;

        _label.gameObject.SetActive(false);
    }

    /// <summary>One marker: the type's icon (or a dot), name on hover.</summary>
    internal sealed class Marker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MarkerLayer _layer;

        public MarkerEntry Entry { get; set; }

        public Image Image { get; private set; }

        public RectTransform Rect { get; private set; }

        public static Marker Create(MarkerLayer layer, MarkerEntry entry)
        {
            var go = new GameObject("Marker", typeof(RectTransform));
            go.transform.SetParent(layer.transform, false);
            var image = go.AddComponent<Image>();
            image.sprite = entry.Type.Icon != null
                ? entry.Type.Icon
                : GeneratedSprites.Dot;
            image.preserveAspect = true;

            var marker = go.AddComponent<Marker>();
            marker._layer = layer;
            marker.Entry = entry;
            marker.Image = image;
            marker.Rect = (RectTransform)go.transform;
            marker.Rect.anchorMin = marker.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            marker.Rect.pivot = new Vector2(0.5f, 0.5f);
            return marker;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _layer.ShowLabel(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _layer.HideLabel(this);
        }
    }
}
