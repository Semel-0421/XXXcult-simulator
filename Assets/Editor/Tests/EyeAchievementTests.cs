using System.IO;
using System.Linq;
using NUnit.Framework;

// 황금의 눈동자 7단계: 전용 업적과 카운터 (Docs/achievements_v2_eye.csv, EyeCounters)
public class EyeAchievementTests
{
    const int Rich = 999;
    string tempProfile, previousPath;
    bool previousEnabled;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        tempProfile = Path.Combine(Path.GetTempPath(), "xxxcult_eye_ach_" + System.Guid.NewGuid().ToString("N") + ".json");
        Profile.Path = tempProfile;
        Profile.Enabled = true;
        EyeRules.Begin(31);
        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
    }

    [TearDown]
    public void TearDown()
    {
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        if (File.Exists(tempProfile)) File.Delete(tempProfile);
        GameSession.StartNew(1);
    }

    static bool Unlocked(string name) => Profile.IsUnlocked(Achievements.All.First(a => a.Mode == "eye" && a.Name == name).Id);

    [Test]
    public void Catalog_HasEyeBundle_NoRewards()
    {
        var eye = Achievements.All.Where(a => a.Mode == "eye").ToList();
        Assert.AreEqual(97, eye.Count);
        Assert.IsTrue(eye.All(a => a.Reward.Kind == RewardKind.None));
        Assert.AreEqual(eye.Count, eye.Select(a => a.Id).Distinct().Count());
        foreach (var e in EyeEndings.All) Assert.IsTrue(eye.Any(a => a.Name == Lang.Pick(e.NameKey)), e.Id);
    }

    [Test]
    public void SectorAndTree_CountersUnlockAchievements()
    {
        EyeRules.ChooseSector(EyeSector.Samael);
        Assert.AreEqual(1, Profile.CountOf("eye_start_samael"));
        var aeons = GameSession.God.Aeons;
        for (int l = 1; l <= 4; l++)
        {
            aeons.Learn(AeonSkillCatalog.Of(EyeSector.Samael, l, AeonKind.Good), Rich, 10);
            aeons.Learn(AeonSkillCatalog.Of(EyeSector.Samael, l, AeonKind.Evil), Rich, 10);
        }
        aeons.Learn(AeonSkillCatalog.Stage(EyeSector.Samael, 5), Rich, 10);
        Achievements.Evaluate();
        Assert.IsTrue(Unlocked("사마엘의 눈길"));
        Assert.IsTrue(Unlocked("사마엘의 그노시스"));
        Assert.IsTrue(Unlocked("아이온의 싹"));
        Assert.IsFalse(Unlocked("사마엘의 문턱"));
    }

    [Test]
    public void Calamity_CountsKind()
    {
        EyeRules.ChooseSector(EyeSector.Satan);
        GameSession.Cult[StatId.Offerings].Set(500);
        GameSession.Cult[StatId.Authority].Set(200);
        var q = EyeRules.BeginCalamity();
        foreach (var e in q.Choices[0].Success.CultEffects) GameSession.Cult[e.Stat].Add(e.Amount);
        EyeRules.EndCalamity();
        Assert.AreEqual(1, Profile.CountOf("eye_calamities"));
        Assert.AreEqual(1, Profile.CountOf("eye_give_cult"));
        EyeRules.RestCalamity();
        Assert.AreEqual(2, Profile.CountOf("eye_calamities"));
        Assert.AreEqual(1, Profile.CountOf("eye_rest_calamities"));
        Achievements.Evaluate();
        Assert.IsTrue(Unlocked("첫 재앙"));
        Assert.IsTrue(Unlocked("잠든 사이"));
    }

    [Test]
    public void Ending_RecordsAscensionAndUnlocks()
    {
        EyeRules.ChooseSector(EyeSector.Yahweh);
        var aeons = GameSession.God.Aeons;
        for (int l = 1; l <= 4; l++)
        {
            aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, l, AeonKind.Good), Rich, 10);
            aeons.Learn(AeonSkillCatalog.Of(EyeSector.Yahweh, l, AeonKind.Evil), Rich, 10);
        }
        for (int l = 5; l <= 10; l++) aeons.Learn(AeonSkillCatalog.Stage(EyeSector.Yahweh, l), Rich, 10);
        var ending = GodRules.DueEnding();
        Assert.AreEqual("eye_yahweh_good", ending.Id);
        Progress.Reach(ending);
        Assert.AreEqual(1, Profile.CountOf("eye_usurped"));
        Assert.AreEqual(1, Profile.CountOf("eye_ascended"));
        Assert.IsTrue(Unlocked("질투하는 거짓 신"));
        Assert.IsTrue(Unlocked("첫 찬탈"));
        Assert.IsTrue(Unlocked("흠 없는 왕좌"));   // 재앙 0회
        Assert.IsTrue(Unlocked("빠른 찬탈"));      // 교단 0년
        Assert.IsTrue(Unlocked("한 얼굴만"));      // 야훼 노드만
        Assert.AreEqual(0, Profile.CountOf("god_ascended"));
    }

    [Test]
    public void GodModeRun_DoesNotTouchEyeCounters()
    {
        GodRules.Begin(41);
        EyeCounters.OnAction(GodAction.Wonder);
        EyeCounters.OnCalamity("cult");
        Assert.AreEqual(0, Profile.CountOf("eye_act_Wonder"));
        Assert.AreEqual(0, Profile.CountOf("eye_calamities"));
    }
}
