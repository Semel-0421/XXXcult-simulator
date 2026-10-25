using System.IO;
using NUnit.Framework;

// 황금의 눈동자 모드 1·2단계: 해금 조건(DEC-028 A), 모드 플래그·저장, 시작 상태와 신 모드 시스템 연결
public class EyeRulesTests
{
    string tempProfile, previousPath, tempDir, previousDir;
    bool previousEnabled;

    [SetUp]
    public void SetUp()
    {
        previousPath = Profile.Path;
        previousEnabled = Profile.Enabled;
        tempProfile = Path.Combine(Path.GetTempPath(), "xxxcult_eye_profile_" + System.Guid.NewGuid().ToString("N") + ".json");
        Profile.Path = tempProfile;
        Profile.Enabled = true;
        previousDir = SaveSlots.Directory;
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_eye_save_" + System.Guid.NewGuid().ToString("N"));
        SaveSlots.Directory = tempDir;
    }

    [TearDown]
    public void TearDown()
    {
        Profile.Enabled = previousEnabled;
        Profile.Path = previousPath;
        if (File.Exists(tempProfile)) File.Delete(tempProfile);
        SaveSlots.Directory = previousDir;
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        GameSession.StartNew(1);
    }

    static void See(string endingId) => Profile.Count($"ending_{endingId}");

    [Test]
    public void Locked_UntilGodAscensionAndMirrorEndingBothSeen()
    {
        Assert.AreEqual(0, EyeRules.UnlockProgress);
        Assert.IsFalse(EyeRules.Unlocked);

        See(GodEndings.AscensionIds[0]);
        Assert.AreEqual(1, EyeRules.UnlockProgress);
        Assert.IsFalse(EyeRules.Unlocked);

        See(MirrorEndings.All[0].Id);
        Assert.AreEqual(2, EyeRules.UnlockProgress);
        Assert.IsTrue(EyeRules.Unlocked);
    }

    [Test]
    public void GodDefeatEndings_DoNotCount()
    {
        See(GodRules.Vessel.Id);
        See(GodRules.Forgotten.Id);
        See(GodRules.Disbanded.Id);
        See(MirrorEndings.All[0].Id);
        Assert.IsFalse(EyeRules.GodAscensionSeen);
        Assert.AreEqual(1, EyeRules.UnlockProgress);
    }

    [Test]
    public void NormalEndings_DoNotCount()
    {
        foreach (var ending in Endings.All) See(ending.Id);
        Assert.AreEqual(0, EyeRules.UnlockProgress);
    }

    [Test]
    public void LockedLabel_FillsHalfPerCondition()
    {
        Assert.AreEqual(0f, EyeRules.LockedFill(0));
        Assert.AreEqual(0.5f, EyeRules.LockedFill(1));
        Assert.AreEqual(1f, EyeRules.LockedFill(5));
        Assert.AreEqual("(1/2)", EyeRules.LockedCount(1));
        Assert.AreEqual("(2/2)", EyeRules.LockedCount(3));
    }

    [Test]
    public void EyeMode_SurvivesSaveAndClearsOnNewGame()
    {
        GameSession.StartNew(7);
        GameSession.SetEyeMode(true);
        GameSession.SetEyeSector(EyeSector.Samael);
        SaveSlots.Write(1, GameSession.Capture());

        GameSession.StartNew(8);
        Assert.IsFalse(GameSession.EyeMode);
        Assert.AreEqual(EyeSector.None, GameSession.EyeSector);

        GameSession.Restore(SaveSlots.Read(1));
        Assert.IsTrue(GameSession.EyeMode);
        Assert.AreEqual(EyeSector.Samael, GameSession.EyeSector);
        Assert.IsFalse(GameSession.GodMode);
        Assert.IsFalse(GameSession.MirrorMode);
    }

    [Test]
    public void Begin_StartsAdultVesselOnGodSystems_WithoutGodRunCount()
    {
        EyeRules.Begin(3);
        Assert.IsTrue(GameSession.EyeMode);
        Assert.IsTrue(GameSession.GodMode); // 그릇·접신·압박·축성은 신 모드 것을 쓴다
        Assert.IsFalse(GameSession.MirrorMode);
        Assert.AreEqual(EyeSector.None, GameSession.EyeSector);
        Assert.AreEqual(GodRules.StartAgeYears, GameSession.Age.Years);
        Assert.AreEqual(CultStatus.CommunionMax, GameSession.Cult[StatId.Communion].Value);
        Assert.AreEqual(GodRules.StartFollowers, GameSession.Cult[StatId.Followers].Value);
        Assert.AreEqual(1, Profile.CountOf("eye_runs"));
        Assert.AreEqual(0, Profile.CountOf("god_runs"));
    }

    [Test]
    public void ChooseSector_SetsSelectedDemiurge()
    {
        EyeRules.Begin(3);
        EyeRules.ChooseSector(EyeSector.Yaldabaoth);
        Assert.AreEqual(EyeSector.Yaldabaoth, GameSession.EyeSector);
        EyeRules.ChooseSector(EyeSector.None);
        Assert.AreEqual(EyeSector.Yaldabaoth, GameSession.EyeSector);
    }

    [Test]
    public void Founding_OpensCultActionsWithoutNature()
    {
        EyeRules.Begin(3);
        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
        Assert.IsTrue(GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration));
        Assert.IsFalse(GameSession.God.Tree.HasNature);
    }

    [Test]
    public void NoGifted_AndNoDefeatEndings()
    {
        EyeRules.Begin(3);
        Assert.AreEqual(GiftedV2Effects.EyeMode, GiftedV2Effects.CurrentMode);
        foreach (var e in GiftedV2Catalog.All) Assert.IsFalse(GiftedV2Effects.AppliesIn(e, GiftedV2Effects.CurrentMode), e.Id);

        GameSession.FoundCult("황금");
        EyeRules.OnFounded();
        GameSession.Personal[PersonalStatId.Health].Set(0);
        GameSession.Cult[StatId.Followers].Set(0);
        Assert.IsNull(GodRules.ForcedEnding());
        Assert.IsNull(GodRules.DueEnding());
    }

    [Test]
    public void QuestionText_ListsFourNumberedLinkedAnswers()
    {
        var text = EyeRules.QuestionText();
        for (int i = 1; i <= 4; i++) StringAssert.Contains($"{i}. <link=\"e{i}\">", text);
        foreach (var sector in EyeRules.Sectors) Assert.AreNotEqual(EyeRules.SectorKey(sector) + "_answer", Lang.Get(EyeRules.SectorKey(sector) + "_answer"));
    }
}
