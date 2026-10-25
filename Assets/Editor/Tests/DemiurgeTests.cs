using System.IO;
using System.Linq;
using NUnit.Framework;

// 황금의 눈동자 4단계: 데미우르고스 스탯·재앙 (SYS_DEMIURGE 4·5절)
public class DemiurgeTests
{
    const int Rich = 999;

    [SetUp]
    public void SetUp()
    {
        EyeRules.Begin(11);
        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
    }

    [TearDown]
    public void TearDown() => GameSession.StartNew(1);

    static DemiurgeState Demiurge => GameSession.God.Demiurge;
    static AeonTree Aeons => GameSession.God.Aeons;

    [Test]
    public void ChooseSector_CopiesStartingStats_RatioOne()
    {
        Assert.IsFalse(Demiurge.Copied);
        EyeRules.ChooseSector(EyeSector.Samael);
        Assert.IsTrue(Demiurge.Copied);
        Assert.AreEqual(GameSession.Cult[StatId.Followers].Value, Demiurge[StatId.Followers]);
        Assert.AreEqual(GameSession.Personal[PersonalStatId.Health].Value, Demiurge[PersonalStatId.Health]);
        Assert.AreEqual(1f, EyeRules.Dominance, 1e-4);
        Assert.IsFalse(DemiurgeState.PersonalStats.Contains(PersonalStatId.Miracle));
        Assert.IsFalse(DemiurgeState.CultStats.Contains(StatId.Communion));
    }

