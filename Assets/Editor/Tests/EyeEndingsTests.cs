using System.Linq;
using NUnit.Framework;

// 황금의 눈동자 6단계: 엔딩 13종과 승천 판정 (SYS_DEMIURGE 7절)
public class EyeEndingsTests
{
    const int Rich = 999;

    [SetUp]
    public void SetUp()
    {
        EyeRules.Begin(21);
        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
    }

    [TearDown]
    public void TearDown() => GameSession.StartNew(1);

    static AeonTree Aeons => GameSession.God.Aeons;

    [Test]
    public void Catalog_Has13Endings_8Normal5Hidden()
    {
        Assert.AreEqual(13, EyeEndings.All.Length);
        Assert.AreEqual(13, EyeEndings.All.Select(e => e.Id).Distinct().Count());
        Assert.AreEqual(5, EyeEndings.All.Count(e => e.Hidden));
        Assert.AreEqual(4, EyeEndings.All.Count(e => e.Kind == EndingKind.Bad));
        foreach (var sector in EyeRules.Sectors)
        {
            var key = sector.ToString().ToLowerInvariant();
            foreach (var suffix in new[] { "good", "bad", "other" }) Assert.IsNotNull(EyeEndings.Find($"eye_{key}_{suffix}"), $"{key}_{suffix}");
        }
        foreach (var e in EyeEndings.All)
        {
            var text = string.Format(Lang.Get(e.TextKey), "황금", 12);
            StringAssert.Contains("황금", text, e.Id);
            Assert.IsFalse(string.IsNullOrEmpty(Lang.Get(e.NameKey)));
        }
    }

    static void ReachThreshold(EyeSector sector)
    {
        for (int level = 0; level <= 4; level++)
        {
            Aeons.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Good), Rich, 10);
            Aeons.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Evil), Rich, 10);
        }
        for (int level = 5; level <= 9; level++) Aeons.Learn(AeonSkillCatalog.Stage(sector, level), Rich, 10);
    }

    static void Ascend(EyeSector sector) => Assert.IsTrue(Aeons.Learn(AeonSkillCatalog.Stage(sector, 10), Rich, 10));

    [Test]
    public void NoEnding_BeforeAscension()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        ReachThreshold(EyeSector.Satan);
        Assert.IsNull(GodRules.DueEnding());
        Assert.IsNull(GodRules.ForcedEnding());
    }

    [Test]
    public void Ascension_StartSector_IsGood()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        ReachThreshold(EyeSector.Satan);
        Ascend(EyeSector.Satan);
        Assert.AreEqual("eye_satan_good", GodRules.DueEnding().Id);
    }

    [Test]
    public void Ascension_OverTwiceDominance_IsBad()
    {
        EyeRules.ChooseSector(EyeSector.Samael);
        ReachThreshold(EyeSector.Samael);
        Ascend(EyeSector.Samael);
        foreach (var id in DemiurgeState.CultStats) GameSession.Cult[id].Set(1);
        foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(1);
        Assert.Greater(EyeRules.Dominance, EyeRules.LoseDominanceAt);
        Assert.AreEqual("eye_samael_bad", GodRules.DueEnding().Id);
    }

    [Test]
    public void Ascension_OtherSector_IsHiddenOtherEyes()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        ReachThreshold(EyeSector.Yaldabaoth);
        Ascend(EyeSector.Yaldabaoth);
        var ending = GodRules.DueEnding();
        Assert.AreEqual("eye_yaldabaoth_other", ending.Id);
        Assert.IsTrue(ending.Hidden);
    }

    [Test]
    public void Ascension_AllFourGnosis_IsFourFaces_BadStillWins()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        ReachThreshold(EyeSector.Satan);
        foreach (var sector in new[] { EyeSector.Yahweh, EyeSector.Samael, EyeSector.Yaldabaoth })
        {
            for (int level = 0; level <= 4; level++)
            {
                Aeons.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Good), Rich, 10);
                Aeons.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Evil), Rich, 10);
            }
            Aeons.Learn(AeonSkillCatalog.Stage(sector, 5), Rich, 10);
        }
        Ascend(EyeSector.Satan);
        // 성장 A로 데미우르고스가 커져도, 교주가 전부 절반(500·50) 이상이면 배드가 나지 않는다
        foreach (var id in DemiurgeState.CultStats) GameSession.Cult[id].Set(500);
        foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(50);
        Assert.AreEqual("eye_four_faces", GodRules.DueEnding().Id);

        foreach (var id in DemiurgeState.CultStats) GameSession.Cult[id].Set(1);
        foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(1);
        Assert.AreEqual("eye_satan_bad", GodRules.DueEnding().Id); // 주도권 상실이 히든보다 먼저
    }

    [Test]
    public void Quiz_HidesHiddenUntilSeen()
    {
        var hidden = EyeEndings.Find("eye_four_faces");
        Assert.AreEqual("?", EndingQuiz.Of(hidden));
        Assert.AreNotEqual("?", EndingQuiz.Of(EyeEndings.Find("eye_satan_good")));
    }
}
