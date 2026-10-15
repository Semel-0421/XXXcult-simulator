using System;
using NUnit.Framework;

public class GiftedTests
{
    [Test]
    public void Adjust_IsAlwaysFavorable()
    {
        Assert.AreEqual(10.3f, GiftedEffects.Adjust(10, 3f), 1e-4);
        Assert.AreEqual(-9.7f, GiftedEffects.Adjust(-10, 3f), 1e-4);
        Assert.AreEqual(-10.2f, GiftedEffects.Adjust(-10, -2f), 1e-4);
    }

    [Test]
    public void Apply_SumsOwnedGiftedPercent()
    {
        var personal = new PersonalStatus();
        GiftedGranter.Grant(personal.Talents, "no.1"); // 재산 +2.5%

        GiftedEffects.Apply(personal, PersonalStatId.Wealth, 40);

        Assert.AreEqual(41,personal[PersonalStatId.Wealth].Value);
    }

    [Test]
    public void AddWithRemainder_CarriesFractions()
    {
        var personal = new PersonalStatus();

        for (var i = 0; i < 4; i++) personal.AddWithRemainder(PersonalStatId.Health, 10.3f);

        Assert.AreEqual(41, personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void GrantRandom_SameSeedSameResult_NoDuplicates()
    {
        var a = new TalentList();
        var b = new TalentList();

        GiftedGranter.GrantRandom(a, new Random(42), 5);
        GiftedGranter.GrantRandom(b, new Random(42), 5);

        Assert.AreEqual(5, a.Owned.Count);
        for (var i = 0; i < 5; i++) Assert.AreEqual(a.Owned[i].Id, b.Owned[i].Id);
    }

    [Test]
    public void Grant_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GiftedGranter.Grant(new TalentList(), "no.999"));
    }
}