    [Test]
    public void CounterGain_PerSectorEvilMinusGood()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        Assert.AreEqual(0, EyeRules.CounterGain(Aeons)); // 악 0단계 +1, 선 0단계 -1
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 2, AeonKind.Evil), Rich, 10);
        Assert.AreEqual(1 + 1 + 2 - 1, EyeRules.CounterGain(Aeons));
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, 0, AeonKind.Good), Rich, 10); // 다른 섹터 선은 그 섹터만 줄인다
        Assert.AreEqual(3, EyeRules.CounterGain(Aeons));
    }

    [Test]
    public void Quarter_AddsCounter_AndZeroStatPenalty_RevivesOnlyCultStats()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        EyeRules.Quarter();
        Assert.AreEqual(1, Demiurge.Counter);
        GameSession.Personal[PersonalStatId.Health].Set(0);
        GameSession.Cult[StatId.Followers].Set(0);
        EyeRules.Quarter();
        Assert.AreEqual(1 + 1 + 2 * EyeRules.ZeroPenalty, Demiurge.Counter);
        Assert.AreEqual(0, GameSession.Personal[PersonalStatId.Health].Value); // 체력은 휴식으로 회복 (되살리지 않음)
        Assert.AreEqual(1, GameSession.Cult[StatId.Followers].Value);
    }

    [Test]
    public void CalamityDue_AtThreshold()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        Demiurge.AddCounter(EyeRules.CalamityThreshold - 1);
        Assert.IsFalse(EyeRules.CalamityDue);
        Demiurge.AddCounter(1);
        Assert.IsTrue(EyeRules.CalamityDue);
    }

    [Test]
    public void DamageRate_GrowsWithEvilAndShrinksWithGood()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh); // 악 1개, 선 1개 (피해 -5%)
        Assert.AreEqual(0.06f * 0.95f, EyeRules.DamageRate(Aeons), 1e-5);
        for (int l = 1; l <= 4; l++) Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, l, AeonKind.Evil), Rich, 10);
        Assert.AreEqual(0.10f * 0.95f, EyeRules.DamageRate(Aeons), 1e-5);
    }

    [Test]
    public void Calamity_TransfersExactLossToDemiurge_AndResetsCounter()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        GameSession.Cult[StatId.Offerings].Set(500);
        GameSession.Cult[StatId.Authority].Set(200);
        Demiurge.AddCounter(EyeRules.CalamityThreshold);
        var beforeOfferings = Demiurge[StatId.Offerings];
        var ratioBefore = EyeRules.Dominance;

        var question = EyeRules.BeginCalamity();
        Assert.AreEqual(3, question.Choices.Length);
        var outcome = question.Choices[0].Success; // 교단을 내준다
        foreach (var e in outcome.CultEffects) GameSession.Cult[e.Stat].Add(e.Amount);
        var lostOfferings = 500 - GameSession.Cult[StatId.Offerings].Value;
        Assert.Greater(lostOfferings, 0);

        var taken = EyeRules.EndCalamity();
        Assert.AreEqual(beforeOfferings + lostOfferings, Demiurge[StatId.Offerings]);
        Assert.AreEqual(0, Demiurge.Counter);
        Assert.AreEqual(1, Demiurge.Calamities);
        StringAssert.Contains(StatLabel.Of(StatId.Offerings), taken);
        Assert.Greater(EyeRules.Dominance, ratioBefore); // 빼앗긴 만큼 주도권이 넘어간다
    }

    [Test]
    public void Ratio_UsesCappedPartsAndTwoTimesBoundary()
    {
        EyeRules.ChooseSector(EyeSector.Yaldabaoth);
        var mine = DemiurgeState.WeightOf(GameSession.Cult, GameSession.Personal);
        Demiurge.Absorb(StatId.Followers, 1_000_000); // 신도는 1000명에서 잘린다
        Assert.LessOrEqual(Demiurge.Weight, mine + 1f + 1e-4);
    }

    [Test]
    public void SaveRoundTrip_KeepsDemiurgeAndCounter()
    {
        var previousDir = SaveSlots.Directory;
        var dir = Path.Combine(Path.GetTempPath(), "xxxcult_demiurge_" + System.Guid.NewGuid().ToString("N"));
        SaveSlots.Directory = dir;
        try
        {
            EyeRules.ChooseSector(EyeSector.Samael);
            Demiurge.Absorb(PersonalStatId.Health, 7);
            Demiurge.AddCounter(13);
            Demiurge.ResetCounter();
            Demiurge.AddCounter(5);
            var health = Demiurge[PersonalStatId.Health];
            SaveSlots.Write(3, GameSession.Capture());
            GameSession.StartNew(2);
            GameSession.Restore(SaveSlots.Read(3));
            Assert.IsTrue(Demiurge.Copied);
            Assert.AreEqual(health, Demiurge[PersonalStatId.Health]);
            Assert.AreEqual(5, Demiurge.Counter);
            Assert.AreEqual(1, Demiurge.Calamities);
        }
        finally
        {
            SaveSlots.Directory = previousDir;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Test]
    public void DemiurgeStats_CapAt1000And100()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        Demiurge.Absorb(StatId.Offerings, 5000);
        Demiurge.Absorb(StatId.Followers, 5000);
        Demiurge.Absorb(PersonalStatId.Wealth, 500);
        Assert.AreEqual(1000, Demiurge[StatId.Offerings]);
        Assert.AreEqual(1000, Demiurge[StatId.Followers]);
        Assert.AreEqual(100, Demiurge[PersonalStatId.Wealth]);
    }

    [Test]
    public void FullDemiurge_CannotBeatPlayerAtHalfEverywhere()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        foreach (var id in DemiurgeState.CultStats) Demiurge.Absorb(id, 10_000);
        foreach (var id in DemiurgeState.PersonalStats) Demiurge.Absorb(id, 1_000);
        foreach (var id in DemiurgeState.CultStats) GameSession.Cult[id].Set(500);
        foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(50);
        Assert.AreEqual(2f, EyeRules.Dominance, 1e-4);
        Assert.IsFalse(EyeRules.Dominance > EyeRules.LoseDominanceAt); // 정확히 2배는 교주 쪽
        GameSession.Cult[StatId.Doctrine].Set(499);
        Assert.Greater(EyeRules.Dominance, EyeRules.LoseDominanceAt);
    }

    [Test]
    public void RestCalamity_TakesFromHighestStatByRatio()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        foreach (var id in DemiurgeState.CultStats) GameSession.Cult[id].Set(100);
        foreach (var id in DemiurgeState.PersonalStats) GameSession.Personal[id].Set(20);
        GameSession.Cult[StatId.Offerings].Set(900); // 0.9: 가장 높다
        GameSession.Personal[PersonalStatId.Health].Set(80); // 0.8
        Demiurge.AddCounter(EyeRules.CalamityThreshold);
        var before = Demiurge[StatId.Offerings];
        var text = EyeRules.RestCalamity();
        var lost = 900 - GameSession.Cult[StatId.Offerings].Value;
        var rate = EyeRules.DamageRate(Aeons) * EyeRules.RestDamageScale;
        Assert.AreEqual((int)System.Math.Round(900 * rate, System.MidpointRounding.AwayFromZero), lost);
        Assert.AreEqual(before + lost, Demiurge[StatId.Offerings]);
        Assert.AreEqual(80, GameSession.Personal[PersonalStatId.Health].Value);
        Assert.AreEqual(0, Demiurge.Counter);
        StringAssert.Contains(StatLabel.Of(StatId.Offerings), text);
    }
}
