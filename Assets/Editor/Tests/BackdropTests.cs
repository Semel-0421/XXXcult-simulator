using NUnit.Framework;
using UnityEngine;

public class BackdropTests
{
    static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    [Test]
    public void Axes_UseReputationAndVirtueMinusDesire()
    {
        var cult = new CultStatus();
        var personal = new PersonalStatus();
        var (x0, y0) = Backdrop.Axes(cult, personal);
        Assert.AreEqual(0f, x0, 1e-4);
        Assert.AreEqual(0f, y0, 1e-4);

        cult[StatId.Reputation].Set(1000);
        foreach (var stat in new[] { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity })
            personal[stat].Set(60);
        var (x, y) = Backdrop.Axes(cult, personal);
        Assert.AreEqual(1f, x, 1e-4);
        Assert.AreEqual(1f, y, 1e-4);

        foreach (var stat in new[] { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition })
            personal[stat].Set(100);
        cult[StatId.Reputation].Set(0);
        var (lowX, lowY) = Backdrop.Axes(cult, personal);
        Assert.AreEqual(-1f, lowX, 1e-4);
        Assert.AreEqual(-1f, lowY, 1e-4);
    }

    [Test]
    public void SymbolFollowsQuadrant_AndWeightsSumToOne()
    {
        var halo = Backdrop.Compute(0.9f, 0.9f).Weights;
        var crown = Backdrop.Compute(0.9f, -0.9f).Weights;
        var thorns = Backdrop.Compute(-0.9f, 0.9f).Weights;
        var pentagram = Backdrop.Compute(-0.9f, -0.9f).Weights;
        Assert.AreEqual((int)Backdrop.Symbol.Halo, System.Array.IndexOf(halo, Mathf.Max(halo)));
        Assert.AreEqual((int)Backdrop.Symbol.Crown, System.Array.IndexOf(crown, Mathf.Max(crown)));
        Assert.AreEqual((int)Backdrop.Symbol.Thorns, System.Array.IndexOf(thorns, Mathf.Max(thorns)));
        Assert.AreEqual((int)Backdrop.Symbol.Pentagram, System.Array.IndexOf(pentagram, Mathf.Max(pentagram)));
        foreach (var weights in new[] { halo, crown, thorns, pentagram, Backdrop.Compute(0, 0).Weights, Backdrop.Compute(1, 0).Weights })
            Assert.AreEqual(1f, weights[0] + weights[1] + weights[2] + weights[3], 1e-4);
    }

    [Test]
    public void ContrastGrowsWithExtremity()
    {
        float Contrast(float x, float y)
        {
            var state = Backdrop.Compute(x, y);
            var best = 0f;
            for (int i = 0; i < 4; i++) best = Mathf.Max(best, Mathf.Abs(Luminance(state.Colors[i]) - Luminance(state.Background)));
            return best;
        }

        Assert.AreEqual(0f, Backdrop.Compute(0, 0).Extremity, 1e-4);
        Assert.Less(Contrast(0.2f, 0.2f), Contrast(0.5f, 0.5f));
        Assert.Less(Contrast(0.5f, 0.5f), Contrast(0.9f, 0.9f));
        Assert.Less(Contrast(-0.2f, -0.2f), Contrast(-0.9f, -0.9f));
    }

    [Test]
    public void BackgroundDarkensAsReputationFalls_AndBrightensAsItRises()
    {
        var low = Luminance(Backdrop.Compute(-1, 0).Background);
        var mid = Luminance(Backdrop.Compute(0, 0).Background);
        var high = Luminance(Backdrop.Compute(1, 0).Background);
        Assert.Less(low, mid);
        Assert.Less(mid, high);
    }

    [Test]
    public void SymbolSprites_AreGenerated()
    {
        foreach (Backdrop.Symbol symbol in System.Enum.GetValues(typeof(Backdrop.Symbol)))
        {
            var sprite = BackdropSymbols.Get(symbol);
            Assert.IsNotNull(sprite);
            Assert.AreEqual(sprite.rect.width, sprite.rect.height);
        }
    }
}
