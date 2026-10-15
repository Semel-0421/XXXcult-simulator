using System.Linq;
using NUnit.Framework;

public class GodTreeTests
{
    static GodSkill S(string id) => GodSkillCatalog.Find(id);

    // 가지 하나(핵 아래 31개)를 바깥부터 모두 익힌다
    static void CompleteBranch(GodTree tree, GodSkill core)
    {
        foreach (var skill in Subtree(core).OrderBy(s => s.Ring))
            if (!tree.IsLearned(skill)) Assert.IsTrue(tree.Learn(skill, 99), skill.Id);
    }

    static System.Collections.Generic.IEnumerable<GodSkill> Subtree(GodSkill skill) =>
        new[] { skill }.Concat(skill.Children.SelectMany(Subtree));

    static GodSkill Core(GodBranch branch) => GodSkillCatalog.Cores.First(c => c.Branch == branch);

    [Test]
    public void Catalog_Is125Nodes_RadialFromOuter64ToCenter()
    {
        var all = GodSkillCatalog.All;
        Assert.AreEqual(125, all.Count);
        Assert.AreEqual(64, all.Count(s => s.Ring == 1));
        Assert.AreEqual(32, all.Count(s => s.Ring == 2));
        Assert.AreEqual(16, all.Count(s => s.Ring == 3));
        Assert.AreEqual(8, all.Count(s => s.Ring == 4));
        Assert.AreEqual(4, all.Count(s => s.Ring == 5));
        Assert.AreEqual(4, GodSkillCatalog.Ascension.Children.Count);
        foreach (var skill in all.Where(s => s.Ring >= 2 && s.Ring <= 5))
            Assert.AreEqual(2, skill.Children.Count, skill.Id);
        // 공격형·방어형이 반씩
        Assert.AreEqual(48, all.Count(s => s.Kind == GodSkillKind.Attack));
        Assert.AreEqual(48, all.Count(s => s.Kind == GodSkillKind.Defense));
        // 신격 둘, 인격 둘
        Assert.AreEqual(2, GodSkillCatalog.Cores.Count(c => c.IsDivine));
        foreach (var branch in new[] { GodBranch.Miracle, GodBranch.Prophecy, GodBranch.Mercy, GodBranch.Desire })
            Assert.AreEqual(31, all.Count(s => s.Branch == branch), branch.ToString());
        Assert.IsTrue(all.Where(s => !s.IsCenter).All(s => s.Effects.Length > 0), "승천 외 모든 노드는 효과가 있어야 함");
    }

    [Test]
    public void Nature_IsFreeOuterNode_AndRequiredBeforeLearning()
    {
        var tree = new GodTree();
        var leaf = S("l_health_atk1");
        Assert.IsFalse(tree.CanLearn(leaf, 99), "본성을 고르기 전에는 익힐 수 없음");
        Assert.IsFalse(tree.ChooseNature(S("t_health_atk")), "본성은 바깥 노드만");
        Assert.IsTrue(tree.ChooseNature(leaf));
        Assert.IsTrue(tree.IsLearned(leaf));
        Assert.AreEqual(0, tree.SpentPoints, "본성은 무료");
        Assert.IsFalse(tree.ChooseNature(S("l_health_atk2")), "본성은 하나만");
    }

    [Test]
    public void InnerNode_NeedsBothOuterNodes_AndCostsPoints()
    {
        var tree = new GodTree();
        tree.ChooseNature(S("l_health_atk1"));
        var inner = S("t_health_atk");
        Assert.IsFalse(tree.CanLearn(inner, 99), "바깥 노드 하나만으로는 안 열림");
        Assert.AreEqual(1, tree.LearnedChildren(inner));
        Assert.IsFalse(tree.Learn(S("l_health_atk2"), 0), "포인트 부족");
        Assert.IsTrue(tree.Learn(S("l_health_atk2"), 1));
        Assert.IsTrue(tree.CanLearn(inner, 1));
        Assert.IsTrue(tree.Learn(inner, 1));
        Assert.AreEqual(2, tree.SpentPoints);
        Assert.AreEqual(2, tree.ReachedStage, "고리 2 = 잔영");
    }

