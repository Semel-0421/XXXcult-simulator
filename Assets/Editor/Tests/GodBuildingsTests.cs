using System.Linq;
using NUnit.Framework;

// 준비된 신 모드: 봉헌 건축(축성), 전용 이벤트, 잠금 제목
public class GodBuildingsTests
{
    [TearDown] public void Reset() => GameSession.StartNew(1);

    static GodSkill S(string id) => GodSkillCatalog.Find(id);

    // 기적 가지 안쪽까지 익혀 조각상·마을 해금과 고리 단계 3(화신)을 만든다
    static void StartWithStatue()
    {
        GodRules.Begin(5);
        GameSession.FoundCult("t");
        var tree = GameSession.God.Tree;
        tree.ChooseNature(S("l_wealth_atk2"));
        foreach (var id in new[] { "l_wealth_atk1", "t_wealth_atk", "l_wealth_def1", "l_wealth_def2", "t_wealth_def", "s_wealth" })
            Assert.IsTrue(tree.Learn(S(id), 999), id);
        GameSession.Cult[StatId.Offerings].Set(1000);
    }

    [Test]
    public void Build_NeedsUnlockStageOfferingsAndCommunion_AndCostsATurnOfResources()
    {
        GodRules.Begin(5);
        GameSession.FoundCult("t");
        var god = GameSession.God;
        var cult = GameSession.Cult;
        Assert.AreEqual("locked", god.Buildings.Blocker(GodBuildingType.Statue, god.Tree, cult));
        god.Tree.ChooseNature(S("l_wealth_atk2")); // 조각상 해금, 단계 1
        cult[StatId.Offerings].Set(10);
        Assert.AreEqual("offerings", god.Buildings.Blocker(GodBuildingType.Statue, god.Tree, cult));
        cult[StatId.Offerings].Set(500);
        cult[StatId.Communion].Set(1);
        Assert.AreEqual("communion", god.Buildings.Blocker(GodBuildingType.Statue, god.Tree, cult));
        cult[StatId.Communion].Set(10);
        var turns = GameSession.CultTime.Turns;
        Assert.IsTrue(Turns.Consecrate(GodBuildingType.Statue));
        Assert.AreEqual(1, god.Buildings.LevelOf(GodBuildingType.Statue));
        Assert.AreEqual(turns + 1, GameSession.CultTime.Turns, "축성은 행동 턴을 쓴다");
        Assert.AreEqual(10 - GodBuildings.CommunionCost, cult[StatId.Communion].Value, 1);
        Assert.AreEqual("stage", god.Buildings.Blocker(GodBuildingType.Statue, god.Tree, cult), "레벨 2는 단계 2(잔영) 필요");
        Assert.IsFalse(Turns.Consecrate(GodBuildingType.Statue), "지을 수 없으면 턴을 쓰지 않음");
        Assert.AreEqual(turns + 1, GameSession.CultTime.Turns);
    }

    [Test]
    public void Floors_HoldStats_UpkeepDowngrades_AndValueRaisesEarthScale()
    {
        StartWithStatue();
        var god = GameSession.God;
        var cult = GameSession.Cult;
        Assert.IsTrue(god.Buildings.Build(GodBuildingType.Village, god.Tree, cult));
        cult[StatId.Communion].Set(10);
        Assert.IsTrue(god.Buildings.Build(GodBuildingType.Village, god.Tree, cult));
        Assert.AreEqual(GodBuildings.CostOf(1) + GodBuildings.CostOf(2), god.Buildings.Value);

        cult[StatId.Followers].Set(0);
        god.Buildings.EnforceFloors(cult);
        Assert.AreEqual(20, cult[StatId.Followers].Value, "공동체 마을 Lv.2 = 신도 최소 20명");

        cult[StatId.Offerings].Set(0);
        god.Buildings.PayUpkeep(cult);
        Assert.AreEqual(1, god.Buildings.LevelOf(GodBuildingType.Village), "유지비를 못 내면 한 레벨 하락");
        Assert.AreEqual(GodBuildings.CostOf(1), god.Buildings.Value);
    }

    [Test]
    public void Seize_LowersHighestBuilding_AndEmptyMeansDissolutionAtEarthFive()
    {
        var buildings = new GodBuildings();
        var rng = new System.Random(1);
        Assert.IsFalse(buildings.Seize(rng, 0), "압류할 건물이 없음");
        buildings.Restore(new[] { 1, 0, 2, 0, 0 }, false);
        Assert.IsTrue(buildings.Seize(rng, 0));
        Assert.AreEqual(1, buildings.LevelOf(GodBuildingType.Library));
        Assert.IsTrue(buildings.Seize(rng, 100), "방어 100%면 막음");
        Assert.AreEqual(1, buildings.LevelOf(GodBuildingType.Library));

        GodRules.Begin(5);
        GameSession.FoundCult("t");
        GameSession.God.Tree.ChooseNature(S("l_health_atk1"));
        GameSession.God.Pressure.AddHeat(0, 5000); // 지상 5단계
        GameSession.Cult[StatId.Reputation].Set(900);
        var quarterRng = new System.Random(2);
        for (int i = 0; i < 40 && !GameSession.God.Buildings.DissolutionOrdered; i++) GameSession.God.Quarter(GameSession.Cult, quarterRng);
        Assert.IsTrue(GameSession.God.Buildings.DissolutionOrdered, "건물이 없으면 분기마다 확률로 해산 명령");
        Assert.AreEqual(GodRules.Disbanded, Progress.ForcedEnding());
    }

