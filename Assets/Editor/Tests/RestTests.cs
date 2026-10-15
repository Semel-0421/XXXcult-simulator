using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class RestTests
{
    static int Raised(PersonalStatus p) => Rest.UpdateRaised(p, 0);

    [Test]
    public void ZeroStats_OnlyCountsRaisedVisibleStatsAtZero()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Health].Set(5);
        p[PersonalStatId.Piety].Set(4);
        var mask = Raised(p);
        p[PersonalStatId.Health].Set(0);   // 올랐다가 0으로 떨어짐 -> 대상
        p[PersonalStatId.Piety].Set(2);    // 0 아님
        // Mentality는 한 번도 오르지 않은 0 -> 대상 아님
        p[PersonalStatId.Miracle].Set(3);
        mask = Rest.UpdateRaised(p, mask);
        p[PersonalStatId.Miracle].Set(0);  // 숨겨진 스탯 -> 제외

        var zero = Rest.ZeroStats(p, mask);
        CollectionAssert.AreEqual(new[] { PersonalStatId.Health }, zero);
    }

    [Test]
    public void ApplyTurn_RestingGainsThree_OthersLoseOne_FourTurnsIsOneYear()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Health].Set(0);
        p[PersonalStatId.Mentality].Set(10);
        p[PersonalStatId.Piety].Set(0);
        var resting = new List<PersonalStatId> { PersonalStatId.Health };

        for (int t = 0; t < Rest.Turns; t++) Rest.ApplyTurn(p, new CultStatus(), resting);

        Assert.AreEqual(Rest.Recover * Rest.Turns, p[PersonalStatId.Health].Value);
        Assert.AreEqual(10 - Rest.Drain * Rest.Turns, p[PersonalStatId.Mentality].Value);
        Assert.AreEqual(0, p[PersonalStatId.Piety].Value, "이미 0인 스탯은 더 내려가지 않는다");
    }

    [Test]
    public void SimultaneousZeroStats_RestTogether()
    {
        var p = new PersonalStatus();
        foreach (var id in new[] { PersonalStatId.Health, PersonalStatId.Mentality, PersonalStatId.Piety }) p[id].Set(6);
        var mask = Raised(p);
        p[PersonalStatId.Health].Set(0);
        p[PersonalStatId.Mentality].Set(0);

        var resting = Rest.ZeroStats(p, mask);
        Assert.AreEqual(2, resting.Count);
        Rest.ApplyTurn(p, new CultStatus(), resting);
        Assert.AreEqual(3, p[PersonalStatId.Health].Value);
        Assert.AreEqual(3, p[PersonalStatId.Mentality].Value);
        Assert.AreEqual(5, p[PersonalStatId.Piety].Value);
    }

    [Test]
    public void StatThatHitsZeroDuringRest_TriggersAnotherRest_UntilNoneAreZero()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Health].Set(6);
        p[PersonalStatId.Mentality].Set(6);
        p[PersonalStatId.Piety].Set(2);   // 첫 휴식 두 번째 분기에 0이 된다
        var mask = Raised(p);
        p[PersonalStatId.Health].Set(0);

        int rests = 0;
        while (true)
        {
            var resting = Rest.ZeroStats(p, mask);
            if (resting.Count == 0) break;
            rests++;
            Assert.Less(rests, 20, "휴식이 끝나지 않는다");
            for (int t = 0; t < Rest.Turns; t++) Rest.ApplyTurn(p, new CultStatus(), resting);
        }

        // 체력 휴식 중 신앙심이 0이 되어 한 번 더, 그 휴식 중 정신력이 0이 되어 또 한 번 더
        Assert.AreEqual(3, rests, "휴식 도중 0이 된 스탯 때문에 다시 휴식");
        Assert.IsTrue(Enumerable.Range(0, 14).Select(i => (PersonalStatId)i)
            .Where(id => Rest.InScope(id) && (mask & (1 << (int)id)) != 0)
            .All(id => p[id].Value > 0));
    }

    [Test]
    public void EveryCultAction_CanRaiseSomePersonalStat_OnSuccessAndFailure()
    {
        foreach (CultAction action in System.Enum.GetValues(typeof(CultAction)))
            foreach (var success in new[] { true, false })
                Assert.IsTrue(CultActions.PersonalEffectsOf(action, success).Any(e => e.Amount > 0), $"{action} {(success ? "성공" : "실패")}");
    }

    [Test]
    public void VirtueAndHiddenStats_AreOutsideRestScope()
    {
        var p = new PersonalStatus();
        var virtues = new[] { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };
        foreach (var id in virtues) p[id].Set(5);
        p[PersonalStatId.Miracle].Set(5);
        p[PersonalStatId.Health].Set(5);
        var mask = Raised(p);
        foreach (var id in virtues) p[id].Set(0);
        p[PersonalStatId.Miracle].Set(0);

        Assert.AreEqual(0, Rest.ZeroStats(p, mask).Count, "덕목류·히든은 0이어도 휴식하지 않는다");

        // 욕망류도 휴식 범위 밖
        p[PersonalStatId.Lust].Set(5);
        mask = Rest.UpdateRaised(p, mask);
        p[PersonalStatId.Lust].Set(0);
        Assert.AreEqual(0, Rest.ZeroStats(p, mask).Count, "욕망류도 휴식하지 않는다");

        // 휴식 중에도 덕목류와 히든은 깎이지 않는다
        foreach (var id in virtues) p[id].Set(9);
        p[PersonalStatId.Miracle].Set(9);
        Rest.ApplyTurn(p, new CultStatus(), new List<PersonalStatId>());
        foreach (var id in virtues) Assert.AreEqual(9, p[id].Value, id.ToString());
        Assert.AreEqual(9, p[PersonalStatId.Miracle].Value);
        Assert.AreEqual(4, p[PersonalStatId.Health].Value, "건강군은 깎인다");
    }

    [Test]
    public void RestTurn_AlsoDrainsVisibleCultStats()
    {
        var p = new PersonalStatus();
        var cult = new CultStatus();
        foreach (StatId id in System.Enum.GetValues(typeof(StatId))) cult[id].Set(10);
        Rest.ApplyTurn(p, cult, new List<PersonalStatId>());
        foreach (StatId id in System.Enum.GetValues(typeof(StatId)))
        {
            var expected = CultStatus.IsHidden(id) ? 10 : 10 - Rest.CultDrain;
            Assert.AreEqual(expected, cult[id].Value, id.ToString());
        }
    }

    [Test]
    public void EveryCultAction_CanRaiseHealthGroupStat_OnSuccessAndFailure()
    {
        var group = new[] { PersonalStatId.Health, PersonalStatId.Mentality, PersonalStatId.Piety, PersonalStatId.Repute, PersonalStatId.Wealth };
        foreach (CultAction action in System.Enum.GetValues(typeof(CultAction)))
            foreach (var success in new[] { true, false })
                Assert.IsTrue(CultActions.PersonalEffectsOf(action, success).Any(e => e.Amount > 0 && System.Array.IndexOf(group, e.Stat) >= 0),
                    $"{action} {(success ? "성공" : "실패")}");
    }

    [Test]
    public void PersonalRaisedMask_SurvivesSaveAndLoad()
    {
        GameSession.StartNew(8);
        GameSession.Personal[PersonalStatId.Health].Set(7);
        GameSession.MergePersonalRaised(Rest.UpdateRaised(GameSession.Personal, 0));
        var json = UnityEngine.JsonUtility.ToJson(GameSession.Capture());
        GameSession.StartNew(1);
        Assert.AreEqual(0, GameSession.PersonalRaisedMask);
        GameSession.Restore(UnityEngine.JsonUtility.FromJson<SaveData>(json));
        Assert.AreNotEqual(0, GameSession.PersonalRaisedMask & (1 << (int)PersonalStatId.Health));
    }
}
