using NUnit.Framework;

// 황금의 눈동자 시뮬레이션 변형(EyeVariants)이 튜닝값을 원복하는지 검증 (2026-10-26 P12)
public class EyeVariantsTests
{
    [Test]
    public void Run_RestoresTunablesAndLeavesNormalMode()
    {
        var calamityThreshold = EyeRules.CalamityThreshold;
        var goodOffset = EyeRules.GoodOffset;
        var gnosisDoctrine = EyeRules.GnosisDoctrine;
        var gnosisCost = AeonSkill.GnosisCost;
        var ascensionCost = AeonSkill.AscensionCost;
        var years = EyeSimulation.MaxYears;
        EyeSimulation.MaxYears = 1;
        try
        {
            var report = EyeVariants.Run(1, 7);
            StringAssert.Contains("V5", report);
        }
        finally
        {
            EyeSimulation.MaxYears = years;
        }
        Assert.AreEqual(calamityThreshold, EyeRules.CalamityThreshold);
        Assert.AreEqual(goodOffset, EyeRules.GoodOffset);
        Assert.AreEqual(gnosisDoctrine, EyeRules.GnosisDoctrine);
        Assert.AreEqual(gnosisCost, AeonSkill.GnosisCost);
        Assert.AreEqual(ascensionCost, AeonSkill.AscensionCost);
        Assert.IsFalse(GameSession.EyeMode);
    }
}
