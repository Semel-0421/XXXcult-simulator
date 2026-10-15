using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class LifeEventTests
{
    [Test]
    public void Catalog_Has200EventsWithTwoToFiveChoices()
    {
        Assert.AreEqual(200, LifeEvents.All.Length);
        Assert.IsTrue(LifeEvents.All.All(e => e.Question.Choices.Length is >= 2 and <= 5));
        Assert.AreEqual(LifeEvents.All.Length, LifeEvents.All.Select(e => e.Id).Distinct().Count());
    }

    [Test]
    public void EveryAgeUntilFounding_HasEvents()
    {
        for (int age = 0; age <= 18; age++)
            Assert.IsTrue(LifeEvents.All.Any(e => e.Fits(age)), $"age {age}");
    }

    [Test]
    public void HiddenGifts_AreOnlyFateOrAppearance()
    {
        var categories = new HashSet<string> { "운명", "외모" };
        foreach (var id in LifeEvents.All.SelectMany(e => e.Question.Choices).SelectMany(AllGrants))
                Assert.IsTrue(categories.Contains(GiftedCatalog.Find(id).Category), id);
    }

    [Test]
    public void Pick_FitsAgeAndDoesNotRepeat()
    {
        var used = new HashSet<string>();
        var rng = new Random(1);
        for (int i = 0; i < 20; i++)
        {
            var picked = LifeEvent.Pick(LifeEvents.All, 8, used, rng);
            Assert.IsTrue(picked.Fits(8));
        }
        Assert.AreEqual(20, used.Count);
    }

    [Test]
    public void FoundingTurn_IsAge18FourthQuarter()
    {
        Assert.AreEqual(18, LifeFlow.FoundingTurn / GameClock.QuartersPerYear);
        Assert.AreEqual(3, LifeFlow.FoundingTurn % GameClock.QuartersPerYear);
    }

    [Test]
    public void EveryRandomChoice_HasDistinctFailureText()
    {
        foreach (var choice in LifeEvents.All.SelectMany(e => e.Question.Choices))
        {
            if (choice.Chance >= 1f) continue;
            Assert.IsNotNull(choice.Fail, choice.Text);
            Assert.IsFalse(string.IsNullOrEmpty(choice.Fail.Text), choice.Text);
            Assert.AreNotEqual(choice.Success.Text, choice.Fail.Text, choice.Text);
        }
    }

    static IEnumerable<string> AllGrants(Choice choice) =>
        choice.Success.Grants.Concat(choice.Fail?.Grants ?? System.Array.Empty<string>());
}
