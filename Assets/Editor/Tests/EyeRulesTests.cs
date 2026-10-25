using System.IO;
using NUnit.Framework;

// 황금의 눈동자 모드 1단계: 해금 조건(DEC-028 A), 모드 플래그, 저장
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
        SaveSlots.Write(1, GameSession.Capture());

        GameSession.StartNew(8);
        Assert.IsFalse(GameSession.EyeMode);

        GameSession.Restore(SaveSlots.Read(1));
        Assert.IsTrue(GameSession.EyeMode);
        Assert.IsFalse(GameSession.GodMode);
        Assert.IsFalse(GameSession.MirrorMode);
    }
}