    [Test]
    public void Ascension_OpensAfterAnyTwoCores_AndFirstTwoDecideEnding()
    {
        var tree = new GodTree();
        tree.ChooseNature(S("l_health_atk1"));
        CompleteBranch(tree, Core(GodBranch.Miracle));
        Assert.IsFalse(tree.AscensionOpen);
        Assert.IsFalse(tree.CanLearn(GodSkillCatalog.Ascension, 99));
        CompleteBranch(tree, Core(GodBranch.Desire));
        Assert.IsTrue(tree.AscensionOpen);
        Assert.AreEqual("god_miracle_desire", tree.AscensionEndingId);
        CompleteBranch(tree, Core(GodBranch.Mercy)); // 세 번째 핵은 엔딩을 바꾸지 않음
        Assert.AreEqual("god_miracle_desire", tree.AscensionEndingId);
        Assert.AreEqual(3, GodSkillCatalog.Ascension.Cost);
        Assert.IsTrue(tree.Learn(GodSkillCatalog.Ascension, 3));
        Assert.IsTrue(tree.Ascended);
        Assert.AreEqual(6, tree.ReachedStage);
    }

    [Test]
    public void EndingIds_FourMixedPairs_AndTwoHidden()
    {
        Assert.AreEqual("god_miracle_mercy", GodEndings.IdFor(GodBranch.Mercy, GodBranch.Miracle));
        Assert.AreEqual("god_prophecy_desire", GodEndings.IdFor(GodBranch.Prophecy, GodBranch.Desire));
        Assert.AreEqual(GodEndings.DivinePair, GodEndings.IdFor(GodBranch.Prophecy, GodBranch.Miracle));
        Assert.AreEqual(GodEndings.HumanPair, GodEndings.IdFor(GodBranch.Mercy, GodBranch.Desire));
        Assert.AreEqual(6, GodEndings.AscensionIds.Distinct().Count());
    }

    [Test]
    public void MinimumAscensionCost_Is67Points()
    {
        var tree = new GodTree();
        tree.ChooseNature(S("l_health_atk1"));
        CompleteBranch(tree, Core(GodBranch.Miracle));
        CompleteBranch(tree, Core(GodBranch.Prophecy));
        tree.Learn(GodSkillCatalog.Ascension, 99);
        Assert.AreEqual(67 - 1, tree.SpentPoints, "본성 1개는 무료라 66");
    }

    [Test]
    public void AttackBoostsGainsOnly_DefenseGuardsLossesOnly()
    {
        var tree = new GodTree();
        tree.ChooseNature(S("l_miracle_atk1"));  // gain:Followers+5
        tree.Learn(S("l_patience_def1"), 99);    // guard:Followers+5
        Assert.AreEqual(5, tree.Gain(StatId.Followers));
        Assert.AreEqual(5, tree.Guard(StatId.Followers));
        Assert.AreEqual(10.5f, tree.Adjust(StatId.Followers, 10), 0.001f);
        Assert.AreEqual(-9.5f, tree.Adjust(StatId.Followers, -10), 0.001f);
        Assert.AreEqual(-1f, GodEffects.Adjust(-10, 0, 200), 0.001f, "방어는 90%까지만");
        Assert.AreEqual(5f, GodEffects.Resisted(10, 50), 0.001f);
        Assert.AreEqual(-10f, GodEffects.Resisted(-10, 50), 0.001f, "내려가는 압박은 그대로");
    }

