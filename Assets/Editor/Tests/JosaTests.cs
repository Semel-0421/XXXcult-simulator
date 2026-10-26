using NUnit.Framework;

public class JosaTests
{
    [TestCase("잔치을(를) 했다", "잔치를 했다")]
    [TestCase("신전을(를)", "신전을")]
    [TestCase("자애이(가)", "자애가")]
    [TestCase("청렴이(가)", "청렴이")]
    [TestCase("사탄은(는)", "사탄은")]
    [TestCase("야훼은(는)", "야훼는")]
    [TestCase("사탄와(과)", "사탄과")]
    [TestCase("야훼와(과)", "야훼와")]
    [TestCase("Lv.3을(를)", "Lv.3을(를)")]
    [TestCase("", "")]
    public void Fix_PicksParticleByFinalConsonant(string input, string expected)
    {
        Assert.AreEqual(expected, Josa.Fix(input));
    }

    [Test]
    public void Fix_NullStaysNull()
    {
        Assert.IsNull(Josa.Fix(null));
    }
}
