using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 행동 결과 픽토그램(8프레임 가로 시트)을 코드로 그려 Assets/Resources/Pictograms에 저장한다.
// 메뉴: Tools > Pictograms > Generate All. 모양을 바꾸려면 아래 Draw* 함수를 고치고 다시 생성한다.
public static class PictogramGenerator
{
    public const int Size = 128, Frames = 8;
    const int SS = 2;               // 슈퍼샘플링 (2x2)
    const float Outline = 2.2f;
    const string Folder = "Assets/Resources/Pictograms";

    static readonly Color Fig = new Color32(242, 212, 120, 255), Prop = new Color32(235, 235, 242, 255), Dark = new Color32(20, 14, 28, 255);
    static readonly Color Red = new Color32(220, 60, 60, 255), Blue = new Color32(110, 160, 230, 255), Gray = new Color32(150, 150, 170, 255);
    static readonly Color Pink = new Color32(240, 110, 140, 255), Spark = new Color32(255, 250, 200, 255), Wood = new Color32(150, 100, 70, 255);

    // 행동 키 = 시트 이름 접두어 (ActionResultView와 맞춘다)
    static readonly Dictionary<string, Func<int, float, List<Shape>>> Drawers = new()
    {
        ["Exercise_ok"] = ExerciseOk, ["Exercise_fail"] = ExerciseFail,
        ["Meditate_ok"] = MeditateOk, ["Meditate_fail"] = MeditateFail,
        ["Study_ok"] = StudyOk, ["Study_fail"] = StudyFail,
        ["Socialize_ok"] = SocializeOk, ["Socialize_fail"] = SocializeFail,
        ["Pray_ok"] = PrayOk, ["Pray_fail"] = PrayFail,
        ["Proselytize_ok"] = ProselytizeOk, ["Proselytize_fail"] = ProselytizeFail,
        ["Preach_ok"] = PreachOk, ["Preach_fail"] = PreachFail,
        ["Serve_ok"] = ServeOk, ["Serve_fail"] = ServeFail,
        ["Desire_ok"] = DesireOk, ["Desire_fail"] = DesireFail,
        ["CultPray_ok"] = CultPrayOk, ["CultPray_fail"] = CultPrayFail,
        ["Consecrate_ok"] = ConsecrateOk, // 준비된 신의 축성 (짓지 못하면 턴을 쓰지 않아 실패 그림은 없다)
    };

    public static IEnumerable<string> Keys => Drawers.Keys;

    [MenuItem("Tools/Pictograms/Generate All")]
    public static void GenerateAll()
    {
        foreach (var key in Drawers.Keys) Generate(key, refresh: false);
        AssetDatabase.Refresh();
    }

