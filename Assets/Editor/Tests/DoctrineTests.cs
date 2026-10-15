using System.Linq;
using NUnit.Framework;

public class DoctrineTests
{
    [Test]
    public void Catalog_Has120Skills_TwoPathsFrom6_And16Finals()
    {
        Assert.AreEqual(120, DoctrineCatalog.All.Count);
        Assert.AreEqual(120, DoctrineCatalog.All.Select(s => s.Id).Distinct().Count());
        Assert.AreEqual(8, DoctrineCatalog.All.Select(s => s.Branch).Distinct().Count());
        foreach (var group in DoctrineCatalog.All.GroupBy(s => s.Branch))
        {
            Assert.AreEqual(15, group.Count(), group.Key.ToString());
            Assert.IsTrue(group.Where(s => s.Tier <= 5).All(s => s.Path == ""));
            Assert.AreEqual(5, group.Count(s => s.Path == "A"));
            Assert.AreEqual(5, group.Count(s => s.Path == "B"));
            Assert.IsTrue(DoctrineCatalog.At(group.Key, 5).Repeatable, $"{group.Key} 5단계는 중복 레벨업");
        }
        Assert.AreEqual(16, DoctrineCatalog.All.Count(s => s.IsFinal));
        Assert.IsTrue(DoctrineCatalog.All.Where(s => s.IsFinal).All(s => s.MaxLevel == 2));
        Assert.AreEqual(8, DoctrineCatalog.All.Count(s => s.Repeatable));
    }

