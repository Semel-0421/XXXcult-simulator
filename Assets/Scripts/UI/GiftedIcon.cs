using System;
using System.Collections.Generic;
using UnityEngine;

// 기프티드를 작은 사각 픽토그램으로 그린다. 분류가 색을, 분류 안의 순번이 도형(10)×장식(5)을 정해 서로 겹치지 않는다.
public static class GiftedIcon
{
    const int Size = 64;
    const int ShapeCount = 10, OrnamentCount = 5;

    static readonly Dictionary<string, Sprite> Cache = new();
    static Dictionary<string, int> indexInCategory;

    static readonly Dictionary<string, Color> CategoryColors = new()
    {
        ["운명"] = new Color(0.96f, 0.80f, 0.36f),
        ["성격"] = new Color(0.66f, 0.52f, 0.95f),
        ["특기"] = new Color(0.36f, 0.78f, 0.82f),
        ["외모"] = new Color(0.94f, 0.50f, 0.62f),
    };

    public static Sprite Of(Talent talent)
    {
        if (Cache.TryGetValue(talent.Id, out var cached) && cached != null) return cached;
        var sprite = Draw(talent);
        Cache[talent.Id] = sprite;
        return sprite;
    }

    static int IndexOf(Talent talent)
    {
        if (indexInCategory == null)
        {
            indexInCategory = new Dictionary<string, int>();
            var counters = new Dictionary<string, int>();
            foreach (var g in GiftedCatalog.All)
            {
                counters.TryGetValue(g.Category, out var n);
                indexInCategory[g.Id] = n;
                counters[g.Category] = n + 1;
            }
        }
        return indexInCategory.TryGetValue(talent.Id, out var index) ? index : 0;
    }

    static Sprite Draw(Talent talent)
    {
        var index = IndexOf(talent);
        var shape = index % ShapeCount;
        var ornament = index / ShapeCount % OrnamentCount;
        var accent = CategoryColors.TryGetValue(talent.Category, out var c) ? c : Color.white;
        var back = accent * 0.22f; back.a = 1;
        var border = accent * 0.75f; border.a = 1;

        var pixels = new Color32[Size * Size];
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            // -1..1 좌표 (y는 위쪽이 +)
            float x = (px + 0.5f) / Size * 2 - 1, y = (py + 0.5f) / Size * 2 - 1;
            Color color = back;
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > 0.88f) color = border;

            var d = ShapeDistance(shape, x / 0.8f, y / 0.8f) * 0.8f;
            var cover = Mathf.Clamp01(0.5f - d * Size / 2f);
            color = Color.Lerp(color, accent, cover);
            color = Color.Lerp(color, accent, OrnamentCover(ornament, x, y));
            pixels[py * Size + px] = color;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = $"Gifted_{talent.Id}" };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    // 도형의 부호 있는 거리 (음수 = 안쪽)
    static float ShapeDistance(int shape, float x, float y)
    {
        float ax = Mathf.Abs(x), ay = Mathf.Abs(y), r = Mathf.Sqrt(x * x + y * y);
        switch (shape)
        {
            case 0: return r - 0.55f;                                                     // 원
            case 1: return Mathf.Abs(r - 0.5f) - 0.1f;                                    // 고리
            case 2: return ax + ay - 0.65f;                                               // 마름모
            case 3: return Mathf.Max(ax * 0.866f + y * 0.5f, -y) - 0.4f;                  // 삼각형
            case 4: return Mathf.Min(Mathf.Max(ax - 0.18f, ay - 0.6f), Mathf.Max(ax - 0.6f, ay - 0.18f)); // 십자
            case 5: return r - (0.38f + 0.2f * Mathf.Cos(5 * Mathf.Atan2(x, y)));         // 별
            case 6: return Mathf.Max(r - 0.6f, -(Dist(x - 0.28f, y - 0.1f) - 0.5f));     // 초승달
            case 7: return Mathf.Max(ax * 0.866f + ay * 0.5f, ay) - 0.55f;                // 육각형
            case 8: return Mathf.Max(Dist(x, y + 0.5f) - 0.9f, Dist(x, y - 0.5f) - 0.9f); // 눈
            default: return Mathf.Abs(Mathf.Max(ax, ay) - 0.5f) - 0.1f;                   // 네모 테두리
        }
    }

    static float Dist(float x, float y) => Mathf.Sqrt(x * x + y * y);

    static float OrnamentCover(int ornament, float x, float y)
    {
        switch (ornament)
        {
            case 1: return Dist(Mathf.Abs(x) - 0.7f, Mathf.Abs(y) - 0.7f) < 0.09f ? 1 : 0;                // 네 모서리 점
            case 2: return Mathf.Abs(x) < 0.4f && y > -0.8f && y < -0.7f ? 1 : 0;                         // 아래 막대
            case 3: return Dist(Mathf.Abs(x) - 0.72f, y) < 0.09f ? 1 : 0;                                 // 양옆 점
            case 4: return Dist(x, y - 0.78f) < 0.09f ? 1 : 0;                                            // 위 점
            default: return 0;
        }
    }
}