    [Test]
    public void GodEvents_Has200_PressureEventsGatedByStage()
    {
        Assert.AreEqual(200, GodEvents.All.Length);
        Assert.AreEqual(80, GodEvents.All.Count(e => e.IsNegative));
        Assert.AreEqual(GodEvents.All.Length, GodEvents.All.Select(e => e.Id).Distinct().Count());
        GodRules.Begin(5);
        GameSession.FoundCult("t");
        var cult = GameSession.Cult;
        var hv01 = GodEvents.All.First(e => e.Id == "hv01");
        var hv40 = GodEvents.All.First(e => e.Id == "hv40");
        Assert.AreEqual(0, GameSession.God.Pressure.Stage(Pressure.Heaven, cult));
        Assert.IsFalse(hv01.Condition(cult), "압박 1단계 전에는 안 나옴");
        GameSession.God.Pressure.AddHeat(5000, 0);
        Assert.IsTrue(hv01.Condition(cult));
        Assert.IsTrue(hv40.Condition(cult));
        Assert.Greater(GameSession.God.Pressure.EventChance(cult), 0);
        Assert.AreSame(GodEvents.All, Turns.CultPool);
    }

    [Test]
    public void GodEventEffects_ApplyHeatPointsAndSeizure()
    {
        GodRules.Begin(5);
        GameSession.FoundCult("t");
        var god = GameSession.God;
        god.Tree.ChooseNature(S("l_wealth_atk2")); // 압류 방어 효과 없이 조각상만 연다
        GameSession.Cult[StatId.Offerings].Set(500);
        Assert.IsTrue(god.Buildings.Build(GodBuildingType.Statue, god.Tree, GameSession.Cult));
        var points = god.Points.Earned;
        var outcome = new Outcome("x").X(GodEventEffect.Envy, 10).X(GodEventEffect.Suspicion, 10).X(GodEventEffect.Seize, 1);
        GodRules.AfterEvent(outcome);
        Assert.Greater(god.Pressure.Envy, 0);
        Assert.Greater(god.Pressure.Suspicion, 0);
        for (int i = 0; i < 40; i++) GodRules.AfterEvent(new Outcome("p").X(GodEventEffect.Points, 1));
        Assert.That(god.Points.Earned - points, Is.InRange(5, 35), "방어 보상은 절반 확률로 받는다");
        Assert.AreEqual(0, god.Buildings.LevelOf(GodBuildingType.Statue), "압류로 한 레벨 하락");
    }

    [Test]
    public void Buildings_SurviveSaveAndLoad()
    {
        StartWithStatue();
        var god = GameSession.God;
        god.Buildings.Build(GodBuildingType.Statue, god.Tree, GameSession.Cult);
        var data = GameSession.Capture();
        GameSession.StartNew(9);
        GameSession.Restore(data);
        Assert.AreEqual(1, GameSession.God.Buildings.LevelOf(GodBuildingType.Statue));
        Assert.IsFalse(GameSession.God.Buildings.DissolutionOrdered);
    }

    [Test]
    public void Consecrate_HasPictogram() =>
        Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>("Pictograms/Consecrate_ok"));

    [Test]
    public void LockedTitle_FillsPerEnding_AndLabelImagesExist()
    {
        Assert.AreEqual(0f, GodRules.LockedFill(0));
        Assert.AreEqual(0.5f, GodRules.LockedFill(2));
        Assert.AreEqual(1f, GodRules.LockedFill(4));
        Assert.AreEqual("(4/5)", GodRules.LockedCount(4));
        foreach (var key in new[] { "god_mode", "mirror_mode" })
            foreach (var lang in new[] { "ko", "en" })
                foreach (var suffix in new[] { "", "_locked", "_lit" })
                    Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>($"UI/MenuLabels/{key}_{lang}{suffix}"), $"{key}_{lang}{suffix}");
    }

    [Test]
    public void BroadRings_LowerDissolutionChance()
    {
        Assert.AreEqual(GodBuildings.DissolutionChancePerQuarter, GodBuildings.DissolutionChance(0), 1e-6);
        Assert.Less(GodBuildings.DissolutionChance(5), GodBuildings.DissolutionChance(2));
        Assert.GreaterOrEqual(GodBuildings.DissolutionChance(10), 0f);
    }
}
