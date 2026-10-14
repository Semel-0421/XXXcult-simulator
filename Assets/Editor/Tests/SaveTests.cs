using System.IO;
using System.Linq;
using NUnit.Framework;

public class SaveTests
{
    string tempDir;
    string previousDir;

    [SetUp]
    public void SetUp()
    {
        previousDir = SaveSlots.Directory;
        tempDir = Path.Combine(Path.GetTempPath(), "xxxcult_save_test_" + System.Guid.NewGuid().ToString("N"));
        SaveSlots.Directory = tempDir;
    }

    [TearDown]
    public void TearDown()
    {
        SaveSlots.Directory = previousDir;
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    [Test]
    public void GameRng_RestoredState_ContinuesSameSequence()
    {
        var a = new GameRng(42);
        for (int i = 0; i < 10; i++) a.NextDouble();
        var b = new GameRng(a.State);
        for (int i = 0; i < 20; i++) Assert.AreEqual(a.Next(1000), b.Next(1000));
        Assert.AreEqual(new GameRng(7).Next(100), new GameRng(7).Next(100));
    }

    [Test]
    public void RoundTrip_KeepsEverythingNeededToContinue()
    {
        GameSession.StartNew(123);
        GameSession.Personal.Name = "아벨";
        GameSession.Personal.Gender = Gender.Female;
        GiftedGranter.Grant(GameSession.Personal.Talents, "no.1");
        GameSession.Personal[PersonalStatId.Health].Set(37);
        GameSession.Personal.AddWithRemainder(PersonalStatId.Piety, 0.4f);
        for (int i = 0; i < 7; i++) GameSession.AdvanceTurn();
        GameSession.FoundCult("별빛");
        GameSession.Cult[StatId.Followers].Set(55);
        GameSession.Cult.Doctrines.LevelUp(DoctrineSkill.Declaration);
        GameSession.UsedEvents.Add("life.001");
        GameSession.Rng.NextDouble();
        var expectedNext = new GameRng(((GameRng)GameSession.Rng).State).NextDouble();

        SaveSlots.Write(5, GameSession.Capture());
        GameSession.StartNew(999);
        GameSession.Restore(SaveSlots.Read(5));

        Assert.AreEqual("아벨", GameSession.Personal.Name);
        Assert.AreEqual(Gender.Female, GameSession.Personal.Gender);
        Assert.AreEqual(37, GameSession.Personal[PersonalStatId.Health].Value);
        Assert.IsTrue(GameSession.Personal.Talents.Has("no.1"));
        Assert.AreEqual(7, GameSession.Age.Turns);
        Assert.IsTrue(GameSession.CultFounded);
        Assert.AreEqual("별빛", GameSession.Cult.Name);
        Assert.AreEqual(55, GameSession.Cult[StatId.Followers].Value);
        Assert.IsTrue(GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration));
        Assert.IsTrue(GameSession.UsedEvents.Contains("life.001"));
        Assert.AreEqual(expectedNext, GameSession.Rng.NextDouble(), 0);
        Assert.IsTrue(GameSession.ConsumeResume());
        Assert.IsFalse(GameSession.ConsumeResume());
    }

    [Test]
    public void Slots_Are1To100_AndCanBeDeleted()
    {
        Assert.AreEqual(100, SaveSlots.MaxSlots);
        GameSession.StartNew(1);
        SaveSlots.Write(1, GameSession.Capture());
        SaveSlots.Write(100, GameSession.Capture());
        Assert.IsTrue(SaveSlots.Exists(1));
        Assert.IsTrue(SaveSlots.Exists(100));
        Assert.IsFalse(SaveSlots.Exists(2));
        Assert.IsNull(SaveSlots.Read(2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => SaveSlots.Exists(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => SaveSlots.Exists(101));

        SaveSlots.Delete(1);
        Assert.IsFalse(SaveSlots.Exists(1));
        Assert.IsFalse(Directory.GetFiles(tempDir).Any(f => f.EndsWith(".tmp")));
    }

    [Test]
    public void CorruptFile_ReadsAsNull()
    {
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "slot_003.json"), "{ not json");
        Assert.IsNull(SaveSlots.Read(3));
        Assert.IsTrue(SaveSlots.Exists(3));
    }
}

public class GiftedInfoTests
{
    [Test]
    public void EffectLines_NeverShowHiddenStats()
    {
        var hiddenNames = new[] { Lang.Get("stat_Communion"), Lang.Get("pstat_Miracle") };
        foreach (var talent in GiftedCatalog.All)
            foreach (var line in GiftedInfo.EffectLines(talent))
                foreach (var hidden in hiddenNames)
                    Assert.IsFalse(line.Contains(hidden), $"{talent.Name}: {line}");
    }

    [Test]
    public void EffectLines_ListSpecialsAndPercents()
    {
        var orator = GiftedCatalog.All.First(g => g.Name == "웅변가");
        var lines = string.Join("\n", GiftedInfo.EffectLines(orator));
        StringAssert.Contains("+3%", lines);
        StringAssert.Contains("+10%p", lines);
    }

    [Test]
    public void Icons_AreUniquePerGiftedAndSquare()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var talent in GiftedCatalog.All)
        {
            var sprite = GiftedIcon.Of(talent);
            Assert.AreEqual(sprite.rect.width, sprite.rect.height);
            var key = talent.Category + System.Convert.ToBase64String(sprite.texture.GetRawTextureData());
            Assert.IsTrue(seen.Add(key), $"{talent.Name} icon duplicates another");
        }
    }
}
