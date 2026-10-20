using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class GiftedSpecialTests
{
    [Test]
    public void Catalog_Has140UniqueGiftedByCategory()
    {
        Assert.AreEqual(140, GiftedCatalog.All.Count);
        Assert.AreEqual(140, GiftedCatalog.All.Select(g => g.Name).Distinct().Count());
        var counts = GiftedCatalog.All.GroupBy(g => g.Category).ToDictionary(g => g.Key, g => g.Count());
        Assert.AreEqual(20, counts["운명"]);
        Assert.AreEqual(30, counts["성격"]);
        Assert.AreEqual(50, counts["특기"]);
        Assert.AreEqual(20, counts["외모"]);
        Assert.AreEqual(20, counts["업적"]);
        Assert.Greater(GiftedCatalog.All.Count(g => g.Specials.Length > 0), 20);
    }

    [Test]
    public void ChanceSpecial_RaisesMatchingActionOnly()
    {
        var gifted = new TalentList();
        GiftedGranter.Grant(gifted, "no.3"); // 웅변가: 설교 +10, 포교 +5
        Assert.AreEqual(0.10f, GiftedSpecials.ChanceBonus(gifted, "Preach"), 1e-4);
        Assert.AreEqual(0.05f, GiftedSpecials.ChanceBonus(gifted, "Proselytize"), 1e-4);
        Assert.AreEqual(0f, GiftedSpecials.ChanceBonus(gifted, "Serve"), 1e-4);
    }

    [Test]
    public void FailHalf_SoftensFailedAction()
    {
        var personal = new PersonalStatus();
        GiftedGranter.Grant(personal.Talents, Find("신중함").Id); // failhalf:All
        personal[PersonalStatId.Health].Set(10);
        LifeActions.Apply(personal, LifeAction.Exercise, success: false); // -2 -> -1
        Assert.AreEqual(9, personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void YearlySpecial_AppliesOnNewYear()
    {
        GameSession.StartNew(1);
        GiftedGranter.Grant(GameSession.Personal.Talents, Find("장수의 혈통").Id); // year:Health+2
        for (int i = 0; i < 4; i++) GameSession.AdvanceTurn();
        Assert.AreEqual(2, GameSession.Personal[PersonalStatId.Health].Value);
    }

    static Talent Find(string name) => GiftedCatalog.All.First(g => g.Name == name);
}

public class SocialStandingTests
{
    [Test]
    public void Tiers_FollowReputation()
    {
        Assert.AreEqual(Standing.Dissolved, SocialStanding.Of(0));
        Assert.AreEqual(Standing.Persecuted, SocialStanding.Of(100));
        Assert.AreEqual(Standing.Watched, SocialStanding.Of(250));
        Assert.AreEqual(Standing.Neutral, SocialStanding.Of(CultStatus.StartingReputation));
        Assert.AreEqual(Standing.Respected, SocialStanding.Of(750));
        Assert.AreEqual(Standing.Recognized, SocialStanding.Of(950));
    }

    [Test]
    public void LowStanding_DrainsFollowers_HighStandingDecaysReputation()
    {
        // 신도 증감은 턴이 아니라 해마다: 기본 +2, 명망 400 기준 100마다 ±2 (0 구간 없음)
        var cult = new CultStatus();
        cult[StatId.Followers].Set(10);
        cult[StatId.Reputation].Set(100);
        SocialStanding.ApplyTurn(cult);
        Assert.AreEqual(10, cult[StatId.Followers].Value, "턴마다는 신도가 바뀌지 않는다");
        SocialStanding.YearEnd(cult);
        Assert.AreEqual(6, cult[StatId.Followers].Value, "명망 100: 기본 2 + (-3단계 × 2) = -4");

        cult[StatId.Reputation].Set(950);
        SocialStanding.ApplyTurn(cult);
        Assert.AreEqual(947, cult[StatId.Reputation].Value);
        Assert.AreEqual(10, SocialStanding.Recruits(947), "명망 947: 기본 2 + 6단계×2 = 14 → 최대 10");
    }
}
