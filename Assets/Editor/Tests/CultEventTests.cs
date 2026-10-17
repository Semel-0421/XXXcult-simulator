using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class CultEventTests
{
    [Test]
    public void Catalog_Has300RandomAnd38ConditionalWith2To5Choices()
    {
        Assert.AreEqual(300, CultEvents.All.Count(e => e.Kind == CultEvent.EventKind.Random)); // 기존 150 + 일반 150
        Assert.AreEqual(38, CultEvents.All.Count(e => e.IsConditional));
        Assert.AreEqual(16, CultEvents.All.Count(e => e.IsNegative));
        Assert.IsTrue(CultEvents.All.All(e => e.Question.Choices.Length is >= 2 and <= 5));
    }

    [Test]
    public void HiddenGifts_ArePersonalityOrTalentOnly()
    {
        var categories = new HashSet<string> { "성격", "특기" };
        foreach (var id in CultEvents.All.SelectMany(e => e.Question.Choices).SelectMany(c => c.Success.Grants.Concat(c.Fail?.Grants ?? System.Array.Empty<string>())))
            Assert.IsTrue(categories.Contains(GiftedCatalog.Find(id).Category), id);
    }

    [Test]
    public void Ratio_UsesDoctrineLevelAndClamps()
    {
        var cult = new CultStatus();
        cult[StatId.Doctrine].Set(350); // 로그 곡선으로 6레벨
        cult[StatId.Followers].Set(5000);
        Assert.AreEqual(0.6f, cult.Ratio(StatId.Doctrine), 1e-4);
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

    [Test]
    public void EveryRolledCultChoice_HasPersonalStatBonus()
    {
        foreach (var e in CultEvents.All)
            foreach (var choice in e.Question.Choices)
                if (choice.Chance < 1f) Assert.IsNotNull(choice.BonusStat, $"{e.Id}: {choice.Text}");
    }

    [Test]
    public void StatBonus_ConvertsPersonalStatToPercent()
    {
        var choice = new Choice("t", 0.6f, new Outcome("s"), new Outcome("f")).With(PersonalStatId.Piety);
        var p = new PersonalStatus();
        Assert.AreEqual(0.6f, choice.ChanceFor(p), 1e-4);
        p[PersonalStatId.Piety].Set(50);
        Assert.AreEqual(0.6f + Choice.MaxStatBonus / 2, choice.ChanceFor(p), 1e-4);
        p[PersonalStatId.Piety].Set(100);
        Assert.AreEqual(0.6f + Choice.MaxStatBonus, choice.ChanceFor(p), 1e-4);
        var sure = new Choice("t", 0.95f, new Outcome("s"), new Outcome("f")).With(PersonalStatId.Piety);
        Assert.AreEqual(0.99f, sure.ChanceFor(p), 1e-4, "최대 99%");
        Assert.AreEqual(1f, new Choice("t", new Outcome("s")).ChanceFor(p), "확정 선택지는 그대로");
    }

    [Test]
    public void EveryRandomChoice_HasDistinctFailureText()
    {
        foreach (var choice in CultEvents.All.SelectMany(e => e.Question.Choices))
        {
            if (choice.Chance >= 1f) continue;
            Assert.IsNotNull(choice.Fail, choice.Text);
            Assert.IsFalse(string.IsNullOrEmpty(choice.Fail.Text), choice.Text);
            Assert.AreNotEqual(choice.Success.Text, choice.Fail.Text, choice.Text);
        }
    }

    [Test]
    public void NegativeEvents_TwoPerStat_NearZero()
    {
        var negatives = CultEvents.All.Where(e => e.IsNegative).ToList();
        Assert.AreEqual(16, negatives.Count);

        var fresh = new CultStatus(); // 갓 만든 교단: 대부분의 스탯이 바닥
        Assert.IsTrue(negatives.Count(e => e.Condition(fresh)) >= 14);

        var strong = new CultStatus();
        foreach (var id in new[] { StatId.LeaderFaith, StatId.FollowerFaith, StatId.Authority, StatId.Offerings, StatId.Followers, StatId.Communion, StatId.Reputation, StatId.Doctrine })
            strong[id].Set(900);
        Assert.AreEqual(0, negatives.Count(e => e.Condition(strong)));
    }

    [Test]
    public void NegativeChance_StartsAtYear3_AndRisesEachYear()
    {
        Assert.AreEqual(0f, CultEvent.NegativeChanceAt(1));
        Assert.AreEqual(0f, CultEvent.NegativeChanceAt(2));
        Assert.AreEqual(CultEvent.NegativeBaseChance, CultEvent.NegativeChanceAt(3), 1e-5);
        Assert.Greater(CultEvent.NegativeChanceAt(4), CultEvent.NegativeChanceAt(3));
        Assert.Greater(CultEvent.NegativeChanceAt(10), CultEvent.NegativeChanceAt(5));
        Assert.AreEqual(CultEvent.NegativeMaxChance, CultEvent.NegativeChanceAt(100), 1e-5);
    }

    [Test]
    public void RollNegative_NeverBeforeYear3_AndMatchesChanceAfter()
    {
        var cult = new CultStatus();
        var rng = new Random(11);
        for (int i = 0; i < 200; i++)
            Assert.IsNull(CultEvent.RollNegative(CultEvents.All, cult, new HashSet<string>(), rng, 2));

        int hits = 0;
        const int Trials = 4000;
        for (int i = 0; i < Trials; i++)
            if (CultEvent.RollNegative(CultEvents.All, cult, new HashSet<string>(), rng, 8) != null) hits++;
        Assert.AreEqual(CultEvent.NegativeChanceAt(8), hits / (double)Trials, 0.03);
    }

    [Test]
    public void RollRandom_NeverReturnsNegativeEvents()
    {
        var cult = new CultStatus();
        var rng = new Random(5);
        for (int i = 0; i < 2000; i++)
        {
            var e = CultEvent.RollRandom(CultEvents.All, cult, new HashSet<string>(), rng);
            if (e != null) Assert.AreEqual(CultEvent.EventKind.Random, e.Kind);
        }
    }

    [Test]
    public void ReputationBoost_StepsEvery100Below400()
    {
        Assert.AreEqual(0, CultEvent.ReputationBoostSteps(400));
        Assert.AreEqual(1, CultEvent.ReputationBoostSteps(399));
        Assert.AreEqual(1, CultEvent.ReputationBoostSteps(300));
        Assert.AreEqual(2, CultEvent.ReputationBoostSteps(299));
        Assert.AreEqual(4, CultEvent.ReputationBoostSteps(0));
        Assert.AreEqual(4, CultEvent.ReputationBoostSteps(-5));
    }
}
