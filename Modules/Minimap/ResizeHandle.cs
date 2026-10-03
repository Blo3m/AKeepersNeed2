using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Modules.Minimap;

/// <summary>
/// The minimap's bottom-left corner grip. The minimap is anchored by its top-right corner, so
/// dragging the grip left or down makes it bigger and right or up smaller; it stays square.
/// </summary>
internal sealed class ResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 _start;
    private float _startSize;
    private bool _dragging;

    public RectTransform Target { get; set; }

    public event Action Released;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = eventData.button == PointerEventData.InputButton.Left
            && Target != null
            && TryPoint(eventData, out _start);
        _startSize = Target != null ? Target.sizeDelta.x : 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || !TryPoint(eventData, out Vector2 point))
        {
            return;
        }
        float grow = ((_start.x - point.x) + (_start.y - point.y)) * 0.5f;
        float size = Mathf.Clamp(_startSize + grow, MinimapView.MinSize, MinimapView.MaxSize);
        Target.sizeDelta = new Vector2(size, size);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragging)
        {
            _dragging = false;
            Released?.Invoke();
        }
    }

    private bool TryPoint(PointerEventData eventData, out Vector2 point)
    {
        point = Vector2.zero;
        var parent = Target.parent as RectTransform;
        return parent != null
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out point
            );
    }
}
