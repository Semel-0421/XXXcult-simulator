using System.IO;
using System.Linq;
using NUnit.Framework;

public class AchievementTests
{
    string tempProfile, previousPath;
    bool previousEnabled;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        tempProfile = Path.Combine(Path.GetTempPath(), "xxxcult_profile_" + System.Guid.NewGuid().ToString("N") + ".json");
        Profile.Path = tempProfile;
        Profile.Enabled = true;
        GameSession.StartNew(1);
    }

    [TearDown]
    public void TearDown()
    {
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        if (File.Exists(tempProfile)) File.Delete(tempProfile);
    }

    [Test]
    public void Catalog_Has100Achievements_With20GiftedAnd10StatRewards()
    {
        var all = AchievementCatalog.All;
        Assert.AreEqual(100, all.Count);
        Assert.AreEqual(100, all.Select(a => a.Id).Distinct().Count());
        Assert.AreEqual(100, all.Select(a => a.Name).Distinct().Count());
        Assert.AreEqual(20, all.Count(a => a.Reward.Kind == RewardKind.Gifted));
        Assert.AreEqual(10, all.Count(a => a.Reward.Kind == RewardKind.Stat));
    }

    [Test]
    public void GiftedRewards_AreNewAchievementGifted_UsedOnce()
    {
        var ids = AchievementCatalog.All.Where(a => a.Reward.Kind == RewardKind.Gifted).Select(a => a.Reward.GiftedId).ToList();
        Assert.AreEqual(20, ids.Distinct().Count());
        foreach (var id in ids)
            Assert.AreEqual(GiftedGranter.RewardCategory, GiftedCatalog.Find(id).Category, id);
        Assert.AreEqual(20, GiftedCatalog.All.Count(g => g.Category == GiftedGranter.RewardCategory));
    }

    [Test]
    public void GrantRandom_NeverPicksAchievementGifted()
    {
        var talents = new TalentList();
        GiftedGranter.GrantRandom(talents, new System.Random(1), 500);
        Assert.IsFalse(talents.Owned.Any(g => g.Category == GiftedGranter.RewardCategory));
        Assert.AreEqual(120, talents.Owned.Count);
    }

    [Test]
    public void Condition_ParsesAndEvaluatesAgainstSession()
    {
        Assert.IsFalse(AchievementCondition.Parse("age>=3").IsMet());
        for (int i = 0; i < 12; i++) GameSession.AdvanceTurn();
        Assert.IsTrue(AchievementCondition.Parse("age>=3").IsMet());

        GameSession.Count("act_Preach", 20);
        Assert.IsTrue(AchievementCondition.Parse("c:act_Preach>=20").IsMet());
        Assert.IsFalse(AchievementCondition.Parse("c:act_Preach>=21").IsMet());

        Assert.IsFalse(AchievementCondition.Parse("standing>=Respected").IsMet()); // 창설 전에는 불가
        GameSession.FoundCult("별빛");
        GameSession.Cult[StatId.Reputation].Set(750);
        Assert.IsTrue(AchievementCondition.Parse("standing>=Respected").IsMet());
        Assert.Throws<System.ArgumentException>(() => AchievementCondition.Parse("nonsense>=3"));
    }

    [Test]
    public void Evaluate_UnlocksOnce_PersistsAndGrantsGifted()
    {
        var unlockedNames = new System.Collections.Generic.List<string>();
        void Listener(Achievement a) => unlockedNames.Add(a.Name);
        Achievements.Unlocked += Listener;
        try
        {
            GameSession.Count("act_Exercise", 10);
            Achievements.Evaluate();
            Achievements.Evaluate();
            Assert.AreEqual(1, unlockedNames.Count(n => n == "단련"));
            Assert.IsTrue(GameSession.Personal.Talents.Has("no.139"));

            Profile.Reset(); // 파일에서 다시 읽어도 남아 있어야 함
            Assert.IsTrue(Profile.IsUnlocked(AchievementCatalog.All.First(a => a.Name == "단련").Id));
        }
        finally { Achievements.Unlocked -= Listener; }
    }

    [Test]
    public void StartRewards_GrantGiftedAndStatBonus_OnNewRun()
    {
        Profile.MarkUnlocked(AchievementCatalog.All.First(a => a.Name == "단련").Id);       // 기프티드 no.139
        Profile.MarkUnlocked(AchievementCatalog.All.First(a => a.Name == "튼튼한 몸").Id);   // 체력 +5
        GameSession.StartNew(2);
        Achievements.ApplyStartRewards(GameSession.Personal);
        Assert.IsTrue(GameSession.Personal.Talents.Has("no.139"));
        Assert.AreEqual(5, GameSession.Personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void RewardsOff_SkipsGiftedAndStatBonus_ButStillUnlocks()
    {
        Profile.RewardsEnabled = false;
        GameSession.Count("act_Exercise", 10);
        Achievements.Evaluate();
        Assert.IsTrue(Profile.IsUnlocked(AchievementCatalog.All.First(a => a.Name == "단련").Id));
        Assert.IsFalse(GameSession.Personal.Talents.Has("no.139"));

        Profile.MarkUnlocked(AchievementCatalog.All.First(a => a.Name == "튼튼한 몸").Id);
        GameSession.StartNew(2);
        Achievements.ApplyStartRewards(GameSession.Personal);
        Assert.AreEqual(0, GameSession.Personal[PersonalStatId.Health].Value);
        Assert.IsFalse(GameSession.Personal.Talents.Has("no.139"));

        Profile.Reset(); // 설정이 파일에 남는다
        Assert.IsFalse(Profile.RewardsEnabled);
        Profile.RewardsEnabled = true;
        Achievements.ApplyStartRewards(GameSession.Personal);
        Assert.IsTrue(GameSession.Personal.Talents.Has("no.139"));
    }

    [Test]
    public void Counters_SurviveSaveAndLoad()
    {
        GameSession.Count("events", 7);
        var data = GameSession.Capture();
        GameSession.StartNew(5);
        Assert.AreEqual(0, GameSession.CountOf("events"));
        GameSession.Restore(data);
        Assert.AreEqual(7, GameSession.CountOf("events"));
    }

    [Test]
    public void EnabledOff_DoesNotTouchProfile()
    {
        Profile.Enabled = false;
        GameSession.Count("act_Exercise", 50);
        Achievements.Evaluate();
        Profile.Count("runs");
        Assert.AreEqual(0, Profile.UnlockedCount);
        Assert.IsFalse(File.Exists(tempProfile));
    }
}

public class AudioTests
{
    [Test]
    public void MusicKeys_FollowStandingTier()
    {
        Assert.AreEqual("tier_Persecuted", MusicKeys.ForStanding(Standing.Persecuted));
        Assert.AreEqual("tier_Recognized", MusicKeys.ForStanding(Standing.Recognized));
        Assert.AreEqual(new[] { "tier_Dissolved", "tier_Persecuted" }, MusicKeys.Candidates("tier_Dissolved"));
        Assert.AreEqual(new[] { "menu" }, MusicKeys.Candidates(MusicKeys.Menu));
    }

    [Test]
    public void MenuDepth_NeverGoesNegative_AndNests()
    {
        AudioManager.MenuClosed();
        Assert.IsFalse(AudioManager.MenuOpen);
        AudioManager.MenuOpened();
        AudioManager.MenuOpened();
        AudioManager.MenuClosed();
        Assert.IsTrue(AudioManager.MenuOpen);
        AudioManager.MenuClosed();
        Assert.IsFalse(AudioManager.MenuOpen);
    }
}
