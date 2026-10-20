using System.IO;
using NUnit.Framework;

// 거울 세계 자동 플레이 집계: 스킬 타입별 150판씩, 결과를 Temp/mirror_sim.txt에 남긴다 (실행은 수동 테스트)
public class MirrorSimTests
{
    [Test, Explicit("수동 집계용: 수백 판을 돌려 오래 걸린다")]
    public void MirrorStyleAverages()
    {
        var sb = new System.Text.StringBuilder();
        // 욕망/미덕/중립(번갈아) 타입마다 봇 3명 × 100판 = 300판, 총 900판
        foreach (BalanceSimulation.MirrorStyle style in new[] { BalanceSimulation.MirrorStyle.Desire, BalanceSimulation.MirrorStyle.Virtue, BalanceSimulation.MirrorStyle.Balanced })
            for (int bot = 0; bot < 3; bot++)
                sb.Append(BalanceSimulation.RunMirror(style, 100, 20261020 + bot * 101 + (int)style * 1000));
        File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath, "..", "Temp", "mirror_sim.txt"), sb.ToString());
        Assert.Pass();
    }
}
