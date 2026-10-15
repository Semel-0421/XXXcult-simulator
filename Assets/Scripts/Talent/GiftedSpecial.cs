// 기프티드 보유 특수효과: 행동 성공률 보정, 실패 불이익 절반, 해마다 스탯 변화.
// Action은 행동 키(LifeAction 이름, 교단 행동은 CultActions.Key) 또는 All.
public readonly struct GiftedSpecial
{
    public enum Type { Chance, FailHalf, Yearly }
    public const string AllActions = "All";

    public Type Kind { get; }
    public string Action { get; }
    public int Amount { get; }
    public PersonalStatId? PersonalStat { get; }
    public StatId? CultStat { get; }

    GiftedSpecial(Type kind, string action, int amount, PersonalStatId? personal, StatId? cult)
    {
        Kind = kind; Action = action; Amount = amount; PersonalStat = personal; CultStat = cult;
    }

    public static GiftedSpecial Chance(string action, int percentPoints) => new(Type.Chance, action, percentPoints, null, null);
    public static GiftedSpecial FailHalf(string action) => new(Type.FailHalf, action, 0, null, null);
    public static GiftedSpecial Yearly(PersonalStatId stat, int amount) => new(Type.Yearly, null, amount, stat, null);
    public static GiftedSpecial Yearly(StatId stat, int amount) => new(Type.Yearly, null, amount, null, stat);

    public bool Covers(string action) => Action == AllActions || Action == action;
}

public static class GiftedSpecials
{
    // 보유 기프티드의 해당 행동 성공률 보정 합 (0~1 단위)
    public static float ChanceBonus(TalentList gifted, string action)
    {
        int total = 0;
        foreach (var g in gifted.Effective)
            foreach (var s in g.Specials)
                if (s.Kind == GiftedSpecial.Type.Chance && s.Covers(action)) total += s.Amount;
        return total / 100f;
    }

    public static bool HalvesFailure(TalentList gifted, string action)
    {
        foreach (var g in gifted.Effective)
            foreach (var s in g.Specials)
                if (s.Kind == GiftedSpecial.Type.FailHalf && s.Covers(action)) return true;
        return false;
    }

    // 실패 불이익 절반 적용 (0 쪽으로 버림: -1 → 0, -15 → -7)
    public static int Soften(int amount, bool halve) => halve && amount < 0 ? amount / 2 : amount;

    public static void ApplyYearly(PersonalStatus personal, CultStatus cult, bool cultFounded)
    {
        foreach (var g in personal.Talents.Effective)
            foreach (var s in g.Specials)
            {
                if (s.Kind != GiftedSpecial.Type.Yearly) continue;
                if (s.PersonalStat is { } p) personal[p].Add(s.Amount);
                else if (cultFounded && s.CultStat is { } c) cult.AddWithRemainder(c, s.Amount);
            }
    }
}
