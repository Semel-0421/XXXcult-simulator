using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class CultEventTests
{
    [Test]
    public void Catalog_Has150RandomAnd38ConditionalWith2To5Choices()
    {
        Assert.AreEqual(150, CultEvents.All.Count(e => !e.IsConditional));
        Assert.AreEqual(38, CultEvents.All.Count(e => e.IsConditional));
        Assert.IsTrue(CultEvents.All.All(e => e.Question.Choices.Length is >= 2 and <= 5));
    }

    [Test]
    public void HiddenGifts_ArePersonalityOrTalentOnly()
    {
        var categories = new HashSet<string> { "성격", "특기" };
        foreach (var id in CultEvents.All.SelectMany(e => e.Question.Choices).SelectMany(c => c.GrantedGifted))
            Assert.IsTrue(categories.Contains(GiftedCatalog.Find(id).Category), id);
    }

    [Test]
    public void Ratio_UsesDoctrineLevelAndClamps()
    {
        var cult = new CultStatus();
        cult[StatId.Doctrine].Set(350); // 4레벨
        cult[StatId.Followers].Set(5000);
        Assert.AreEqual(0.4f, cult.Ratio(StatId.Doctrine), 1e-4);
        Assert.AreEqual(1f, cult.Ratio(StatId.Followers), 1e-4);
        Assert.AreEqual(0.5f, cult.Ratio(StatId.Reputation), 1e-4);
    }

    [Test]
    public void RollRandom_TriggersAbout60Percent()
    {
        var cult = new CultStatus();
        var rng = new Random(7);
        int hits = 0;
        for (int i = 0; i < 2000; i++)
            if (CultEvent.RollRandom(CultEvents.All, cult, new HashSet<string>(), rng) != null) hits++;
        Assert.AreEqual(0.6, hits / 2000.0, 0.04);
    }

    [Test]
    public void RollRandom_FavorsHighStatThemes()
    {
        var cult = new CultStatus();
        cult[StatId.Offerings].Set(1000);
        var rng = new Random(3);
        int offerings = 0, total = 0;
        for (int i = 0; i < 3000; i++)
        {
            var e = CultEvent.RollRandom(CultEvents.All, cult, new HashSet<string>(), rng);
            if (e == null) continue;
            total++;
            if (e.Theme == StatId.Offerings) offerings++;
        }
        Assert.Greater(offerings / (double)total, 1.0 / 8);
    }

    [Test]
    public void ConditionalEvent_FiresOnceWhenConditionMet()
    {
        var cult = new CultStatus();
        var used = new HashSet<string>();
        Assert.IsNull(CultEvent.FindTriggered(CultEvents.All, cult, used));

        cult[StatId.Followers].Set(25);
        cult[StatId.Offerings].Set(100); // 금고 바닥 이벤트(cd10)가 같이 걸리지 않게
        var first = CultEvent.FindTriggered(CultEvents.All, cult, used);
        Assert.AreEqual("cd01", first.Id);
        Assert.IsNull(CultEvent.FindTriggered(CultEvents.All, cult, used));
    }
}
