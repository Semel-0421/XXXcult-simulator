// 교단 창설 전 행동(운동/명상/공부/사교활동/기도)과 그 효과.
public enum LifeAction { Exercise, Meditate, Study, Socialize, Pray }

public static class LifeActions
{
    static StatEffect E(PersonalStatId stat, int amount) => new(stat, amount);

    public static StatEffect[] EffectsOf(LifeAction action) => action switch
    {
        LifeAction.Exercise => new[] { E(PersonalStatId.Health, 3), E(PersonalStatId.Appetite, 1) },
        LifeAction.Meditate => new[] { E(PersonalStatId.Mentality, 2), E(PersonalStatId.Temperance, 1), E(PersonalStatId.Patience, 1) },
        LifeAction.Study => new[] { E(PersonalStatId.Mentality, 2), E(PersonalStatId.Ambition, 1) },
        LifeAction.Socialize => new[] { E(PersonalStatId.Repute, 2), E(PersonalStatId.Charity, 1), E(PersonalStatId.Lust, 1) },
        _ => new[] { E(PersonalStatId.Piety, 3), E(PersonalStatId.Miracle, 1) },
    };

    public static void Apply(PersonalStatus personal, LifeAction action)
    {
        foreach (var effect in EffectsOf(action))
            GiftedEffects.Apply(personal, effect.Stat, effect.Amount);
    }
}