    public static void Generate(string key, bool refresh = true)
    {
        Directory.CreateDirectory(Folder);
        var path = $"{Folder}/{key}.png";
        File.WriteAllBytes(path, Render(Drawers[key]).EncodeToPNG());
        if (!refresh) return;
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    // ── 렌더링 ─────────────────────────────────────────────

    enum Kind { Capsule, Circle, Box, Triangle, Ring }

    struct Shape
    {
        public Kind Kind; public Vector2 A, B, C; public float R; public Color Fill; public bool Outlined;
        public Vector2 Min, Max;
    }

    static Texture2D Render(Func<int, float, List<Shape>> draw)
    {
        var tex = new Texture2D(Size * Frames, Size, TextureFormat.RGBA32, false);
        var pixels = new Color[Size * Frames * Size];
        for (int f = 0; f < Frames; f++)
        {
            float t = (1 - Mathf.Cos(2 * Mathf.PI * f / Frames)) / 2; // 0→1→0 왕복
            var shapes = draw(f, t);
            for (int i = 0; i < shapes.Count; i++) shapes[i] = WithBounds(shapes[i]);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                            sum += Sample(shapes, new Vector2(x + (sx + 0.5f) / SS, y + (sy + 0.5f) / SS));
                    var c = sum / (SS * SS);
                    if (c.a > 0) { c.r /= c.a; c.g /= c.a; c.b /= c.a; } // 프리멀티플라이 해제
                    pixels[(Size - 1 - y) * Size * Frames + f * Size + x] = c;
                }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // 프리멀티플라이 알파로 아래→위 순서 합성
    static Color Sample(List<Shape> shapes, Vector2 p)
    {
        Color acc = Color.clear;
        foreach (var s in shapes)
        {
            if (p.x < s.Min.x || p.y < s.Min.y || p.x > s.Max.x || p.y > s.Max.y) continue;
            float d = Distance(s, p);
            Color c;
            if (d < 0) c = s.Fill;
            else if (s.Outlined && d < Outline) c = new Color(Dark.r, Dark.g, Dark.b, s.Fill.a);
            else continue;
            var pre = new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
            acc = pre + acc * (1 - c.a);
        }
        return acc;
    }

    static float Distance(Shape s, Vector2 p) => s.Kind switch
    {
        Kind.Capsule => Segment(p, s.A, s.B) - s.R,
        Kind.Circle => (p - s.A).magnitude - s.R,
        Kind.Box => RoundBox(p - s.A, s.B, s.R),
        Kind.Triangle => Tri(p, s.A, s.B, s.C),
        _ => Mathf.Abs((p - s.A).magnitude - s.R) - s.B.x,
    };

    static Shape WithBounds(Shape s)
    {
        float pad = Outline + 1;
        switch (s.Kind)
        {
            case Kind.Capsule: s.Min = Vector2.Min(s.A, s.B) - Vector2.one * (s.R + pad); s.Max = Vector2.Max(s.A, s.B) + Vector2.one * (s.R + pad); break;
            case Kind.Circle: s.Min = s.A - Vector2.one * (s.R + pad); s.Max = s.A + Vector2.one * (s.R + pad); break;
            case Kind.Box: s.Min = s.A - s.B - Vector2.one * pad; s.Max = s.A + s.B + Vector2.one * pad; break;
            case Kind.Triangle: s.Min = Vector2.Min(s.A, Vector2.Min(s.B, s.C)) - Vector2.one * pad; s.Max = Vector2.Max(s.A, Vector2.Max(s.B, s.C)) + Vector2.one * pad; break;
            default: s.Min = s.A - Vector2.one * (s.R + s.B.x + pad); s.Max = s.A + Vector2.one * (s.R + s.B.x + pad); break;
        }
        return s;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a; float h = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - a - ab * h).magnitude;
    }

    static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * r;
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
    }

    static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d = Mathf.Min(Segment(p, a, b), Mathf.Min(Segment(p, b, c), Segment(p, c, a)));
        float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
        bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
        return inside ? -d : d;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    // ── 도형 헬퍼 ─────────────────────────────────────────

    static Shape Cap(Vector2 a, Vector2 b, float r, Color c, bool o = true) => new() { Kind = Kind.Capsule, A = a, B = b, R = r, Fill = c, Outlined = o };
    static Shape Dot(Vector2 a, float r, Color c, bool o = true) => new() { Kind = Kind.Circle, A = a, R = r, Fill = c, Outlined = o };
    static Shape Box(Vector2 center, Vector2 half, Color c, float round = 2, bool o = true) => new() { Kind = Kind.Box, A = center, B = half, R = round, Fill = c, Outlined = o };
    static Shape Tri(Vector2 a, Vector2 b, Vector2 c, Color col, bool o = true) => new() { Kind = Kind.Triangle, A = a, B = b, C = c, Fill = col, Outlined = o };
    static Shape Ring(Vector2 c, float r, float w, Color col) => new() { Kind = Kind.Ring, A = c, R = r, B = new Vector2(w, 0), Fill = col, Outlined = false };
    static Vector2 V(float x, float y) => new(x, y);
    static Color Alpha(Color c, float a) => new(c.r, c.g, c.b, Mathf.Clamp01(a));

    // 막대 인형 관절
    struct Body { public Vector2 Head, Neck, Pelvis, ElbowL, HandL, ElbowR, HandR, KneeL, FootL, KneeR, FootR; }

