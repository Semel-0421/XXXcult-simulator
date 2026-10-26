using System.Linq;
using NUnit.Framework;

public class EyeEventSectorTests
{
    [TestCase(EyeSector.Yahweh)]
    [TestCase(EyeSector.Satan)]
    [TestCase(EyeSector.Samael)]
    [TestCase(EyeSector.Yaldabaoth)]
    public void EachSector_HasTenEvents(EyeSector sector)
    {
        // SYS_DEMIURGE 8단계: 섹터마다 확률 이벤트 10 + 그노시스 단계 진입 5 + 찬탈 직전 1
        Assert.AreEqual(10, EyeEvents.All.Count(e => !e.IsConditional && EyeRules.SectorOf(e) == sector));
        Assert.AreEqual(16, EyeEvents.All.Count(e => EyeRules.SectorOf(e) == sector));
    }

    [Test]
    public void GodAndMirrorEvents_HaveNoSector()
    {
        Assert.IsTrue(GodEvents.All.Concat(MirrorEvents.All).All(e => EyeRules.SectorOf(e) == EyeSector.None));
    }
}
