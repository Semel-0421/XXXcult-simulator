using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 코드로 그리는 장식 그림 모음: 금실 박음질한 책 테두리, 황금 월계관.
// 이미지 파일 없이 만들고, 같은 모양은 한 번만 만들어 재사용한다.
public static class UiSkin
{
    public static readonly Color Gold = new(0.83f, 0.69f, 0.22f);       // 더 깊은 금색
    public static readonly Color GoldLight = new(0.95f, 0.82f, 0.42f);
    public static readonly Color GoldDark = new(0.42f, 0.28f, 0.08f);
    public static readonly Color Crimson = new(0.55f, 0.08f, 0.14f);   // 더 어두운 진홍
    public static readonly Color Ink = new(0.04f, 0.02f, 0.08f);       // 먹빛 (배경용)
    public static readonly Color Parchment = new(0.88f, 0.82f, 0.68f); // 양피지색 (텍스트용)

    const int FrameSize = 96, FrameBorder = 14;
    static readonly Dictionary<string, Sprite> Cache = new();

    // 자줏빛 천 바탕 + 가장자리 가죽 테두리 + 안쪽 8px 금실 점선 (책 표지처럼)
    public static Sprite BookFrame(string key, Color top, Color bottom)
    {
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached; // 에디터에서 지워진 것은 다시 만든다
        const int S = FrameSize;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            var c = Color.Lerp(bottom, top, y / (float)(S - 1));
            int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
            if (edge < 3) c = Color.Lerp(GoldDark, Gold, edge / 3f);                 // 바깥 금테
            else if (edge < 16) c = Color.Lerp(c * 0.7f, c, (edge - 3) / 13f);       // 가장자리 안쪽 그늘
            if (x < 9) c *= 0.6f;                                                     // 책등
            if (edge == 12) c = Color.Lerp(c, Gold, 0.35f);                           // 얇은 쪽 선

            if (edge == 7 || edge == 8)                                               // 금실 박음질
            {
                bool horizontal = Mathf.Min(y, S - 1 - y) <= Mathf.Min(x, S - 1 - x);
                int along = horizontal ? x : y;
                if (along % 7 < 4) c = GoldLight;
            }
            c.a = 1f;
            px[y * S + x] = c;
        }
        var sprite = MakeSprite($"BookFrame_{key}", S, px, new Vector4(FrameBorder, FrameBorder, FrameBorder, FrameBorder));
        Cache[key] = sprite;
        return sprite;
    }

    // 황금 월계관: 아래에서 위로 가지 두 줄, 잎은 바깥·안쪽으로 번갈아. 아래는 붉은 리본, 위는 보석.
    public static Sprite Laurel()
    {
        const string key = "Laurel";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached; // 에디터에서 지워진 것은 다시 만든다
        const int S = 256;
        var px = new Color[S * S];
        var center = new Vector2(128, 120);
        const float R = 98f;

        // 가지 줄기
        for (int side = 1; side >= -1; side -= 2)
            for (int i = 0; i <= 200; i++)
            {
                float a = Mathf.Lerp(262f, 98f, i / 200f) * Mathf.Deg2Rad;
                var p = Branch(center, R, a, side);
                Dot(px, S, p, 2.2f, new Color(0.6f, 0.42f, 0.14f));
            }

        // 잎 (가지마다 12쌍)
        for (int side = 1; side >= -1; side -= 2)
            for (int i = 0; i < 12; i++)
            {
                float a = Mathf.Lerp(262f, 98f, (i + 0.5f) / 12f) * Mathf.Deg2Rad;
                var normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var tip = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
                var root = Branch(center, R, a, side);
                var outward = (tip * 0.7f + normal * 0.7f).normalized;
                var inward = (tip * 0.7f - normal * 0.7f).normalized;
                Leaf(px, S, root, Mirror(outward, side), 26f, 7f);
                Leaf(px, S, root, Mirror(inward, side), 22f, 6f);
            }

        // 아래 붉은 리본
        for (int y = 14; y <= 30; y++)
        for (int x = 116; x <= 140; x++)
            px[y * S + x] = (x == 116 || x == 140 || y == 14 || y == 30) ? Gold : Crimson;

        // 위 보석
        Dot(px, S, new Vector2(128, 236), 10f, Gold);
        Dot(px, S, new Vector2(128, 236), 6f, Crimson);
        Dot(px, S, new Vector2(128, 236), 2.5f, GoldLight);

        var sprite = MakeSprite(key, S, px, Vector4.zero);
        Cache[key] = sprite;
        return sprite;
    }

    static Vector2 Branch(Vector2 c, float r, float a, int side)
    {
        var p = c + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        return side > 0 ? p : new Vector2(2 * c.x - p.x, p.y);
    }

    static Vector2 Mirror(Vector2 dir, int side) => side > 0 ? dir : new Vector2(-dir.x, dir.y);

    static void Dot(Color[] px, int S, Vector2 center, float radius, Color color)
    {
        for (int y = Mathf.Max(0, Mathf.FloorToInt(center.y - radius)); y <= Mathf.Min(S - 1, Mathf.CeilToInt(center.y + radius)); y++)
        for (int x = Mathf.Max(0, Mathf.FloorToInt(center.x - radius)); x <= Mathf.Min(S - 1, Mathf.CeilToInt(center.x + radius)); x++)
        {
            var d = new Vector2(x + 0.5f, y + 0.5f) - center;
            if (d.sqrMagnitude <= radius * radius) { var c = color; c.a = 1f; px[y * S + x] = c; }
        }
    }

    // 뿌리에서 dir 방향으로 뻗는 잎 한 장 (양 끝이 뾰족하고 가운데 잎맥이 있다)
    static void Leaf(Color[] px, int S, Vector2 root, Vector2 dir, float length, float width)
    {
        var perp = new Vector2(-dir.y, dir.x);
        var tip = root + dir * length;
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(root.x, tip.x) - width - 1));
        int maxX = Mathf.Min(S - 1, Mathf.CeilToInt(Mathf.Max(root.x, tip.x) + width + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(root.y, tip.y) - width - 1));
        int maxY = Mathf.Min(S - 1, Mathf.CeilToInt(Mathf.Max(root.y, tip.y) + width + 1));
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            var rel = new Vector2(x + 0.5f, y + 0.5f) - root;
            float u = Vector2.Dot(rel, dir) / length;
            if (u < 0f || u > 1f) continue;
            float v = Vector2.Dot(rel, perp);
            float w = width * Mathf.Sin(Mathf.PI * u);
            if (w <= 0.01f || Mathf.Abs(v) > w) continue;
            Color c;
            if (Mathf.Abs(v) < 0.7f) c = GoldDark * 1.15f;
            else c = Gold * (0.72f + 0.33f * (1f - Mathf.Abs(v) / w));
            c.a = 1f;
            px[y * S + x] = c;
        }
    }

    static Sprite MakeSprite(string name, int size, Color[] pixels, Vector4 border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    // 기존 패널 이미지를 책 표지 모양으로 바꾼다 (외곽 금색 선은 그림 안에 있으므로 Outline은 끈다)
    public static void ApplyBookFrame(Image image, string key, Color top, Color bottom)
    {
        image.sprite = BookFrame(key, top, bottom);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        if (image.TryGetComponent<Outline>(out var outline)) outline.enabled = false;
    }
}
