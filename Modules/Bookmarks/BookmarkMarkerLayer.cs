using System.Collections.Generic;
using AKeepersNeed2.Shared.Bookmarks;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Bookmarks;

/// <summary>
/// Container on the map's <c>mapRect</c> holding the bookmark markers, rebuilt each time the map
/// redraws (bookmarks don't move). The map only covers the outdoor world, so only bookmarks in the
/// world scene are drawn: the player's scene while they're out in the world, otherwise the last
/// world scene seen, so outdoor bookmarks still show when the map is opened indoors. Removes itself
/// once "Show Bookmarks on Map" is off.
/// </summary>
internal sealed class BookmarkMarkerLayer : MonoBehaviour
{
    private const string ObjectName = "AKN_BookmarkMarkers";
    private const float MarkerSize = 10f;
    private const float LabelGap = 6f;

    private static string _worldSceneId;

    private readonly List<BookmarkMarker> _markers = new List<BookmarkMarker>();

    private RectTransform _mapRect;
    private RectTransform _label;
    private TextMeshProUGUI _labelText;
    private BookmarkMarker _hovered;

    public static BookmarkMarkerLayer Ensure(RectTransform mapRect)
    {
        Transform existing = mapRect.Find(ObjectName);
        BookmarkMarkerLayer layer = existing != null ? existing.GetComponent<BookmarkMarkerLayer>() : null;
        if (layer == null)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(mapRect, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            layer = go.AddComponent<BookmarkMarkerLayer>();
            layer._mapRect = mapRect;
            layer.BuildLabel();
        }
        layer.transform.SetAsLastSibling();
        return layer;
    }

    public void Rebuild()
    {
        HideLabel(_hovered);
        foreach (BookmarkMarker marker in _markers)
        {
            Destroy(marker.gameObject);
        }
        _markers.Clear();

        PlayerData player = MainGame.PlayerData;
        if (player == null)
        {
            return;
        }
        if (MapProjection.IsPlayerInWorld())
        {
            _worldSceneId = player.currentGameSceneId;
        }
        Vector2 mapSize = _mapRect.sizeDelta;
        foreach (Bookmark bookmark in BookmarkStore.All)
        {
            if (_worldSceneId != null && bookmark.Scene != _worldSceneId)
            {
                continue;
            }
            if (!MapProjection.TryWorldToMap(bookmark.Position, mapSize, out Vector2 point))
            {
                continue;
            }
            BookmarkMarker marker = BookmarkMarker.Create(this, bookmark, MarkerSize);
            marker.Rect.anchoredPosition = point;
            _markers.Add(marker);
        }
        _label.SetAsLastSibling();
    }

    public void ShowLabel(BookmarkMarker marker)
    {
        _hovered = marker;
        string name = marker.Bookmark.Name;
        _labelText.text = name;
        Vector2 textSize = _labelText.GetPreferredValues(name);
        _label.sizeDelta = new Vector2(textSize.x + 16f, textSize.y + 6f);
        _label.anchoredPosition = marker.Rect.anchoredPosition + new Vector2(0f, MarkerSize * 0.75f + LabelGap);
        _label.gameObject.SetActive(true);
        _label.SetAsLastSibling();
    }

    public void HideLabel(BookmarkMarker marker)
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
        if (!BookmarksModule.ShowOnMap)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        HideLabel(_hovered);
    }

    private void Update()
    {
        if (!BookmarksModule.ShowOnMap)
        {
            Destroy(gameObject);
        }
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
}
