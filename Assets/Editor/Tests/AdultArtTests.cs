using System.Linq;
using NUnit.Framework;
using UnityEngine;

// 성인 삽화는 성인 인증 + 콘텐츠 켬 + 게임 속 인물 만 19세 이상일 때만 불러온다.
public class AdultArtTests
{
    const string VerifiedPref = "adult_verified", ContentPref = "adult_content";
    int savedVerified, savedContent;

    [SetUp]
    public void SavePrefs()
    {
        savedVerified = PlayerPrefs.GetInt(VerifiedPref, 0);
        savedContent = PlayerPrefs.GetInt(ContentPref, 1);
        GameSession.StartNew(1);
        AgeTo(AdultGate.AdultAge);
    }

    [TearDown]
    public void RestorePrefs()
    {
        PlayerPrefs.SetInt(VerifiedPref, savedVerified);
        PlayerPrefs.SetInt(ContentPref, savedContent);
        PlayerPrefs.Save();
    }

    static void AgeTo(int years)
    {
        while (GameSession.Age.Years < years) GameSession.Age.Advance();
    }

    static void SetPrefs(bool verified, bool content)
    {
        PlayerPrefs.SetInt(VerifiedPref, verified ? 1 : 0);
        PlayerPrefs.SetInt(ContentPref, content ? 1 : 0);
    }

    // Docs/adult_art_prompts.csv에 있는 삽화 키 전부 (이벤트 id·행동키_ok|fail)
    static string[] ArtKeys() => Resources.LoadAll<Texture2D>("AdultArt").Select(t => t.name).ToArray();

    [Test]
    public void EveryAdultEventAndCultActionHasArt()
    {
        var keys = ArtKeys();
        foreach (var e in AdultEvents.All) Assert.Contains(e.Id, keys, $"{e.Id} 삽화 없음");
        foreach (var action in new[] { "CultPray", "Preach", "Proselytize", "Serve", "Desire" })
            foreach (var result in new[] { "ok", "fail" })
                Assert.Contains($"{action}_{result}", keys);
        Assert.Contains("Consecrate_ok", keys);
    }

    [Test]
    public void Active_LoadsArt()
    {
        SetPrefs(verified: true, content: true);
        Assert.IsTrue(AdultGate.Active);
        foreach (var key in ArtKeys()) Assert.IsNotNull(AdultArt.Load(key), key);
    }

    [Test]
    public void NotVerified_LoadsNothing()
    {
        SetPrefs(verified: false, content: true);
        Assert.IsFalse(AdultGate.Active);
        foreach (var key in ArtKeys()) Assert.IsNull(AdultArt.Load(key), key);
    }

    [Test]
    public void ContentTurnedOff_LoadsNothing()
    {
        SetPrefs(verified: true, content: false);
        Assert.IsFalse(AdultGate.Active);
        foreach (var key in ArtKeys()) Assert.IsNull(AdultArt.Load(key), key);
    }

    [Test]
    public void UnderageCharacter_LoadsNothing()
    {
        SetPrefs(verified: true, content: true);
        GameSession.StartNew(1);
        AgeTo(AdultGate.AdultAge - 1);
        Assert.IsFalse(AdultGate.Active);
        foreach (var key in ArtKeys()) Assert.IsNull(AdultArt.Load(key), key);
    }

    [Test]
    public void Inactive_NeverRollsAdultEvents()
    {
        SetPrefs(verified: false, content: true);
        var adultIds = AdultEvents.All.Select(e => e.Id).ToHashSet();
        for (int i = 0; i < 400; i++)
        {
            var next = Turns.NextCultEvent();
            Assert.IsFalse(next != null && adultIds.Contains(next.Id), next?.Id);
        }
    }
}
