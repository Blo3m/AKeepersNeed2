using AKeepersNeed2.Shared.Bookmarks;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Bookmarks;

/// <summary>One bookmark on the map, a gold diamond: hover shows the name, shift-click teleports.</summary>
internal sealed class BookmarkMarker :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IMapClickTarget
{
    private static readonly Color Fill = new Color(0.95f, 0.75f, 0.25f, 1f);
    private static readonly Color Edge = new Color(0.15f, 0.08f, 0.04f, 1f);

    private BookmarkMarkerLayer _layer;

    public Bookmark Bookmark { get; private set; }

    public RectTransform Rect { get; private set; }

    public bool HandlesShiftClick => true;

    public static BookmarkMarker Create(BookmarkMarkerLayer layer, Bookmark bookmark, float size)
    {
        var go = new GameObject($"Bookmark_{bookmark.Id}", typeof(RectTransform));
        go.transform.SetParent(layer.transform, false);

        var image = go.AddComponent<Image>();
        image.color = Fill;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Edge;
        outline.effectDistance = new Vector2(1f, -1f);

        var marker = go.AddComponent<BookmarkMarker>();
        marker._layer = layer;
        marker.Bookmark = bookmark;
        marker.Rect = (RectTransform)go.transform;
        marker.Rect.anchorMin = marker.Rect.anchorMax = new Vector2(0.5f, 0.5f);
        marker.Rect.pivot = new Vector2(0.5f, 0.5f);
        marker.Rect.sizeDelta = new Vector2(size, size);
        marker.Rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && InputGate.ShiftHeld)
        {
            BookmarkTeleport.Go(Bookmark);
        }
    }
}
