using NUnit.Framework;
using UnityEngine;

public class UiSkinTests
{
    [Test]
    public void BookFrame_HasBorderForNineSlice_AndIsCached()
    {
        var a = UiSkin.BookFrame("TestBook", new Color(0.4f, 0.1f, 0.4f), new Color(0.2f, 0.05f, 0.3f));
        var b = UiSkin.BookFrame("TestBook", Color.black, Color.black);
        Assert.AreSame(a, b);
        Assert.Greater(a.border.x, 0f);
        Assert.AreEqual(a.border.x, a.border.w, 1e-4);
    }

    [Test]
    public void BookFrame_HasGoldStitchingOnPurpleCover()
    {
        var sprite = UiSkin.BookFrame("TestStitch", new Color(0.4f, 0.1f, 0.4f), new Color(0.2f, 0.05f, 0.3f));
        var pixels = sprite.texture.GetPixels();
        int gold = 0;
        foreach (var p in pixels)
            if (p.r > 0.9f && p.g > 0.85f && p.b > 0.5f) gold++;
        Assert.Greater(gold, 100, "금실 박음질이 있어야 한다");
    }

    [Test]
    public void Laurel_IsGoldAndSymmetric()
    {
        var sprite = UiSkin.Laurel();
        var tex = sprite.texture;
        int w = tex.width;
        int opaque = 0, leftCount = 0, rightCount = 0;
        var pixels = tex.GetPixels();
        for (int y = 0; y < tex.height; y++)
        for (int x = 0; x < w; x++)
        {
            if (pixels[y * w + x].a < 0.5f) continue;
            opaque++;
            if (x < w / 2) leftCount++; else rightCount++;
        }
        Assert.Greater(opaque, 2000, "잎과 가지가 충분히 그려져야 한다");
        Assert.AreEqual(1f, (float)Mathf.Min(leftCount, rightCount) / Mathf.Max(leftCount, rightCount), 0.15f, "좌우 대칭");
    }
}
