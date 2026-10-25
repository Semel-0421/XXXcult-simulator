using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// 황금의 눈동자 8단계: 이벤트 (Docs/eye/eye_events.csv → EyeEvents, EyeRules.NextEvent)
public class EyeEventsTests
{
    const int Rich = 999;

    [SetUp]
    public void SetUp()
    {
        EyeRules.Begin(51);
        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
    }

    [TearDown]
    public void TearDown() => GameSession.StartNew(1);

    static AeonTree Aeons => GameSession.God.Aeons;

    [Test]
    public void Catalog_Has65Events()
    {
        Assert.AreEqual(65, EyeEvents.All.Length);
        Assert.AreEqual(65, EyeEvents.All.Select(e => e.Id).Distinct().Count());
        foreach (var sector in EyeRules.Sectors)
            Assert.AreEqual(10, EyeEvents.All.Count(e => !e.IsConditional && EyeRules.SectorOf(e) == sector), sector.ToString());
        Assert.AreEqual(25, EyeEvents.All.Count(e => e.IsConditional));
        Assert.IsTrue(EyeEvents.All.All(e => e.Question.Choices.Length >= 2));
    }

    [Test]
    public void GnosisStageEvent_FiresOnceAfterLearning_AndReverses()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        var used = new HashSet<string>();
        Assert.IsNull(CultEvent.FindTriggered(EyeEvents.All, GameSession.Cult, used));
        Assert.IsFalse(EyeRules.Reversed);
        for (int l = 1; l <= 4; l++)
        {
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, l, AeonKind.Good), Rich, 10);
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, l, AeonKind.Evil), Rich, 10);
        }
        Aeons.Learn(AeonSkillCatalog.Stage(EyeSector.Satan, 5), Rich, 10);
        Assert.IsTrue(EyeRules.Reversed);
        var fired = new[] { CultEvent.FindTriggered(EyeEvents.All, GameSession.Cult, used), CultEvent.FindTriggered(EyeEvents.All, GameSession.Cult, used) }.Select(e => e?.Id).ToList();
        CollectionAssert.AreEquivalent(new[] { "sag5", "eyerev" }, fired);
        Assert.IsNull(CultEvent.FindTriggered(EyeEvents.All, GameSession.Cult, used));
    }

    [Test]
    public void AscendEvent_NeedsThroneSectorAndDoctrine10()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        for (int l = 0; l <= 4; l++)
        {
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, l, AeonKind.Good), Rich, 10);
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, l, AeonKind.Evil), Rich, 10);
        }
        for (int l = 5; l <= 9; l++) Aeons.Learn(AeonSkillCatalog.Stage(EyeSector.Yahweh, l), Rich, 10);
        Assert.IsFalse(EyeRules.ReadyToAscend(EyeSector.Yahweh)); // 교리 0
        GameSession.Cult[StatId.Doctrine].Set(1000);
        Assert.IsTrue(EyeRules.ReadyToAscend(EyeSector.Yahweh));
        Assert.IsFalse(EyeRules.ReadyToAscend(EyeSector.Satan));
    }

    [Test]
    public void NextEvent_SectorEventsOnlyForTouchedSectors()
    {
        EyeRules.ChooseSector(EyeSector.Samael);
        var rng = new System.Random(3);
        var seen = new HashSet<EyeSector>();
        for (int i = 0; i < 400; i++)
        {
            var e = EyeRules.NextEvent(GameSession.Cult, new HashSet<string>(), rng, () => null);
            if (e != null && !e.IsConditional && EyeEvents.All.Contains(e)) seen.Add(EyeRules.SectorOf(e));
        }
        CollectionAssert.AreEquivalent(new[] { EyeSector.Samael }, seen);
    }

    [Test]
    public void CalamityEffect_RaisesCounter()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        var outcome = new Outcome("x").X(GodEventEffect.Calamity, 3);
        GodRules.AfterEvent(outcome);
        Assert.AreEqual(3, GameSession.God.Demiurge.Counter);
        GodRules.AfterEvent(new Outcome("y").X(GodEventEffect.Calamity, -5));
        Assert.AreEqual(0, GameSession.God.Demiurge.Counter);
    }

    [Test]
    public void EventPool_FindsEyeGodAndMirrorIds()
    {
        var ids = Turns.CultPool.Select(e => e.Id).ToHashSet();
        Assert.IsTrue(ids.Contains("ya01"));
        Assert.IsTrue(ids.Contains(GodEvents.All[0].Id));
        Assert.IsTrue(ids.Contains(MirrorEvents.All[0].Id));
    }
}
