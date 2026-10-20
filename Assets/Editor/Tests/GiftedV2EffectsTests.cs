using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class GiftedV2EffectsTests
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
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_gv2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Profile.Path = Path.Combine(tempDir, "profile.json");
        Profile.Enabled = true;
        GiftedV2Effects.AdultEnabled = () => false;
        GiftedV2Effects.ResetForTests();
        GameSession.StartNew(1);
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

    static void Own(string giftedId) => Profile.MarkUnlocked(GiftedV2Catalog.Find(giftedId).LinkedAchievement);

    static readonly Endings.Ending AnyEnding = Endings.All.First();

    static void AssertNoOp()
    {
        var check = new CheckContext(CheckKind.Cult, "Preach");
        Assert.AreEqual(0f, GiftedV2Effects.SuccessChanceBonus(check));
        Assert.IsFalse(GiftedV2Effects.ResolveCheck(check, false));
        Assert.IsTrue(GiftedV2Effects.ResolveCheck(check, true));
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Reputation, -10));
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Followers, 10));
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(PersonalStatId.Health, -3));
        Assert.IsFalse(GiftedV2Effects.TryPreventDeath());
        Assert.AreSame(AnyEnding, GiftedV2Effects.FilterEnding(AnyEnding));
    }

    [Test]
    public void NothingOwned_AllHooksAreNoOp_InEveryMode()
    {
        Assert.IsEmpty(GiftedV2Effects.Owned().ToList());
        AssertNoOp();
        GameSession.SetMirrorMode(true);
        AssertNoOp();
        GodRules.Begin(1);
        AssertNoOp();
    }

    [Test]
    public void ProfileDisabledOrRewardsOff_OwnsNothing()
    {
        Own("v2n001");
        Assert.IsTrue(GiftedV2Effects.IsActive("v2n001"));
        Profile.RewardsEnabled = false;
        Assert.IsFalse(GiftedV2Effects.IsActive("v2n001"));
        Profile.RewardsEnabled = true;
        Profile.Enabled = false; // 에디터 테스트·시뮬레이션은 실제 프로필이 있어도 효과가 없다
        Assert.IsFalse(GiftedV2Effects.IsActive("v2n001"));
        AssertNoOp();
    }

    [Test]
    public void Shared_ActiveInEveryMode_Exclusive_OnlyInOwnMode()
    {
        Own("v2n001"); // 일반·공유
        Own("v2m107"); // 거울 전용
        Own("v2g066"); // 신 전용
        foreach (var mode in new[] { "normal", "god", "mirror" })
        {
            Assert.IsTrue(GiftedV2Effects.IsActive("v2n001", mode), mode);
            Assert.AreEqual(mode == "mirror", GiftedV2Effects.IsActive("v2m107", mode), mode);
            Assert.AreEqual(mode == "god", GiftedV2Effects.IsActive("v2g066", mode), mode);
        }
        CollectionAssert.AreEquivalent(new[] { "v2n001", "v2m107" }, GiftedV2Effects.Owned("mirror").Select(e => e.Id).ToArray());
    }

    [Test]
    public void SharedFlag_MatchesCatalog_FirstTenOfEachMode()
    {
        Assert.AreEqual(30, GiftedV2Catalog.All.Count(e => e.Shared));
        foreach (var e in GiftedV2Catalog.All.Where(e => !e.AdultGated))
            Assert.AreEqual(e.Shared, new[] { "normal", "god", "mirror" }.All(m => GiftedV2Effects.AppliesIn(e, m)), e.Id);
    }

    [Test]
    public void AdultGated_OnlyWithAdultAchievementsOption()
    {
        var adult = GiftedV2Catalog.All.Single(e => e.AdultGated);
        Own(adult.Id);
        Assert.IsFalse(GiftedV2Effects.IsActive(adult, adult.Mode));
        GiftedV2Effects.AdultEnabled = () => true;
        Assert.IsTrue(GiftedV2Effects.IsActive(adult, adult.Mode));
        if (!adult.Shared) Assert.IsFalse(GiftedV2Effects.IsActive(adult, adult.Mode == "god" ? "mirror" : "god"));
    }

    [Test]
    public void Ownership_IsTheLinkedAchievement_ThatRewardsIt()
    {
        foreach (var e in GiftedV2Catalog.All)
        {
            var a = Achievements.All.SingleOrDefault(x => x.Reward.Kind == RewardKind.Gifted && x.Reward.GiftedId == e.Id);
            Assert.IsNotNull(a, e.Id);
            Assert.AreEqual(e.LinkedAchievement, a.Id, e.Id);
        }
    }

    [Test]
    public void Tokens_AreLimitedPerRun_AndResetOnNewRun()
    {
        Assert.IsTrue(GiftedV2Effects.TryUseToken("v2n009", 3));
        Assert.IsTrue(GiftedV2Effects.TryUseToken("v2n009", 3));
        Assert.IsTrue(GiftedV2Effects.TryUseToken("v2n009", 3));
        Assert.IsFalse(GiftedV2Effects.TryUseToken("v2n009", 3));
        Assert.AreEqual(3, GiftedV2Effects.TokensUsed("v2n009"));
        Assert.IsTrue(GiftedV2Effects.TryUseToken("v2n002"));
        Assert.IsFalse(GiftedV2Effects.TryUseToken("v2n002"));
        Assert.AreEqual(3, Counters.Get(GiftedV2Effects.TokenPrefix + "v2n009", perRun: true));
        GameSession.StartNew(2);
        Assert.AreEqual(0, GiftedV2Effects.TokensUsed("v2n009"));
        Assert.IsTrue(GiftedV2Effects.TryUseToken("v2n002"));
    }

    [Test]
    public void Register_RejectsUnknownAndDuplicateIds()
    {
        GiftedV2Effects.ResetForTests(registerTable: false);
        Assert.Throws<ArgumentException>(() => GiftedV2Effects.Register("nope", new EffectDef()));
        GiftedV2Effects.Register("v2n003", new EffectDef { GainPercent = 5 });
        Assert.Throws<ArgumentException>(() => GiftedV2Effects.Register("v2n003", new EffectDef()));
    }

    [Test]
    public void Sample_StubbornLuck_RescuesSomeFailures()
    {
        Own("v2n001");
        var check = new CheckContext(CheckKind.Event, null);
        var rescued = Enumerable.Range(0, 400).Count(_ => GiftedV2Effects.ResolveCheck(check, false));
        Assert.That(rescued, Is.InRange(60, 140)); // 기대값 100
    }

    [Test]
    public void Sample_InvertedRepute_CutsReputationLossInMirrorOnly()
    {
        Own("v2m107");
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Reputation, -10));
        GameSession.SetMirrorMode(true);
        Assert.AreEqual(0.8f, GiftedV2Effects.StatMultiplier(StatId.Reputation, -10), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Reputation, 10));
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -10));
        GameSession.Cult[StatId.Reputation].Set(100);
        GiftedEffects.Apply(GameSession.Cult, GameSession.Personal.Talents, StatId.Reputation, -10);
        Assert.AreEqual(92, GameSession.Cult[StatId.Reputation].Value);
    }

    [Test]
    public void Sample_SpringOfFaith_AddsGodPointsAtYearEnd()
    {
        Own("v2g066");
        GodRules.Begin(1);
        GameSession.FoundCult("t");
        var before = GameSession.God.Points.Earned;
        GiftedV2Effects.OnYearEnd();
        Assert.AreEqual(before + GiftedV2Table.GodFaithPointsPerYear, GameSession.God.Points.Earned);
    }
}
