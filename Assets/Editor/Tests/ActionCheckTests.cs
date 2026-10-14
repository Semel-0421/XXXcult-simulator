using System;
using NUnit.Framework;

public class ActionCheckTests
{
    [Test]
    public void Chance_Is70PercentPlusUpTo25()
    {
        Assert.AreEqual(0.70f, ActionCheck.Chance(0f), 1e-4);
        Assert.AreEqual(0.825f, ActionCheck.Chance(0.5f), 1e-4);
        Assert.AreEqual(0.95f, ActionCheck.Chance(2f), 1e-4);
    }

    [Test]
    public void Roll_MatchesChanceOverManyTries()
    {
        var rng = new Random(9);
        int ok = 0;
        for (int i = 0; i < 4000; i++) if (ActionCheck.Roll(0f, rng)) ok++;
        Assert.AreEqual(0.7, ok / 4000.0, 0.03);
    }

    [Test]
    public void EveryAction_HasHarmfulFailure()
    {
        foreach (LifeAction a in Enum.GetValues(typeof(LifeAction)))
            Assert.IsTrue(Array.Exists(LifeActions.EffectsOf(a, false), e => e.Amount < 0), a.ToString());
        foreach (CultAction a in Enum.GetValues(typeof(CultAction)))
            Assert.IsTrue(Array.Exists(CultActions.CultEffectsOf(a, false), e => e.Amount < 0), a.ToString());
    }

    [Test]
    public void FailedExercise_LowersHealth()
    {
        var personal = new PersonalStatus();
        personal[PersonalStatId.Health].Set(10);
        LifeActions.Apply(personal, LifeAction.Exercise, success: false);
        Assert.AreEqual(8, personal[PersonalStatId.Health].Value);
    }

    [Test]
    public void EveryActionHasPictograms()
    {
        foreach (LifeAction a in Enum.GetValues(typeof(LifeAction)))
            foreach (var r in new[] { "ok", "fail" })
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>($"Pictograms/{a}_{r}"), $"{a}_{r}");
        foreach (CultAction a in Enum.GetValues(typeof(CultAction)))
            foreach (var r in new[] { "ok", "fail" })
            {
                var key = a == CultAction.Pray ? "CultPray" : a.ToString();
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>($"Pictograms/{key}_{r}"), $"{key}_{r}");
            }
    }
}