    [Test]
    public void EffectTotals_ChanceResistAndSpecials()
    {
        var tree = new GodTree();
        tree.ChooseNature(S("l_miracle_atk2")); // chance:Wonder+5
        tree.Learn(S("l_temperance_def1"), 99); // resist:Heaven+5
        tree.Learn(S("l_wealth_def1"), 99);     // seize+5
        Assert.AreEqual(5, tree.Chance(GodAction.Wonder));
        Assert.AreEqual(0, tree.Chance(GodAction.Feast));
        Assert.AreEqual(5, tree.Resist(Pressure.Heaven));
        Assert.AreEqual(0, tree.Resist(Pressure.Earth));
        Assert.AreEqual(5, tree.Total(GodEffect.Type.SeizeGuard));
        Assert.IsTrue(GodSkillInfo.EffectLines(S("c_miracle")).All(line => !line.Contains("{")), "효과 문구 서식이 채워져야 함");
    }

    [Test]
    public void Points_YearlyFaithBonusSecularStagesAndDefense()
    {
        Assert.AreEqual(0, GodPoints.FaithBonus(0, 500));
        Assert.AreEqual(2, GodPoints.FaithBonus(100, 500), "신도 100 × 신앙 50% = 50 → 2");
        Assert.AreEqual(GodPoints.MaxFaithPerYear, GodPoints.FaithBonus(10000, 1000), "해마다 최대 5");
        Assert.AreEqual(3, GodPoints.FaithBonus(100, 500, 50), "faith 효과 +50%");

        var points = new GodPoints();
        Assert.AreEqual(4, points.YearEnd(100, 500));
        Assert.AreEqual(4, points.SecularStageReached(2), "1·2단계 처음 도달 = 2 × 2");
        Assert.AreEqual(0, points.SecularStageReached(2), "같은 단계는 다시 주지 않음");
        Assert.AreEqual(0, points.SecularStageReached(1));
        Assert.AreEqual(2, points.SecularStageReached(3));
        points.Defense();
        Assert.AreEqual(11, points.Earned);

        var tree = new GodTree();
        tree.ChooseNature(S("l_health_atk1"));
        tree.Learn(S("l_health_atk2"), 99);
        Assert.AreEqual(10, points.Available(tree));
    }

    [Test]
    public void GodState_SurvivesSaveAndLoad_AndYearEndPaysPoints()
    {
        var profileWasEnabled = Profile.Enabled;
        Profile.Enabled = false; // 업적·프로필 파일을 건드리지 않는다
        try { SaveRoundTrip(); }
        finally
        {
            Profile.Enabled = profileWasEnabled;
            GameSession.StartNew();
        }
    }

    static void SaveRoundTrip()
    {
        GameSession.StartNew(7);
        GameSession.SetGodMode(true);
        GameSession.FoundCult("시험");
        var god = GameSession.God;
        god.Tree.ChooseNature(S("l_lust_atk1"));
        god.Points.Defense(5);
        god.Tree.Learn(S("l_lust_atk2"), god.Available);
        god.Points.SecularStageReached(1);
        GameSession.Cult[StatId.Followers].Set(100);
        GameSession.Cult[StatId.FollowerFaith].Set(500);
        var before = god.Points.Earned;
        for (int i = 0; i < 4; i++) GameSession.AdvanceTurn();
        Assert.AreEqual(before + 4, god.Points.Earned, "해가 바뀌면 기본 2 + 신앙 비례 2");

        var data = GameSession.Capture();
        GameSession.StartNew(1);
        Assert.IsFalse(GameSession.GodMode);
        GameSession.Restore(data);
        var restored = GameSession.God;
        Assert.IsTrue(GameSession.GodMode);
        Assert.AreEqual("l_lust_atk1", restored.Tree.NatureId);
        Assert.IsTrue(restored.Tree.IsLearned(S("l_lust_atk2")));
        Assert.AreEqual(god.Points.Earned, restored.Points.Earned);
        Assert.AreEqual(1, restored.Points.SecularStageAwarded);
        Assert.AreEqual(god.Available, restored.Available);
    }
}
