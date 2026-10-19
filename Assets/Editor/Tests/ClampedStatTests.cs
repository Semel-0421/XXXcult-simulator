using NUnit.Framework;

public class ClampedStatTests
{
    [Test]
    public void Constructor_ClampsStartValueToRange()
    {
        Assert.AreEqual(10, new ClampedStat(50, max: 10).Value);
        Assert.AreEqual(0, new ClampedStat(-5).Value);
        Assert.AreEqual(ClampedStat.DefaultMax, new ClampedStat().Max);
    }

    [Test]
    public void Set_ClampsAndRaisesChangedWithNewValue()
    {
        var stat = new ClampedStat(max: 100);
        var received = -1;
        stat.Changed += v => received = v;

        stat.Set(150);

        Assert.AreEqual(100, stat.Value);
        Assert.AreEqual(100, received);
    }

    [Test]
    public void Set_SameValue_DoesNotRaiseChanged()
    {
        var stat = new ClampedStat(100, max: 100);
        var calls = 0;
        stat.Changed += _ => calls++;

        stat.Set(100);
        stat.Set(500); // 이미 최대값이므로 클램프 후 동일

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Add_HandlesNegativeAmounts()
    {
        var stat = new ClampedStat(30);

        stat.Add(-50);

        Assert.AreEqual(0, stat.Value);
    }

    [Test]
    public void Add_DoesNotOverflowAtIntMax()
    {
        var stat = new ClampedStat(int.MaxValue - 1, max: int.MaxValue);

        stat.Add(10);

        Assert.AreEqual(int.MaxValue, stat.Value);
    }
}
