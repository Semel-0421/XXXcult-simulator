// 교단 창설 전 행동(운동/명상/공부/사교활동/기도)과 성공·실패 효과.
public enum LifeAction { Exercise, Meditate, Study, Socialize, Pray }

public static class LifeActions
{
    static StatEffect E(PersonalStatId stat, int amount) => new(stat, amount);

    // 성공 확률을 올려 주는 관련 스탯
    public static PersonalStatId KeyStat(LifeAction action) => action switch
    {
        LifeAction.Exercise => PersonalStatId.Health,
        LifeAction.Meditate => PersonalStatId.Temperance,
        LifeAction.Study => PersonalStatId.Mentality,
        LifeAction.Socialize => PersonalStatId.Repute,
        _ => PersonalStatId.Piety,
    };

    public static StatEffect[] EffectsOf(LifeAction action, bool success) => (action, success) switch
    {
        (LifeAction.Exercise, true) => new[] { E(PersonalStatId.Health, 3), E(PersonalStatId.Appetite, 1) },
        (LifeAction.Exercise, false) => new[] { E(PersonalStatId.Health, -2), E(PersonalStatId.Mentality, -1) },     // 부상
        (LifeAction.Meditate, true) => new[] { E(PersonalStatId.Mentality, 2), E(PersonalStatId.Temperance, 1), E(PersonalStatId.Patience, 1) },
        (LifeAction.Meditate, false) => new[] { E(PersonalStatId.Mentality, -1), E(PersonalStatId.Temperance, -1) },  // 잡념
        (LifeAction.Study, true) => new[] { E(PersonalStatId.Mentality, 2), E(PersonalStatId.Ambition, 1) },
        (LifeAction.Study, false) => new[] { E(PersonalStatId.Mentality, -1), E(PersonalStatId.Patience, -1) },       // 머리만 아픔
        (LifeAction.Socialize, true) => new[] { E(PersonalStatId.Repute, 2), E(PersonalStatId.Charity, 1), E(PersonalStatId.Lust, 1) },
        (LifeAction.Socialize, false) => new[] { E(PersonalStatId.Repute, -2), E(PersonalStatId.Charity, -1) },       // 말실수
        (LifeAction.Pray, true) => new[] { E(PersonalStatId.Piety, 3), E(PersonalStatId.Miracle, 1) },
        _ => new[] { E(PersonalStatId.Piety, -1), E(PersonalStatId.Mentality, -1) },                                  // 응답 없는 기도
    };

    public static bool Roll(PersonalStatus personal, LifeAction action, System.Random rng, float extraBonus = 0) =>
        ActionCheck.Roll(personal[KeyStat(action)].Value / (float)PersonalStatus.MaxValue, rng,
            GiftedSpecials.ChanceBonus(personal.Talents, action.ToString()) + extraBonus);

    public static void Apply(PersonalStatus personal, LifeAction action, bool success)
    {
        var halve = !success && GiftedSpecials.HalvesFailure(personal.Talents, action.ToString());
        foreach (var effect in EffectsOf(action, success))
            GiftedEffects.Apply(personal, effect.Stat, GiftedSpecials.Soften(effect.Amount, halve));
    }
}
