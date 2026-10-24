using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class GiftedV2NormalTests
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
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_gv2n_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Profile.Path = Path.Combine(tempDir, "profile.json");
        Profile.Enabled = true;
        GiftedV2Effects.AdultEnabled = () => false;
        GiftedV2Table.RollOverride = () => 0.0; // 50% 판정은 항상 성공으로 고정
        GiftedV2Effects.ResetForTests();
        GameSession.StartNew(1);
    }

    [TearDown]
    public void TearDown()
    {
        GiftedV2Effects.AdultEnabled = previousAdult;
        GiftedV2Table.RollOverride = null;
        GiftedV2Effects.ResetForTests();
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        GameSession.StartNew(1);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    static void Own(string id) => Profile.MarkUnlocked(GiftedV2Catalog.Find(id).LinkedAchievement);

    [Test]
    public void AllNormalEntries_AreRegistered()
    {
        foreach (var e in GiftedV2Catalog.ForMode("normal"))
            Assert.IsTrue(GiftedV2Effects.IsRegistered(e.Id), e.Id);
    }

    [Test]
    public void SecondWind_PreventsDeathOncePerRun()
    {
        Own("v2n002");
        Assert.IsTrue(GiftedV2Effects.TryPreventDeath());
        Assert.IsFalse(GiftedV2Effects.TryPreventDeath());
    }

    [Test]
    public void SecondWind_FailedRollStillSpendsToken()
    {
        Own("v2n002");
        GiftedV2Table.RollOverride = () => 0.99;
        Assert.IsFalse(GiftedV2Effects.TryPreventDeath());
        GiftedV2Table.RollOverride = () => 0.0;
        Assert.IsFalse(GiftedV2Effects.TryPreventDeath());
    }

    [Test]
    public void WideCircle_RaisesEventGainsOnly()
    {
        Own("v2n003");
        using (GiftedV2Effects.During(ChangeSource.Event, "x"))
            Assert.AreEqual(1.025f, GiftedV2Effects.StatMultiplier(StatId.Followers, 10), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Followers, 10));
    }

    [Test]
    public void SkillChecks_QuietEye_OnlyMeditation()
    {
        Own("v2n012");
        Assert.AreEqual(GiftedV2Table.NormalCheckBonus, GiftedV2Effects.SuccessChanceBonus(new CheckContext(CheckKind.Life, "Meditate")), 1e-5f);
        Assert.AreEqual(0f, GiftedV2Effects.SuccessChanceBonus(new CheckContext(CheckKind.Life, "Study")));
    }

    [Test]
    public void DawnVigor_AddsHealthOnExerciseSuccess()
    {
        Own("v2n011");
        var before = GameSession.Personal[PersonalStatId.Health].Value;
        GiftedV2Effects.ResolveCheck(new CheckContext(CheckKind.Life, "Exercise"), true);
        Assert.AreEqual(before + 1, GameSession.Personal[PersonalStatId.Health].Value);
        GiftedV2Effects.ResolveCheck(new CheckContext(CheckKind.Life, "Exercise"), false);
        Assert.AreEqual(before + 1, GameSession.Personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void ActionGains_DevoutKnees_And_Proselytizer()
    {
        Own("v2n015"); Own("v2n016");
        using (GiftedV2Effects.During(ChangeSource.Action, "Proselytize"))
        {
            Assert.AreEqual(1.15f, GiftedV2Effects.StatMultiplier(StatId.Followers, 5), 1e-5f);
            Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Reputation, 2));
        }
        using (GiftedV2Effects.During(ChangeSource.Action, "CultPray"))
            Assert.AreEqual(1.2f, GiftedV2Effects.StatMultiplier(StatId.LeaderFaith, 20), 1e-5f);
    }

    [Test]
    public void LossReducers_AuthorityAndOfferings()
    {
        Own("v2n022"); Own("v2n023");
        Assert.AreEqual(0.8f, GiftedV2Effects.StatMultiplier(StatId.Authority, -10), 1e-5f);
        Assert.AreEqual(0.8f, GiftedV2Effects.StatMultiplier(StatId.Offerings, -10), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Authority, 10));
    }

    [Test]
    public void PoorMansBlessing_OnlyWhenWealthZero()
    {
        Own("v2n029");
        GameSession.Personal[PersonalStatId.Wealth].Set(0);
        Assert.AreEqual(1.03f, GiftedV2Effects.StatMultiplier(PersonalStatId.Piety, 5), 1e-5f);
        GameSession.Personal[PersonalStatId.Wealth].Set(5);
        Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(PersonalStatId.Piety, 5));
    }

    [Test]
    public void MemoryOfReturn_AddsStartBonus()
    {
        Own("v2n038");
        var before = GameSession.Personal[PersonalStatId.Piety].Value;
        GiftedV2Effects.OnRunStart();
        Assert.AreEqual(before + GiftedV2Table.NormalStartPersonalBonus, GameSession.Personal[PersonalStatId.Piety].Value);
    }

    [Test]
    public void ShieldAgainstDissolution_RestoresReputationOnce()
    {
        Own("v2n037");
        GameSession.Cult[StatId.Reputation].Set(0);
        var dissolved = Endings.Find(Endings.Dissolved);
        Assert.IsNull(GiftedV2Effects.FilterEnding(dissolved));
        Assert.AreEqual(GiftedV2Table.NormalReputationRestore, GameSession.Cult[StatId.Reputation].Value);
        GameSession.Cult[StatId.Reputation].Set(0);
        Assert.AreSame(dissolved, GiftedV2Effects.FilterEnding(dissolved));
    }

    [Test]
    public void FlashOfInsight_RerollsUpToTwiceTimesPerRun()
    {
        Own("v2n009");
        var check = new CheckContext(CheckKind.Life, "Study", 1f); // 재굴림은 항상 성공
        Assert.IsTrue(GiftedV2Effects.ResolveCheck(check, false));
        Assert.IsTrue(GiftedV2Effects.ResolveCheck(check, false));
        Assert.IsFalse(GiftedV2Effects.ResolveCheck(check, false));
    }

    [Test]
    public void LongView_AddsDoctrinePointPerFiveYears()
    {
        Own("v2n036");
        Assert.AreEqual(2, GiftedV2Effects.DoctrinePointBonusNormal(10));
        Assert.AreEqual(0, GiftedV2Effects.DoctrinePointBonusNormal(4));
    }

    [Test]
    public void DoctrineSage_ReducesSpentPoints()
    {
        Assert.AreEqual(20, GiftedV2Effects.DoctrineSpentAdjustNormal(20));
        Own("v2n025");
        Assert.AreEqual(18, GiftedV2Effects.DoctrineSpentAdjustNormal(20));
        Assert.AreEqual(5, GiftedV2Effects.DoctrineSpentAdjustNormal(5));
    }

    [Test]
    public void UnwaveringFlock_CutsDecayLossToFollowers()
    {
        Own("v2n024");
        Assert.AreEqual(0.8f, GiftedV2Effects.DecayMultiplierNormal(StatId.Followers, 0.5f, "decay:misfortune"), 1e-5f);
        Assert.AreEqual(1f, GiftedV2Effects.DecayMultiplierNormal(StatId.Offerings, 3f, "decay:misfortune"));
    }

    [Test]
    public void AdultEventBonus_NeedsAdultOption()
    {
        Own("v2n034");
        var adultId = AdultEvents.All[0].Id;
        using (GiftedV2Effects.During(ChangeSource.Event, adultId))
            Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, 10));
        GiftedV2Effects.AdultEnabled = () => true;
        using (GiftedV2Effects.During(ChangeSource.Event, adultId))
            Assert.AreEqual(1.1f, GiftedV2Effects.StatMultiplier(StatId.Offerings, 10), 1e-5f);
    }

    [Test]
    public void UnownedHooks_AreNoOp()
    {
        Assert.IsFalse(GiftedV2Effects.MisfortuneGainBlockedNormal());
        Assert.AreEqual(1f, GiftedV2Effects.DecayMultiplierNormal(StatId.Followers, 1f, "k"));
        Assert.AreEqual(0, GiftedV2Effects.DoctrinePointBonusNormal(20));
        Assert.IsFalse(GiftedV2Effects.GiftedBonusRollNormal());
        var q = new Question("q", new Choice("a", new Outcome("o")));
        Assert.AreSame(q, GiftedV2Effects.WithRescueChoiceNormal(q));
    }
}
