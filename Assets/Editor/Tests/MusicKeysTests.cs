using NUnit.Framework;

public class MusicKeysTests
{
    [Test]
    public void God_PlaysStrongerPressure_OrBothWhenEqual()
    {
        Assert.AreEqual(MusicKeys.GodMain, MusicKeys.ForGod(2, 2));
        Assert.AreEqual(MusicKeys.GodHeaven, MusicKeys.ForGod(4, 3));
        Assert.AreEqual(MusicKeys.GodEarth, MusicKeys.ForGod(1, 3));
        Assert.AreEqual(MusicKeys.GodClash, MusicKeys.ForGod(4, 4));
    }

    [Test]
    public void Endings_MapToThreeMoods()
    {
        Assert.AreEqual(MusicKeys.EndingDeath, MusicKeys.ForEnding(Endings.Find(Endings.DeathLeader)));
        Assert.AreEqual("tier_Dissolved", MusicKeys.ForEnding(Endings.Find(Endings.Dissolved)));
        Assert.AreEqual(MusicKeys.EndingRuin, MusicKeys.ForEnding(GodRules.Forgotten));
        Assert.AreEqual(MusicKeys.EndingGlory, MusicKeys.ForEnding(new Endings.Ending("final_x_good", EndingKind.Good, "x")));
        Assert.AreEqual(MusicKeys.EndingRuin, MusicKeys.ForEnding(new Endings.Ending("final_x_bad", EndingKind.Bad, "x")));
    }

    [Test]
    public void EventSfx_SplitsAdultNegativeAndOthers()
    {
        Assert.AreEqual("adult_open", MusicKeys.EventSfx(AdultEvents.All[0].Id));
        Assert.AreEqual("event_bad", MusicKeys.EventSfx(System.Array.Find(CultEvents.All, e => e.IsNegative).Id));
        Assert.AreEqual("event_open", MusicKeys.EventSfx(System.Array.Find(CultEvents.All, e => !e.IsNegative).Id));
    }

    [Test]
    public void EffectiveVolumes_ApplyMasterAndMute()
    {
        float m = AudioManager.MusicVolume, s = AudioManager.SfxVolume, master = AudioManager.MasterVolume;
        bool mm = AudioManager.MusicMuted, sm = AudioManager.SfxMuted;
        try
        {
            AudioManager.MusicVolume = 0.8f; AudioManager.SfxVolume = 0.5f; AudioManager.MasterVolume = 0.5f;
            AudioManager.MusicMuted = false; AudioManager.SfxMuted = true;
            Assert.AreEqual(0.4f, AudioManager.EffectiveMusic, 1e-4);
            Assert.AreEqual(0f, AudioManager.EffectiveSfx);
        }
        finally
        {
            AudioManager.MusicVolume = m; AudioManager.SfxVolume = s; AudioManager.MasterVolume = master;
            AudioManager.MusicMuted = mm; AudioManager.SfxMuted = sm; AudioManager.SaveVolumes();
        }
    }
}
