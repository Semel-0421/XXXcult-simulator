using System.IO;
using NUnit.Framework;

public class ProgressTests
{
    string tempProfile, previousPath;
    bool previousEnabled;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        tempProfile = Path.Combine(Path.GetTempPath(), "xxxcult_progress_test_" + System.Guid.NewGuid().ToString("N") + ".json");
        Profile.Path = tempProfile;
        Profile.Enabled = true;
    }

    [TearDown]
    public void TearDown()
    {
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        if (File.Exists(tempProfile)) File.Delete(tempProfile);
        GameSession.StartNew(1);
    }

    [Test]
    public void Mortality_FollowsAgeAndSex()
    {
        Assert.Less(Mortality.AnnualRate(10, Gender.Male), Mortality.AnnualRate(0, Gender.Male), "영아기가 아동기보다 높다");
        Assert.Less(Mortality.AnnualRate(30, Gender.Male), Mortality.AnnualRate(60, Gender.Male));
        Assert.Less(Mortality.AnnualRate(60, Gender.Male), Mortality.AnnualRate(80, Gender.Male));
        Assert.Less(Mortality.AnnualRate(50, Gender.Female), Mortality.AnnualRate(50, Gender.Male), "같은 나이면 여자가 낮다");
        // 50~54세 남자: 5년 사망확률 0.018712 → 1년 약 0.38%
        Assert.AreEqual(0.0038, Mortality.AnnualRate(52, Gender.Male), 0.0002);
        // 분기 확률 4번 = 1년 확률
        var quarter = Mortality.QuarterRate(52, Gender.Male);
        Assert.AreEqual(Mortality.AnnualRate(52, Gender.Male), 1 - System.Math.Pow(1 - quarter, 4), 1e-9);
    }

    [Test]
    public void Death_IsAForcedEnding_BeforeAndAfterFounding()
    {
        GameSession.StartNew(3);
        Assert.IsNull(Progress.ForcedEnding());
        for (int i = 0; i < 2000 && !GameSession.Dead; i++) GameSession.AdvanceTurn(); // 언젠가는 사망
        Assert.IsTrue(GameSession.Dead);
        Assert.AreEqual(Endings.DeathYoung, Progress.ForcedEnding().Id);
        GameSession.FoundCult("시험교단");
        Assert.AreEqual(Endings.DeathLeader, Progress.ForcedEnding().Id);
    }

    [Test]
    public void Run_IsEndingsSeenPlusOne()
    {
        Assert.AreEqual(1, Progress.NextRun);
        Endings.Reach(Endings.Dissolved);
        Endings.Reach(Endings.Dissolved);
        Endings.Reach(Endings.DeathLeader);
        Assert.AreEqual(3, Progress.CompletedRuns);
        Assert.AreEqual(4, Progress.NextRun);
        Assert.AreEqual(3, Legacy.LevelFor(Progress.CompletedRuns, 0), "엔딩마다 계승 1단계");
    }

    [Test]
    public void FirstRun_LocksFinalSkill_AndLaterRunsUnlockIt()
    {
        var final = DoctrineCatalog.At(DoctrineBranch.Charity, 10, "A");
        GameSession.StartNew(4);
        Progress.BeginRun(1);
        Assert.IsTrue(GameSession.Cult.Doctrines.IsFinalLocked(final));
        Assert.IsFalse(GameSession.Cult.Doctrines.CanLearn(final, 999));

        GameSession.StartNew(4);
        Progress.BeginRun(2);
        Assert.IsTrue(GameSession.Cult.Doctrines.IsFinalLocked(final), "2회차까지 잠김");

        GameSession.StartNew(4);
        Progress.BeginRun(Progress.FinalSkillRun);
        Assert.IsFalse(GameSession.Cult.Doctrines.IsFinalLocked(final));
    }

    [Test]
    public void Threshold_StepsDownToSeventyFive_ByRunFive()
    {
        Assert.AreEqual(95, Progress.BranchThreshold(3));
        Assert.AreEqual(90, Progress.BranchThreshold(4));
        Assert.AreEqual(75, Progress.BranchThreshold(5));
        Assert.AreEqual(75, Progress.BranchThreshold(9));
        Assert.AreEqual(Endings.BranchStatAbove, Progress.BranchThreshold(0), "회차가 없으면 기본 문턱");

        var p = new PersonalStatus();
        p[PersonalStatId.Charity].Set(83);
        Assert.IsNull(Endings.Decide(p, DoctrineBranch.Charity, Progress.BranchThreshold(4)));
        Assert.IsNotNull(Endings.Decide(p, DoctrineBranch.Charity, Progress.BranchThreshold(5)));
    }

    [Test]
    public void TraitBonus_GrowsPerCompletedRun_UpToCap()
    {
        GameSession.StartNew(5);
        Progress.BeginRun(1);
        Assert.AreEqual(0, GameSession.Personal[PersonalStatId.Charity].Value);

        GameSession.StartNew(5);
        Progress.BeginRun(3);
        Assert.AreEqual(2 * Progress.TraitBonusPerRun, GameSession.Personal[PersonalStatId.Charity].Value);
        Assert.AreEqual(2 * Progress.TraitBonusPerRun, GameSession.Personal[PersonalStatId.Lust].Value);
        Assert.AreEqual(0, GameSession.Personal[PersonalStatId.Health].Value, "건강류는 그대로");

        Assert.AreEqual(Progress.MaxTraitBonus, Progress.TraitBonus(20));
    }

    [Test]
    public void Run_SurvivesSaveAndLoad()
    {
        GameSession.StartNew(6);
        Progress.BeginRun(1);
        var json = UnityEngine.JsonUtility.ToJson(GameSession.Capture());
        GameSession.StartNew(1);
        GameSession.Restore(UnityEngine.JsonUtility.FromJson<SaveData>(json));
        Assert.AreEqual(1, GameSession.Run);
        Assert.IsTrue(GameSession.Cult.Doctrines.FinalLocked);
    }
}
