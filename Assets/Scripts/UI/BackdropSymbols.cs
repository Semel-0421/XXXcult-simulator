using System;
using UnityEngine;

// 배경 문양 네 가지를 코드로 그린다 (흰색 + 알파 마스크, 색은 Image.color로 입힌다).
public static class BackdropSymbols
{
    const int Size = 256;
    static readonly Sprite[] Cache = new Sprite[4];

    public static Sprite Get(Backdrop.Symbol symbol)
    {
        var index = (int)symbol;
        if (Cache[index] != null) return Cache[index];

        Func<float, float, float> shape = symbol switch
        {
            Backdrop.Symbol.Halo => Halo,
            Backdrop.Symbol.Crown => Crown,
            Backdrop.Symbol.Thorns => Thorns,
            _ => Pentagram,
        };
        Cache[index] = Render(shape, symbol.ToString());
        return Cache[index];
    }

    // 각 도형 함수는 -1~1 좌표(y 위쪽 +)에서 안쪽이면 1, 바깥이면 0을 돌려준다
    static Sprite Render(Func<float, float, float> shape, string name)
    {
        const int Samples = 2; // 2x2 슈퍼샘플링으로 가장자리를 부드럽게
        var pixels = new Color32[Size * Size];
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            float cover = 0;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
            {
                float x = (px + (sx + 0.5f) / Samples) / Size * 2 - 1;
                float y = (py + (sy + 0.5f) / Samples) / Size * 2 - 1;
                cover += shape(x, y);
            }
            cover /= Samples * Samples;
            pixels[py * Size + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(cover * 255));
        }
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = $"Backdrop_{name}", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    static readonly float[] CrownPeaks = { -0.6f, 0f, 0.6f };

    static float Inside(bool value) => value ? 1f : 0f;
    static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

    // 후광: 겹친 두 고리와 사방으로 뻗는 빛살
    static float Halo(float x, float y)
    {
        var r = Length(x, y);
        var angle = Mathf.Atan2(y, x);
        var ringOuter = Mathf.Abs(r - 0.86f) < 0.022f;
        var ringInner = Mathf.Abs(r - 0.46f) < 0.05f;
        var core = r < 0.12f;
        const int Rays = 16;
        var phase = Mathf.Repeat(angle / (2 * Mathf.PI) * Rays, 1f) - 0.5f; // -0.5~0.5
        var width = Mathf.Lerp(0.16f, 0.05f, Mathf.InverseLerp(0.56f, 0.78f, r));
        var ray = r > 0.56f && r < 0.78f && Mathf.Abs(phase) < width;
        return Inside(ringOuter || ringInner || core || ray);
    }

    // 왕관: 띠 위로 다섯 뾰족한 끝과 구슬
    static float Crown(float x, float y)
    {
        var band = y > -0.52f && y < -0.30f && Mathf.Abs(x) < 0.66f;
        var u = Mathf.Repeat((x + 0.6f) / 0.6f, 1f);                  // 봉우리 사이 한 주기
        var edge = -0.10f + 0.50f * Mathf.Abs(2 * u - 1);            // 봉우리 높이 곡선
        var body = y >= -0.30f && Mathf.Abs(x) <= 0.66f && y <= edge;
        var orbs = false;
        foreach (var px in CrownPeaks)
            orbs |= Length(x - px, y - 0.46f) < 0.075f;
        var gem = Length(x, y + 0.41f) < 0.06f;
        return Inside((band || body || orbs) && !gem);
    }

    // 가시관: 고리 둘레의 날카로운 가시
    static float Thorns(float x, float y)
    {
        var r = Length(x, y);
        var angle = Mathf.Atan2(y, x);
        var ring = Mathf.Abs(r - 0.52f) < 0.045f;
        const int Count = 14;
        var local = Mathf.Repeat(angle / (2 * Mathf.PI) * Count, 1f) - 0.5f; // 가시 중심에서 -0.5~0.5
        var along = Mathf.Abs(r - 0.52f) / 0.30f;                              // 고리에서 멀어질수록 가늘어짐
        var arc = Mathf.Abs(local) * (2 * Mathf.PI / Count) * r;               // 호 길이 거리
        var thorn = r > 0.5f && r < 0.82f && arc < 0.07f * (1 - along);
        var inner = r < 0.5f && r > 0.28f && arc < 0.05f * (1 - Mathf.Abs(r - 0.52f) / 0.26f) && ((int)Mathf.Floor(angle / (2 * Mathf.PI) * Count + 100)) % 2 == 0;
        return Inside(ring || thorn || inner);
    }

    // 역오망성: 아래로 뾰족한 별과 바깥 원
    static float Pentagram(float x, float y)
    {
        var r = Length(x, y);
        var circle = Mathf.Abs(r - 0.84f) < 0.025f;
        var vertices = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            var a = (270f + 72f * i) * Mathf.Deg2Rad; // 270도 = 아래쪽 꼭짓점
            vertices[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.78f;
        }
        var star = false;
        for (int i = 0; i < 5 && !star; i++)
            star = DistanceToSegment(new Vector2(x, y), vertices[i], vertices[(i + 2) % 5]) < 0.028f;
        return Inside(circle || star);
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }
}
