using System.Linq;
using NUnit.Framework;

public class AdultEventTests
{
    [Test]
    public void AdultEvents_HaveOwnIds_AndADeclineChoice()
    {
        var ids = AdultEvents.All.Select(e => e.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
        // UsedEvents를 본편·신 모드와 같이 쓰므로 id가 겹치면 안 된다
        Assert.IsFalse(ids.Intersect(CultEvents.All.Concat(GodEvents.All).Select(e => e.Id)).Any());
        // 모든 성인 이벤트에는 거절·자제(확정 성공) 선택지가 있다
        Assert.IsTrue(AdultEvents.All.All(e => e.Question.Choices.Any(c => c.Chance >= 1f)));
    }

    [Test]
    public void EachModeHasItsOwnThirtyAdultEvents()
    {
        Assert.AreEqual(30, AdultEvents.NormalOnly.Length);
        Assert.AreEqual(30, AdultEvents.GodOnly.Length);
        // 모드 전용 이벤트는 다른 모드 풀에 섞이지 않는다
        Assert.IsFalse(AdultEvents.Pool(false).Intersect(AdultEvents.GodOnly).Any());
        Assert.IsFalse(AdultEvents.Pool(true).Intersect(AdultEvents.NormalOnly).Any());
        Assert.IsTrue(AdultEvents.Shared.All(e => AdultEvents.Pool(false).Contains(e) && AdultEvents.Pool(true).Contains(e)));
        // 모두 반복 등장하는 확률 이벤트
        Assert.IsTrue(AdultEvents.All.All(e => e.Kind == CultEvent.EventKind.Random));
    }

    [Test]
    public void ActiveGodMode_RollsOnlyGodPoolAdultEvents()
    {
        int savedVerified = UnityEngine.PlayerPrefs.GetInt("adult_verified", 0), savedContent = UnityEngine.PlayerPrefs.GetInt("adult_content", 1);
        try
        {
            UnityEngine.PlayerPrefs.SetInt("adult_verified", 1);
            UnityEngine.PlayerPrefs.SetInt("adult_content", 1);
            GodRules.Begin(3);
            Assert.IsTrue(AdultGate.Active);
            var normalOnly = AdultEvents.NormalOnly.Select(e => e.Id).ToHashSet();
            var seenGodOnly = false;
            for (int i = 0; i < 2000; i++)
            {
                var next = Turns.NextCultEvent();
                if (next == null) continue;
                Assert.IsFalse(normalOnly.Contains(next.Id), next.Id);
                seenGodOnly |= AdultEvents.GodOnly.Contains(next);
            }
            Assert.IsTrue(seenGodOnly);
        }
        finally
        {
            UnityEngine.PlayerPrefs.SetInt("adult_verified", savedVerified);
            UnityEngine.PlayerPrefs.SetInt("adult_content", savedContent);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
