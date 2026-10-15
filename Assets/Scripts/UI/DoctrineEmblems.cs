using System;
using System.Collections.Generic;
using UnityEngine;

// 최종 스킬 16개의 문양. 가지마다 상징(하트·동전·성배·왕관·초승달·십자·가시관·눈)이 있고,
// A 갈래는 바깥 고리, B 갈래는 바깥 빛살로 구분한다. 흰색 마스크로 그려 Image.color로 색을 입힌다.
public static class DoctrineEmblems
{
    const int Size = 256;
    static readonly Dictionary<string, Sprite> Cache = new();

    public static readonly Color VirtueColor = new(0.98f, 0.86f, 0.50f);
    public static readonly Color DesireColor = new(0.90f, 0.30f, 0.38f);

    public static Color ColorOf(DoctrineSkill skill) => skill.IsDesire ? DesireColor : VirtueColor;

    public static Sprite Get(DoctrineSkill skill)
    {
        if (Cache.TryGetValue(skill.Id, out var cached) && cached != null) return cached;
        Func<float, float, bool> motif = Motif(skill.Branch);
        Func<float, float, bool> frame = skill.Path == "B" ? Rays : Ring;
        var sprite = Render((x, y) => motif(x / 0.78f, y / 0.78f) || frame(x, y), skill.Id);
        Cache[skill.Id] = sprite;
        return sprite;
    }

    static Func<float, float, bool> Motif(DoctrineBranch branch) => branch switch
    {
        DoctrineBranch.Lust => Heart,
        DoctrineBranch.Greed => Coin,
        DoctrineBranch.Appetite => Chalice,
        DoctrineBranch.Ambition => Crown,
        DoctrineBranch.Temperance => Crescent,
        DoctrineBranch.Charity => Cross,
        DoctrineBranch.Patience => Thorns,
        _ => Eye,
    };

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
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = $"Emblem_{name}", wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    static float R(float x, float y) => Mathf.Sqrt(x * x + y * y);

    // 갈래 표시
    static bool Ring(float x, float y) => Mathf.Abs(R(x, y) - 0.9f) < 0.035f;

    static bool Rays(float x, float y)
    {
        var r = R(x, y);
        if (r < 0.8f || r > 0.97f) return false;
        var phase = Mathf.Repeat(Mathf.Atan2(y, x) / (2 * Mathf.PI) * 12, 1f) - 0.5f;
        return Mathf.Abs(phase) < 0.16f * (1 - (r - 0.8f) / 0.17f);
    }

    // 가지 상징
    static bool Heart(float x, float y)
    {
        x *= 1.25f; y = y * 1.25f + 0.15f;
        var a = x * x + y * y - 1;
        return a * a * a - x * x * y * y * y <= 0;
    }

    static bool Coin(float x, float y)
    {
        var r = R(x, y);
        var hole = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) < 0.18f;
        return (r < 0.6f && !hole) || Mathf.Abs(r - 0.7f) < 0.035f;
    }

    static bool Chalice(float x, float y)
    {
        var bowl = x * x + (y - 0.3f) * (y - 0.3f) < 0.5f * 0.5f && y < 0.3f;
        var rim = Mathf.Abs(y - 0.32f) < 0.035f && Mathf.Abs(x) < 0.55f;
        var stem = Mathf.Abs(x) < 0.07f && y > -0.6f && y < -0.15f;
        var foot = Mathf.Abs(x) < 0.36f && y > -0.72f && y < -0.6f;
        return bowl || rim || stem || foot;
    }

    static readonly float[] CrownPeaks = { -0.6f, 0f, 0.6f };

    static bool Crown(float x, float y)
    {
        var band = y > -0.55f && y < -0.32f && Mathf.Abs(x) < 0.66f;
        var u = Mathf.Repeat((x + 0.6f) / 0.6f, 1f);
        var body = y >= -0.32f && Mathf.Abs(x) <= 0.66f && y <= -0.12f + 0.5f * Mathf.Abs(2 * u - 1);
        foreach (var px in CrownPeaks)
            if (R(x - px, y - 0.46f) < 0.08f) return true;
        return band || body;
    }

    static bool Crescent(float x, float y) => R(x, y) < 0.65f && R(x - 0.3f, y - 0.12f) > 0.52f;

    static bool Cross(float x, float y)
    {
        var vertical = Mathf.Abs(x) < 0.1f && y > -0.72f && y < 0.62f;
        var horizontal = Mathf.Abs(y - 0.18f) < 0.1f && Mathf.Abs(x) < 0.5f;
        var ring = Mathf.Abs(R(x, y - 0.18f) - 0.36f) < 0.05f;
        return vertical || horizontal || ring;
    }

    static bool Thorns(float x, float y)
    {
        var r = R(x, y);
        if (Mathf.Abs(r - 0.48f) < 0.06f) return true;
        const int Count = 12;
        var local = Mathf.Repeat(Mathf.Atan2(y, x) / (2 * Mathf.PI) * Count, 1f) - 0.5f;
        var arc = Mathf.Abs(local) * (2 * Mathf.PI / Count) * r;
        return r > 0.48f && r < 0.74f && arc < 0.08f * (1 - (r - 0.48f) / 0.26f);
    }

    static bool Eye(float x, float y)
    {
        var lens = R(x, y + 0.5f) < 0.9f && R(x, y - 0.5f) < 0.9f;
        var r = R(x, y);
        var iris = Mathf.Abs(r - 0.24f) < 0.045f;
        var pupil = r < 0.11f;
        return (lens && !(r < 0.32f && !iris && !pupil));
    }
}
