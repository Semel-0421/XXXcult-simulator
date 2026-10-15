// 교단 창설 후 행동(포교/설교/봉사/욕구/기도)과 성공·실패 효과. 교단 스탯과 개인 스탯이 함께 변한다.
public enum CultAction { Proselytize, Preach, Serve, Desire, Pray }

public static class CultActions
{
    static CultEffect C(StatId stat, int amount) => new(stat, amount);
    static StatEffect P(PersonalStatId stat, int amount) => new(stat, amount);

    // 성공 확률을 올려 주는 관련 교단 스탯 (교리는 레벨 기준)
    public static StatId KeyStat(CultAction action) => action switch
    {
        CultAction.Proselytize => StatId.Reputation,
        CultAction.Preach => StatId.Doctrine,
        CultAction.Serve => StatId.Offerings,
        CultAction.Desire => StatId.Authority,
        _ => StatId.LeaderFaith,
    };

    public static CultEffect[] CultEffectsOf(CultAction action, bool success = true) => (action, success) switch
    {
        (CultAction.Proselytize, true) => new[] { C(StatId.Followers, 5), C(StatId.Reputation, 2), C(StatId.FollowerFaith, -5) },
        (CultAction.Proselytize, false) => new[] { C(StatId.Reputation, -15), C(StatId.FollowerFaith, -5) },            // 봉변
        (CultAction.Preach, true) => new[] { C(StatId.FollowerFaith, 20), C(StatId.Doctrine, 10), C(StatId.Authority, 5) },
        (CultAction.Preach, false) => new[] { C(StatId.FollowerFaith, -10), C(StatId.Authority, -10) },                 // 설교가 빗나감
        (CultAction.Serve, true) => new[] { C(StatId.Reputation, 8), C(StatId.FollowerFaith, 5), C(StatId.Offerings, -10) },
        (CultAction.Serve, false) => new[] { C(StatId.Offerings, -15), C(StatId.Reputation, -5) },                       // 일만 키움
        (CultAction.Desire, true) => new[] { C(StatId.Offerings, 30), C(StatId.Authority, 10), C(StatId.Reputation, -10), C(StatId.FollowerFaith, -5) },
        (CultAction.Desire, false) => new[] { C(StatId.Reputation, -20), C(StatId.FollowerFaith, -10), C(StatId.Offerings, -10) }, // 탐욕이 드러남
        (CultAction.Pray, true) => new[] { C(StatId.LeaderFaith, 20), C(StatId.FollowerFaith, 5), C(StatId.Communion, 1) },
        _ => new[] { C(StatId.LeaderFaith, -10), C(StatId.FollowerFaith, -5) },                                          // 신의 침묵
    };

    public static StatEffect[] PersonalEffectsOf(CultAction action, bool success = true) => (action, success) switch
    {
        (CultAction.Proselytize, true) => new[] { P(PersonalStatId.Repute, 1), P(PersonalStatId.Health, -1) },
        (CultAction.Proselytize, false) => new[] { P(PersonalStatId.Repute, -1), P(PersonalStatId.Health, -1), P(PersonalStatId.Patience, 1), P(PersonalStatId.Mentality, 1) },
        (CultAction.Preach, true) => new[] { P(PersonalStatId.Mentality, 1), P(PersonalStatId.Ambition, 1) },
        (CultAction.Preach, false) => new[] { P(PersonalStatId.Mentality, -1), P(PersonalStatId.Temperance, 1), P(PersonalStatId.Piety, 1) },
        (CultAction.Serve, true) => new[] { P(PersonalStatId.Charity, 2), P(PersonalStatId.Health, -1), P(PersonalStatId.Repute, 1) },
        (CultAction.Serve, false) => new[] { P(PersonalStatId.Health, -2), P(PersonalStatId.Charity, 1), P(PersonalStatId.Repute, 1) },
        (CultAction.Desire, true) => new[] { P(PersonalStatId.Lust, 1), P(PersonalStatId.Greed, 1), P(PersonalStatId.Appetite, 1), P(PersonalStatId.Wealth, 1) },
        (CultAction.Desire, false) => new[] { P(PersonalStatId.Lust, 1), P(PersonalStatId.Integrity, -1), P(PersonalStatId.Wealth, 1) },
        (CultAction.Pray, true) => new[] { P(PersonalStatId.Piety, 2), P(PersonalStatId.Miracle, 1) },
        _ => new[] { P(PersonalStatId.Piety, -1), P(PersonalStatId.Mentality, -1), P(PersonalStatId.Patience, 1), P(PersonalStatId.Health, 1) },
    };

    // 기프티드 특수효과·픽토그램에서 쓰는 행동 키 (생활 기도와 구분하려고 교단 기도는 CultPray)
    public static string Key(CultAction action) => action == CultAction.Pray ? "CultPray" : action.ToString();

    public static bool Roll(CultStatus cult, TalentList gifted, CultAction action, System.Random rng) =>
        ActionCheck.Roll(cult.Ratio(KeyStat(action)), rng, GiftedSpecials.ChanceBonus(gifted, Key(action)));

    public static void Apply(CultStatus cult, PersonalStatus personal, CultAction action, bool success = true)
    {
        var halve = !success && GiftedSpecials.HalvesFailure(personal.Talents, Key(action));
        foreach (var effect in CultEffectsOf(action, success))
            GiftedEffects.Apply(cult, personal.Talents, effect.Stat, GiftedSpecials.Soften(effect.Amount, halve));
        foreach (var effect in PersonalEffectsOf(action, success))
            GiftedEffects.Apply(personal, effect.Stat, GiftedSpecials.Soften(effect.Amount, halve));
    }
}