    static Body Stand(float x = 64, float y = 0) => new()
    {
        Head = V(x, 30 + y), Neck = V(x, 42 + y), Pelvis = V(x, 72 + y),
        ElbowL = V(x - 9, 56 + y), HandL = V(x - 11, 70 + y), ElbowR = V(x + 9, 56 + y), HandR = V(x + 11, 70 + y),
        KneeL = V(x - 6, 90 + y), FootL = V(x - 9, 108 + y), KneeR = V(x + 6, 90 + y), FootR = V(x + 9, 108 + y),
    };

    // 옆모습으로 무릎 꿇은 자세 (왼쪽을 향함)
    static Body Kneel(float x = 64, float y = 0) => new()
    {
        Head = V(x - 4, 40 + y), Neck = V(x - 2, 52 + y), Pelvis = V(x + 2, 80 + y),
        ElbowL = V(x - 16, 64 + y), HandL = V(x - 18, 48 + y), ElbowR = V(x - 12, 66 + y), HandR = V(x - 16, 50 + y),
        KneeL = V(x - 2, 100 + y), FootL = V(x + 20, 104 + y), KneeR = V(x + 4, 100 + y), FootR = V(x + 26, 104 + y),
    };

    static Body Sit(float x = 64, float y = 0) => new()
    {
        Head = V(x, 46 + y), Neck = V(x, 58 + y), Pelvis = V(x, 88 + y),
        ElbowL = V(x - 13, 74 + y), HandL = V(x - 18, 92 + y), ElbowR = V(x + 13, 74 + y), HandR = V(x + 18, 92 + y),
        KneeL = V(x - 20, 100 + y), FootL = V(x + 4, 104 + y), KneeR = V(x + 20, 100 + y), FootR = V(x - 4, 104 + y),
    };

    static Body Scale(Body b, Vector2 origin, float s)
    {
        Vector2 S(Vector2 p) => origin + (p - origin) * s;
        return new Body { Head = S(b.Head), Neck = S(b.Neck), Pelvis = S(b.Pelvis), ElbowL = S(b.ElbowL), HandL = S(b.HandL), ElbowR = S(b.ElbowR), HandR = S(b.HandR), KneeL = S(b.KneeL), FootL = S(b.FootL), KneeR = S(b.KneeR), FootR = S(b.FootR) };
    }

    static void Figure(List<Shape> l, Body b, float s = 1, Color? color = null)
    {
        var c = color ?? Fig; float w = 3.2f * s;
        l.Add(Cap(b.Neck, b.ElbowL, w, c)); l.Add(Cap(b.ElbowL, b.HandL, w, c));
        l.Add(Cap(b.Neck, b.ElbowR, w, c)); l.Add(Cap(b.ElbowR, b.HandR, w, c));
        l.Add(Cap(b.Pelvis, b.KneeL, w, c)); l.Add(Cap(b.KneeL, b.FootL, w, c));
        l.Add(Cap(b.Pelvis, b.KneeR, w, c)); l.Add(Cap(b.KneeR, b.FootR, w, c));
        l.Add(Cap(b.Neck, b.Pelvis, w * 1.15f, c));
        l.Add(Dot(b.Head, 9 * s, c));
    }

    static void Sparkle(List<Shape> l, Vector2 c, float size)
    {
        if (size < 0.5f) return;
        l.Add(Cap(c - V(size, 0), c + V(size, 0), 1.3f, Spark, false));
        l.Add(Cap(c - V(0, size), c + V(0, size), 1.3f, Spark, false));
    }

    static void Cross(List<Shape> l, Vector2 c, float s, Color col)
    {
        l.Add(Cap(c - V(s, s), c + V(s, s), 2.5f, col)); l.Add(Cap(c - V(s, -s), c + V(s, -s), 2.5f, col));
    }

    static void Heart(List<Shape> l, Vector2 c, float s, Color col, bool broken = false)
    {
        l.Add(Dot(c + V(-5, -2) * s, 5.5f * s, col)); l.Add(Dot(c + V(5, -2) * s, 5.5f * s, col));
        l.Add(Tri(c + V(-10.5f, 0) * s, c + V(10.5f, 0) * s, c + V(0, 11) * s, col));
        if (broken) { l.Add(Cap(c + V(0, -7) * s, c + V(-3, -1) * s, 2f, Dark, false)); l.Add(Cap(c + V(-3, -1) * s, c + V(3, 4) * s, 2f, Dark, false)); l.Add(Cap(c + V(3, 4) * s, c + V(0, 11) * s, 2f, Dark, false)); }
    }

