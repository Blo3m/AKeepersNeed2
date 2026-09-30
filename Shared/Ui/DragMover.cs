using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// Put on a handle (a title strip, a frame) to drag <see cref="Target"/> around inside its parent
/// with the left mouse button. The target stays fully inside the parent while it fits, and covers it
/// while it's bigger (see <see cref="ClampToParent"/>). Buttons under the handle still click: uGUI
/// cancels the click once a drag starts. <see cref="Released"/> fires when a drag ends, for saving
/// the new position.
/// </summary>
internal sealed class DragMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 _grabOffset;
    private bool _dragging;

    public RectTransform Target { get; set; }

    public event Action Released;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = false;
        if (eventData.button != PointerEventData.InputButton.Left
            || Target == null
            || !TryParentPoint(eventData, out Vector2 start))
        {
            return;
        }
        _grabOffset = start - Target.anchoredPosition;
        _dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || !TryParentPoint(eventData, out Vector2 point))
        {
            return;
        }
        Target.anchoredPosition = point - _grabOffset;
        ClampToParent(Target);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
        {
            return;
        }
        _dragging = false;
        Released?.Invoke();
    }

    /// <summary>
    /// Shifts <paramref name="rect"/> (scale included) so it lies inside its parent on each axis
    /// where it fits, and leaves no uncovered edge where it doesn't.
    /// </summary>
    public static void ClampToParent(RectTransform rect)
    {
        var parent = rect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);
        Rect bounds = parent.rect;
        rect.anchoredPosition += new Vector2(
            Shift(min.x, max.x, bounds.xMin, bounds.xMax),
            Shift(min.y, max.y, bounds.yMin, bounds.yMax)
        );
    }

    private static float Shift(float min, float max, float lower, float upper)
    {
        bool fits = max - min <= upper - lower;
        if (fits ? min < lower : min > lower)
        {
            return lower - min;
        }
        if (fits ? max > upper : max < upper)
        {
            return upper - max;
        }
        return 0f;
    }

    private bool TryParentPoint(PointerEventData eventData, out Vector2 point)
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
