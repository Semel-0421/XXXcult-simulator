using System.Linq;
using NUnit.Framework;

public class MisfortuneTests
{
    [Test]
    public void NoStacks_NoDecay()
    {
        var cult = new CultStatus();
        cult[StatId.FollowerFaith].Set(300);
        Misfortune.ApplyTurn(cult, 0, 0f);
        Assert.AreEqual(300, cult[StatId.FollowerFaith].Value);
        Assert.AreEqual(CultStatus.StartingReputation, cult[StatId.Reputation].Value);
    }

    [Test]
    public void Stacks_DecayEveryVisibleCultStat_Proportionally()
    {
        var cult = new CultStatus();
        foreach (var id in new[] { StatId.LeaderFaith, StatId.FollowerFaith, StatId.Authority, StatId.Offerings, StatId.Doctrine })
            cult[id].Set(500);
        cult[StatId.Followers].Set(100);

        Misfortune.ApplyTurn(cult, 2, 0f);
        Assert.AreEqual(CultStatus.StartingReputation - 6, cult[StatId.Reputation].Value);
        Assert.AreEqual(495, cult[StatId.FollowerFaith].Value); // -8을 교리 8레벨(40%)이 막아 -4.8
        Assert.AreEqual(494, cult[StatId.LeaderFaith].Value);
        Assert.AreEqual(99, cult[StatId.Followers].Value);
        Assert.AreEqual(0, cult[StatId.Communion].Value); // 히든 스탯은 건드리지 않음
    }

    [Test]
    public void Resistance_ReducesDecay_AndStacksCap()
    {
        var full = Misfortune.DecayPerTurn(4, 0f).ToDictionary(d => d.stat, d => d.amount);
        var half = Misfortune.DecayPerTurn(4, 0.5f).ToDictionary(d => d.stat, d => d.amount);
        Assert.AreEqual(full[StatId.Reputation] / 2, half[StatId.Reputation], 1e-4);
        var capped = Misfortune.DecayPerTurn(99, 0f).First(d => d.stat == StatId.Reputation).amount;
        var max = Misfortune.DecayPerTurn(Misfortune.MaxStacks, 0f).First(d => d.stat == StatId.Reputation).amount;
        Assert.AreEqual(max, capped, 1e-4);
    }

    [Test]
    public void Legacy_GrowsWithGameOversAndAchievements()
    {
        Assert.AreEqual(0, Legacy.LevelFor(0, 9));
        Assert.AreEqual(1, Legacy.LevelFor(1, 0));
        Assert.AreEqual(0, Legacy.LevelFor(0, 19));
        Assert.AreEqual(1, Legacy.LevelFor(0, 20), "업적 20개 = 1단계");
        Assert.AreEqual(2, Legacy.LevelFor(1, 25));
        Assert.AreEqual(0f, Legacy.ResistanceFor(0));
        Assert.Greater(Legacy.ResistanceFor(3), Legacy.ResistanceFor(1));
        Assert.AreEqual(Legacy.MaxResistance, Legacy.ResistanceFor(1000), 1e-4);
    }

    [Test]
    public void MisfortuneStacks_SurviveSaveAndCap()
    {
        GameSession.StartNew(1);
        for (int i = 0; i < 15; i++) GameSession.AddMisfortune();
        Assert.AreEqual(Misfortune.MaxStacks, GameSession.Misfortune);
        var data = GameSession.Capture();
        GameSession.StartNew(2);
        Assert.AreEqual(0, GameSession.Misfortune);
        GameSession.Restore(data);
        Assert.AreEqual(Misfortune.MaxStacks, GameSession.Misfortune);
    }
}
