using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class GiftedV2MirrorTests
{
    string tempDir, previousPath;
    bool previousEnabled;
    Func<bool> previousAdult;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        previousAdult = GiftedV2Effects.AdultEnabled;
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_gv2m_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Profile.Path = Path.Combine(tempDir, "profile.json");
        Profile.Enabled = true;
        GiftedV2Effects.AdultEnabled = () => false;
        GiftedV2Effects.ResetForTests();
        GameSession.StartNew(1);
        GameSession.SetMirrorMode(true);
    }

    [TearDown]
    public void TearDown()
    {
        GiftedV2Effects.AdultEnabled = previousAdult;
        GiftedV2Effects.ResetForTests();
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        GameSession.StartNew(1);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    static void Own(string id) => Profile.MarkUnlocked(GiftedV2Catalog.Find(id).LinkedAchievement);

    static void CultYear(int years)
    {
        GameSession.FoundCult("t");
        GameSession.CultTime.Restore(years * GameClock.QuartersPerYear);
    }

    [Test]
    public void EveryMirrorEntry_IsRegistered()
    {
        var mirror = GiftedV2Catalog.All.Where(e => e.Mode == "mirror").ToList();
        Assert.AreEqual(40, mirror.Count);
        foreach (var e in mirror) Assert.IsTrue(GiftedV2Effects.IsRegistered(e.Id), e.Id);
    }

    [Test]
    public void OfferingsSeawall_HalvesLossOnlyAtOrBelow300()
    {
        Own("v2m108");
        GameSession.Cult[StatId.Offerings].Set(200);
        Assert.AreEqual(0.5f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -10), 1e-5f);
        GameSession.Cult[StatId.Offerings].Set(400);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -10));
    }

    [Test]
    public void PreludeAndFinaleFlames_FollowCultYears()
    {
        Own("v2m113"); Own("v2m112");
        CultYear(2);
        Assert.AreEqual(1.1f, GiftedV2Effects.StatMultiplier(StatId.Followers, 10), 1e-5f);
        GameSession.CultTime.Restore(20 * GameClock.QuartersPerYear);
        Assert.AreEqual(1.05f, GiftedV2Effects.StatMultiplier(StatId.Followers, 10), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Followers, -10)); // 보상(증가)에만 붙는다
    }

    [Test]
    public void FollowerSeawall_CutsFollowerLossBy20()
    {
        Own("v2m109");
        Assert.AreEqual(0.8f, GiftedV2Effects.StatMultiplier(StatId.Followers, -5), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -5));
    }

    [Test]
    public void Misfortune_ShieldReduces_ThirtiethYearBlocks_YearlyDropWorks()
    {
        Assert.IsTrue(GiftedV2Mirror.AllowsMisfortuneGain());
        Own("v2m094");
        var kept = Enumerable.Range(0, 400).Count(_ => GiftedV2Mirror.AllowsMisfortuneGain());
        Assert.That(kept, Is.InRange(280, 350)); // 기대값 320
        Own("v2m118");
        CultYear(30);
        Assert.IsFalse(GiftedV2Mirror.AllowsMisfortuneGain());

        Own("v2m095");
        GameSession.CultTime.Restore(0);
        for (int i = 0; i < 6; i++) GameSession.AddMisfortune(); // 불운 방패 때문에 일부는 막힐 수 있다
        while (GameSession.Misfortune < 6) GameSession.AddMisfortune();
        var before = GameSession.Misfortune;
        GiftedV2Effects.OnYearEnd();
        Assert.AreEqual(before - 1, GameSession.Misfortune);
    }

    [Test]
    public void YearlyRecruits_AppliesBonusesAndLossRelief()
    {
        Assert.AreEqual(2f, GiftedV2Mirror.YearlyRecruits(2, 500));
        Own("v2m101");
        Assert.AreEqual(3f, GiftedV2Mirror.YearlyRecruits(2, 500));
        Assert.AreEqual(-3f, GiftedV2Mirror.YearlyRecruits(-3, 300)); // 명망 400 미만이라 +1 없음
        Own("v2m091");
        Assert.AreEqual(-1f, GiftedV2Mirror.YearlyRecruits(-3, 300), 1e-5f);
        Assert.AreEqual(0f, GiftedV2Mirror.YearlyRecruits(-1, 300), 1e-5f);
    }

    [Test]
    public void Conversions_RaiseCultScore()
    {
        GameSession.Cult[StatId.Reputation].Set(1000);
        GameSession.Cult[StatId.Followers].Set(50);
        Assert.AreEqual(1250f, CultScore.Reputation(GameSession.Cult), 1e-3f);
        Own("v2m092");
        Assert.AreEqual(1275f, CultScore.Reputation(GameSession.Cult), 1e-3f);
    }

    [Test]
    public void DoctrinePoints_SpringAddsOnePerCultYear()
    {
        var baseline = DoctrinePoints.Earned(10, 0, 0);
        Own("v2m102");
        Assert.AreEqual(baseline + 10, DoctrinePoints.Earned(10, 0, 0));
        Own("v2m104");
        Assert.AreEqual(baseline + 10 + (int)Math.Floor((baseline + 10) * 0.1), DoctrinePoints.Earned(10, 0, 0));
    }

    [Test]
    public void LastReflection_SurvivesCollapseOnce()
    {
        var absorbed = MirrorRules.Find("inv_end_absorbed");
        Assert.AreSame(absorbed, GiftedV2Effects.FilterEnding(absorbed));
        Own("v2m120");
        GameSession.Cult[StatId.Reputation].Set(0);
        Assert.IsNull(GiftedV2Effects.FilterEnding(absorbed));
        Assert.AreEqual(GiftedV2Mirror.SurviveReputation, GameSession.Cult[StatId.Reputation].Value);
        Assert.AreSame(absorbed, GiftedV2Effects.FilterEnding(absorbed)); // 회차당 1번
    }

    [Test]
    public void ShieldAgainstCollapse_RestoresFollowersOnceAfterYear5()
    {
        Own("v2m100");
        CultYear(3);
        GiftedV2Mirror.AfterTurn();
        Assert.AreEqual(0, GameSession.Cult[StatId.Followers].Value); // 붕괴 판정 전에는 쓰지 않는다
        GameSession.CultTime.Restore(5 * GameClock.QuartersPerYear);
        GiftedV2Mirror.AfterTurn();
        Assert.AreEqual(GiftedV2Mirror.RestoredFollowers, GameSession.Cult[StatId.Followers].Value);
        GameSession.Cult[StatId.Followers].Set(0);
        GiftedV2Mirror.AfterTurn();
        Assert.AreEqual(0, GameSession.Cult[StatId.Followers].Value);
    }

    [Test]
    public void LedgerOfCracks_EveryFifthFailureGuaranteesNextSuccess()
    {
        Own("v2m087");
        var check = new CheckContext(CheckKind.Life, "Study");
        for (int i = 0; i < 4; i++) Assert.IsFalse(GiftedV2Effects.ResolveCheck(check, false));
        Assert.IsFalse(GiftedV2Effects.ResolveCheck(check, false)); // 5번째 실패
        Assert.IsTrue(GiftedV2Effects.ResolveCheck(check, false));  // 다음은 확정 성공
        Assert.IsFalse(GiftedV2Effects.ResolveCheck(check, false)); // 한 번뿐
    }

    [Test]
    public void ReflectedBreath_BlocksOneDropToZeroPerRun()
    {
        Own("v2m082");
        GameSession.Personal[PersonalStatId.Health].Set(3);
        GiftedEffects.Apply(GameSession.Personal, PersonalStatId.Health, -10);
        Assert.AreEqual(1, GameSession.Personal[PersonalStatId.Health].Value);
        GiftedEffects.Apply(GameSession.Personal, PersonalStatId.Health, -10);
        Assert.AreEqual(0, GameSession.Personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void FinalSkillRoad_WaivesTheStepBeforeTheFinal()
    {
        var tree = GameSession.Cult.Doctrines;
        tree.Restore(DoctrineSkill.DeclarationId, 1);
        var step8 = DoctrineCatalog.At(DoctrineBranch.Lust, 8, "A");
        var final = DoctrineCatalog.At(DoctrineBranch.Lust, 10, "A");
        tree.Restore(step8.Id, 1);
        Assert.IsFalse(tree.PrerequisitesMet(final));
        Own("v2m103");
        Assert.IsTrue(tree.PrerequisitesMet(final));
    }

    [Test]
    public void ExclusiveEffects_DoNothingOutsideMirror()
    {
        Own("v2m108"); Own("v2m101"); Own("v2m094");
        GameSession.SetMirrorMode(false);
        GameSession.Cult[StatId.Offerings].Set(100);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -10));
        Assert.AreEqual(2f, GiftedV2Mirror.YearlyRecruits(2, 500));
        Assert.IsTrue(GiftedV2Mirror.AllowsMisfortuneGain());
    }
}