    static void Cloud(List<Shape> l, Vector2 c, Color col)
    {
        l.Add(Dot(c + V(-10, 2), 8, col)); l.Add(Dot(c + V(10, 2), 8, col)); l.Add(Dot(c + V(0, -4), 10, col)); l.Add(Box(c + V(0, 6), V(16, 4), col, 3));
    }

    static void Question(List<Shape> l, Vector2 c, float s, Color col)
    {
        l.Add(Cap(c + V(-4, -6) * s, c + V(0, -9) * s, 1.8f * s, col)); l.Add(Cap(c + V(0, -9) * s, c + V(4, -6) * s, 1.8f * s, col));
        l.Add(Cap(c + V(4, -6) * s, c + V(0, 0) * s, 1.8f * s, col)); l.Add(Cap(c, c + V(0, 3) * s, 1.8f * s, col)); l.Add(Dot(c + V(0, 8) * s, 2f * s, col));
    }

    static void Zee(List<Shape> l, Vector2 c, float s)
    {
        l.Add(Cap(c + V(-4, -4) * s, c + V(4, -4) * s, 1.4f, Prop)); l.Add(Cap(c + V(4, -4) * s, c + V(-4, 4) * s, 1.4f, Prop)); l.Add(Cap(c + V(-4, 4) * s, c + V(4, 4) * s, 1.4f, Prop));
    }

    static void Drop(List<Shape> l, Vector2 c, Color col)
    {
        l.Add(Dot(c, 3.2f, col)); l.Add(Tri(c + V(-3, -1), c + V(3, -1), c + V(0, -8), col));
    }

    static Vector2 Lerp(Vector2 a, Vector2 b, float t) => Vector2.Lerp(a, b, t);

    // ── 생활 행동 ─────────────────────────────────────────

