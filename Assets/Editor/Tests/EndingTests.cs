using System.IO;
using NUnit.Framework;
using UnityEngine;

public class EndingTests
{
    string tempProfile, previousPath;
    bool previousEnabled;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        tempProfile = Path.Combine(Path.GetTempPath(), "xxxcult_ending_test_" + System.Guid.NewGuid().ToString("N") + ".json");
        Profile.Path = tempProfile;
        Profile.Enabled = true;
    }

    [TearDown]
    public void TearDown()
    {
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        if (File.Exists(tempProfile)) File.Delete(tempProfile);
    }

    [Test]
    public void Reach_RecordsSeenEnding_AndMarksGameEnded()
    {
        GameSession.StartNew(1);
        Assert.IsFalse(Endings.Seen(Endings.Dissolved));
        Assert.IsFalse(GameSession.Ended);

        Endings.Reach(Endings.Dissolved);

        Assert.IsTrue(Endings.Seen(Endings.Dissolved));
        Assert.AreEqual(1, Endings.SeenCount);
        Assert.IsTrue(GameSession.Ended);
        Profile.Reset(); // 파일에서 다시 읽어도 남아 있어야 함
        Assert.IsTrue(Endings.Seen(Endings.Dissolved));
    }

    [Test]
    public void Catalog_HasUniqueIds_AndLanguageTexts()
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var ending in Endings.All)
        {
            Assert.IsTrue(ids.Add(ending.Id), ending.Id);
            Assert.AreNotEqual(ending.NameKey, Lang.Get(ending.NameKey), "이름 문구 없음: " + ending.Id);
            Assert.AreNotEqual(ending.DescriptionKey, Lang.Get(ending.DescriptionKey), "설명 문구 없음: " + ending.Id);
            if (ending.Id != Endings.Dissolved)
                Assert.AreNotEqual(ending.TextKey, Lang.Get(ending.TextKey), "엔딩 글 없음: " + ending.Id);
        }
    }

    [Test]
    public void Catalog_Has32FinalEndings_16Good16Bad()
    {
        int good = 0, bad = 0, finals = 0;
        foreach (var skill in DoctrineCatalog.All) if (skill.IsFinal) finals++;
        foreach (var ending in Endings.All)
        {
            if (ending.Final == null) continue;
            if (ending.Kind == EndingKind.Good) good++;
            else if (ending.Kind == EndingKind.Bad) bad++;
        }
        Assert.AreEqual(16, finals);
        Assert.AreEqual(16, good);
        Assert.AreEqual(16, bad);
        Assert.AreEqual(1 + 2 + 32 + 15, Endings.All.Count); // 해체 + 사망 2 + 최종 스킬 32 + 극단 15
    }

    static void SetGroup(PersonalStatus p, PersonalStatId[] ids, int value)
    {
        foreach (var id in ids) p[id].Set(value);
    }

    static readonly PersonalStatId[] Virtues = { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };
    static readonly PersonalStatId[] Desires = { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition };

    [Test]
    public void Decide_NeedsBranchStatAbove75()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Temperance].Set(75);
        Assert.IsNull(Endings.Decide(p, DoctrineBranch.Temperance), "75는 넘은 것이 아니다");
        p[PersonalStatId.Temperance].Set(76);
        Assert.IsNotNull(Endings.Decide(p, DoctrineBranch.Temperance));
    }

    [Test]
    public void Decide_VirtueBranch_GoodWhenDesireAverageAtMost50()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Charity].Set(90);
        SetGroup(p, Desires, 50);
        Assert.AreEqual(EndingKind.Good, Endings.Decide(p, DoctrineBranch.Charity), "욕망 평균 50은 이하");
        SetGroup(p, Desires, 51);
        Assert.AreEqual(EndingKind.Bad, Endings.Decide(p, DoctrineBranch.Charity), "욕망 평균 51은 초과");
    }

    [Test]
    public void Decide_DesireBranch_GoodWhenVirtueAverageAbove50()
    {
        var p = new PersonalStatus();
        p[PersonalStatId.Greed].Set(90);
        SetGroup(p, Virtues, 50);
        Assert.AreEqual(EndingKind.Bad, Endings.Decide(p, DoctrineBranch.Greed), "미덕 평균 50은 이하");
        SetGroup(p, Virtues, 51);
        Assert.AreEqual(EndingKind.Good, Endings.Decide(p, DoctrineBranch.Greed), "미덕 평균 51은 초과");
    }

    [Test]
    public void FindExtreme_FindsZeroOrMax_ButNotReputationZero()
    {
        GameSession.StartNew(5);
        var cult = GameSession.Cult;
        // 시작 스탯 중 0인 것은 극단이 아니라 시작값일 수 있으므로 모두 중간 값으로 맞춘다
        foreach (StatId id in System.Enum.GetValues(typeof(StatId)))
            cult[id].Set(System.Math.Min(5, cult[id].Max - 1) > 0 ? System.Math.Min(5, cult[id].Max - 1) : 1);
        Assert.IsNull(Endings.FindExtreme(cult, ~0));

        cult[StatId.Offerings].Set(0);
        Assert.IsNull(Endings.FindExtreme(cult, 0), "한 번도 오른 적 없는 0은 바닥이 아니다");
        var low = Endings.FindExtreme(cult, ~0);
        Assert.AreEqual((StatId.Offerings, false), low.Value);

        cult[StatId.Offerings].Set(5);
        cult[StatId.Reputation].Set(0);
        Assert.IsNull(Endings.FindExtreme(cult, ~0), "명망 0은 해체 엔딩");

        cult[StatId.Reputation].Set(5);
        cult[StatId.Communion].Set(cult[StatId.Communion].Max); // 상한이 10인 스탯
        Assert.AreEqual((StatId.Communion, true), Endings.FindExtreme(cult, ~0).Value);

        cult[StatId.Communion].Set(5);
        cult[StatId.Doctrine].Set(1000);
        Assert.AreEqual((StatId.Doctrine, true), Endings.FindExtreme(cult, ~0).Value);
    }

    [Test]
    public void FirstExtreme_IsRecordedOnce_AndSurvivesSaveAndLoad()
    {
        GameSession.StartNew(6);
        GameSession.RecordExtreme(Endings.ExtremeId(StatId.Authority, true));
        GameSession.RecordExtreme(Endings.ExtremeId(StatId.Offerings, false)); // 처음 것만 남는다
        Assert.AreEqual("extreme_Authority_max", GameSession.FirstExtremeId);
        Assert.IsNotNull(Endings.Find(GameSession.FirstExtremeId));

        var json = JsonUtility.ToJson(GameSession.Capture());
        GameSession.StartNew(1);
        GameSession.Restore(JsonUtility.FromJson<SaveData>(json));
        Assert.AreEqual("extreme_Authority_max", GameSession.FirstExtremeId);
    }

    [Test]
    public void OnlyOneFinalSkillCanBeChosen()
    {
        GameSession.StartNew(3);
        var tree = GameSession.Cult.Doctrines;
        DoctrineSkill first = null, second = null;
        foreach (var skill in DoctrineCatalog.All)
        {
            if (!skill.IsFinal) continue;
            if (first == null) first = skill;
            else if (second == null && skill.Branch != first.Branch) second = skill;
        }
        Assert.IsTrue(tree.LevelUp(first));
        Assert.IsFalse(tree.LevelUp(second), "다른 최종 스킬은 배울 수 없다");
        Assert.IsFalse(tree.CanLearn(second, 99));
        Assert.IsTrue(tree.IsFinalTaken(second));
        Assert.AreEqual(first.Id, tree.EmblemId);
    }

    [Test]
    public void EveryBranchHasFinalSkills_ThatCanReachMaxLevel()
    {
        foreach (var branch in new[] { DoctrineBranch.Temperance, DoctrineBranch.Lust, DoctrineBranch.Ambition })
        {
            var finals = new System.Collections.Generic.List<DoctrineSkill>();
            foreach (var skill in DoctrineCatalog.All)
                if (skill.Branch == branch && skill.IsFinal) finals.Add(skill);
            Assert.AreEqual(2, finals.Count, branch + " 최종 스킬은 갈래마다 하나씩");
            foreach (var skill in finals) Assert.AreEqual(2, skill.MaxLevel);
        }
    }

    [Test]
    public void PendingEvent_SurvivesSaveAndLoad_AndStartedFlagIsSet()
    {
        GameSession.StartNew(9);
        GameSession.SetPending("lf01", false);
        var json = JsonUtility.ToJson(GameSession.Capture());

        GameSession.StartNew(1);
        Assert.IsFalse(GameSession.Started);
        GameSession.Restore(JsonUtility.FromJson<SaveData>(json));

        Assert.AreEqual("lf01", GameSession.PendingEventId);
        Assert.IsFalse(GameSession.PendingAdvancesTurn);
        Assert.IsTrue(GameSession.Started);
        Assert.IsFalse(GameSession.Ended);
    }
}
