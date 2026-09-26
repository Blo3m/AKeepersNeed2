using UnityEngine;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// A <see cref="RectTransform"/> anchor pair (<c>anchorMin</c>/<c>anchorMax</c>) with named
/// presets for the layouts the menu uses, so rect setup reads as intent instead of four vectors.
/// </summary>
internal readonly struct Anchors
{
    public static readonly Anchors Fill = new Anchors(Vector2.zero, Vector2.one);
    public static readonly Anchors Top = new Anchors(new Vector2(0f, 1f), Vector2.one);
    public static readonly Anchors Bottom = new Anchors(Vector2.zero, new Vector2(1f, 0f));
    public static readonly Anchors Left = new Anchors(Vector2.zero, new Vector2(0f, 1f));
    public static readonly Anchors Right = new Anchors(new Vector2(1f, 0f), Vector2.one);
    public static readonly Anchors BottomLeftHalf = new Anchors(Vector2.zero, new Vector2(0.5f, 0f));
    public static readonly Anchors BottomRightHalf = new Anchors(new Vector2(0.5f, 0f), new Vector2(1f, 0f));

    public readonly Vector2 Min;
    public readonly Vector2 Max;

    public Anchors(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }
}
