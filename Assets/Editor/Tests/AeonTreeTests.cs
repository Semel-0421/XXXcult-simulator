using System.IO;
using System.Linq;
using NUnit.Framework;

// 황금의 눈동자 3단계: 아이온 트리 데이터·규칙 (SYS_DEMIURGE 6절)
public class AeonTreeTests
{
    const int Rich = 999;

    [Test]
    public void Catalog_Has16NodesPerSector()
    {
        Assert.AreEqual(64, AeonSkillCatalog.All.Count);
        foreach (var sector in EyeRules.Sectors)
        {
            var nodes = AeonSkillCatalog.OfSector(sector).ToList();
            Assert.AreEqual(16, nodes.Count, sector.ToString());
            for (int level = 0; level <= 4; level++)
            {
                Assert.IsNotNull(AeonSkillCatalog.Of(sector, level, AeonKind.Good));
                Assert.IsNotNull(AeonSkillCatalog.Of(sector, level, AeonKind.Evil));
            }
            for (int level = 5; level <= 9; level++) Assert.AreEqual(AeonKind.Gnosis, AeonSkillCatalog.Stage(sector, level).Kind);
            Assert.AreEqual(AeonKind.Ascension, AeonSkillCatalog.Stage(sector, 10).Kind);
        }
    }

