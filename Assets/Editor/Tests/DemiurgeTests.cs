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
        Assert.AreEqual(0, EyeRules.CounterGain(Aeons)); // 악 0단계 +1, 선 0단계 -2 (최소 0)
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 2, AeonKind.Evil), Rich, 10);
        Assert.AreEqual(1 + 1 + 2 - EyeRules.GoodOffset, EyeRules.CounterGain(Aeons));
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, 0, AeonKind.Good), Rich, 10); // 다른 섹터 선은 그 섹터만 줄인다
        Assert.AreEqual(2, EyeRules.CounterGain(Aeons));
    }

    // 성장 A: 악한 아이온 1단계 → 무게 0.16을 사탄 주력 넷(봉헌금·권위·재력·평판)에 4등분 (교단 +40, 개인 +4)
    [Test]
    public void Growth_EvilAeonLearned()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        int off = Demiurge[StatId.Offerings], wealth = Demiurge[PersonalStatId.Wealth], health = Demiurge[PersonalStatId.Health];
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        Assert.AreEqual(off + 40, Demiurge[StatId.Offerings]);
        Assert.AreEqual(wealth + 4, Demiurge[PersonalStatId.Wealth]);
        Assert.AreEqual(health, Demiurge[PersonalStatId.Health]); // 주력이 아닌 스탯은 그대로
    }

    // 성장 B: 악 우위 1 → 분기마다 무게 0.1 (교단 25, 개인 2.5). 소수는 쌓였다가 더해진다
    [Test]
    public void Growth_CohabitEachQuarter()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        Assert.AreEqual(1, EyeRules.EvilSurplus(Aeons, EyeSector.Satan)); // 악 0·1단계 둘 − 선 0단계 하나
        int off = Demiurge[StatId.Offerings], wealth = Demiurge[PersonalStatId.Wealth];
        EyeRules.Quarter();
        EyeRules.Quarter();
        Assert.AreEqual(off + 50, Demiurge[StatId.Offerings]);
        Assert.AreEqual(wealth + 5, Demiurge[PersonalStatId.Wealth]);
    }

    // 성장 C: 재앙마다 무게 0.5 (빼앗은 양과 별도로 교단 +125, 개인 +12)
    [Test]
    public void Growth_Calamity()
    {
        EyeRules.ChooseSector(EyeSector.Yaldabaoth);
        var q = EyeRules.BeginCalamity();
        int rep = Demiurge[StatId.Reputation], health = Demiurge[PersonalStatId.Health];
        Turns.Answer(q.Choices[1]); // 몸 둘: 평판·체력 (명망은 빼앗기지 않음)
        EyeRules.EndCalamity();
        Assert.AreEqual(rep + 125, Demiurge[StatId.Reputation]);
        Assert.Greater(Demiurge[PersonalStatId.Health], health + 12 - 1);
    }

    [Test]
    public void GodPressure_IsOffInEyeMode()
    {
        EyeRules.ChooseSector(EyeSector.Yaldabaoth);
        var cult = GameSession.Cult;
        cult[StatId.Offerings].Set(1000); cult[StatId.Authority].Set(1000); cult[StatId.Reputation].Set(500);
        cult[StatId.Followers].Set(1000); cult[StatId.FollowerFaith].Set(1000); cult[StatId.Doctrine].Set(1000);
        var pressure = GameSession.God.Pressure;
        Assert.AreEqual(0, pressure.Stage(Pressure.Earth, cult));
        Assert.AreEqual(0, pressure.Stage(Pressure.Heaven, cult));
        pressure.ApplyQuarter(cult);
        Assert.AreEqual(500, cult[StatId.Reputation].Value);
        Assert.AreEqual(1000, cult[StatId.FollowerFaith].Value);
        Assert.AreEqual(0f, GodRules.EventPenalty);
    }

    [Test]
    public void GnosisStage_RaisesDoctrineAtOnce()
    {
        EyeRules.ChooseSector(EyeSector.Samael);
        for (int l = 1; l <= 4; l++)
        {
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Samael, l, AeonKind.Good), Rich, 10);
            Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Samael, l, AeonKind.Evil), Rich, 10);
        }
        var before = GameSession.Cult[StatId.Doctrine].Value;
        Aeons.Learn(AeonSkillCatalog.Stage(EyeSector.Samael, 5), Rich, 10);
        Assert.AreEqual(before + EyeRules.GnosisDoctrine, GameSession.Cult[StatId.Doctrine].Value);
    }

    [Test]
    public void Quarter_AddsCounter_AndZeroStatPenalty_RevivesOnlyCultStats()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 1, AeonKind.Evil), Rich, 10);
        Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Satan, 2, AeonKind.Evil), Rich, 10);
        EyeRules.Quarter();
        Assert.AreEqual(2, Demiurge.Counter); // 악 1+1+2 - 선 0단계 하나 × 2
        GameSession.Personal[PersonalStatId.Health].Set(0);
        GameSession.Cult[StatId.Followers].Set(0);
        EyeRules.Quarter();
        Assert.AreEqual(2 + 2 + 2 * EyeRules.ZeroPenalty, Demiurge.Counter);
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
        Assert.AreEqual(beforeOfferings + lostOfferings + (int)(EyeRules.CalamityGrowth / 4 * ClampedStat.DefaultMax), Demiurge[StatId.Offerings]); // 빼앗은 양 + 성장 C
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
