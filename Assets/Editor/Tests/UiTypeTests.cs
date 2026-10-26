using NUnit.Framework;

public class UiTypeTests
{
    [TearDown]
    public void TearDown() => UiType.ForceMobile = false;

    [Test]
    public void Desktop_KeepsSizes()
    {
        UiType.ForceMobile = false;
        Assert.AreEqual(false, UiType.Mobile);
        Assert.AreEqual(1f, UiType.TooltipScale, 1e-4f);
        Assert.AreEqual(18f, UiType.PointerGap, 1e-4f);
        Assert.AreEqual(9f, UiType.MinFont(9), 1e-4f);
        Assert.AreEqual(14f, UiType.MinFont(14), 1e-4f);
    }

    [Test]
    public void Mobile_EnlargesTooltipsAndFloorsFonts()
    {
        UiType.ForceMobile = true;
        Assert.AreEqual(true, UiType.Mobile);
        Assert.AreEqual(1.35f, UiType.TooltipScale, 1e-4f);
        Assert.AreEqual(56f, UiType.PointerGap, 1e-4f);
        Assert.AreEqual(12f, UiType.MinFont(9), 1e-4f);
        Assert.AreEqual(14f, UiType.MinFont(14), 1e-4f);
    }
}
