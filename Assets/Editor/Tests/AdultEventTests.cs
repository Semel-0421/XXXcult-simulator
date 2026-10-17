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
}
