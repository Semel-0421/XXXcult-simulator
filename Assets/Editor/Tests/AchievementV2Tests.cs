using System.Linq;
using NUnit.Framework;

public class AchievementV2Tests
{
    static int Count(string mode) => AchievementCatalogV2.All.Count(a => a.Mode == mode);

    [Test]
    public void Catalog_Sizes()
    {
        Assert.AreEqual(121, Count("normal"));
        Assert.AreEqual(93, Count("god"));
        Assert.AreEqual(119, Count("mirror"));
        Assert.AreEqual(15, AchievementCatalogV2.All.Count(a => a.Hidden));
        Assert.AreEqual(15, Count("hidden"));
        Assert.AreEqual(6, AchievementCatalogV2.All.Count(a => a.Adult));
    }

    [Test]
    public void Ids_AreUniqueAcrossAllCatalogs()
    {
        var ids = Achievements.All.Select(a => a.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }

    [Test]
    public void EveryCondition_Parses()
    {
        foreach (var a in AchievementCatalogV2.All)
            Assert.DoesNotThrow(() => AchievementCondition.Parse(a.ConditionText), a.Id + ": " + a.ConditionText);
    }

    [Test]
    public void HiddenAchievements_HaveNoReward()
    {
        Assert.IsTrue(AchievementCatalogV2.All.Where(a => a.Hidden).All(a => a.Reward.Kind == RewardKind.None));
    }

    [Test]
    public void NewCounterTokens_ReadCounters()
    {
        Counters.ClearAll();
        var cond = AchievementCondition.Parse("NEW:v2test_a>=3 v2test_b<=1 v2test_c==2 v2test_flag");
        Assert.IsFalse(cond.IsMet());
        Counters.Set("v2test_a", 3); Counters.Set("v2test_c", 2); Counters.Set("v2test_flag", 1);
        Assert.IsFalse(cond.IsMet()); // 아직 세지 않은 카운터의 <= 조건은 미달성
        Counters.Set("v2test_b", 0);
        Assert.IsTrue(cond.IsMet());
        Counters.Set("v2test_b", 2);
        Assert.IsFalse(cond.IsMet());
        Counters.ClearAll();
    }

    [Test]
    public void HoldToken_UsesHoldCounter()
    {
        Counters.ClearAll();
        var cond = AchievementCondition.Parse("NEW:v2test_Envy<=10 for 10y");
        Assert.IsFalse(cond.IsMet());
        Counters.Set("hold:v2test_Envy<=10", 10);
        Assert.IsTrue(cond.IsMet());
        Counters.ClearAll();
    }

    [Test]
    public void ProfileToken_FallsBackToCounters()
    {
        Counters.ClearAll();
        var cond = AchievementCondition.Parse("profile:v2test_runs>=2");
        Assert.IsFalse(cond.IsMet());
        Counters.Add("v2test_runs", 2);
        Assert.IsTrue(cond.IsMet());
        Counters.ClearAll();
    }

    [Test]
    public void Tabs_SplitByMode_HiddenTabEmptyUntilUnlocked()
    {
        Assert.AreEqual(93, AchievementTabs.Of(AchievementTabs.Tab.God).Count());
        Assert.AreEqual(119, AchievementTabs.Of(AchievementTabs.Tab.Mirror).Count());
        Assert.IsFalse(AchievementTabs.Of(AchievementTabs.Tab.Normal).Any(a => a.Hidden));
        Assert.AreEqual(0, AchievementTabs.Of(AchievementTabs.Tab.Hidden).Count());
    }
}
