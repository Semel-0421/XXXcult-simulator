using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

// 신 모드 v2 기프티드 효과 (v2g041~080)
public class GiftedV2GodTests
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
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_gv2g_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Profile.Path = Path.Combine(tempDir, "profile.json");
        Profile.Enabled = true;
        Counters.ClearAll();
        GiftedV2Effects.AdultEnabled = () => false;
        GiftedV2Effects.ResetForTests();
        GodRules.Begin(1);
        GameSession.FoundCult("t");
    }

    [TearDown]
    public void TearDown()
    {
        GiftedV2Effects.AdultEnabled = previousAdult;
        GiftedV2Effects.ResetForTests();
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        Counters.ClearAll();
        GameSession.StartNew(1);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    static void Own(params string[] ids) { foreach (var id in ids) Profile.MarkUnlocked(GiftedV2Catalog.Find(id).LinkedAchievement); }

    [Test]
    public void EveryGodEntryIsRegistered()
    {
        foreach (var e in GiftedV2Catalog.All.Where(e => e.Mode == "god"))
            Assert.IsTrue(GiftedV2Effects.IsRegistered(e.Id), e.Id);
    }

    [Test]
    public void Wonder_AndCommunion_AddSuccessChance_ForOwnAction()
    {
        Own("v2g051", "v2g055");
        Assert.AreEqual(GiftedV2Table.WonderBonus, GiftedV2Effects.SuccessChanceBonus(new CheckContext(CheckKind.Cult, "Proselytize")), 1e-5f);
        Assert.AreEqual(GiftedV2Table.CommuneBonus, GiftedV2Effects.SuccessChanceBonus(new CheckContext(CheckKind.Cult, "CultPray")), 1e-5f);
        Assert.AreEqual(0f, GiftedV2Effects.SuccessChanceBonus(new CheckContext(CheckKind.Cult, "Preach")));
    }

    [Test]
    public void Revelation_BoostsOnlyPreachGains()
    {
        Own("v2g052");
        using (GiftedV2Effects.During(ChangeSource.Action, "Preach"))
        {
            Assert.AreEqual(1.15f, GiftedV2Effects.StatMultiplier(StatId.FollowerFaith, 20), 1e-5f);
            Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.FollowerFaith, -10), 1e-5f);
        }
        using (GiftedV2Effects.During(ChangeSource.Action, "Serve"))
            Assert.AreEqual(1f, GiftedV2Effects.StatMultiplier(StatId.FollowerFaith, 20), 1e-5f);
    }

    [Test]
    public void Heat_EnvyAndSuspicionGain_AreCut()
    {
        var p = GameSession.God.Pressure;
        p.AddHeat(10, 10);
        Assert.AreEqual(10f, p.Envy, 1e-4f);
        Own("v2g062", "v2g063");
        p.Restore(0, 0);
        p.AddHeat(10, 10);
        Assert.AreEqual(8f, p.Envy, 1e-4f);
        Assert.AreEqual(8f, p.Suspicion, 1e-4f);
        p.AddHeat(-3, -3); // 감소는 그대로
        Assert.AreEqual(5f, p.Envy, 1e-4f);
    }

    [Test]
    public void IndulgenceEvent_PressureCut_OnlyForIndulgenceIds()
    {
        Assert.AreEqual(1f, GiftedV2GodHooks.EventHeatScale("lu01"));
        Own("v2g073");
        Assert.AreEqual(0.75f, GiftedV2GodHooks.EventHeatScale("lu01"), 1e-5f);
        Assert.AreEqual(1f, GiftedV2GodHooks.EventHeatScale("hv01"));
    }

    [Test]
    public void BuildingCost_AndFloor_FollowOwnership()
    {
        var cost = GodBuildings.CostOf(2);
        var floor = GodBuildings.Floor(GodBuildingType.Statue, 3);
        Assert.AreEqual(100, cost);
        Own("v2g056", "v2g078", "v2g057");
        Assert.AreEqual(75, GodBuildings.CostOf(2)); // 100 x (1 - 0.15 - 0.10)
        Assert.Greater(GodBuildings.Floor(GodBuildingType.Statue, 3), floor);
    }

    [Test]
    public void Ember_GivesBonusAfterThreeFailures_ThenResets()
    {
        Own("v2g043");
        var c = new CheckContext(CheckKind.Cult, "Preach");
        for (int i = 0; i < GiftedV2Table.EmberFails; i++)
        {
            Assert.AreEqual(0f, GiftedV2Effects.SuccessChanceBonus(c));
            GiftedV2Effects.ResolveCheck(c, false);
        }
        Assert.AreEqual(GiftedV2Table.EmberBonus, GiftedV2Effects.SuccessChanceBonus(c), 1e-5f);
        GiftedV2Effects.ResolveCheck(c, false);
        Assert.AreEqual(0f, GiftedV2Effects.SuccessChanceBonus(c));
    }

    [Test]
    public void UnexpectedBoon_VoidsOnlyTheFirstFailure()
    {
        Own("v2g049");
        var c = new CheckContext(CheckKind.Life, "Study");
        Assert.IsTrue(GiftedV2Effects.ResolveCheck(c, false));
        Assert.IsFalse(GiftedV2Effects.ResolveCheck(c, false));
    }

    [Test]
    public void HeavensLedger_PaysOnePointPerNewEvent()
    {
        Own("v2g047");
        var before = GameSession.God.Points.Earned;
        var c = new CheckContext(CheckKind.Event, "hv01");
        GiftedV2Effects.ResolveCheck(c, true);
        GiftedV2Effects.ResolveCheck(c, false);
        Assert.AreEqual(before + 1, GameSession.God.Points.Earned);
        GiftedV2Effects.ResolveCheck(new CheckContext(CheckKind.Event, "ea01"), true);
        Assert.AreEqual(before + 2, GameSession.God.Points.Earned);
    }

    [Test]
    public void FinalRevelation_HalvesPressureOncePerRun()
    {
        var p = GameSession.God.Pressure;
        p.Restore(40, 20);
        GiftedV2GodHooks.BeforeAscension();
        Assert.AreEqual(40f, p.Envy);
        Own("v2g080");
        GiftedV2GodHooks.BeforeAscension();
        GiftedV2GodHooks.BeforeAscension();
        Assert.AreEqual(20f, p.Envy, 1e-4f);
        Assert.AreEqual(10f, p.Suspicion, 1e-4f);
    }

    [Test]
    public void GodQuarterCentury_AddsPoint()
    {
        Own("v2g077");
        GameSession.Age.Restore((GodRules.StartAgeYears + 25) * GameClock.QuartersPerYear);
        var before = GameSession.God.Points.Earned;
        GiftedV2Effects.OnYearEnd();
        Assert.AreEqual(before + 1, GameSession.God.Points.Earned);
    }

    [Test]
    public void NothingOwned_GodHooksAreNoOp()
    {
        Assert.AreEqual(1f, GiftedV2GodHooks.HeatGainScale(true));
        Assert.AreEqual(50, GodBuildings.CostOf(1));
        Assert.AreEqual(0, GiftedV2GodHooks.AscensionCostEase);
        Assert.IsFalse(GiftedV2GodHooks.ShowsChoiceChance);
        Assert.IsFalse(GiftedV2GodHooks.SuppressRepeatedEvent(new Random(1)));
    }
}
