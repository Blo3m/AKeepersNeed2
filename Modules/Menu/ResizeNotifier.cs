using System;
using UnityEngine;

namespace AKeepersNeed2.Modules.Menu;

/// <summary>
/// Fires <see cref="Resized"/> whenever the GameObject's RectTransform changes size, including
/// its first layout after being shown. The item list uses it to size its row pool.
/// </summary>
internal sealed class ResizeNotifier : MonoBehaviour
{
    public Action Resized;

    private void OnRectTransformDimensionsChange()
    {
        Resized?.Invoke();
    }
}