    [Test]
    public void Calamity_EvilTiersAndGnosisOnly()
    {
        var evil = Enumerable.Range(0, 5).Select(l => AeonSkillCatalog.Of(EyeSector.Satan, l, AeonKind.Evil).Calamity).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 1, 2, 3, 4 }, evil);
        Assert.IsTrue(AeonSkillCatalog.All.Where(s => s.Kind == AeonKind.Good).All(s => s.Calamity == 0 && s.CalamityGuard > 0));
        Assert.IsTrue(AeonSkillCatalog.All.Where(s => s.Kind == AeonKind.Gnosis).All(s => s.Calamity == 1));
    }

    [Test]
    public void Grant_GivesStartSectorBothLevel0Free()
    {
        var tree = new AeonTree();
        tree.Grant(EyeSector.Satan);
        Assert.IsTrue(tree.IsLearned(AeonSkillCatalog.Of(EyeSector.Satan, 0, AeonKind.Good)));
        Assert.IsTrue(tree.IsLearned(AeonSkillCatalog.Of(EyeSector.Satan, 0, AeonKind.Evil)));
        Assert.AreEqual(0, tree.SpentPoints);
        tree.Grant(EyeSector.Yahweh); // 두 번은 받지 않는다
        Assert.AreEqual(2, tree.Learned.Count());
    }

    [Test]
    public void Learn_NeedsStartDoctrineLevelPointsAndSameLine()
    {
        var tree = new AeonTree();
        var y1g = AeonSkillCatalog.Of(EyeSector.Yahweh, 1, AeonKind.Good);
        Assert.AreEqual("start", tree.Blocker(y1g, Rich, 10));
        tree.Grant(EyeSector.Yahweh);
        Assert.AreEqual("doctrine", tree.Blocker(y1g, Rich, 0));
        Assert.AreEqual("points", tree.Blocker(y1g, 0, 1));
        Assert.IsTrue(tree.Learn(y1g, 1, 1));
        Assert.AreEqual(1, tree.SpentPoints);

        var y2e = AeonSkillCatalog.Of(EyeSector.Yahweh, 2, AeonKind.Evil);
        Assert.AreEqual("prereq", tree.Blocker(y2e, Rich, 10)); // 악 1단계가 없다

        var sa0g = AeonSkillCatalog.Of(EyeSector.Satan, 0, AeonKind.Good);
        Assert.IsTrue(tree.Learn(sa0g, Rich, 0)); // 다른 섹터 0단계는 조건 없음
        Assert.AreEqual(2, tree.SpentPoints);
    }

    static void LearnLines(AeonTree tree, EyeSector sector)
    {
        for (int level = 0; level <= 4; level++)
        {
            tree.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Good), Rich, 10);
            tree.Learn(AeonSkillCatalog.Of(sector, level, AeonKind.Evil), Rich, 10);
        }
    }

    [Test]
    public void Gnosis_NeedsBothLevel4Aeons()
    {
        var tree = new AeonTree();
        tree.Grant(EyeSector.Samael);
        var gnosis = AeonSkillCatalog.Stage(EyeSector.Samael, 5);
        for (int level = 1; level <= 4; level++) tree.Learn(AeonSkillCatalog.Of(EyeSector.Samael, level, AeonKind.Good), Rich, 10);
        Assert.AreEqual("prereq", tree.Blocker(gnosis, Rich, 10)); // 선한 아이온만으로는 닿지 못한다
        LearnLines(tree, EyeSector.Samael);
        Assert.IsTrue(tree.Learn(gnosis, Rich, 5));
        Assert.AreEqual("prereq", tree.Blocker(AeonSkillCatalog.Stage(EyeSector.Samael, 7), Rich, 10));
        Assert.IsTrue(tree.Learn(AeonSkillCatalog.Stage(EyeSector.Samael, 6), Rich, 6));
    }

    static void ReachThreshold(AeonTree tree, EyeSector sector)
    {
        LearnLines(tree, sector);
        for (int level = 5; level <= 9; level++) Assert.IsTrue(tree.Learn(AeonSkillCatalog.Stage(sector, level), Rich, 10), $"{sector} {level}");
    }

    [Test]
    public void Ascension_OnlyForFirstSectorAtThreshold_AndNeedsDoctrine10()
    {
        var tree = new AeonTree();
        tree.Grant(EyeSector.Yahweh);
        ReachThreshold(tree, EyeSector.Satan);
        ReachThreshold(tree, EyeSector.Yahweh);
        Assert.AreEqual(EyeSector.Satan, tree.AscensionSector);
        Assert.AreEqual("ascension", tree.Blocker(AeonSkillCatalog.Stage(EyeSector.Yahweh, 10), Rich, 10));
        var satanAscension = AeonSkillCatalog.Stage(EyeSector.Satan, 10);
        Assert.AreEqual("doctrine", tree.Blocker(satanAscension, Rich, 8)); // 승천은 교리 9단계
        Assert.IsTrue(tree.Learn(satanAscension, Rich, 10));
        Assert.IsTrue(tree.Ascended);
    }

    [Test]
    public void Calamity_SumsLearnedNodes()
    {
        var tree = new AeonTree();
        tree.Grant(EyeSector.Yaldabaoth);
        Assert.AreEqual(1, tree.CalamityPerQuarter);
        Assert.AreEqual(5, tree.CalamityGuard);
        ReachThreshold(tree, EyeSector.Yaldabaoth);
        Assert.AreEqual(1 + 1 + 2 + 3 + 4 + 5, tree.CalamityPerQuarter); // DEC-028 K: 9단계까지 분기당 +16
        Assert.AreEqual(25, tree.CalamityGuard);
    }

    [Test]
    public void GodSystems_SeeAeonEffectsPointsStageAndBuildings()
    {
        var god = new GodState();
        Assert.AreEqual(0, god.Tree.Gain(StatId.Doctrine));
        god.Aeons.Grant(EyeSector.Yahweh);
        Assert.AreEqual(9, god.Tree.Gain(StatId.Doctrine));
        Assert.AreEqual(6, god.Tree.Guard(StatId.FollowerFaith));
        Assert.AreEqual(0, god.Tree.Resist(Pressure.Heaven)); // 12차: 눈동자 모드에 신계 압박이 없어 야훼 선의 신계 저항을 뺐다
        Assert.AreEqual(1, god.Tree.ReachedStage);
        Assert.AreEqual(1.5f, god.Tree.PressureNodeWeight, 1e-4);

        god.Points.Add(5);
        god.Aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yaldabaoth, 0, AeonKind.Good), god.Available, 0);
        Assert.AreEqual(4, god.Available);
        Assert.AreEqual(4, god.Tree.Resist(Pressure.Earth)); // 얄다바오트 선 0단계 지상 저항
        Assert.IsTrue(GodBuildings.IsUnlocked(GodBuildingType.Shelter, god.Tree));
        Assert.IsFalse(GodBuildings.IsUnlocked(GodBuildingType.Temple, god.Tree));
    }

    [Test]
    public void ChooseSector_GrantsAndSaveRoundTripKeepsTree()
    {
        var previousDir = SaveSlots.Directory;
        var dir = Path.Combine(Path.GetTempPath(), "xxxcult_aeon_" + System.Guid.NewGuid().ToString("N"));
        SaveSlots.Directory = dir;
        try
        {
            EyeRules.Begin(5);
            EyeRules.ChooseSector(EyeSector.Satan);
            var aeons = GameSession.God.Aeons;
            ReachThreshold(aeons, EyeSector.Samael);
            SaveSlots.Write(2, GameSession.Capture());

            GameSession.StartNew(6);
            Assert.AreEqual(0, GameSession.God.Aeons.Learned.Count());

            GameSession.Restore(SaveSlots.Read(2));
            var restored = GameSession.God.Aeons;
            Assert.AreEqual(EyeSector.Satan, restored.StartSector);
            Assert.AreEqual(2 + 10 + 5, restored.Learned.Count());
            Assert.AreEqual(EyeSector.Samael, restored.AscensionSector);
            Assert.AreEqual(0, restored.SpentPoints - (10 + 5 * 2)); // 사탄 0단계 둘은 무료
            Assert.AreEqual(10, GameSession.God.Tree.Gain(StatId.Offerings)); // 효과가 다시 계산된다 (사탄 악 0단계)
            Assert.Greater(GameSession.God.Tree.Gain(StatId.Followers), 0);   // 사마엘 악·그노시스
        }
        finally
        {
            SaveSlots.Directory = previousDir;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            GameSession.StartNew(1);
        }
    }

    [Test]
    public void DoctrineNeed_AeonsCapAt3_GnosisFrom4()
    {
        var need = Enumerable.Range(0, 11).Select(l => l <= 4
            ? AeonSkillCatalog.Of(EyeSector.Satan, l, AeonKind.Good).DoctrineNeed
            : AeonSkillCatalog.Stage(EyeSector.Satan, l).DoctrineNeed).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 3, 4, 5, 6, 7, 8, 9 }, need);
    }
}
