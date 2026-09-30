using System.Collections.Generic;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Modules.MapMarkers;

/// <summary>
/// Looks up the game's UI sprites by name without its "missing sprite" warnings: the collection's
/// private name → atlas table is checked first, since icons like <c>i_b_&lt;object id&gt;</c> only
/// exist for some objects.
/// </summary>
internal static class GameSprites
{
    private static readonly AccessTools.FieldRef<EasySpritesCollection, Dictionary<string, string>> NamesRef =
        AccessTools.FieldRefAccess<EasySpritesCollection, Dictionary<string, string>>("hash");

    private const int DotSize = 32;

    private static Sprite _dot;

    /// <summary>
    /// A soft-edged round dot for types without an icon. Built once: a plain <c>Image</c> without a
    /// sprite draws a white square.
    /// </summary>
    public static Sprite Dot
    {
        get
        {
            if (_dot != null)
            {
                return _dot;
            }
            var texture = new Texture2D(DotSize, DotSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            float radius = DotSize * 0.5f;
            var pixels = new Color32[DotSize * DotSize];
            for (int y = 0; y < DotSize; y++)
            {
                for (int x = 0; x < DotSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    // Dark rim, light centre, anti-aliased edge.
                    float alpha = Mathf.Clamp01(radius - distance);
                    byte shade = distance > radius - 3f ? (byte)40 : (byte)255;
                    pixels[y * DotSize + x] = new Color32(shade, shade, shade, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            _dot = Sprite.Create(texture, new Rect(0f, 0f, DotSize, DotSize), new Vector2(0.5f, 0.5f));
            return _dot;
        }
    }

    /// <summary>The first of <paramref name="names"/> the game has, or null.</summary>
    public static Sprite First(params string[] names)
    {
        EasySpritesCollection sprites = EasySpritesCollection.Instance;
        if (sprites == null)
        {
            return null;
        }
        sprites.Initialize();
        Dictionary<string, string> known = NamesRef(sprites);
        foreach (string name in names)
        {
            if (!string.IsNullOrEmpty(name) && known != null && known.ContainsKey(name))
            {
                return sprites.GetSprite(name);
            }
        }
        return null;
    }
}
