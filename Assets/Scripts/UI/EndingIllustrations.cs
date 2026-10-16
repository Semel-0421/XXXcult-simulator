using System;
using System.Collections.Generic;
using UnityEngine;

// 엔딩 삽화(픽토그램): 최종 스킬 엔딩은 그 스킬의 문양을, 해체·사망·극단 엔딩은 전용 상징을 그린다.
// 흰색 마스크로 그려 Image.color로 색을 입힌다 (굿 = 금색, 배드 = 진홍, 중립 = 연보라).
public static class EndingIllustrations
{
    const int Size = 256;
    static readonly Dictionary<string, Sprite> Cache = new();

    public static Color ColorOf(Endings.Ending ending) => ending.Kind switch
    {
        EndingKind.Good => DoctrineEmblems.VirtueColor,
        EndingKind.Bad => DoctrineEmblems.DesireColor,
        _ => new Color(0.78f, 0.7f, 0.9f),
    };

    public static Sprite Get(Endings.Ending ending)
    {
        if (ending.Final != null) return DoctrineEmblems.Get(ending.Final);
        if (Cache.TryGetValue(ending.Id, out var cached) && cached != null) return cached;
        var sprite = Render(Symbol(ending.Id), ending.Id);
        Cache[ending.Id] = sprite;
        return sprite;
    }

    static Func<float, float, bool> Symbol(string id)
    {
        if (id == Endings.Dissolved) return Broken;
        if (id == Endings.DeathYoung) return Hourglass;
        if (id == Endings.DeathLeader) return Grave;
        // 준비된 신: 승천은 빛살, 그릇의 붕괴는 모래시계, 잊힌 신은 틈 난 고리, 해산은 깨진 고리 (색·문양은 같아도 텍스처는 엔딩마다 따로)
        if (id == GodRules.Vessel.Id) return Hourglass;
        if (id == GodRules.Forgotten.Id) return BrokenRing;
        if (id == GodRules.Disbanded.Id) return Broken;
        if (id.StartsWith("god_")) return Sunburst;
        return id.EndsWith("_max") ? Sunburst : BrokenRing;
    }

    static Sprite Render(Func<float, float, bool> shape, string name)
    {
        const int Samples = 2;
        var pixels = new Color32[Size * Size];
        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            int hits = 0;
            for (int sy = 0; sy < Samples; sy++)
            for (int sx = 0; sx < Samples; sx++)
            {
                float x = (px + (sx + 0.5f) / Samples) / Size * 2 - 1;
                float y = (py + (sy + 0.5f) / Samples) / Size * 2 - 1;
                if (shape(x, y)) hits++;
            }
            pixels[py * Size + px] = new Color32(255, 255, 255, (byte)(hits * 255 / (Samples * Samples)));
        }
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = $"Ending_{name}", wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    static float R(float x, float y) => Mathf.Sqrt(x * x + y * y);

    // 해체: 깨진 고리 위에 X
    static bool Broken(float x, float y)
    {
        var ring = Mathf.Abs(R(x, y) - 0.86f) < 0.035f && !(x > 0.1f && y > -0.2f && y < 0.2f);
        var cross = R(x, y) < 0.7f && (Mathf.Abs(x - y) < 0.06f || Mathf.Abs(x + y) < 0.06f);
        return ring || cross;
    }

    // 요절: 모래시계
    static bool Hourglass(float x, float y)
    {
        var bars = Mathf.Abs(y) > 0.8f && Mathf.Abs(x) < 0.5f;
        var upper = y >= 0 && Mathf.Abs(x) <= (0.8f - y) * 0.6f;
        var lower = y < 0 && Mathf.Abs(x) <= (0.8f + y) * 0.6f;
        return bars || upper || lower;
    }

    // 교주의 죽음: 고리 안의 십자가
    static bool Grave(float x, float y)
    {
        var ring = Mathf.Abs(R(x, y) - 0.86f) < 0.035f;
        var vertical = Mathf.Abs(x) < 0.09f && y > -0.6f && y < 0.6f;
        var horizontal = Mathf.Abs(y - 0.18f) < 0.09f && Mathf.Abs(x) < 0.4f;
        return ring || vertical || horizontal;
    }

    // 극단(최대): 빛살
    static bool Sunburst(float x, float y)
    {
        var r = R(x, y);
        if (r < 0.36f) return true;
        if (r > 0.92f) return false;
        var phase = Mathf.Repeat(Mathf.Atan2(y, x) / (2 * Mathf.PI) * 12, 1f) - 0.5f;
        return Mathf.Abs(phase) < 0.14f;
    }

    // 극단(최소): 틈이 난 고리
    static bool BrokenRing(float x, float y)
    {
        if (Mathf.Abs(R(x, y) - 0.7f) >= 0.04f) return false;
        var segment = Mathf.Repeat(Mathf.Atan2(y, x) / (2 * Mathf.PI) * 4, 1f);
        return segment > 0.2f;
    }
}
