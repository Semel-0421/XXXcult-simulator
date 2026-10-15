using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class MoodEventTests
{
    static IEnumerable<CultEvent> Moods => CultEvents.All.Where(e => e.IsMood);

    static void SetTrait(PersonalStatus personal, CultEvent.MoodTrait trait, float value)
    {
        var ids = trait == CultEvent.MoodTrait.Desire
            ? new[] { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition }
            : new[] { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };
        foreach (var id in ids) personal[id].Set((int)value);
    }

    [Test]
    public void Catalog_HasTwentyMoodEventsPerTrait()
    {
        Assert.AreEqual(20, Moods.Count(e => e.Trait == CultEvent.MoodTrait.Desire));
        Assert.AreEqual(20, Moods.Count(e => e.Trait == CultEvent.MoodTrait.Virtue));
    }

    [Test]
    public void MoodEvents_OnlyGiveNonNegativeEffects()
    {
        foreach (var e in Moods)
            foreach (var choice in e.Question.Choices)
            {
                var outcome = choice.Resolve(true);
                foreach (var effect in outcome.Effects) Assert.GreaterOrEqual(effect.Amount, 0, e.Id);
                foreach (var effect in outcome.CultEffects) Assert.GreaterOrEqual(effect.Amount, 0, e.Id);
            }
    }

    [Test]
    public void RollMood_NeverFiresWhenTraitsAreLow()
    {
        var personal = new PersonalStatus();
        var rng = new GameRng(7);
        for (int i = 0; i < 5000; i++)
            Assert.IsNull(CultEvent.RollMood(CultEvents.All, personal, new HashSet<string>(), rng));
    }

    static HashSet<string> UsedMilestones()
    {
        var used = new HashSet<string>();
        foreach (var trait in new[] { CultEvent.MoodTrait.Desire, CultEvent.MoodTrait.Virtue })
            foreach (var milestone in CultEvent.MoodMilestones) used.Add(CultEvent.MilestoneKey(trait, milestone));
        return used;
    }

    [Test]
    public void RollMood_GuaranteedOnceAt20AndOnceAt50_PerTrait()
    {
        var personal = new PersonalStatus();
        var used = new HashSet<string>();
        var rng = new GameRng(1);

        SetTrait(personal, CultEvent.MoodTrait.Virtue, 25f);
        var first = CultEvent.RollMood(CultEvents.All, personal, used, rng);
        Assert.IsNotNull(first, "평균 20에서 확정 발생");
        Assert.AreEqual(CultEvent.MoodTrait.Virtue, first.Trait);
        // 30을 넘지 않으니 더 나오지 않는다
        for (int i = 0; i < 3000; i++) Assert.IsNull(CultEvent.RollMood(CultEvents.All, personal, used, rng));

        SetTrait(personal, CultEvent.MoodTrait.Virtue, 55f);
        var second = CultEvent.RollMood(CultEvents.All, personal, used, rng);
        Assert.IsNotNull(second, "평균 50에서 확정 발생");
        Assert.AreEqual(CultEvent.MoodTrait.Virtue, second.Trait);
    }

    [Test]
    public void RollMood_NoRandomRollUntilAbove30()
    {
        var personal = new PersonalStatus();
        SetTrait(personal, CultEvent.MoodTrait.Desire, 30f); // 30은 넘은 것이 아니다
        var rng = new GameRng(5);
        for (int i = 0; i < 5000; i++)
            Assert.IsNull(CultEvent.RollMood(CultEvents.All, personal, UsedMilestones(), rng));
    }

    [Test]
    public void RollMood_PicksHigherTraitOnly_AndAboutThreePercent()
    {
        var personal = new PersonalStatus();
        SetTrait(personal, CultEvent.MoodTrait.Desire, 80f);
        SetTrait(personal, CultEvent.MoodTrait.Virtue, 60f);
        var rng = new GameRng(11);
        int fired = 0;
        const int tries = 20000;
        for (int i = 0; i < tries; i++)
        {
            var picked = CultEvent.RollMood(CultEvents.All, personal, UsedMilestones(), rng);
            if (picked == null) continue;
            fired++;
            Assert.AreEqual(CultEvent.MoodTrait.Desire, picked.Trait);
        }
        float rate = fired / (float)tries;
        Assert.That(rate, Is.InRange(0.025f, 0.035f));
    }

    [Test]
    public void RollMood_DoesNotRepeatUsedEvents()
    {
        var personal = new PersonalStatus();
        SetTrait(personal, CultEvent.MoodTrait.Virtue, 100f);
        var rng = new GameRng(3);
        var used = UsedMilestones();
        var seen = new HashSet<string>();
        for (int i = 0; i < 200000 && seen.Count < 20; i++)
        {
            var picked = CultEvent.RollMood(CultEvents.All, personal, used, rng);
            if (picked != null) Assert.IsTrue(seen.Add(picked.Id), picked.Id + " 반복");
        }
        Assert.AreEqual(20, seen.Count);
    }
}