    static List<Shape> ExerciseOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand();
        b.HandL = Lerp(V(46, 44), V(50, 10), t); b.ElbowL = Lerp(V(46, 58), V(48, 26), t);
        b.HandR = Lerp(V(82, 44), V(78, 10), t); b.ElbowR = Lerp(V(82, 58), V(80, 26), t);
        Figure(l, b);
        l.Add(Cap(b.HandL - V(8, 0), b.HandR + V(8, 0), 2.4f, Prop));
        l.Add(Box(b.HandL - V(10, 0), V(3, 8), Prop)); l.Add(Box(b.HandR + V(10, 0), V(3, 8), Prop));
        Sparkle(l, V(20, 20), 6 * t); Sparkle(l, V(108, 24), 6 * t);
        return l;
    }

    static List<Shape> ExerciseFail(int f, float t)
    {
        var l = new List<Shape>(); float shake = (f % 2 == 0 ? 1 : -1) * 1.2f;
        var b = Stand(60 + shake);
        b.Head = V(46 + shake, 50); b.Neck = V(52 + shake, 58); b.Pelvis = V(66 + shake, 76);
        b.ElbowR = V(76 + shake, 70); b.HandR = V(70 + shake, 76); b.ElbowL = V(50 + shake, 72); b.HandL = V(56 + shake, 86);
        b.KneeL = V(56 + shake, 92); b.KneeR = V(70 + shake, 92);
        Figure(l, b);
        l.Add(Cap(V(84, 104), V(112, 104), 2.4f, Prop)); l.Add(Box(V(82, 104), V(3, 8), Prop)); l.Add(Box(V(114, 104), V(3, 8), Prop));
        Cross(l, V(74, 64), 4 + 2 * t, Red);
        Drop(l, V(34, 40 + 6 * t), Blue);
        return l;
    }

    static List<Shape> MeditateOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Sit(64, -6 * t);
        l.Add(Ring(b.Head, 14 + 5 * t, 1.5f, Alpha(Spark, 0.5f + 0.4f * t)));
        b.HandL = V(48, 92 - 6 * t); b.HandR = V(80, 92 - 6 * t);
        Figure(l, b);
        Sparkle(l, V(26, 36 + 4 * t), 5 * t); Sparkle(l, V(102, 30 - 4 * t), 5 * (1 - t));
        return l;
    }

    static List<Shape> MeditateFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Sit();
        b.Head = Lerp(V(64, 48), V(74, 56), t);
        Figure(l, b);
        for (int i = 0; i < 3; i++)
        {
            float k = ((f + i * 3) % Frames) / (float)Frames;
            Zee(l, V(86 + 10 * k + i * 4, 40 - 30 * k), 0.8f + k);
        }
        return l;
    }

    static List<Shape> StudyOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Sit();
        b.Head = V(64, 46 + 2 * t); b.HandL = V(54, 76); b.ElbowL = V(48, 70); b.HandR = V(74, 76); b.ElbowR = V(80, 70);
        Figure(l, b);
        l.Add(Tri(V(64, 82), V(46, 70), V(48, 86), Prop)); l.Add(Tri(V(64, 82), V(82, 70), V(80, 86), Prop));
        bool on = f % 4 < 2;
        l.Add(Dot(V(64, 16), 7, on ? Spark : Gray)); l.Add(Box(V(64, 26), V(3, 3), Prop, 1));
        if (on) { Sparkle(l, V(50, 12), 4); Sparkle(l, V(78, 12), 4); }
        return l;
    }

    static List<Shape> StudyFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Sit();
        b.Head = V(64, 68); b.Neck = V(64, 74); b.HandL = V(52, 80); b.ElbowL = V(46, 78); b.HandR = V(76, 80); b.ElbowR = V(82, 78);
        l.Add(Box(V(64, 86), V(18, 4), Prop, 1));
        Figure(l, b);
        Cloud(l, V(64, 28 - 2 * t), Gray);
        Question(l, V(64, 26 - 2 * t), 1, Dark);
        return l;
    }

    static List<Shape> SocializeOk(int f, float t)
    {
        var l = new List<Shape>();
        var a = Scale(Stand(40), V(40, 108), 0.9f); a.HandR = Lerp(V(56, 46), V(52, 30), t); a.ElbowR = V(50, 50);
        var c = Scale(Stand(88), V(88, 108), 0.9f); c.HandL = Lerp(V(72, 30), V(76, 46), t); c.ElbowL = V(78, 50);
        Figure(l, a, 0.9f); Figure(l, c, 0.9f);
        Heart(l, V(64, 22 - 3 * t), 0.8f + 0.2f * t, Pink);
        return l;
    }

    static List<Shape> SocializeFail(int f, float t)
    {
        var l = new List<Shape>();
        var a = Scale(Stand(38), V(38, 108), 0.9f); a.HandR = V(58, 58); a.ElbowR = V(48, 56);
        float walk = 6 * f / (float)Frames;
        var c = Scale(Stand(86 + walk), V(86 + walk, 108), 0.9f);
        c.FootL = V(80 + walk - 4 * t, 106); c.FootR = V(92 + walk + 4 * t, 106);
        Figure(l, a, 0.9f); Figure(l, c, 0.9f, Alpha(Fig, 1 - 0.4f * f / Frames));
        Heart(l, V(62, 28), 0.9f, Pink, broken: true);
        return l;
    }

    static List<Shape> PrayOk(int f, float t)
    {
        var l = new List<Shape>();
        for (int i = -2; i <= 2; i++) l.Add(Cap(V(64 + i * 6, -4), V(64 + i * 14, 34), 2f, Alpha(Spark, 0.25f + 0.35f * t), false));
        Figure(l, Kneel());
        Sparkle(l, V(30, 30), 5 * t); Sparkle(l, V(98, 26), 5 * (1 - t));
        return l;
    }

    static List<Shape> PrayFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Kneel(); b.Head = V(62, 44);
        Figure(l, b);
        Cloud(l, V(64, 14), Gray);
        for (int i = 0; i < 4; i++) { float y = 26 + ((f * 4 + i * 7) % 18); l.Add(Cap(V(48 + i * 10, y), V(46 + i * 10, y + 5), 1.4f, Blue, false)); }
        return l;
    }

    // ── 교단 행동 ─────────────────────────────────────────

    static List<Shape> ProselytizeOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(36); b.HandR = Lerp(V(50, 40), V(52, 34), t); b.ElbowR = V(46, 48);
        Figure(l, b);
        l.Add(Tri(b.HandR, b.HandR + V(14, -8), b.HandR + V(14, 8), Prop));
        int count = 1 + f * 3 / Frames;
        for (int i = 0; i < count; i++)
        {
            var x = 76 + i * 17; var small = Scale(Stand(x), V(x, 108), 0.55f); Figure(l, small, 0.55f);
        }
        l.Add(Cap(V(96, 30), V(108, 30), 2f, Spark)); l.Add(Cap(V(102, 24), V(102, 36), 2f, Spark));
        return l;
    }

    static List<Shape> ProselytizeFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(36); b.HandR = V(52, 36); b.ElbowR = V(46, 48);
        Figure(l, b);
        l.Add(Tri(b.HandR, b.HandR + V(14, -8), b.HandR + V(14, 8), Prop));
        float walk = 14 * f / (float)Frames;
        for (int i = 0; i < 3; i++)
        {
            var x = 80 + i * 14 + walk; var small = Scale(Stand(x), V(x, 108), 0.55f);
            Figure(l, small, 0.55f, Alpha(Fig, 1 - 0.7f * f / Frames));
        }
        Cross(l, V(98, 32), 6, Red);
        return l;
    }

    static List<Shape> PreachOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand();
        b.HandL = Lerp(V(46, 50), V(40, 22), t); b.ElbowL = Lerp(V(50, 58), V(46, 36), t);
        b.HandR = Lerp(V(82, 50), V(88, 22), t); b.ElbowR = Lerp(V(78, 58), V(82, 36), t);
        Figure(l, b);
        l.Add(Box(V(64, 96), V(20, 16), Wood));
        for (int i = 0; i < 2; i++) { float k = ((f + i * 4) % Frames) / (float)Frames; l.Add(Ring(b.Head, 14 + 26 * k, 1.5f, Alpha(Spark, 1 - k))); }
        return l;
    }

    static List<Shape> PreachFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand();
        b.HandL = V(44, 46); b.ElbowL = V(48, 56); b.HandR = V(84, 46); b.ElbowR = V(80, 56); b.Head = V(64 + 2 * t, 32);
        Figure(l, b);
        l.Add(Box(V(64, 96), V(20, 16), Wood));
        float k = f / (float)(Frames - 1);
        l.Add(Dot(V(120 - 46 * k, 20 + 40 * k * k - 20 * k), 5, Red));
        Question(l, V(30, 26), 1.1f, Prop);
        return l;
    }

    // 축성: 두 팔을 든 신 앞에서 기둥이 땅에서 솟고, 반쯤 오르면 금빛 지붕이 얹히며 빛이 번진다
    static List<Shape> ConsecrateOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(34);
        b.HandL = Lerp(V(18, 46), V(14, 22), t); b.ElbowL = Lerp(V(24, 56), V(20, 36), t);
        b.HandR = Lerp(V(50, 46), V(56, 24), t); b.ElbowR = Lerp(V(44, 56), V(48, 36), t);
        Figure(l, b);
        float k = f / (float)(Frames - 1), h = 10 + 46 * k, top = 104 - h; // 0→1: 건물이 솟는다
        l.Add(Box(V(88, 106), V(26, 4), Gray));
        foreach (var x in new[] { 74f, 88f, 102f }) l.Add(Box(V(x, 104 - h / 2), V(4, h / 2), Prop));
        if (k > 0.5f) l.Add(Tri(V(66, top), V(110, top), V(88, top - 32 * (k - 0.5f)), Fig));
        for (int i = 0; i < 2; i++) { float r = ((f + i * 4) % Frames) / (float)Frames; l.Add(Ring(V(88, top), 10 + 30 * r, 1.5f, Alpha(Spark, 1 - r))); }
        return l;
    }

    static List<Shape> ServeOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(46); b.HandR = Lerp(V(62, 66), V(70, 64), t); b.ElbowR = V(56, 60);
        Figure(l, b);
        var k = Scale(Kneel(92), V(92, 108), 0.7f); k.HandL = V(80, 74); k.ElbowL = V(86, 80);
        Figure(l, k, 0.7f);
        l.Add(Box(b.HandR + V(4, 2), V(7, 4), Prop, 3));
        Heart(l, V(80, 40 - 12 * t), 0.6f, Pink);
        return l;
    }

    static List<Shape> ServeFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(50);
        b.HandL = Lerp(V(36, 40), V(40, 30), t); b.HandR = Lerp(V(66, 30), V(62, 40), t); b.ElbowL = V(42, 50); b.ElbowR = V(60, 50);
        Figure(l, b);
        float k = f / (float)(Frames - 1);
        l.Add(Box(V(80 + 10 * k, 62 + 40 * k), V(7, 4), Prop, 3));
        for (int i = 0; i < 3; i++) Drop(l, V(84 + i * 8 + 6 * k, 70 + 34 * k + i * 3), Blue);
        return l;
    }

    static List<Shape> DesireOk(int f, float t)
    {
        var l = new List<Shape>();
        l.Add(Box(V(64, 70), V(26, 34), Wood, 4));
        var b = Sit(64, -8); Figure(l, b);
        l.Add(Tri(V(56, 32), V(72, 32), V(64, 22), Spark)); l.Add(Tri(V(54, 32), V(60, 32), V(54, 24), Spark)); l.Add(Tri(V(68, 32), V(74, 32), V(74, 24), Spark));
        int pile = 1 + f / 2;
        for (int i = 0; i < pile; i++) l.Add(Dot(V(54 + (i % 4) * 7, 98 - (i / 4) * 5), 4, Spark));
        float y = 8 + 80 * ((f % 4) / 4f);
        l.Add(Dot(V(64, y), 4, Spark));
        return l;
    }

    static List<Shape> DesireFail(int f, float t)
    {
        var l = new List<Shape>();
        l.Add(Box(V(64, 70), V(26, 34), Wood, 4));
        var b = Sit(64, -8); b.HandL = V(56, 34); b.ElbowL = V(48, 46); b.HandR = V(72, 34); b.ElbowR = V(80, 46);
        Figure(l, b);
        float k = f / (float)(Frames - 1);
        for (int i = 0; i < 6; i++) l.Add(Dot(V(48 + i * 7 + (i - 2.5f) * 10 * k, 98 + 14 * k * (i % 2)), 4, Alpha(Spark, 1 - 0.5f * k)));
        l.Add(Cap(V(40, 14), V(46, 22), 1.6f, Red)); l.Add(Cap(V(46, 22), V(42, 28), 1.6f, Red)); l.Add(Cap(V(88, 14), V(82, 22), 1.6f, Red)); l.Add(Cap(V(82, 22), V(86, 28), 1.6f, Red));
        return l;
    }

    static List<Shape> CultPrayOk(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(64, 4);
        b.HandL = V(44, 22); b.ElbowL = V(50, 38); b.HandR = V(84, 22); b.ElbowR = V(78, 38);
        l.Add(Box(V(64, 110), V(40, 6), Wood));
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? 30 : 98; l.Add(Box(V(x, 96), V(3, 10), Prop, 1));
            l.Add(Dot(V(x, 82 - (f % 2)), 3 + t, Spark, false));
        }
        Figure(l, b);
        l.Add(Box(V(64, 10), V(14, 5), Alpha(Spark, 0.6f + 0.4f * t), 5, false)); l.Add(Dot(V(64, 10), 3, Dark, false));
        return l;
    }

    static List<Shape> CultPrayFail(int f, float t)
    {
        var l = new List<Shape>(); var b = Stand(64, 4); b.Head = V(64, 38);
        b.HandL = V(54, 76); b.HandR = V(74, 76);
        l.Add(Box(V(64, 110), V(40, 6), Wood));
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? 30 : 98; l.Add(Box(V(x, 96), V(3, 10), Prop, 1));
            for (int s = 0; s < 3; s++) { float k = ((f + s * 3) % Frames) / (float)Frames; l.Add(Dot(V(x + 4 * Mathf.Sin(k * 6), 84 - 40 * k), 2 + 3 * k, Alpha(Gray, 1 - k), false)); }
        }
        Figure(l, b);
        return l;
    }
}
