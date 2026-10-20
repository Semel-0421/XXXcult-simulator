using System.IO;
using NUnit.Framework;

// 준비된 신 자동 플레이 집계: 욕망형/미덕형 각 150판(봇 5명 × 30판), 결과를 Temp/god_sim.txt에 남긴다 (수동 실행)
public class GodStyleSimTests
{
    [Test, Explicit("수동 집계용: 수백 판을 돌려 오래 걸린다")]
    public void GodStyleAverages()
    {
        var desire = GodSimulation.Run(5, 30, 20261020, GodSimulation.Strategy.Rush, new[] { GodBranch.Desire, GodBranch.Miracle }).Format();
        var virtue = GodSimulation.Run(5, 30, 20261021, GodSimulation.Strategy.Rush, new[] { GodBranch.Mercy, GodBranch.Prophecy }).Format();
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "Temp", "god_sim.txt"),
            "[욕망형] 욕망·기적 가지\n" + desire + "\n[미덕형] 자비·예언 가지\n" + virtue);
        Assert.Pass();
    }
}
