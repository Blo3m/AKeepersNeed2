using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Fires <see cref="Released"/> when the pointer is released on (or after dragging) the
/// GameObject — used to defer the UI-scale confirm dialog to slider release.
/// </summary>
internal sealed class PointerUpNotifier : MonoBehaviour, IPointerUpHandler, IEndDragHandler
{
    public Action Released;

    public void OnPointerUp(PointerEventData eventData)
    {
        Released?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Released?.Invoke();
    }
}
