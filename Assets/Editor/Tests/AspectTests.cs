using NUnit.Framework;
using UnityEngine;

public class AspectTests
{
    [Test]
    public void Modes_CycleAndRatiosAreCorrect()
    {
        Assert.AreEqual(16f / 9f, AspectFit.Ratio("auto"), 1e-4);
        Assert.AreEqual("auto", AspectFit.Next("4:3"));
        Assert.AreEqual("16:9", AspectFit.Next("auto"));
        Assert.AreEqual(20f / 9f, AspectFit.Ratio("20:9"), 1e-4);
        Assert.AreEqual(4f / 3f, AspectFit.Ratio("4:3"), 1e-4);
    }

    [Test]
    public void FrameSize_NeverShrinksBelowDesign()
    {
        var design = new Vector2(1920, 1080);
        Assert.AreEqual(design, AspectFit.FrameSize(design, 16f / 9f));
        Assert.AreEqual(new Vector2(2400, 1080), AspectFit.FrameSize(design, 20f / 9f)); // 넓은 폰: 가로만 늘어남
        Assert.AreEqual(new Vector2(1920, 1440), AspectFit.FrameSize(design, 4f / 3f));  // 태블릿: 세로만 늘어남
    }

    [Test]
    public void FitScale_FitsWholeFrameInsideCanvas()
    {
        var frame = new Vector2(1920, 1080);
        Assert.AreEqual(0.5f, AspectFit.FitScale(new Vector2(960, 1000), frame), 1e-4);  // 좁은 화면: 가로에 맞춤
        Assert.AreEqual(0.5f, AspectFit.FitScale(new Vector2(2000, 540), frame), 1e-4);  // 넓은 화면: 세로에 맞춤
    }
}