    [Test]
    public void PathChoice_LocksTheOtherPath()
    {
        var tree = new DoctrineTree();
        tree.LevelUp(DoctrineSkill.Declaration);
        for (int t = 1; t <= 5; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Charity, t));
        var a6 = DoctrineCatalog.At(DoctrineBranch.Charity, 6, "A");
        var b6 = DoctrineCatalog.At(DoctrineBranch.Charity, 6, "B");
        Assert.IsTrue(tree.CanLearn(a6, 99));
        Assert.IsTrue(tree.CanLearn(b6, 99));
        tree.LevelUp(b6);
        Assert.IsFalse(tree.CanLearn(a6, 99));
        Assert.IsTrue(tree.IsPathBlocked(a6));
        Assert.IsTrue(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Charity, 7, "B"), 99));
        Assert.IsFalse(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Charity, 7, "A"), 99));
    }

    [Test]
    public void FinalSkill_BecomesEmblem_AndUpgradesAtCenter()
    {
        var tree = new DoctrineTree();
        tree.LevelUp(DoctrineSkill.Declaration);
        for (int t = 1; t <= 5; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Greed, t));
        for (int t = 6; t <= 9; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Greed, t, "A"));
        var final = DoctrineCatalog.At(DoctrineBranch.Greed, 10, "A");
        Assert.IsNull(tree.Emblem);
        Assert.IsTrue(tree.CanLearn(final, 4));
        tree.LevelUp(final);
        Assert.AreEqual(final, tree.Emblem);
        Assert.IsFalse(tree.CanLearn(final, 99)); // 노드에서는 1레벨까지만
        Assert.IsFalse(tree.CanUpgradeFinal(DoctrineSkill.FinalUpgradeCost - 1));
        Assert.IsTrue(tree.UpgradeFinal(DoctrineSkill.FinalUpgradeCost));
        Assert.AreEqual(2, tree.LevelOf(final));
        Assert.IsFalse(tree.CanUpgradeFinal(99));

        // 다른 가지의 최종 스킬을 배우면 중앙 문양이 그것으로 바뀐다
        for (int t = 1; t <= 5; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Patience, t));
        for (int t = 6; t <= 10; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Patience, t, "B"));
        Assert.AreEqual(DoctrineCatalog.At(DoctrineBranch.Patience, 10, "B"), tree.Emblem);
    }

    [Test]
    public void Emblem_SurvivesSaveAndLoad()
    {
        GameSession.StartNew(3);
        var tree = GameSession.Cult.Doctrines;
        tree.LevelUp(DoctrineSkill.Declaration);
        for (int t = 1; t <= 5; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Lust, t));
        for (int t = 6; t <= 10; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Lust, t, "B"));
        var data = GameSession.Capture();
        GameSession.StartNew(4);
        GameSession.Restore(data);
        Assert.AreEqual("lust10b", GameSession.Cult.Doctrines.EmblemId);
    }

    [Test]
    public void EmblemSprites_AreGeneratedForAllFinals()
    {
        foreach (var final in DoctrineCatalog.All.Where(s => s.IsFinal))
            Assert.IsNotNull(DoctrineEmblems.Get(final), final.Id);
    }

    [Test]
    public void Points_YearsAchievementsAndDoctrineLevel()
    {
        Assert.AreEqual(0, DoctrinePoints.Earned(0, 0, 0));
        Assert.AreEqual(3 + 5 + 2 * 4, DoctrinePoints.Earned(3, 5, 4));
    }

    [Test]
    public void Learning_RequiresDeclarationPreviousTierAndPoints()
    {
        var tree = new DoctrineTree();
        var first = DoctrineCatalog.At(DoctrineBranch.Greed, 1);
        var second = DoctrineCatalog.At(DoctrineBranch.Greed, 2);
        Assert.IsFalse(tree.CanLearn(first, 10)); // 교리 선포 전
        tree.LevelUp(DoctrineSkill.Declaration);
        Assert.IsTrue(tree.CanLearn(first, 1));
        Assert.IsFalse(tree.CanLearn(first, 0));  // 포인트 부족
        Assert.IsFalse(tree.CanLearn(second, 10)); // 앞 단계 미습득
        tree.LevelUp(first);
        Assert.IsTrue(tree.CanLearn(second, 10));
        Assert.IsFalse(tree.CanLearn(first, 10));  // 이미 최대
        Assert.AreEqual(first.Cost, tree.SpentPoints);
    }

    [Test]
    public void CostRisesWithTier()
    {
        Assert.AreEqual(1, DoctrineCatalog.At(DoctrineBranch.Lust, 1).Cost);
        Assert.AreEqual(2, DoctrineCatalog.At(DoctrineBranch.Lust, 4).Cost);
        Assert.AreEqual(3, DoctrineCatalog.At(DoctrineBranch.Lust, 7, "A").Cost);
        Assert.AreEqual(4, DoctrineCatalog.At(DoctrineBranch.Lust, 10, "B").Cost);
        Assert.AreEqual(0, DoctrineSkill.Declaration.Cost);
    }

    [Test]
    public void LearnedDoctrine_AppliesLikeGifted_ScaledByLevel()
    {
        GameSession.StartNew(1);
        var tree = GameSession.Cult.Doctrines;
        tree.LevelUp(DoctrineSkill.Declaration);
        var greed1 = DoctrineCatalog.At(DoctrineBranch.Greed, 1); // 봉헌금 +5%
        tree.LevelUp(greed1);
        Assert.AreEqual(5f, GiftedEffects.TotalPercent(GameSession.Personal.Talents, StatId.Offerings), 1e-4);

        // 여러 번 올리는 스킬은 레벨만큼 곱해진다
        var repeat = DoctrineCatalog.At(DoctrineBranch.Greed, 5); // 봉헌금 +4%/레벨
        for (int tier = 2; tier <= 4; tier++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Greed, tier));
        tree.LevelUp(repeat);
        tree.LevelUp(repeat);
        Assert.AreEqual(2, tree.LevelOf(repeat));
        Assert.AreEqual(5f + 5f + 8f + 8f, GiftedEffects.TotalPercent(GameSession.Personal.Talents, StatId.Offerings), 1e-4);
        Assert.IsTrue(GameSession.Personal.Talents.Owned.Count == 0); // 기프티드 목록에는 섞이지 않음
    }

    [Test]
    public void ResistanceSkills_AddMisfortuneResistance()
    {
        var tree = new DoctrineTree();
        tree.LevelUp(DoctrineSkill.Declaration);
        var patience5 = DoctrineCatalog.At(DoctrineBranch.Patience, 5); // 불운 저항 4%/레벨
        tree.Restore(patience5.Id, 3);
        Assert.AreEqual(0.12f, tree.Resistance, 1e-4);
    }

    [Test]
    public void OppositeBranch_LockedAt5AndAbove_AfterSixthTierChosen()
    {
        var tree = new DoctrineTree();
        tree.LevelUp(DoctrineSkill.Declaration);
        for (int t = 1; t <= 5; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Lust, t));
        for (int t = 1; t <= 4; t++) tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Temperance, t)); // 정반대 가지 1~4단계
        Assert.IsFalse(tree.IsOppositeLocked(DoctrineCatalog.At(DoctrineBranch.Temperance, 5)));
        Assert.IsTrue(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Temperance, 5), 99));

        tree.LevelUp(DoctrineCatalog.At(DoctrineBranch.Lust, 6, "A")); // 음욕 6단계 선택
        Assert.IsTrue(tree.IsOppositeLocked(DoctrineCatalog.At(DoctrineBranch.Temperance, 5)));
        Assert.IsFalse(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Temperance, 5), 99));          // 5단계 이상 잠김
        Assert.IsFalse(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Temperance, 6, "A"), 99));
        Assert.IsFalse(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Temperance, 6, "B"), 99));
        Assert.IsFalse(tree.CanLearn(DoctrineCatalog.At(DoctrineBranch.Temperance, 4), 99)); // 이미 배운 1단계 스킬은 다시 배울 수 없음
        Assert.IsFalse(tree.IsOppositeLocked(DoctrineCatalog.At(DoctrineBranch.Greed, 6, "A"))); // 다른 가지는 영향 없음
    }

    [Test]
    public void OppositeMapping_IsSymmetricAndSwapsDesireAndVirtue()
    {
        var all = new[] { DoctrineBranch.Lust, DoctrineBranch.Greed, DoctrineBranch.Appetite, DoctrineBranch.Ambition,
            DoctrineBranch.Temperance, DoctrineBranch.Charity, DoctrineBranch.Patience, DoctrineBranch.Integrity };
        var desire = new[] { DoctrineBranch.Lust, DoctrineBranch.Greed, DoctrineBranch.Appetite, DoctrineBranch.Ambition };
        foreach (var branch in all)
        {
            Assert.AreNotEqual(branch, branch.Opposite());
            Assert.AreEqual(branch, branch.Opposite().Opposite());
            Assert.AreNotEqual(desire.Contains(branch), desire.Contains(branch.Opposite())); // 욕망 ↔ 덕목
        }
    }
}
