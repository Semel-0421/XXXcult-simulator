using System.Linq;
using NUnit.Framework;

// 준비된 신 모드 규칙: 시작 상태, 패배·승천 판정, 압박 두 축과 히든 규칙
public class GodRulesTests
{
    [TearDown] public void Reset() => GameSession.StartNew(1);

    [Test]
    public void Begin_AdultVessel_NatureOpensCultActions()
    {
        GodRules.Begin(3);
        Assert.IsTrue(GameSession.GodMode);
        Assert.AreEqual(100, GameSession.Personal[PersonalStatId.Miracle].Value);
        Assert.GreaterOrEqual(GameSession.Age.Turns, LifeFlow.FoundingTurn);
        GameSession.FoundCult("t");
        Assert.IsFalse(GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration));
        GameSession.God.Tree.ChooseNature(GodSkillCatalog.All.First(s => s.IsOuter));
        Assert.IsTrue(GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration));
        Assert.IsNull(Progress.ForcedEnding());
    }

    [Test]
    public void VesselAtZero_EndsInCollapse_AndAscensionPicksPairEnding()
    {
        GodRules.Begin(3);
        GameSession.FoundCult("t");
        GameSession.Personal[PersonalStatId.Health].Set(0);
        Assert.AreEqual(GodRules.Vessel, Progress.ForcedEnding());

        GodRules.Begin(3);
        GameSession.FoundCult("t");
        var tree = GameSession.God.Tree;
        tree.ChooseNature(GodSkillCatalog.All.First(s => s.IsOuter && s.Branch == GodBranch.Miracle));
        foreach (var branch in new[] { GodBranch.Miracle, GodBranch.Mercy })
            foreach (var skill in GodSkillCatalog.All.Where(s => s.Branch == branch).OrderBy(s => s.Ring))
                tree.Learn(skill, 999);
        Assert.IsTrue(tree.Learn(GodSkillCatalog.Ascension, 999));
        Assert.AreEqual("god_miracle_mercy", Progress.DueEnding().Id);
    }

    [Test]
    public void Pressure_GrowsWithNodes_AndHiddenPairLeavesOneAxisOpen()
    {
        GodRules.Begin(3);
        GameSession.FoundCult("t");
        var cult = GameSession.Cult;
        var god = GameSession.God;
        var before = god.Pressure.Stage(Pressure.Earth, cult);
        var tree = god.Tree;
        tree.ChooseNature(GodSkillCatalog.All.First(s => s.IsOuter && s.Branch == GodBranch.Miracle));
        foreach (var branch in new[] { GodBranch.Miracle, GodBranch.Prophecy })
            foreach (var skill in GodSkillCatalog.All.Where(s => s.Branch == branch).OrderBy(s => s.Ring))
                tree.Learn(skill, 999);
        Assert.Greater(god.Pressure.Stage(Pressure.Earth, cult), before, "익힌 노드가 지상 압박을 키워야 함");
        Assert.AreEqual(0.9f, god.Pressure.Resistance(Pressure.Heaven), 0.001f, "신격 둘이면 신계는 상한까지 막음");
        Assert.Less(god.Pressure.Resistance(Pressure.Earth), 0.3f, "신격 둘이면 지상은 거의 비어 있음");

        Assert.Greater(god.Pressure.Stage(Pressure.Earth, cult), 0, "신격 깊이가 지상 압박을 키워야 함");
        Assert.Greater(GodRules.EventPenalty, 0f, "압박이 있으면 이벤트 성공률이 깎여야 함");

        var rep = cult[StatId.Reputation].Value;
        god.Quarter(cult);
        Assert.Less(cult[StatId.Reputation].Value, rep, "지상 압박이 명망을 깎아야 함");
    }

    [Test]
    public void GodScale_OnlyInGodMode()
    {
        GameSession.StartNew(1);
        Assert.AreEqual(1f, GodRules.Scale(StatId.Followers, 5));
        Assert.AreEqual(0f, GodRules.ChanceBonus(CultAction.Proselytize));
    }
}
