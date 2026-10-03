using UnityEngine;

namespace AKeepersNeed2.Shared.Ui;

/// <summary>
/// Small sprites drawn in code, for shapes the game has no sprite for. Each is built once. An
/// <c>Image</c> without a sprite draws a white square, so these stand in where a shape is needed.
/// </summary>
internal static class GeneratedSprites
{
    private const int Size = 32;

    private static Sprite _dot;
    private static Sprite _arrow;

    /// <summary>A round dot: light centre, dark rim, anti-aliased edge. Tint it with the image colour.</summary>
    public static Sprite Dot => _dot != null
        ? _dot
        : _dot = Build(DotPixel);

    /// <summary>A light arrowhead pointing up. Give the image an <c>Outline</c> for a dark edge.</summary>
    public static Sprite Arrow => _arrow != null
        ? _arrow
        : _arrow = Build(ArrowPixel);

    private static Color32 DotPixel(float x, float y)
    {
        float radius = Size * 0.5f;
        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
        float alpha = Mathf.Clamp01(radius - distance);
        byte shade = distance > radius - 3f ? (byte)40 : (byte)255;
        return new Color32(shade, shade, shade, (byte)(alpha * 255f));
    }

    // A chevron (tip at the top, notch at the bottom) as the union of two triangles, since it's
    // concave. Each pixel's alpha comes from how far inside either triangle it is.
    private static Color32 ArrowPixel(float x, float y)
    {
        var tip = new Vector2(Size * 0.5f, Size - 1f);
        var left = new Vector2(3f, 2f);
        var right = new Vector2(Size - 3f, 2f);
        var notch = new Vector2(Size * 0.5f, 10f);
        var point = new Vector2(x, y);
        float inside = Mathf.Max(Inside(point, left, tip, notch), Inside(point, notch, tip, right));
        return new Color32(255, 255, 255, (byte)(Mathf.Clamp01(inside + 1f) * 255f));
    }

    private static float Inside(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Min(EdgeDistance(point, a, b), Mathf.Min(EdgeDistance(point, b, c), EdgeDistance(point, c, a)));
    }

    // Signed distance from the line a→b, positive on its right-hand side (clockwise shapes).
    private static float EdgeDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 edge = (b - a).normalized;
        Vector2 toPoint = point - a;
        return edge.y * toPoint.x - edge.x * toPoint.y;
    }

    private static Sprite Build(System.Func<float, float, Color32> pixel)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[y * Size + x] = pixel(x + 0.5f, y + 0.5f);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
    }
}
