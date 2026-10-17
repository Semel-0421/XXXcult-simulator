using System;
using NUnit.Framework;

public class AdultGateTests
{
    static readonly DateTime Today = new(2026, 10, 16);

    [Test]
    public void Check_UsesFullAgeAroundBirthday()
    {
        Assert.AreEqual(AdultGate.Result.Adult, AdultGate.Check(2007, 10, 16, Today));  // 오늘 만 19세
        Assert.AreEqual(AdultGate.Result.Minor, AdultGate.Check(2007, 10, 17, Today));  // 내일 생일
        Assert.AreEqual(AdultGate.Result.Adult, AdultGate.Check(1990, 1, 1, Today));
        Assert.AreEqual(AdultGate.Result.Minor, AdultGate.Check(2015, 5, 5, Today));
    }

    [Test]
    public void Check_RejectsImpossibleOrFutureDates()
    {
        Assert.AreEqual(AdultGate.Result.Invalid, AdultGate.Check(2001, 2, 29, Today)); // 윤년 아님
        Assert.AreEqual(AdultGate.Result.Invalid, AdultGate.Check(1990, 13, 1, Today));
        Assert.AreEqual(AdultGate.Result.Invalid, AdultGate.Check(1800, 1, 1, Today));
        Assert.AreEqual(AdultGate.Result.Invalid, AdultGate.Check(2027, 1, 1, Today));
    }

    [Test]
    public void AgeOn_LeapDayBirthday_TurnsOverOnMarchFirst()
    {
        var birth = new DateTime(2004, 2, 29);
        Assert.AreEqual(18, AdultGate.AgeOn(birth, new DateTime(2023, 2, 28)));
        Assert.AreEqual(19, AdultGate.AgeOn(birth, new DateTime(2023, 3, 1)));
    }
}
